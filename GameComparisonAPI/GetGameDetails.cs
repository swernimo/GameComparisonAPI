using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Xml.Linq;
using GameComparisonAPI.Entities;

namespace GameComparisonAPI
{
    public static class GetGameDetails
    { 
        static readonly HttpClient client = new HttpClient();
        private static IConfigurationRoot _config;

        [FunctionName("GetGameDetails")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetGameDetails/{id}")] HttpRequest req, int id,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"Getting Game Details for game {id}");

            _config = new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            var response = await client.GetAsync($"{_config["BGGBaseUrl"]}/thing?id={id}&stats=1&type=boardgame,boardgameaccessory,boardgameexpansion");

            var str = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(str);

            var itemXML = doc.Descendants("item").First();
            if (itemXML != null)
            {
                var itemType = itemXML.Attribute("type").Value;
                var itemID = int.Parse(itemXML.Attribute("id").Value);
                var name = itemXML.Elements("name").First(e => e.Attribute("type").Value == "primary").Attribute("value").Value;
                var description = itemXML.Element("description").Value;
                var year = itemXML.Element("yearpublished").Attribute("value").Value;
                var ratingsBlock = itemXML.Element("statistics").Element("ratings");
                var stats = new Statistics
                {
                    MinPlayers = int.Parse(itemXML.Element("minplayers").Attribute("value").Value),
                    MaxPlayers = int.Parse(itemXML.Element("maxplayers").Attribute("value").Value),
                    Description = description,
                    PlayingTime = int.Parse(itemXML.Element("playingtime").Attribute("value").Value),
                    PlayerAge = int.Parse(itemXML.Element("minage").Attribute("value").Value),
                    Complexity = decimal.Parse(ratingsBlock.Element("averageweight").Attribute("value").Value),
                    Rating = decimal.Parse(ratingsBlock.Element("average").Attribute("value").Value),
                    SuggestedPlayerAge = GetSuggesstedPlayerAge(itemXML),
                    RecommendedPlayers = GetRecommendedPlayerCount(itemXML)
                };
                var game = new
                {
                    ObjectType = itemType,
                    Id = itemID,
                    Name = name,
                    ImageUrl = itemXML.Element("image").Value,
                    YearPublished = int.Parse(year),
                    stats.MinPlayers,
                    stats.MaxPlayers,
                    stats.PlayerAge,
                    stats.PlayingTime,
                    stats.Rating,
                    stats.RecommendedPlayers,
                    stats.SuggestedPlayerAge,
                    stats.Description,
                    stats.Complexity
                };

                return new OkObjectResult(game);
            }

            return new OkObjectResult(null);
        }

        private static int GetRecommendedPlayerCount(XElement el)
        {
            var numPlayersPoll = el.Elements("poll").FirstOrDefault(e => e.Attribute("name")?.Value == "suggested_numplayers");
            if (numPlayersPoll != null)
            {
                var results = numPlayersPoll.Descendants("results");
                var top = numPlayersPoll.Descendants().FirstOrDefault();
                var playerCount = 0;
                foreach (var i in results)
                {
                    var oldBest = top.Descendants("result").FirstOrDefault(e => e.Attribute("value")?.Value == "Best");
                    var newBest = i.Descendants("result").FirstOrDefault(e => e.Attribute("value")?.Value == "Best");
                    var oldVotes = int.Parse(oldBest.Attribute("numvotes").Value);
                    var newVotes = int.Parse(newBest.Attribute("numvotes").Value);
                    if (newVotes > oldVotes)
                    {
                        playerCount = int.Parse(i.Attribute("numplayers").Value);
                        top = i;
                    }
                }
                return playerCount;
            }
            return 0;
        }

        private static int GetSuggesstedPlayerAge(XElement el)
        {
            var playerAgePoll = el.Elements("poll").FirstOrDefault(e => e.Attribute("name")?.Value == "suggested_playerage");
            if (playerAgePoll != null)
            {
                var results = playerAgePoll.Descendants("result");
                XElement top = results.FirstOrDefault();
                foreach (var i in results)
                {
                    var oldVotes = int.Parse(top.Attribute("numvotes").Value);
                    var newVotes = int.Parse(i.Attribute("numvotes").Value);
                    if (newVotes > oldVotes)
                    {
                        top = i;
                    }
                }

                if (top != null)
                {
                    return int.Parse(top.Attribute("value").Value);
                }
            }
            return 0;
        }
    }
}
