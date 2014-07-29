
namespace WorkerRole.Models
{
    class ParseResponse
    {
        public string domain { get; set; }
        public object next_page_id { get; set; }
        public string url { get; set; }
        public string short_url { get; set; }
        public string author { get; set; }
        public string excerpt { get; set; }
        public string direction { get; set; }
        public int word_count { get; set; }
        public int total_pages { get; set; }
        public string content { get; set; }
        public string date_published { get; set; }
        public object dek { get; set; }
        public object lead_image_url { get; set; }
        public string title { get; set; }
        public int rendered_pages { get; set; }
    }
}
