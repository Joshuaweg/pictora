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
}