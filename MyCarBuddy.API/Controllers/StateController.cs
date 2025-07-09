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
   // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StateController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StateController> _logger;

        public StateController(IConfiguration configuration, ILogger<StateController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult InsertState(StateModel state)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertStates", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateName", state.StateName);
                        cmd.Parameters.AddWithValue("@IsActive", state.IsActive);
                        conn.Open();
                        // cmd.ExecuteNonQuery();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var stateId = Convert.ToInt32(reader["StateID"]);
                                var status = reader["Status"].ToString();

                                if (status == "EMPTY_NAME")
                                {
                                    return BadRequest(new { message = "State name cannot be empty or null." });
                                }

                                else if (status == "DUPLICATE")
                                {
                                    return BadRequest(new { message = "A state with the same name already exists." });
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
        public IActionResult UpdateState(StateModel state)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateStates", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateID", state.StateId);
                        cmd.Parameters.AddWithValue("@StateName", state.StateName);
                        cmd.Parameters.AddWithValue("@IsActive", state.IsActive);
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
        public IActionResult DeleteState(StateModel state)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteStates", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateID", state.StateId);
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
        public IActionResult GetAllStates()
        {
            try
            {
                List<StateModel> states = new List<StateModel>();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListStates", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                states.Add(new StateModel
                                {
                                    StateId = Convert.ToInt32(reader["StateId"]),
                                    StateName = reader["StateName"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])
                                });
                            }
                        }
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
                return Ok(states);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the states.", error = ex.Message });
            }
        }

        [HttpGet("{stateid}")]
        public IActionResult GetStatesById(int stateid)
        {
            try
            {
                StateModel state = null;
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetStatesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateID", stateid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                state = new StateModel
                                {
                                    StateId = Convert.ToInt32(reader["StateId"]),
                                    StateName = reader["StateName"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])
                                };
                            }
                        }
                        conn.Close();
                    }
                }
                if (state == null)
                {
                    return NotFound(new { message = "state not found" });
                }
                return Ok(state);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the state.", error = ex.Message });
            }
        }
    }
}