using Braintree;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class VehicleModelsController : ControllerBase
    {
        #region IConfiguration

        private readonly IConfiguration _configuration;
        private readonly ILogger<VehicleModelsController> _logger;
        private readonly IWebHostEnvironment _env;

        public VehicleModelsController(IConfiguration configuration, ILogger<VehicleModelsController> logger,IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion


        #region Insert Vehicle Model


        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost("InsertVehicleModel")]
        public async Task<IActionResult> InsertVehicleModel([FromForm] VehicleModelsClass vehiclemodelclass)
        {
            if (vehiclemodelclass == null)
            {
                return BadRequest(new { status = false, message = "Invalid input data." });
            }

            string vehiclemodelimage = null;
            string filePath = null;

            try
            {
                if (vehiclemodelclass.VehicleImages1 != null && vehiclemodelclass.VehicleImages1.Length > 0)
                {
                    var vehicleModelFolder = Path.Combine(_env.WebRootPath, "Images", "VehicleModel");

                    if (!Directory.Exists(vehicleModelFolder))
                        Directory.CreateDirectory(vehicleModelFolder);

                    var originalFileName = Path.GetFileNameWithoutExtension(vehiclemodelclass.VehicleImages1.FileName);
                    var fileExt = Path.GetExtension(vehiclemodelclass.VehicleImages1.FileName);
                    var randomString = GetRandomAlphanumericString(8); // 8-character random string
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                     filePath = Path.Combine(vehicleModelFolder, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        filePath = Path.Combine(vehicleModelFolder, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await vehiclemodelclass.VehicleImages1.CopyToAsync(stream);
                    }

                    vehiclemodelimage = $"/VehicleModel/{uniqueFileName}";
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_InsertVehicleModelRecord", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BrandID", vehiclemodelclass.BrandID);
                        cmd.Parameters.AddWithValue("@ModelName", vehiclemodelclass.ModelName);
                        cmd.Parameters.AddWithValue("@FuelTypeID", vehiclemodelclass.FuelTypeID);
                        cmd.Parameters.AddWithValue("@VehicleImage", (object)vehiclemodelimage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", vehiclemodelclass.IsActive);
                        cmd.Parameters.AddWithValue("@CreatedBy", vehiclemodelclass.CreatedBy);

                        object resultObj = await cmd.ExecuteScalarAsync();

                        if (resultObj != null && int.TryParse(resultObj.ToString(), out int result))
                        {
                            if (result == 1)
                            {
                                return Ok(new { status = true, message = "Vehicle model inserted successfully." });
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                                    System.IO.File.Delete(filePath);

                                return BadRequest(new { status = false, message = "Duplicate ModelName for this BrandID is not allowed." });
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                                System.IO.File.Delete(filePath);

                            return StatusCode(500, new { status = false, message = "Unexpected result from the stored procedure." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }
        }

        #endregion


        #region Update vehicle Model


        [HttpPut("UpdateVehicleModel")]


        public async Task<IActionResult> UpdateVehicleModel([FromForm] VehicleModelsClass vehiclemodelclass)
        {
            string vehiclemodelimage = null;
            string filePath = null;

            try
            {
               
                // Step 1: Fetch old image if not uploading new one
                if (string.IsNullOrEmpty(vehiclemodelimage) && (vehiclemodelclass.VehicleImages1 == null || vehiclemodelclass.VehicleImages1.Length == 0))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    using (SqlCommand cmd = new SqlCommand("sp_GetVehicleModelImageByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ModelID", vehiclemodelclass.ModelID ?? 0);
                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                vehiclemodelimage = reader["VehicleImage"]?.ToString();
                            }
                        }
                    }
                }

                // Step 2: Handle new image upload (always save with unique name)
                if (vehiclemodelclass.VehicleImages1 != null && vehiclemodelclass.VehicleImages1.Length > 0)
                {
                    var vehicleModelFolder = Path.Combine(_env.WebRootPath, "Images", "VehicleModel");
                    if (!Directory.Exists(vehicleModelFolder))
                        Directory.CreateDirectory(vehicleModelFolder);

                    var originalFileName = Path.GetFileNameWithoutExtension(vehiclemodelclass.VehicleImages1.FileName);
                    var fileExt = Path.GetExtension(vehiclemodelclass.VehicleImages1.FileName);
                    var randomString = GetRandomAlphanumericString(8); // 8-character random string
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                     filePath = Path.Combine(vehicleModelFolder, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        filePath = Path.Combine(vehicleModelFolder, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await vehiclemodelclass.VehicleImages1.CopyToAsync(stream);
                    }

                    vehiclemodelimage = $"/VehicleModel/{uniqueFileName}";
                }
                // Step 3: Update vehicle model
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateVehicleModel", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ModelID", vehiclemodelclass.ModelID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandID", vehiclemodelclass.BrandID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModelName", vehiclemodelclass.ModelName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FuelTypeID", vehiclemodelclass.FuelTypeID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@VehicleImage", vehiclemodelimage ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", vehiclemodelclass.IsActive ?? true);
                        cmd.Parameters.AddWithValue("@ModifiedBy", vehiclemodelclass.ModifiedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", vehiclemodelclass.Status ?? (object)DBNull.Value);

                        await conn.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null && Convert.ToInt32(result) == 1)
                        {
                            return Ok(new { status = true, message = "Vehicle model updated successfully." });
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                                System.IO.File.Delete(filePath);

                            return BadRequest(new { status = false, message = "Duplicate ModelName for this BrandID is not allowed." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while updating the record.", error = ex.Message });
            }
        }


        #endregion


        #region GetListVehicleModel

        [HttpGet("GetListVehicleModel")]

        public IActionResult GetListVehicleModel()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListAllVehicleModel", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
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
                        message = "Vehicle model  retrieved successfully.",
                        data = jsonList
                    });
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while fetching the records.", error = ex.Message });
            }
        }

        #endregion

        #region vehiclemodelbyid

        [HttpGet("vehiclemodelbyid")]

        public IActionResult GetVehicleModelByID(int vehiclemodelid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListAllVehicleModelByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ModelID", vehiclemodelid);
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
                    return NotFound(new { message = "Fuel Types  not found" });
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

                return Ok(jsonResult.Count == 1 ? jsonResult[0] : jsonResult);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Fuel types.", error = ex.Message });
            }
        }

        #endregion


        #region vehicelmodelid

        [HttpDelete("vehicelmodelid")]
        public IActionResult DeleteVehicleBrand(int vehicelmodelid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteVehicleModelByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ModelID", vehicelmodelid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
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
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while deleting the record.", error = ex.Message });

            }
        }
        #endregion

    }
}
