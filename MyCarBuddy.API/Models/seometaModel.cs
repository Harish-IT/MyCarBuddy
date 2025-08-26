namespace MyCarBuddy.API.Models
{
    public class seometaModel
    {
        public int? seo_id { get; set; }
        public string page_slug { get; set; } = string.Empty;
        public string seo_title { get; set; } = string.Empty;
        public string seo_description { get; set; } = string.Empty;
        public string seo_keywords { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
        public decimal? seo_score { get; set; }


    }
}
