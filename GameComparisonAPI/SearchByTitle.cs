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
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace GameComparisonAPI
{
    public static class SearchByTitle
    {
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("SearchByTitle")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SearchByTitle/{title}")] HttpRequest req, string title, 
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Searching for game with title {title}.");

            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var response = await client.GetAsync($"{_config["BGGBaseUrl"]}/search?query={title}");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);
            var results = new List<SearchResults>();

            foreach(var el in doc.Elements().Nodes())
            {
                var itemId = ((XElement)el).Attribute("id").Value;
                //var statsResponse = await client.GetAsync($"https://gamecomparison.azurewebsites.net/api/GetGameStatistics/{itemId}?code=rT/jCOHWPKD1H9EUfAsFjbR/XrVxPvqpqB9uRu17hw7RN7fptWVF3Q==");
                //var stats = await statsResponse.Content.ReadAsAsync<Statistics>();
            }

            return new OkObjectResult(results);
        }
    }
}
