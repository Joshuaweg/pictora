using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Pictora.Models {
    public class Progress
    {
        public string status { get; set; } = string.Empty;
        public string response_url { get; set; } = string.Empty;
        public string status_url { get; set; } = string.Empty;
    }

    public class Result
    {
        public string detail { get; set; } = string.Empty;
        public List<Images> images { get; set; } = new();
        public string prompt { get; set; } = string.Empty;
        public string style { get; set; } = string.Empty;
        public string model { get; set; } = string.Empty;
        public DateTime created { get; set; } = DateTime.Now;
    }

    public class Images
    {
        public string url { get; set; } = string.Empty;
        public int width { get; set; } = 0;
        public int height { get; set; } = 0;
    }

    public class Image
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("id")]
        public int NumericId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("model")]
        public string Model { get; set; }

        [BsonElement("style")]
        public string Style { get; set; }

        [BsonElement("prompt")]
        public string Prompt { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("userid")]
        public int UserId { get; set; }

        [BsonElement("created")]
        public DateTime Created { get; set; }

        [BsonElement("baseImage")]
        public int BaseImage { get; set; }

        [BsonElement("upvotes")]
        public int Upvotes { get; set; }

        [BsonElement("downvotes")]
        public int Downvotes { get; set; }

        [BsonElement("tags")]
        public List<string> Tags { get; set; }

        [BsonElement("image_size")]
        public ImageSize ImageSize { get; set; }

        [BsonElement("image_url")]
        public string ImageUrl { get; set; }
    }

    public class ImageSize
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("height")]
        public int Height { get; set; }

        [BsonElement("width")]
        public int Width { get; set; }
    }
}