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
using System.Data.Entity.Infrastructure;
using System.IO;


namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DealerController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<DealerController> _logger;

        public DealerController(IConfiguration configuration, ILogger<DealerController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        #region InsertDealer

        [HttpPost]

        public IActionResult InsertDealer(DealerModel dealer)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_InsertDealers",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DistributorID", dealer.DistributorID);
                        cmd.Parameters.AddWithValue("@FullName", dealer.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", dealer.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Email", dealer.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", dealer.PasswordHash);
                        cmd.Parameters.AddWithValue("@StateID", dealer.StateID);
                        cmd.Parameters.AddWithValue("@CityID", dealer.CityID);
                        cmd.Parameters.AddWithValue("@Address", dealer.Address);
                        cmd.Parameters.AddWithValue("@GSTNumber", dealer.GSTNumber);
                        cmd.Parameters.AddWithValue("@IsActive", dealer.IsActive);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int resultCode = Convert.ToInt32(reader["ResultCode"]);
                                string message = reader["Message"].ToString();

                                if (resultCode == 1)
                                {
                                    int dealerId = Convert.ToInt32(reader["DealerID"]);
                                    return Ok(new { dealerId, message });
                                }
                                else
                                {
                                    return BadRequest(new { message });
                                }
                            }
                        }
                        conn.Close();
                    }
                }
                return StatusCode(500, new { message = "Unknown error occurred." });
            }
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });

            }
        }

        #endregion

        #region UpdateDealer
        [HttpPut]
        public IActionResult UpdateDealer(DealerModel dealer)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateDealers", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DealerID", dealer.DealerID);
                        cmd.Parameters.AddWithValue("@DistributorID", dealer.DistributorID);
                        cmd.Parameters.AddWithValue("@FullName", dealer.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", dealer.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Email", dealer.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", dealer.PasswordHash);
                        cmd.Parameters.AddWithValue("@StateID", dealer.StateID);
                        cmd.Parameters.AddWithValue("@CityID", dealer.CityID);
                        cmd.Parameters.AddWithValue("@Address", dealer.Address);
                        cmd.Parameters.AddWithValue("@GSTNumber", dealer.GSTNumber);
                        cmd.Parameters.AddWithValue("@CreatedDate", dealer.CreatedDate);
                        cmd.Parameters.AddWithValue("@IsActive", dealer.IsActive);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int resultCode = Convert.ToInt32(reader["ResultCode"]);
                                string message = reader["Message"].ToString();

                                if (resultCode == 1)
                                    return Ok(new { message });
                                else if (resultCode < 0)
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
                return StatusCode(500, new { message = "An error occurred while updating the record.", error = ex.Message });
            }
        }

        #endregion

        #region DeleteDealer
        [HttpDelete("dealerid")]

        public IActionResult DeleteDealer(int dealerid)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_DeleteDealers",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DealerID", dealerid);
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

        #region GetAllDealer

        [HttpGet]
        public IActionResult GetAllDealer([FromQuery] int? DistributorID = null)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListDealers", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // If DistributorID is provided, pass the value; otherwise pass DBNull
                        cmd.Parameters.AddWithValue("@DistributorID", (object?)DistributorID ?? DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                    }
                }

                // Convert DataTable to List<Dictionary<string, object>>
                var data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    data.Add(dict);
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving the dealer.",
                    error = ex.Message
                });
            }
        }

        #endregion
        #region GetDealerById

        [HttpGet("dealerid")]

        public IActionResult GetDealerById(int dealerid)
        {
            try
            {
                DataTable dt = new DataTable();
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_GetDealersByID",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DealerID", dealerid);
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
                    return NotFound(new { message = "Skills not found" });
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
                return Ok(Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the dealer.", error = ex.Message });

            }

        }

        #endregion
    }
}
