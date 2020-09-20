using System.Threading.Tasks;
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
using System;
using Microsoft.Azure.Documents.Client;

namespace GameComparisonAPI
{
    public static class GetCollection
    {
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("GetCollection")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetCollection/{username}")] HttpRequest req, string username,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Trying to get collection for {username}");
            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var collection = new List<Game>();
            /*
             check if collection is already in DB & less than week old
                if yes return saved collection
                if no query collection from BGG
             */
            var response = await client.GetAsync($"{_config["BGGBaseUrl"]}/collection/?username={username}&subtype=boardgame");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);

            foreach(var el in doc.Elements().Nodes())
            {
                var item = Game.ParseItem((XElement)el);
                var statsURL = $"{_config["FunctionBaseUrl"]}/GetGameStatistics/{item.Id}?code={_config["FunctionKey"]}";
                var statsResponse = await client.GetAsync(statsURL);
                var stats = await statsResponse.Content.ReadAsAsync<Statistics>();
                if (stats != null)
                {
                    item.Statistics = stats;
                }
                collection.Add(item);
            }
            await SaveCollectionToComos(username, collection);
            log.LogInformation($"Successfully got collection for user {username}");
            return new OkObjectResult(collection);
        }

        private static async Task SaveCollectionToComos(string username, List<Game> collection)
        {
            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "GameCollection");
            await documentClient.CreateDocumentAsync(documentUri, new
            {
                username,
                lastUpdatedUTC = DateTime.Now.ToUniversalTime(),
                collection
            });
        }
    }
}
