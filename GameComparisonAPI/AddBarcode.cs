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
using System.Data.SqlClient;
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

            var connString = _config["DBConnectionString"];

            using (var conn = new SqlConnection(connString))
            {
                var cmdText = @"update Game set barcode = @barcode where Id = @gameId";
                using (var command = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    command.Parameters.Add(new SqlParameter("gameId", gameId));
                    command.Parameters.Add(new SqlParameter("barcode", barcode));
                    await command.ExecuteNonQueryAsync();
                }
            }

            return new OkObjectResult("");
        }
    }
}
