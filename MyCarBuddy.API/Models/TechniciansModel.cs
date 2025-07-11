using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCarBuddy.API.Models
{
    public class TechniciansModel
    {
        [BindNever]
        public int TechID { get; set; }

        [Required(ErrorMessage = "Dealer ID is required.")]
        public int DealerID { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required.")]
        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        [Required(ErrorMessage = "State is required.")]
        public int StateID { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public int CityID { get; set; }

        [Required(ErrorMessage = "Pincode is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid Indian Pincode.")]
        public string Pincode { get; set; }

        [BindNever]
        public string ProfileImage { get; set; }

        public IFormFile ProfileImageFile { get; set; }

        [BindNever]
        public DateTime CreatedDate { get; set; }

        [Required(ErrorMessage = "CreatedBy is required.")]
        public string CreatedBy { get; set; }

        [BindNever]
        public DateTime? ModifiedDate { get; set; }

        [BindNever]
        public string ModifiedBy { get; set; }

        [BindNever]
        public bool IsActive { get; set; }

        [BindNever]
        public int Status { get; set; }

        // --- Document Upload Handling ---

        [NotMapped]
        public List<IFormFile> DocumentFiles { get; set; }

        [FromForm]
        [NotMapped]
        public string DocumentsJson { get; set; }

        [NotMapped]
        public List<TechnicianDocumentModel> Documents
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(DocumentsJson)
                        ? new List<TechnicianDocumentModel>()
                        : JsonConvert.DeserializeObject<List<TechnicianDocumentModel>>(DocumentsJson);
                }
                catch
                {
                    return new List<TechnicianDocumentModel>();
                }
            }
        }
    }

    public class TechnicianDocumentModel
    {
        public int DocTypeID { get; set; }              
        public string DocumentURL { get; set; }        
        public DateTime? UploadedAt { get; set; }
        public bool Verified { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class TechniciansDocumentTypes
    {
        public int DocuTypeID { get; set; }
        public string DocumentType { get; set; }
        
    }
}
