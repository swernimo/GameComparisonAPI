using System.Xml.Linq;
using System.Linq;

namespace GameComparisonAPI.Entities
{
    public class Statistics
    {
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int PlayingTime { get; set; }
        public int PlayerAge { get; set; }
        public string Description { get; set; }
        public int RecommendedPlayers { get; set; }
        public decimal Complexity { get; set; }
        public decimal Rating { get; set; }
        public int SuggestedPlayerAge { get; set; }

        public static Statistics ParseStatistics(XElement el)
        {
            var stats = new Statistics();
            stats.MinPlayers = int.Parse(el.Element("minplayers").Value);
            stats.MaxPlayers = int.Parse(el.Element("maxplayers").Value);
            stats.PlayingTime = int.Parse(el.Element("playingtime").Value);
            stats.PlayerAge = int.Parse(el.Element("age").Value);
            stats.Description = el.Element("description").Value;
            var statsBlock = el.Element("statistics").Element("ratings");
            stats.Complexity = decimal.Parse(statsBlock.Element("averageweight").Value);
            stats.Rating = decimal.Parse(statsBlock.Element("average").Value);
            var playerAgePoll = el.Elements("poll").FirstOrDefault(e => e.Attribute("name")?.Value == "suggested_playerage");
            if(playerAgePoll != null)
            {
                var results = playerAgePoll.Descendants("result");
                XElement top = results.FirstOrDefault();
                foreach(var i in results)
                {
                    var oldVotes = int.Parse(top.Attribute("numvotes").Value);
                    var newVotes = int.Parse(i.Attribute("numvotes").Value);
                    if (newVotes > oldVotes)
                    {
                        top = i;
                    }
                }

                if(top != null)
                {
                    stats.SuggestedPlayerAge = int.Parse(top.Attribute("value").Value);
                }
            }
            var numPlayersPoll = el.Elements("poll").FirstOrDefault(e => e.Attribute("name")?.Value == "suggested_numplayers");
            if(numPlayersPoll != null)
            {
                var results = numPlayersPoll.Descendants("results");
                var top = numPlayersPoll.Descendants().FirstOrDefault();
                var playerCount = 0;
                foreach(var i in results)
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
                stats.RecommendedPlayers = playerCount;
            }
            return stats;
        }
    }
}
