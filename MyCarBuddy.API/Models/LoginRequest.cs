using Microsoft.AspNetCore.Http;

namespace MyCarBuddy.API.Models
{
    public class LoginRequest
    {
        public string PhoneNumber { get; set; }
        public string Password { get; set; }

        public string Email { get; set; }
       
    }

    public class CustomerLoginRequest
    {
        //public string PhoneNumber { get; set; }
        //public string Email { get; set; }
        public string LoginId { get; set; }
        public string OTP { get; set; }

        public string DeviceToken { get; set; }
        public string DeviceId { get; set; }

       
    }

    public class TechLoginRequest
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
       // public string LoginId { get; set; }
        public string OTP { get; set; }

        public string DeviceToken { get; set; }
        public string DeviceId { get; set; }


    }

    public class AdminUpdate
    {
        public int? AdminID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public IFormFile ProfileImage1 {  get; set; }
        public string ProfileImage { get; set; } = string.Empty;

    }
}
