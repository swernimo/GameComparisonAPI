using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using GameComparisonAPI.Entities;

namespace GameComparisonAPI
{
    public static class Search
    {
        [FunctionName("SearchByUPC")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SearchByUPC/{code}")] HttpRequest req, string code,
            ILogger log)
        {
            log.LogInformation($"Searching for barcode: {code}");
            var infoList = new List<SearchResults>();

            using (var connection = new SqlConnection("Server=tcp:gamecomparison.database.windows.net,1433;Initial Catalog=gamecomparison;Persist Security Info=False;User ID=swernimo;Password=xPh7de6g;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
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
