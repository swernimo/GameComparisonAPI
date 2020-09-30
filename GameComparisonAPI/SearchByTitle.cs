using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using GameComparisonAPI.Entities;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Documents.Client;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Text;

namespace GameComparisonAPI
{
    public static class SearchByTitle
    {
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("SearchByTitle")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SearchByTitle")] HttpRequest req, 
            ILogger log, ExecutionContext context)
        {
            var title = req.Query["title"];
            log.LogInformation($"Searching for game with title {title}.");
            var header = req.Headers["DeviceInfo"];

            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            if (!string.IsNullOrWhiteSpace(header))
            {
                byte[] data = Convert.FromBase64String(header);
                var decoded = ASCIIEncoding.ASCII.GetString(data);
                await SaveSearchHistoryToCosmos(decoded, title);
            }

            var response = await client.GetAsync($"{_config["BGGBaseUrl"]}/search?query={title}&type=boardgame,boardgameexpansion");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);
            var results = new List<SearchResults>();

            foreach(var el in doc.Elements().Nodes())
            {
                var itemId = ((XElement)el).Attribute("id").Value;
                var itemUrl = $"{_config["BGGBaseUrl"]}/thing?id={itemId}";
                var itemResponse = await client.GetAsync(itemUrl);
                str = await itemResponse.Content.ReadAsStringAsync();
                doc = XDocument.Parse(str);
                var itemXML = doc.Descendants("item");
                var imageUrl = itemXML.Elements("image").FirstOrDefault()?.Value;
                var name = itemXML.Elements("name").Where(e => e.Attribute("type").Value == "primary").FirstOrDefault()?.Attribute("value").Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var result = new SearchResults
                    {
                        Id = int.Parse(itemId),
                        Title = name,
                        ImageURL = imageUrl
                    };
                    results.Add(result);
                    await SaveSearchResultToComos(result);
                }
            }

            return new OkObjectResult(results);
        }

        private static async Task SaveSearchResultToComos(SearchResults search)
        {
            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "SearchResults");
            await documentClient.CreateDocumentAsync(documentUri, new
            {
                lastUpdatedUTC = DateTime.Now.ToUniversalTime(),
                search
            });
        }

        private static async Task SaveSearchHistoryToCosmos(string deviceHeader, string title)
        {
            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "SearchHistory");
            var dict = deviceHeader.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(part => part.Split(':'))
               .ToDictionary(split => split[0], split => split[1]);
            var uuid = dict["UUID"];
            var appVersion = dict["AppVersion"];
            var platform = dict["Platform"];
            var osVersion = dict["DeviceVersion"];
            var model = dict["DeviceModel"];
            await documentClient.CreateDocumentAsync(documentUri, new
            {
                uuid,
                lastUpdatedUTC = DateTime.Now.ToUniversalTime(),
                device = new
                {
                    appVersion,
                    platform,
                    uuid,
                    osVersion,
                    model
                },
                searchTerms = new {
                    title
                }
            });
        }
    }
}
