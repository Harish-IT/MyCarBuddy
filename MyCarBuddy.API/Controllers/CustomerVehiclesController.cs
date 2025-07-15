using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    public class CustomerVehiclesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomerVehiclesController> _logger;

        public CustomerVehiclesController(IConfiguration configuration, ILogger<CustomerVehiclesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        #region InsertCustomerVehicle

        [HttpPost("InsertCustomerVehicle")]

        public async  Task<IActionResult> InsertCustomerVehicle(CustomerVehiclesModel customervehiclemodel)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_InsertCustomerVehicle",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CustID", customervehiclemodel.CustID);
                        cmd.Parameters.AddWithValue("@VehicleNumber", customervehiclemodel.VehicleNumber);
                        cmd.Parameters.AddWithValue("@YearOfPurchase", customervehiclemodel.YearOfPurchase);
                        cmd.Parameters.AddWithValue("@EngineType", customervehiclemodel.EngineType);
                        cmd.Parameters.AddWithValue("@KilometerDriven", customervehiclemodel.KilometersDriven);
                        cmd.Parameters.AddWithValue("@TransmissionType", customervehiclemodel.TransmissionType);
                        cmd.Parameters.AddWithValue("@CreatedBy", customervehiclemodel.CreatedBy);
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();

                    }

                    return Ok(new { status=true, message = "Vehicle inserted successfully." });
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }

        }

        #endregion

        #region GetCustomerVehicles

        [HttpGet("GetCustomerVehicles")]

        public IActionResult GetCustomerVehicles()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllCustomerVehicles", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
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
                        message = "Customer vehicles  retrieved successfully.",
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

        #region GetCustomerVehiclebyID

        [HttpGet("CustVehicleId")]

        public IActionResult GetCustomerVehiclebyID(int CustVehicleId)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllCustomerVehiclesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@VehicleID", CustVehicleId);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load all columns and rows
                        }
                        conn.Close();
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Customer vehicles   not found" });
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

        #region DeleteVehicleBrand

        [HttpDelete("CustomerVehicleID")]
        public IActionResult DeleteVehicleBrand(int custvehicleid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteCustomerVehicleById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@VehicleID", custvehicleid);

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

                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while deleting the record.",
                    error = ex.Message
                });
            }
        }

        #endregion

    }
}
