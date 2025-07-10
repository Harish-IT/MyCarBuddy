using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TechniciansDetailsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TechniciansDetailsController> _logger;

        public TechniciansDetailsController(IConfiguration configuration, ILogger<TechniciansDetailsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }



        [HttpPost]
        [Route("InsertTechnicians")]
        public async Task<IActionResult> InsertTechnicians([FromForm] TechniciansModel technicians)
        {
            try
            {
                string imagePath = null;
                if (technicians.ProfileImageFile != null && technicians.ProfileImageFile.Length > 0)
                {
                    // Use only the original file name
                    var fileName = Path.GetFileName(technicians.ProfileImageFile.FileName);
                    var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                    if (!Directory.Exists(imagesFolder))
                        Directory.CreateDirectory(imagesFolder);

                    var filePath = Path.Combine(imagesFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await technicians.ProfileImageFile.CopyToAsync(stream);
                    }
                    imagePath = Path.Combine("Images", fileName).Replace("\\", "/");
                }
                // 2. Set the image path in the model (for DB)
                technicians.ProfileImage = imagePath;

                // 3. Insert into database
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertTechniciansDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DealerID", technicians.DealerID);
                        cmd.Parameters.AddWithValue("@FullName", technicians.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", technicians.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Email", technicians.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", technicians.PasswordHash);
                        cmd.Parameters.AddWithValue("@AddressLine1", technicians.AddressLine1);
                        cmd.Parameters.AddWithValue("@AddressLine2", (object)technicians.AddressLine2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StateID", technicians.StateID);
                        cmd.Parameters.AddWithValue("@CityID", technicians.CityID);
                        cmd.Parameters.AddWithValue("@Pincode", technicians.Pincode);
                        cmd.Parameters.AddWithValue("@ProfileImage", (object)technicians.ProfileImage ?? DBNull.Value); // Save path, not file
                        cmd.Parameters.AddWithValue("@CredatedBy", technicians.CredatedBy);

                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true, message = "Technician is inserted" });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Technician is not inserted" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error as per your existing logic
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("UpdateTechnicians")]
        public async Task<IActionResult> UpdateTechnicians([FromForm] TechniciansModel technicians)
        {
            try
            {
                string imagePath = technicians.ProfileImage; // Default to existing path

                // If a new image is uploaded, save it and update the path
                if (technicians.ProfileImageFile != null && technicians.ProfileImageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(technicians.ProfileImageFile.FileName);
                    var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                    if (!Directory.Exists(imagesFolder))
                        Directory.CreateDirectory(imagesFolder);

                    var filePath = Path.Combine(imagesFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await technicians.ProfileImageFile.CopyToAsync(stream);
                    }
                    imagePath = Path.Combine("Images", fileName).Replace("\\", "/");
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateTechniciansDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TechID", technicians.TechID);
                        cmd.Parameters.AddWithValue("@DealerID", technicians.DealerID);
                        cmd.Parameters.AddWithValue("@FullName", technicians.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", technicians.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Email", technicians.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", technicians.PasswordHash);
                        cmd.Parameters.AddWithValue("@AddressLine1", technicians.AddressLine1);
                        cmd.Parameters.AddWithValue("@AddressLine2", (object)technicians.AddressLine2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StateID", technicians.StateID);
                        cmd.Parameters.AddWithValue("@CityID", technicians.CityID);
                        cmd.Parameters.AddWithValue("@Pincode", technicians.Pincode);
                        cmd.Parameters.AddWithValue("@ProfileImage", (object)imagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModifedBy", technicians.ModifedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", technicians.IsActive);
                        cmd.Parameters.AddWithValue("@Status", technicians.Status);

                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true, message = "Technician updated successfully." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Technician not found or not updated." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while updating the record.", error = ex.Message });
            }
        }
    }
}


