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
    public class TimeSlotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TimeSlotController> _logger;
        private readonly IWebHostEnvironment _env;

        public TimeSlotController(IConfiguration configuration, ILogger<TimeSlotController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }


        [HttpPost]

        public IActionResult TimeSlot(TimeSlotModel timeslot)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertTimeSlot", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@StartTime", timeslot.StartTime.ToTimeSpan());
                        cmd.Parameters.AddWithValue("@EndTime", timeslot.EndTime.ToTimeSpan());
                        cmd.Parameters.AddWithValue("@Status", timeslot.Status);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Time slot inserted successfully.." });
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
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });

            }
        }
        [HttpPut]

        public IActionResult UpdateTimeSlot(TimeSlotModel timeupdate)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateTimeSlots", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TsID", timeupdate.TsID);
                        cmd.Parameters.AddWithValue("@StartTime", timeupdate.StartTime.ToTimeSpan());
                        cmd.Parameters.AddWithValue("@EndTime", timeupdate.EndTime.ToTimeSpan());
                        cmd.Parameters.AddWithValue("@Status", timeupdate.Status);



                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Time slot updated successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Time slot not updated." });
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

        #region GetListTimeSlot

        [HttpGet]

        public IActionResult GetListTimeSlot()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllTimeSlot", conn))
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


        #region GetSkillById

        [HttpGet("timeslotid")]

        public IActionResult GetTimeSlotById(int timeslotid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllTimeSlotByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TsID", timeslotid);
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
                    return NotFound(new { message = "Time Slot not found" });
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

        #region DeleteSkill

        [HttpDelete("timeslotId")]

        public IActionResult DeleteTimeSlot(int timeslotId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteTimeSlotById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TsID", timeslotId);
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
