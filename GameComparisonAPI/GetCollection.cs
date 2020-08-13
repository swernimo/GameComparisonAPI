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
    public static class GetCollection
    {
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("GetCollection")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetCollection/{username}")] HttpRequest req, string username,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Trying to get collection for {username}");
            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var collection = new List<Game>();

            var response = await client.GetAsync($"{_config["BGGBaseUrl"]}/collection/?username={username}&subtype=boardgame");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);

            foreach(var el in doc.Elements().Nodes())
            {
                var item = Game.ParseItem((XElement)el);
                await SaveGameToDatabase(item);
                var statsURL = $"{_config["FunctionBaseUrl"]}/GetGameStatistics/{item.Id}?code={_config["FunctionCode"]}";
                var statsResponse = await client.GetAsync(statsURL);
                var stats = await statsResponse.Content.ReadAsAsync<Statistics>();
                if (stats != null)
                {
                    item.Statistics = stats;
                }
                collection.Add(item);
            }
            log.LogInformation($"Successfully got collection for user {username}");
            return new OkObjectResult(collection);
        }

        private static async Task SaveGameToDatabase(Game game)
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
                    command.Parameters.Add(new SqlParameter("gameTitle", game.Name));
                    command.Parameters.Add(new SqlParameter("imageURL", game.ImageUrl));
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
