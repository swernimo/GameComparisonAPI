using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Xml.Linq;
using System.Collections.Generic;
using GameComparisonAPI.Entities;
using System.Linq;

namespace GameComparisonAPI
{
    public static class GetGameStatistics
    {
        static readonly HttpClient client = new HttpClient();

        [FunctionName("GetGameStatistics")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetGameStatistics/{id}")] HttpRequest req, int id,
            ILogger log)
        {
            log.LogInformation($"Getting Statistics for game {id}");
            var url = $"http://www.boardgamegeek.com/xmlapi/boardgame/{id}?stats=1";
            var response = await client.GetAsync(url);

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);

            var element = doc.Descendants("boardgame").First();

            if (element != null)
            {
                var stats = Statistics.ParseStatistics(element);
                return new OkObjectResult(stats);
            }

            return new OkObjectResult("");
        }
    }
}
