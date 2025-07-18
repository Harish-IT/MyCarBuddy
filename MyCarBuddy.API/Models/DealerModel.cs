using System;
using System.ComponentModel.DataAnnotations;

namespace MyCarBuddy.API.Models
{
    public class DealerModel
    {
        public int DealerID { get; set; }

        [Required(ErrorMessage = "DistributorID is required.")]
        public int DistributorID { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [StringLength(15, ErrorMessage = "Phone Number cannot exceed 15 characters.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone Number must be a valid 10-digit Indian number starting with 6-9.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(255, ErrorMessage = "Password cannot exceed 255 characters.")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "StateID is required.")]
        public int StateID { get; set; }

        [Required(ErrorMessage = "CityID is required.")]
        public int CityID { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "GST Number is required.")]
        [StringLength(20, ErrorMessage = "GST Number cannot exceed 20 characters.")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GST Number format.")]
        public string GSTNumber { get; set; }

        [Required(ErrorMessage = "Created Date is required.")]
        public DateTime CreatedDate { get; set; }

        [Required(ErrorMessage = "IsActive is required.")]
        public bool? IsActive { get; set; }
    }
}