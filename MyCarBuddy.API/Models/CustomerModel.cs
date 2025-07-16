using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyCarBuddy.API.Models
{
    public class CustomerModel
    {
        [BindNever]
        public int  CustID { get; set; }

        public string FullName {  get; set; }
        public string PhoneNumber {  get; set; }
        public string AlternateNumber { get; set; } = string.Empty;
        public string Email {  get; set; }

        [BindNever]
        public string ProfileImage {  get; set; }

        public IFormFile ProfileImageFile { get; set; }
        
        [BindNever]
        public bool IsActive { get; set; }

        [BindNever]
        public int Status { get; set;}


    }
}
