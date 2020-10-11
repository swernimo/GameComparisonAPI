using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GameComparisonAPI.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Documents.Client;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using Microsoft.Azure.Documents;

namespace GameComparisonAPI
{
    public static class AddBarcodeFromLibrary
    {
        private static IConfigurationRoot _config;

        [FunctionName("AddBarcodeFromLibrary")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Starting Add Barcode from Library Function");

            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<LibraryItem>(requestBody);
            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "SearchResults");
            var query = documentClient.CreateDocumentQuery(documentUri, new SqlQuerySpec($"SELECT * FROM c where c.search.Id = {data.Id}"), new FeedOptions()
            {
                EnableCrossPartitionQuery = true
            });
            if (query.ToList().Any())
            {
                foreach (var document in query)
                {
                    var search = new SearchResults
                    {
                        Id = document.search.Id,
                        Title = document.search.Title,
                        Barcode = data.Barcode,
                        ImageURL = document.search.ImageURL
                    };

                    document.lastUpdatedUTC = DateTime.UtcNow;
                    document.search = search;

                    await documentClient.UpsertDocumentAsync(documentUri, document);
                }
            } else
            {
                await documentClient.CreateDocumentAsync(documentUri, new
                {
                    lastUpdatedUTC = DateTime.UtcNow,
                    search = new
                    {
                        data.Id,
                        data.Title,
                        data.Barcode,
                        ImageURL = ""
                    }

                });
            }

            return new OkObjectResult("Uploaded barcode");
        }
    }
}
