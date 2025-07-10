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


        [HttpGet]

        public IActionResult GetAllTechnicians()
        {
            try
            {
                DataTable dt = new DataTable();
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_ListTechniciansDetails",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load data into DataTable
                        }
                        conn.Close();
                    }
                }
                // Convert DataTable to JSON-friendly structure
                var jsonResult = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    jsonResult.Add(dict);
                }
                return Ok(new { status = true, data = jsonResult });
            }
               
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status=false, message = "An error occurred while retrieving the Technicians.", error = ex.Message });

            }
        }

        [HttpGet("technicianid")]

        public IActionResult GetTechnicianByID(int technicianid)
        {
            try
            {
                DataTable dt = new DataTable();
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetTechniciansDetailsByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TechID", technicianid);
                        conn.Open();
                        using(SqlDataReader reader=cmd.ExecuteReader())
                        {
                            dt.Load(reader);

                        }
                        conn.Close();

                    }
                }
                if(dt.Rows.Count==0)
                {
                    return NotFound(new { message = "Technicians not found...." });
                }

                var jsonResult = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    jsonResult.Add(dict);
                }

                // If you expect only one row, you can return jsonResult[0]
                return Ok(new { status = true, data = (object)(jsonResult.Count == 1 ? jsonResult[0] : jsonResult) });


            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status=false, message = "An Error occured while retrieving the Technicians by ID..", error = ex.Message });
            }

        }
        [HttpDelete("technicianid")]

        public IActionResult DeleteTechnician(int technicianid)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_DeleteTechniciansDetails",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@TechID", technicianid);
                        conn.Open();
                        using(SqlDataReader reader=cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int success = Convert.ToInt32(reader["Success"]);
                                string message = reader["Message"].ToString();

                                if (success == 1)
                                    return Ok(new { status = true, message });
                                else
                                    return NotFound(new { status = false, message });
                            }
                            else
                            {
                                return StatusCode(500, new { status = false, message = "No response from database." });
                            }
                        }
                        

                    }
                }

            }
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while deleting the record.", error = ex.Message });

            }
        }
    }
}


