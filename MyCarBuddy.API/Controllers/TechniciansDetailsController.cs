using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using Newtonsoft.Json;
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
        private readonly IWebHostEnvironment _env;

        public TechniciansDetailsController(IConfiguration configuration, ILogger<TechniciansDetailsController> logger,IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env=env;
        }

      

        #region InsertTechnicians
        [HttpPost]
        [Route("InsertTechnicians")]
        public async Task<IActionResult> InsertTechnicians([FromForm] TechniciansModel technicians)
        {
            SqlConnection conn = null;
            SqlTransaction transaction = null;

            try
            {
                // Step 1: Validate document file and metadata count BEFORE inserting anything
                int fileCount = technicians.DocumentFiles?.Count ?? 0;
                int metaCount = technicians.Documents?.Count ?? 0;

                if (fileCount != metaCount)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "The number of document files must match the number of document metadata entries.",
                        fileCount,
                        metaCount
                    });
                }

                // Step 2: Save profile image (only if validation passes)
                string imagePath = null;
                if (technicians.ProfileImageFile != null && technicians.ProfileImageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(technicians.ProfileImageFile.FileName);
                    //var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(),  "Images","Technicians");
                    var imagesFolder = Path.Combine(_env.WebRootPath, "Images", "Technicians");
                    if (!Directory.Exists(imagesFolder))
                        Directory.CreateDirectory(imagesFolder);

                    var filePath = Path.Combine(imagesFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await technicians.ProfileImageFile.CopyToAsync(stream);
                    }
                    imagePath = Path.Combine("Technicians", fileName).Replace("\\", "/");
                }

                technicians.ProfileImage = imagePath;

                // Step 3: Begin SQL transaction
                conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();
                transaction = conn.BeginTransaction();

                int techId;

                // Step 4: Insert technician and get TechID
                using (var cmd = new SqlCommand("sp_InsertTechniciansDetails", conn, transaction))
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
                    cmd.Parameters.AddWithValue("@ProfileImage", (object)technicians.ProfileImage ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CredatedBy", technicians.CreatedBy);

                    var result = await cmd.ExecuteScalarAsync();
                    techId = Convert.ToInt32(result);
                }

                // Step 5: Insert documents
                for (int i = 0; i < fileCount; i++)
                {
                    var file = technicians.DocumentFiles[i];
                    var docMeta = technicians.Documents[i];

                    // Save document file
                    var docFileName = Path.GetFileName(file.FileName);
                    var documentsFolder = Path.Combine(_env.WebRootPath, "Documents");
                    if (!Directory.Exists(documentsFolder))
                        Directory.CreateDirectory(documentsFolder);

                    var docFilePath = Path.Combine(documentsFolder, docFileName);
                    using (var stream = new FileStream(docFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    var documentUrl = Path.Combine("Documents", docFileName).Replace("\\", "/");

                    // Insert document record
                    using (var docCmd = new SqlCommand("sp_InsertTechnicianDocument", conn, transaction))
                    {
                        docCmd.CommandType = CommandType.StoredProcedure;
                        docCmd.Parameters.AddWithValue("@TechID", techId);
                        docCmd.Parameters.AddWithValue("@DocTypeID", docMeta.DocTypeID);
                        docCmd.Parameters.AddWithValue("@DocumentURL", documentUrl);
                        docCmd.Parameters.AddWithValue("@UploadedAt", docMeta.UploadedAt ?? DateTime.Now);
                        docCmd.Parameters.AddWithValue("@Verified", docMeta.Verified);
                        docCmd.Parameters.AddWithValue("@VerifiedBy", (object)docMeta.VerifiedBy ?? DBNull.Value);
                        docCmd.Parameters.AddWithValue("@VerifiedAt", (object)docMeta.VerifiedAt ?? DBNull.Value);

                        await docCmd.ExecuteNonQueryAsync();
                    }
                }

                // Step 6: Commit transaction
                transaction.Commit();
                return Ok(new { status = true, message = "Technician and documents inserted successfully." });
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        #endregion

        [HttpPut]
        [Route("UpdateTechnicians")]
        public async Task<IActionResult> UpdateTechnicians([FromForm] TechniciansModel technicians)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Check if technician exists
                bool techExists = false;
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand checkCmd = new SqlCommand("sp_CheckTechnicianExists", conn))
                    {
                        checkCmd.CommandType = CommandType.StoredProcedure;
                        checkCmd.Parameters.AddWithValue("@TechID", technicians.TechID);
                        await conn.OpenAsync();
                        techExists = (int)await checkCmd.ExecuteScalarAsync() > 0;
                        conn.Close();
                    }
                }

                if (!techExists)
                    return NotFound(new { status = false, message = "Technician not found." });

                // Fetch existing profile image if not uploading new one
                string imagePath = technicians.ProfileImage;
                if (string.IsNullOrEmpty(imagePath) && (technicians.ProfileImageFile == null || technicians.ProfileImageFile.Length == 0))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_GetTechnicianProfileImageByID", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@TechID", technicians.TechID);
                            await conn.OpenAsync();
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                                imagePath = result.ToString();
                        }
                    }
                }

                // Upload profile image if new file is provided
                if (technicians.ProfileImageFile != null && technicians.ProfileImageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(technicians.ProfileImageFile.FileName);
                    var imagesFolder = Path.Combine(_env.WebRootPath, "Images", "Technicians");
                    Directory.CreateDirectory(imagesFolder);

                    var filePath = Path.Combine(imagesFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await technicians.ProfileImageFile.CopyToAsync(stream);
                    }

                    imagePath = Path.Combine("Technicians", fileName).Replace("\\", "/");
                }

                // Update technician info (without documents)
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
                        cmd.Parameters.AddWithValue("@AddressLine2", (object?)technicians.AddressLine2 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StateID", technicians.StateID);
                        cmd.Parameters.AddWithValue("@CityID", technicians.CityID);
                        cmd.Parameters.AddWithValue("@Pincode", technicians.Pincode);
                        cmd.Parameters.AddWithValue("@ProfileImage", (object?)imagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy", technicians.CreatedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModifedBy", (object?)technicians.ModifiedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", technicians.IsActive);
                        cmd.Parameters.AddWithValue("@Status", technicians.Status);

                        // Document fields as NULL
                        cmd.Parameters.AddWithValue("@DocID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocuTypeID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocumentURL", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Verified", DBNull.Value);
                        cmd.Parameters.AddWithValue("@VerifiedBy", DBNull.Value);
                        cmd.Parameters.AddWithValue("@VerifiedAt", DBNull.Value);

                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Insert documents only if files and metadata are present
                if ((technicians.DocumentFiles?.Count ?? 0) > 0 && (technicians.Documents?.Count ?? 0) > 0)
                {
                    for (int i = 0; i < Math.Min(technicians.DocumentFiles.Count, technicians.Documents.Count); i++)
                    {
                        var file = technicians.DocumentFiles[i];
                        var docMeta = technicians.Documents[i];

                        var docFileName = Path.GetFileName(file.FileName);
                        var documentUrl = Path.Combine("Documents", docFileName).Replace("\\", "/");

                        // Check if document already exists
                        bool documentExists = false;
                        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                        {
                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM TechnicianDocuments WHERE TechID = @TechID AND DocuTypeID = @DocuTypeID", conn))
                            {
                                checkCmd.Parameters.AddWithValue("@TechID", technicians.TechID);
                                checkCmd.Parameters.AddWithValue("@DocuTypeID", docMeta?.DocTypeID ?? 0);
                                await conn.OpenAsync();
                                documentExists = (int)await checkCmd.ExecuteScalarAsync() > 0;
                            }
                        }

                        if (documentExists)
                            continue;

                        var documentsFolder = Path.Combine(_env.WebRootPath, "Documents");
                        Directory.CreateDirectory(documentsFolder);
                        var docFilePath = Path.Combine(documentsFolder, docFileName);
                        using (var stream = new FileStream(docFilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_UpdateTechniciansDetails", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@TechID", technicians.TechID);
                                cmd.Parameters.AddWithValue("@DealerID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@FullName", DBNull.Value);
                                cmd.Parameters.AddWithValue("@PhoneNumber", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Email", DBNull.Value);
                                cmd.Parameters.AddWithValue("@PasswordHash", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AddressLine1", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AddressLine2", DBNull.Value);
                                cmd.Parameters.AddWithValue("@StateID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@CityID", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Pincode", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ProfileImage", DBNull.Value);
                                cmd.Parameters.AddWithValue("@CreatedBy", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ModifedBy", DBNull.Value);
                                cmd.Parameters.AddWithValue("@IsActive", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Status", DBNull.Value);

                                cmd.Parameters.AddWithValue("@DocID", docMeta?.DocID ?? 0);
                                cmd.Parameters.AddWithValue("@DocuTypeID", docMeta?.DocTypeID != null ? docMeta.DocTypeID : (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DocumentURL", documentUrl);
                                cmd.Parameters.AddWithValue("@Verified", docMeta?.Verified ?? false);
                                cmd.Parameters.AddWithValue("@VerifiedBy", (object?)docMeta?.VerifiedBy ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@VerifiedAt", (object?)docMeta?.VerifiedAt ?? DBNull.Value);

                                await conn.OpenAsync();
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                return Ok(new { status = true, message = "Technician and documents updated successfully.", techId = technicians.TechID });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while updating the record.", error = ex.Message });
            }
        }




        #region GetAllTechnicians
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
                return Ok(new { status = true,jsonResult });
            }
               
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status=false, message = "An error occurred while retrieving the Technicians.", error = ex.Message });

            }
        }

        #endregion

        #region GetTechnicianByID

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

        #endregion

        #region DeleteTechnician
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

        #endregion

        #region GetTechniciansWithDocuments

        [HttpGet("GetTechniciansWithDocuments")]
        public IActionResult GetTechniciansWithDocuments()
        {
            try
            {
                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetTechniciansDetailsWithDocumentTypes", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                }

                // Convert DataTable to JSON-friendly list
                var jsonList = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col] is DBNull ? null : row[col];
                    }
                    jsonList.Add(dict);
                }

                return Ok(new
                {
                    status = true,
                    message = "Technicians with documents retrieved successfully.",
                    data = jsonList
                });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while fetching data.",
                    error = ex.Message
                });
            }
        }

        #endregion
    }
}


