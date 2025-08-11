using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MyCarBuddy.API.Models
{
    public class ServiceImageModel
    {
        public int? ImageID {  get; set; }
        public int? BookingID {  get; set; }
        
        public List <IFormFile> ImageURL1 {  get; set; }

        public string ImageURL { get; set; } = string.Empty;
        public int? UploadedBy {  get; set; }
        public int? TechID {  get; set; }
        public string ImageUploadType { get; set; } = string.Empty;

        public string ImagesType {  get; set; } = string.Empty;

    }
}
