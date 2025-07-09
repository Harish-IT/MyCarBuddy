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
    public class DistributorsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DistributorsController> _logger;

        public DistributorsController(IConfiguration configuration, ILogger<DistributorsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult InsertDistributor(DistributorsModel distributor)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_InsertDistributors",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@FullName", distributor.FullName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PhoneNumber", distributor.PhoneNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", distributor.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PasswordHash", distributor.PasswordHash ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@StateID", distributor.StateID);
                        cmd.Parameters.AddWithValue("@CityID", distributor.CityID);
                        cmd.Parameters.AddWithValue("@Address", distributor.Address ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GSTNumber", distributor.GSTNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedDate", distributor.CreatedDate);
                        cmd.Parameters.AddWithValue("@IsActive", distributor.IsActive);
                        conn.Open();
                        using(SqlDataReader reader=cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                int errorCode = reader["ErrorCode"] != DBNull.Value ? Convert.ToInt32(reader["ErrorCode"]) : 0;
                                string errorMessage = reader["ErrorMessage"] != DBNull.Value ? reader["ErrorMessage"].ToString() : "";

                                if (errorCode == 0)
                                {
                                    int distributorId = reader["DistributorID"] != DBNull.Value ? Convert.ToInt32(reader["DistributorID"]) : 0;
                                    return Ok(new { DistributorID = distributorId, message = "Distributor inserted successfully." });
                                }
                                else
                                {
                                    return BadRequest(new { errorCode, message = errorMessage });
                                }
                            }
                            else
                            {
                                return StatusCode(500, new { message = "No response from database" });

                            }
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });

            }
        }

        [HttpPut]
        public IActionResult UpdateDistributor(DistributorsModel distributor)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_UpdateDistributors",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DistributorID", distributor.DistributorID);
                        cmd.Parameters.AddWithValue("@FullName", distributor.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", distributor.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Email", distributor.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", distributor.PasswordHash);
                        cmd.Parameters.AddWithValue("@StateID", distributor.StateID);
                        cmd.Parameters.AddWithValue("@CityID", distributor.CityID);
                        cmd.Parameters.AddWithValue("@Address", distributor.Address);
                        cmd.Parameters.AddWithValue("@GSTNumber", distributor.GSTNumber);
                        cmd.Parameters.AddWithValue("@CreatedDate", distributor.CreatedDate);
                        cmd.Parameters.AddWithValue("@IsActive", distributor.IsActive);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int errorCode = reader["ErrorCode"] != DBNull.Value ? Convert.ToInt32(reader["ErrorCode"]) : 0;
                                string errorMessage = reader["ErrorMessage"] != DBNull.Value ? reader["ErrorMessage"].ToString() : "";

                                if (errorCode == 0)
                                {
                                    return Ok(new { message = errorMessage });
                                }
                                else
                                {
                                    return BadRequest(new { errorCode, message = errorMessage });
                                }
                            }
                            else
                            {
                                return StatusCode(500, new { message = "No response from database." });
                            }
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
        [HttpDelete]
        public IActionResult DeleteDistributor(DistributorsModel distributor)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_DeleteDistributors",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DistributorID", distributor.DistributorID);
                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (row > 0)
                        {
                            return Ok(new { message = "Record is Deleted" });
                        }
                        else
                        {
                            return NotFound(new { message = "Record is not Deleted.." });
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
        [HttpGet]
        public IActionResult GetAllDistributors()
        {
            try
            {
                List<DistributorsModel>distributors=new List<DistributorsModel>();
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_ListDistributors",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                distributors.Add(new DistributorsModel
                                {
                                    DistributorID = Convert.ToInt32(reader["DistributorID"]),
                                    FullName = reader["FullName"].ToString(),
                                    PhoneNumber = reader["PhoneNumber"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    PasswordHash = reader["PasswordHash"].ToString(),
                                    StateID = Convert.ToInt32(reader["StateID"]),
                                    CityID = Convert.ToInt32(reader["CityID"]),
                                    Address = reader["Address"].ToString(),
                                    GSTNumber = reader["GSTNumber"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])


                                });
                            }
                        }
                        cmd.ExecuteNonQuery();
                        conn.Close();

                    }
                }
                return Ok(distributors);

            }
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the states.", error = ex.Message });


            }
        }
        [HttpGet("distrbutorid")]
        public IActionResult GetDistributorsById(int distributorid)
        {
            try {
                DistributorsModel distributor = null;
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetDistributorsByID", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                distributor = new DistributorsModel
                                {
                                    DistributorID = Convert.ToInt32(reader["DistributorID"]),
                                    FullName = reader["FullName"].ToString(),
                                    PhoneNumber = reader["PhoneNumber"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    PasswordHash = reader["PasswordHash"].ToString(),
                                    StateID = Convert.ToInt32(reader["StateID"]),
                                    CityID = Convert.ToInt32(reader["CityID"]),
                                    Address = reader["Address"].ToString(),
                                    GSTNumber = reader["GSTNumber"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])


                                };
                            }
                        }
                        conn.Close();

                    }

                }
                if (distributor == null)
                {
                    return NotFound(new { message = "city not found" });
                }
                return Ok(distributor);
            }
            catch(Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the state.", error = ex.Message });


            }
        }




    }
}
