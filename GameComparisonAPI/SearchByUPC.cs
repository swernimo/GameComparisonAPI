using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using GameComparisonAPI.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace GameComparisonAPI
{
    public static class Search
    {
        private static IConfigurationRoot _config;

        [FunctionName("SearchByUPC")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SearchByUPC/{code}")] HttpRequest req, string code,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Searching for barcode: {code}");
            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
            var infoList = new List<SearchResults>();

            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "SearchResults");
            var query = documentClient.CreateDocumentQuery(documentUri, new SqlQuerySpec($"SELECT * FROM c where c.search.Barcode = \"{code}\""), new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            });
            foreach (var document in query)
            {
                var search = new SearchResults
                {
                    Id = document.search.Id,
                    Title = document.search.Title,
                    Barcode = code,
                    ImageURL = document.search.ImageURL
                };

                infoList.Add(search);
            }

            return new OkObjectResult(infoList);
        }
    }
}
