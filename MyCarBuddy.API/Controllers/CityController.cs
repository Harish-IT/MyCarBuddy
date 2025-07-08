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
using System.IO;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CityController> _logger;

        public CityController(IConfiguration configuration, ILogger<CityController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult InsertCity(CityModel city)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCities", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateID", city.StateID);
                        cmd.Parameters.AddWithValue("@CityName", city.CityName);
                        cmd.Parameters.AddWithValue("@IsActive", city.IsActive);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var stateId = Convert.ToInt32(reader["CityID"]);
                                var status = reader["Status"].ToString();

                                if (status == "EMPTY_NAME")
                                {
                                    return BadRequest(new { message = "City name cannot be empty or null." });
                                }

                                else if (status == "DUPLICATE")
                                {
                                    return BadRequest(new { message = "A city with the same name already exists." });
                                }
                                else if (status == "SUCCESS")
                                {
                                    return Ok(new { message = "Record is Inserted", StateID = stateId });
                                }
                            }
                        }
                    }
                }
                return Ok(new { message = "Record is Inserted" });

            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });
            }
        }
        [HttpPut]
        public IActionResult UpdateCity(CityModel city)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateCities", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityID", city.CityID);
                        cmd.Parameters.AddWithValue("@StateID", city.StateID);
                        cmd.Parameters.AddWithValue("@CityName", city.CityName);
                        cmd.Parameters.AddWithValue("@IsActive", city.IsActive);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        conn.Close();
                        if (rows > 0)
                        {
                            return Ok(new { message = "Record is updated" });
                        }
                        else
                        {
                            return NotFound(new { message = "Record is Not updated" });
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
        public IActionResult DeleteCity(CityModel city)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteCities", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityID", city.CityID);
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
        public IActionResult GetAllCities()
        {
            try
            {
                List<CityModel> cities = new List<CityModel>();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListCities", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cities.Add(new CityModel
                                {
                                    CityID = Convert.ToInt32(reader["CityID"]),
                                    StateID = Convert.ToInt32(reader["StateID"]),
                                    CityName = reader["CityName"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])

                                });
                            }
                           
                        }
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
                return Ok(cities);


            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the states.", error = ex.Message });
            }
        }
    }
}
