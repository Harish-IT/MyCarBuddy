using Microsoft.AspNetCore.Http;

namespace MyCarBuddy.API.Models
{
    public class BlogsModel
    {
        public string PostTitle { get; set; } = string.Empty;
        public string PostCategory { get; set; } = string.Empty;
        public string PostDescription { get; set; } = string.Empty;

        public IFormFile Thumbnai1 { get; set; }
        public string Thumbnail { get; set; } = string.Empty;

    }
}
