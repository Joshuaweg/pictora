using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Pictora.Models {

    public class Prompt
    {
        public string prompt { get; set; } = string.Empty;
        public Prompt_Size image_size { get; set; } = new();
        // TODO - Add something for models
        // TODO - Add something for style
    }

    public class Prompt_Size {

        // If there was a way to make these read-only if called outside of this class, I'd use that instad of setting them as 'public'
        public int width { get; set; } = 1028;
        public int height { get; set; } = 1028;

        /// <summary>
        /// Sets the values of the images height and width from the drop-down menu
        /// </summary>
        /// <param name="index">The ID of the images size</param>
        public void SetValues(int index) { 
            switch (index)
            {
                case 0: // square
                    width = 512; height = 512; break;
                case 1: // square_hd
                    width = 1028; height = 1028; break;
                case 2: // portrait_4_3
                    width = 768; height = 1024; break;
                case 3: // portrait_16_9
                    width = 576; height = 1024; break;
                case 4: // landscape_3_4
                    width = 1024; height = 768; break;
                case 5: // landscape_9_16
                    width = 1024; height = 576; break;
                case 6: // Wallpaper
                    width = 1920; height = 1080; break;
                default: // Blank (default)
                    width = 1028; height = 1028; break;
            }
            
        }

        /// <summary>
        /// Sets the values of the images size directly. (This is used with the custom option).
        /// </summary>
        /// <param name="w"> The images width </param>
        /// <param name="h"> The images height </param>
        public void SetValues(int w, int h)
        {
            
            width = w;
            height = h;

            // There seems to be a limit on how large of an image this AI model can generate. The limit is (1344 x 1344)
            // Anything lower then (500x500) is just going to look like a color smear. Not sure what the absolute limit is though.
            if (w > 1344)
                width = 1344;
            else if (w < 500)
                width = 500;

            if (h > 1344)
                height = 1344;
            else if (h < 500)
                height = 500;
        }


    }

    public class Progress
    {
        public string status { get; set; } = string.Empty;
        public string response_url { get; set; } = string.Empty;
        public string status_url { get; set; } = string.Empty;
    }

    public class Result
    {
        public string detail { get; set; } = string.Empty;
        public List<Images> images { get; set; } = [];
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