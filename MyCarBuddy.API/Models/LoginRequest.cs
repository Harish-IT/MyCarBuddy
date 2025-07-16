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
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OTP { get; set; }

        public string DeviceToken { get; set; }
        public string DeviceId { get; set; }
    }
}
