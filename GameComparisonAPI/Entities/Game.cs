using System.Xml.Linq;

namespace GameComparisonAPI.Entities
{
    public class Game
    {
        public string ObjectType { get; set; }
        public int Id { get; set; }
        public string SubType { get; set; }
        public string Name { get; set; }
        public int YearPublished { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool Owned { get; set; }
        public int NumberPlays { get; set; }
        public byte[] ImageData { get; set; }

        public static Game ParseItem (XElement el)
        {
            var item = new Game
            {
                ObjectType = el.Attribute("objecttype").Value,
                Id = int.Parse(el.Attribute("objectid").Value),
                SubType = el.Attribute("subtype").Value,
                Name = el.Element("name").Value,
                YearPublished = int.Parse(el.Element("yearpublished").Value),
                ImageUrl = el.Element("image").Value,
                ThumbnailUrl = el.Element("thumbnail").Value,
                Owned = (int.Parse(el.Element("status").Attribute("own").Value) == 1),
                NumberPlays = int.Parse(el.Element("numplays").Value)
            };

            return item;
        }
    }
}
