using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using GameComparisonAPI.Entities;

namespace GameComparisonAPI
{
    public static class AddBarcode
    {
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("AddBarcode")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "AddBarcode/{gameid}/{barcode}")] HttpRequest req, string gameId, string barcode,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Adding barcode: {barcode} to game: {gameId}");
            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "SearchResults");
            var query = documentClient.CreateDocumentQuery(documentUri, new SqlQuerySpec($"SELECT * FROM c where c.search.Id = {gameId}"), new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            });
            foreach(var document in query)
            {
                var search = new SearchResults
                {
                    Id = document.search.Id,
                    Title = document.search.Title,
                    Barcode = barcode,
                    ImageURL = document.search.ImageURL
                };

                document.lastUpdatedUTC = DateTime.UtcNow;
                document.search = search;

                await documentClient.UpsertDocumentAsync(documentUri, document);
            }

            return new OkObjectResult("");
        }
    }
}
