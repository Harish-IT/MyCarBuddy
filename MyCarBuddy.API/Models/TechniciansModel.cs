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
        
        public int? TechID { get; set; }

        public int DealerID { get; set; }

     
        public string FullName { get; set; }

    
        public string PhoneNumber { get; set; }

        public string Email { get; set; }

    
        public string PasswordHash { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public int StateID { get; set; }

        public int CityID { get; set; }

        public string Pincode { get; set; }

        [BindNever]
        public string ProfileImage { get; set; }

        public IFormFile ProfileImageFile { get; set; }

        [BindNever]
        public DateTime CreatedDate { get; set; }

        // [Required(ErrorMessage = "CreatedBy is required.")]
        public string CreatedBy { get; set; } = null;

        [BindNever]
        public DateTime? ModifiedDate { get; set; }


        public string ModifiedBy { get; set; } = null;

        public bool? IsActive { get; set; }

        [BindNever]
        public int Status { get; set; }

        public int? DistributorID { get; set; }

        public List<int> SkillIDs { get; set; }

        // public int? SkillID { get; set; }

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
        public int? DocID {  get; set; }
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
