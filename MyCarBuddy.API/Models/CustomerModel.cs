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

    public class CustomerOTPFlowModel
    {
        public int Step { get; set; } // 1 = Send OTP, 2 = Verify OTP, 3 = Register Customer

        // Common
        public string PhoneNumber { get; set; }
        public string OTP { get; set; }

        // Only for Step 3
        public string FullName { get; set; }
        public string AlternateNumber { get; set; }
        public string Email { get; set; }
        public IFormFile ProfileImageFile { get; set; }
    }

}
