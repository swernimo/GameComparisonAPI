using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using GameComparisonAPI.Entities;
using Microsoft.Extensions.Configuration;

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

            using (var connection = new SqlConnection(_config["DBConnectionString"]))
            {
                connection.Open();
                using (var command = new SqlCommand("select Id, Title, imageUrl from Game where barcode = @barcode", connection))
                {
                    command.Parameters.Add(new SqlParameter("barcode", code));
                    var result = command.ExecuteReader();
                    while(result.Read())
                    {
                        var id = int.Parse(result["Id"].ToString());
                        var title = result["Title"].ToString();
                        var imageUrl = result["imageUrl"].ToString();
                        infoList.Add(new SearchResults
                        {
                            Id = id,
                            Title = title,
                           ImageURL = imageUrl
                        });
                    }

                }
            }


            return new OkObjectResult(infoList);
        }
    }
}
