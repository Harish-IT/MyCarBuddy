using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyCarBuddy.API.Models
{
    public class CustomerModel
    {
<<<<<<< HEAD
        public int?  CustID { get; set; }
=======
        
        public int  CustID { get; set; }
>>>>>>> 473ed5a9cfd9e10fc0e00481349b67c9f1ce3d3e

        public string FullName {  get; set; }
        public string PhoneNumber {  get; set; }
        public string AlternateNumber { get; set; } = string.Empty;
        public string Email {  get; set; }

<<<<<<< HEAD
       
        public string ProfileImage { get; set; }=string.Empty;
=======
        
        public string ProfileImage {  get; set; }
>>>>>>> 473ed5a9cfd9e10fc0e00481349b67c9f1ce3d3e

        public IFormFile ProfileImageFile { get; set; }
        
        
        public bool IsActive { get; set; }

        
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
