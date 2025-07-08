using System;

namespace MyCarBuddy.API.Models
{
    public class TechniciansDetails
    {
        public int TechID { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string ProfileImage { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CredatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifedBy { get; set; }
        public bool IsActive { get; set; }
        public int Status { get; set; }
    }
}
