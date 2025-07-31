using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PlanPackagePriceController : ControllerBase
    {
        #region IConfiguration
        private readonly IConfiguration _configuration;
        private readonly ILogger<PlanPackagePriceController> _logger;
        private readonly IWebHostEnvironment _env;


        public PlanPackagePriceController(IConfiguration configuration, ILogger<PlanPackagePriceController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion

        #region GetRandomAlphanumericString
        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion

        #region AddPlanPackagePrices

        [HttpPost("AddPlanPackagePrice")]

        public async Task<IActionResult> AddPlanPackagePrice([FromForm] PlanPackagePriceModel planpricepackage)
        {
            try
            {
              
                string imageurlpath = string.Empty;

                if (planpricepackage.ImageURL1 != null && planpricepackage.ImageURL1.Length > 0)
                {
                    var Pricefolderpath = Path.Combine(_env.WebRootPath, "Images", "PlanPrice");
                    if (!Directory.Exists(Pricefolderpath))
                        Directory.CreateDirectory(Pricefolderpath);

                    var originalFileName = Path.GetFileNameWithoutExtension(planpricepackage.ImageURL1.FileName);
                    var fileExt = Path.GetExtension(planpricepackage.ImageURL1.FileName);
                    var randomString = GetRandomAlphanumericString(8);
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var PriceImageFullPath = Path.Combine(Pricefolderpath, uniqueFileName);

                    int counter = 1;
                    while (System.IO.File.Exists(PriceImageFullPath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        PriceImageFullPath = Path.Combine(Pricefolderpath, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(PriceImageFullPath, FileMode.Create))
                    {
                        await planpricepackage.ImageURL1.CopyToAsync(stream);
                    }

                    imageurlpath = Path.Combine("PlanPrice", uniqueFileName).Replace("\\", "/");


                }
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertPlanPackagePrice", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@BrandID", planpricepackage.BrandID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModelID", planpricepackage.ModelID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FuelTypeID", planpricepackage.FuelTypeID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PackageID", planpricepackage.PackageID ?? (object)DBNull.Value);
                       
                        cmd.Parameters.AddWithValue("@ImageURL", imageurlpath ?? "");
                        cmd.Parameters.AddWithValue("@IsActive", planpricepackage.IsActive ?? true);
                        cmd.Parameters.AddWithValue("@Serv_Reg_Price", planpricepackage.Serv_Reg_Price);
                        cmd.Parameters.AddWithValue("@Serv_Off_Price", planpricepackage.Serv_Off_Price);
                        cmd.Parameters.AddWithValue("@CreatedBy", planpricepackage.CreatedBy ?? 0);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Plan Package Price inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Insertion failed." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }

        }

        #endregion

        #region UpdatePlanPackagePrice


        [HttpPut("UpdatePlanPackagePrice")]
        public async Task<IActionResult> UpdatePlanPackagePrice([FromForm] PlanPackagePriceModel planpricepackage)
        {
            try
            {
                string imageUrlpath = planpricepackage.ImageURL ?? string.Empty;


                if (string.IsNullOrEmpty(imageUrlpath) && (planpricepackage.ImageURL1 == null || planpricepackage.ImageURL1.Length == 0))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    using (SqlCommand cmd = new SqlCommand("sp_GetPlanPackagePriceImagesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PlanPriceID", planpricepackage.PlanPriceID);

                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                imageUrlpath = reader["ImageURL"]?.ToString();
                            }
                        }
                    }
                }



                if (planpricepackage.ImageURL1 != null && planpricepackage.ImageURL1.Length > 0)
                {
                    var imageFolderPath = Path.Combine(_env.WebRootPath, "Images", "PlanPrice");
                    if (!Directory.Exists(imageFolderPath))
                        Directory.CreateDirectory(imageFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(planpricepackage.ImageURL1.FileName);
                    var fileExt = Path.GetExtension(planpricepackage.ImageURL1.FileName);
                    var randomString = GetRandomAlphanumericString(8);
                    var uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var physicalPath = Path.Combine(imageFolderPath, uniqueFileName);

                    int counter = 1;
                    while (System.IO.File.Exists(physicalPath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        physicalPath = Path.Combine(imageFolderPath, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                    {
                        await planpricepackage.ImageURL1.CopyToAsync(stream);
                    }

                    // Set path to relative path for DB
                    imageUrlpath = Path.Combine("PlanPrice", uniqueFileName).Replace("\\", "/");
                }



                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdatePlanPackagePrice", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@PlanPriceID", planpricepackage.PlanPriceID);
                        cmd.Parameters.AddWithValue("@BrandID", planpricepackage.BrandID ?? 0);
                        cmd.Parameters.AddWithValue("@ModelID", planpricepackage.ModelID ?? 0);
                        cmd.Parameters.AddWithValue("@FuelTypeID", planpricepackage.FuelTypeID ?? 0);
                        cmd.Parameters.AddWithValue("@PackageID", planpricepackage.PackageID ?? 0);
                      
                        cmd.Parameters.AddWithValue("@ImageURL", imageUrlpath);
                        cmd.Parameters.AddWithValue("@IsActive", planpricepackage.IsActive ?? true);
                        cmd.Parameters.AddWithValue("@Serv_Reg_Price", planpricepackage.Serv_Reg_Price);
                        cmd.Parameters.AddWithValue("@Serv_Off_Price", planpricepackage.Serv_Off_Price);
                        cmd.Parameters.AddWithValue("@ModifiedBy", planpricepackage.ModifiedBy ?? 0);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Plan Package Price updated successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Plan Package Price not updated." });
                        }
                    }
                }

            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while updating the record.", error = ex.Message });

            }
        }

        #endregion


        #region GetListPlanPackagePrice

        [HttpGet]

        public IActionResult GetListPlanPackagePrice()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListPlanPackagePrice", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    var Data = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            dict[col.ColumnName] = row[col];
                        }
                        Data.Add(dict);
                    }
                    return Ok(new { status = true, Data });
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Plan Package Price.", error = ex.Message });

            }
        }

        #endregion



        #region SubCategory1ById


        [HttpGet("planpackagepriceid")]

        public IActionResult GetPlanPackagePriceById(int planpackagepriceid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListPlanPackagePriceBYID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PlanPriceID", planpackagepriceid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                }
                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Plan Package Price not found.." });
                }
                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    Data.Add(dict);
                }
                return Ok(Data.Count == 1 ? Data[0] : Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Plan Package Price.", error = ex.Message });

            }

        }

        #endregion


        #region planpackagepriceid

        [HttpDelete("planpackagepriceid")]

        public IActionResult DeletePlanPackagePriceById(int planpackagepriceid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeletePlanPackagePriceByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PlanPriceID", planpackagepriceid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int resultCode = Convert.ToInt32(reader["ResultCode"]);
                                string message = reader["Message"].ToString();

                                if (resultCode == 1)
                                    return Ok(new { message });
                                else if (resultCode == -1)
                                    return BadRequest(new { message });
                                else
                                    return NotFound(new { message });
                            }
                        }
                        conn.Close();
                    }
                }
                return StatusCode(500, new { message = "Unknown error occurred." });
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while deleting the record.", error = ex.Message });

            }
        }

        #endregion

    }
}
