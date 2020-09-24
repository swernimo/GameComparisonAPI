using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Azure.Documents.Client;
using System.IO;
using Newtonsoft.Json;

namespace GameComparisonAPI
{
    public static class SaveTerms
    {
        private static IConfigurationRoot _config;

        [FunctionName("SaveTerms")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SaveTerms")] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Saving Terms of Use");
            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "Terms");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var json = JsonConvert.DeserializeAnonymousType(requestBody, new { username = "", termsAccepted = "" });
            await documentClient.CreateDocumentAsync(documentUri, new
            {
                dateAccepted = DateTime.UtcNow,
                username = json.username,
                termsAccepted = json.termsAccepted
            });
            return new OkObjectResult("");
        }
    }
}
