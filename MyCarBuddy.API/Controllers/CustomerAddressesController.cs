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
using System.Threading.Tasks;
namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerAddressesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomerAddressesController> _logger;
        private readonly IWebHostEnvironment _env;


        public CustomerAddressesController(IConfiguration configuration, ILogger<CustomerAddressesController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #region Insert CustomerAddress
        [HttpPost]
        public IActionResult CustomerAddress(CustomerAddressesModel customeraddress)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCustomerAddress", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CustID", customeraddress.CustID);
                        cmd.Parameters.AddWithValue("@AddressLine1", customeraddress.AddressLine1);
                        cmd.Parameters.AddWithValue("@AddressLine2", customeraddress.AddressLine2);

                      
                        cmd.Parameters.AddWithValue("@StateID", customeraddress.StateID);
                        cmd.Parameters.AddWithValue("@CityID", customeraddress.CityID);
                        cmd.Parameters.AddWithValue("@Pincode", customeraddress.Pincode);
                        cmd.Parameters.Add("@Latitude", SqlDbType.Decimal).Value = customeraddress.Latitude;
                        cmd.Parameters.Add("@Longitude", SqlDbType.Decimal).Value = customeraddress.Longitude;
                        cmd.Parameters.AddWithValue("@IsDefault", customeraddress.IsDefault);
                        cmd.Parameters.AddWithValue("@CreatedBy", customeraddress.CreatedBy);
                        cmd.Parameters.AddWithValue("@IsActive", customeraddress.IsActive);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Customer Addreess inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Customer Addreess not inserted." });
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

        #region Update CustomerAddress


        [HttpPut]
        public IActionResult UpdateAddress(CustomerAddressesModel customeraddress)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateCustomerAddress", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@AddressID", customeraddress.AddressID);
                        cmd.Parameters.AddWithValue("@CustID", customeraddress.CustID);
                        cmd.Parameters.AddWithValue("@AddressLine1", customeraddress.AddressLine1);
                        cmd.Parameters.AddWithValue("@AddressLine2", customeraddress.AddressLine2);
                        cmd.Parameters.AddWithValue("@StateID", customeraddress.StateID);
                        cmd.Parameters.AddWithValue("@CityID", customeraddress.CityID);
                        cmd.Parameters.AddWithValue("@Pincode", customeraddress.Pincode);
                        cmd.Parameters.AddWithValue("@Latitude", customeraddress.Latitude);
                        cmd.Parameters.AddWithValue("@Longitude", customeraddress.Longitude);
                        cmd.Parameters.AddWithValue("@IsDefault", customeraddress.IsDefault);
                        cmd.Parameters.AddWithValue("@ModifiedBy", customeraddress.ModifiedBy);
                        cmd.Parameters.AddWithValue("@IsActive", customeraddress.IsActive);

                        SqlParameter resultParam = new SqlParameter("@Result", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(resultParam);

                        conn.Open();
                        cmd.ExecuteNonQuery();

                        int result = (int)resultParam.Value;
                        if (result == 1)
                        {
                            return Ok(new { status = true, message = "Customer address updated successfully .." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Customer address not found." });
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
                    message = "An error occurred while updating the record.",
                    error = ex.Message
                });
            }


        }

        #endregion

        #region GetCustomerAddressList

        [HttpGet]

        public IActionResult GetListCustomerAddress()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetCustomerAddressList", conn))
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Includes.", error = ex.Message });

            }
        }

        #endregion


        #region AddressId


        [HttpGet("addressid")]

        public IActionResult GetSubCategory1ById(int addressid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetCustomerAddressListByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AddressID", addressid);
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
                    return NotFound(new { message = "Includes not found.." });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Includes.", error = ex.Message });

            }

        }

        #endregion


        #region includeid

        [HttpDelete("addressid")]

        public IActionResult DeleteInclude(int addressid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteCustomerAddress", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AddressID", addressid);
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
