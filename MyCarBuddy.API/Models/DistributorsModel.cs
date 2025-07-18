using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;

public class DistributorsModel
{
    public int DistributorID { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone Number must be a valid 10-digit Indian mobile number.")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(255, ErrorMessage = "Password hash cannot exceed 255 characters.")]
    public string PasswordHash { get; set; }

    [Required(ErrorMessage = "StateID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "StateID must be a positive integer.")]
    public int StateID { get; set; }

    [Required(ErrorMessage = "CityID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "CityID must be a positive integer.")]
    public int CityID { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string Address { get; set; }

    [Required(ErrorMessage = "GST Number is required.")]
    [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GST Number format.")]
    public string GSTNumber { get; set; }

    [BindNever]
    public DateTime CreatedDate { get; set; }
    
    public bool? IsActive { get; set; }
}