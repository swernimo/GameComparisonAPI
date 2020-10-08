using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Documents.Client;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using GameComparisonAPI.Entities;

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
            log.LogInformation("Attempting to save terms of use");
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
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                var payload = JsonConvert.DeserializeObject<SaveTermsPayload>(body);
                await SaveTermsToCosmos(payload, decoded);
                log.LogInformation("Successfully Saved Accepted Terms of Use");
                return new OkObjectResult("Saved terms");
            }
            return new BadRequestResult();
        }

        private static async Task SaveTermsToCosmos(SaveTermsPayload payload, string deviceInfoHeader)
        {
            var url = _config["CosmosURL"];
            var authKey = _config["CosmosAuthorizationKey"];
            var documentClient = new DocumentClient(new Uri(url), authKey);
            var documentUri = UriFactory.CreateDocumentCollectionUri("GameComparison", "Terms");
            var dict = deviceInfoHeader.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(part => part.Split(':'))
               .ToDictionary(split => split[0], split => split[1]);
            var uuid = dict["UUID"];
            var appVersion = dict["AppVersion"];
            var platform = dict["Platform"];
            var osVersion = dict["DeviceVersion"];
            var model = dict["DeviceModel"];
            var timestamp = DateTime.UtcNow;
            await documentClient.CreateDocumentAsync(documentUri, new
            {
                payload.Username,
                dateAccepted = timestamp,
                lastUpdatedUTC = timestamp,
                device = new
                {
                    appVersion,
                    platform,
                    uuid,
                    osVersion,
                    model
                },
                payload.Terms
            });
        }
    }
}
