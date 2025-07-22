using Braintree;
using GSF;
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
using System.Data.Entity.Infrastructure;
using System.IO;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LeaveRequestController> _logger;
        private readonly IWebHostEnvironment _env;

        public LeaveRequestController(IConfiguration configuration, ILogger<LeaveRequestController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpPost]

        public IActionResult LeaveRequest([FromBody] LeaveRequestModel leaverequest)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertLeaveRequest", conn))
                    {
                        if (leaverequest == null || leaverequest.TechID == null || leaverequest.FromDate == null || leaverequest.ToDate == null)
                            return BadRequest("Invalid input.");
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@TechID", leaverequest.TechID.Value);
                            cmd.Parameters.AddWithValue("@FromDate", (DateTime)leaverequest.FromDate);
                            cmd.Parameters.AddWithValue("@ToDate", (DateTime)leaverequest.ToDate);
                            cmd.Parameters.AddWithValue("@LeaveReason", leaverequest.LeaveReason ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@RequestedToId", leaverequest.RequestedToId.Value);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }

                        return Ok(new { message = "Leave request inserted successfully." });
                    }
                }


            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });

            }
        }

        [HttpPut]
        public IActionResult UpdateLeaveRequestStatus(int leaveId, int status)
        {

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateLeaveRequestStatus", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@LeaveId", leaveId);
                        cmd.Parameters.AddWithValue("@Status", status);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                            return NotFound(new { message = "Leave request not found." });
                    }
                }

                return Ok(new { message = "Leave request status updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the status.", error = ex.Message });
            }
        }

        #region GetList LeaveRequest

        [HttpGet]

        public IActionResult GetListLeaveRequest()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllLeaveRequest", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
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
                    return Ok(jsonResult);
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the times slot.", error = ex.Message });

            }
        }
        #endregion


        #region GetLeaveRequestById

        [HttpGet("leaveid")]

        public IActionResult GetLeaveRequestById(int leaveid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListLeaveRequestById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@LeaveId", leaveid);
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
                    return NotFound(new { message = "Leave Request  not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the skills.", error = ex.Message });

            }

        }

        #endregion


    }
}

