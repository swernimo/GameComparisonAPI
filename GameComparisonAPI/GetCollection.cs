using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using Polly;
using System.Xml;
using GameComparisonAPI.Entities;
using System.Collections.Generic;
using System.Xml.Linq;

namespace GameComparisonAPI
{
    public static class GetCollection
    {
        static readonly HttpClient client = new HttpClient();

        [FunctionName("GetCollection")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetCollection/{username}")] HttpRequest req, string username,
            ILogger log)
        {
            log.LogInformation($"Trying to get collection for {username}");
            var collection = new List<CollectionItem>();

            Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.OK).RetryForever();

            var response = await client.GetAsync($"https://www.boardgamegeek.com/xmlapi2/collection/?username={username}&subtype=boardgame");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);

            foreach(var el in doc.Elements().Nodes())
            {
                collection.Add(CollectionItem.ParseItem((XElement)el));
            }

            return new OkObjectResult(collection);
        }
    }
}
