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
using System.Threading.Tasks;
using System.Linq;

namespace GameComparisonAPI
{
    public static class SearchByTitle
    {
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("SearchByTitle")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SearchByTitle/{title}")] HttpRequest req, string title, 
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Searching for game with title {title}.");

            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var response = await client.GetAsync($"{_config["BGGBaseUrl"]}/search?query={title}?type=boardgame,boardgameaccessoryboardgameexpansion");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);
            var results = new List<SearchResults>();

            foreach(var el in doc.Elements().Nodes())
            {
                var itemId = ((XElement)el).Attribute("id").Value;
                var itemUrl = $"{_config["BGGBaseUrl"]}/thing?id={itemId}";//https://www.boardgamegeek.com/xmlapi2/thing?id=131835
                var itemResponse = await client.GetAsync(itemUrl);
                str = await itemResponse.Content.ReadAsStringAsync();
                doc = XDocument.Parse(str);
                var itemXML = doc.Descendants("item");
                var imageUrl = itemXML.Elements("image").FirstOrDefault()?.Value;
                var name = itemXML.Elements("name").Where(e => e.Attribute("type").Value == "primary").FirstOrDefault()?.Attribute("value").Value;
                var result = new SearchResults
                {
                    Id = int.Parse(itemId),
                    Title = name,
                    ImageURL = imageUrl
                };
                results.Add(result);
                await SaveGameToDatabase(result);
            }

            return new OkObjectResult(results);
        }

        private static async Task SaveGameToDatabase(SearchResults game)
        {
            var connString = _config["DBConnectionString"];

            using (var conn = new SqlConnection(connString))
            {
                var cmdText = @"
                if exists(select ID from Game where ID = @gameId)
                    update Game set imageurl = @imageURL where id = @gameId
                else
                    insert into Game(Id, Title, imageURL) values(@gameId, @gameTitle, @imageURL)";
                using (var command = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    command.Parameters.Add(new SqlParameter("gameId", game.Id));
                    command.Parameters.Add(new SqlParameter("gameTitle", game.Title));
                    command.Parameters.Add(new SqlParameter("imageURL", game.ImageURL));
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
