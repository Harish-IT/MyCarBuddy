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
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        #region IConfiguration

        private readonly IConfiguration _configuration;
        private readonly ILogger<FeedbackController> _logger;
        private readonly IWebHostEnvironment _env;

        public FeedbackController(IConfiguration configuration, ILogger<FeedbackController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion

        #region InsertFeedback

        [HttpPost]

        public IActionResult InsertFeedback([FromBody] FeedbackModel feedback)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertFeedback", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@BookingID", feedback.BookingID);
                        cmd.Parameters.AddWithValue("@CustID", feedback.CustID);
                        cmd.Parameters.AddWithValue("@TechID", feedback.TechID);
                        cmd.Parameters.AddWithValue("@TechReview", feedback.TechReview ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ServiceReview", feedback.ServiceReview ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TechRating", feedback.TechRating??(object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ServiceRating", feedback.ServiceRating ?? (object)DBNull.Value);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Feedback inserted successfully." });
            }
            catch (Exception ex)
            {
                // Your error logging here
                return StatusCode(500, new { message = "An error occurred while inserting the feedback.", error = ex.Message });
            }
        }

        #endregion

        #region GetListFeedback

        [HttpGet]
        public IActionResult GetListFeedback()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListFeedback", conn))
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
                            var value = row[col];
                            dict[col.ColumnName] = value == DBNull.Value ? null : value;
                        }
                        Data.Add(dict);
                    }
                    return Ok(Data);
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the feedback.", error = ex.Message });

            }
        }

        #endregion

        #region GetFeedbackListByCustIdBookingId


        [HttpGet("feedback")]
        public IActionResult GetFeedback(
            int? custId = null,
            int? techId = null,
            int? bookingId = null)
        {
            try
            {
                string storedProcedure;
                SqlParameter[] parameters;

                if (custId.HasValue)
                {
                    storedProcedure = "sp_GetListFeedbackby_CustId_BookingId";
                    parameters = new SqlParameter[]
                    {
                    new SqlParameter("@CustID", (object)custId ?? DBNull.Value),
                    new SqlParameter("@BookingID", (object)bookingId ?? DBNull.Value)
                    };
                }
                else if (techId.HasValue)
                {
                    storedProcedure = "sp_GetListFeedbackby_Tech_Booking";
                    parameters = new SqlParameter[]
                    {
                    new SqlParameter("@TechID", (object)techId ?? DBNull.Value),
                    new SqlParameter("@BookingID", (object)bookingId ?? DBNull.Value)
                    };
                }
                else
                {
                    return BadRequest(new { message = "Please provide either custId or techId." });
                }

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand(storedProcedure, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Feedback not found" });
                }

                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        var value = row[col];
                        dict[col.ColumnName] = value == DBNull.Value ? null : value;
                    }
                    Data.Add(dict);
                }

                return Ok(Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the feedback.", error = ex.Message });
            }
        }

        #endregion


        #region GetServiceRating

        [HttpGet("Review")]
        public IActionResult GetServiceRating(int techid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("GetServiceRatingByTech", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TechID", techid);
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
                            var value = row[col];
                            dict[col.ColumnName] = value == DBNull.Value ? null : value;
                        }
                        Data.Add(dict);
                    }
                    return Ok(Data);
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the service rating.", error = ex.Message });

            }
        }

        #endregion


    }
}
