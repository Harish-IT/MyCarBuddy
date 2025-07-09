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

        [HttpDelete("{stateid}")]
        public IActionResult DeleteState(int stateid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteStates", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateID", stateid);
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
                                    return BadRequest(new { message }); // Already inactive
                                else
                                    return NotFound(new { message }); // Not found
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

        [HttpGet]
        public IActionResult GetAllStates()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListStates", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load all columns and rows
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
                return Ok(jsonResult);
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
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetStatesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StateID", stateid);
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
                    return NotFound(new { message = "state not found" });
                }

                // Convert DataTable to JSON-friendly structure (single row)
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = dt.Rows[0][col];
                }

                return Ok(dict);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the state.", error = ex.Message });
            }
        }
    }
}