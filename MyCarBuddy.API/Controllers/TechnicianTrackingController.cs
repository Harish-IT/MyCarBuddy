using Azure.Core;
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
    public class TechnicianTrackingController : ControllerBase
    {
        #region configuration
        private readonly IConfiguration _configuration;
        private readonly ILogger<TechnicianTrackingController> _logger;
        private readonly IWebHostEnvironment _env;


        public TechnicianTrackingController(IConfiguration configuration, ILogger<TechnicianTrackingController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion

        #region UpdateTechnicianTracking

        [HttpPost("UpdateTechnicianTracking")]
        public IActionResult UpdateTechnicianTracking([FromBody] TechnicianTrackingModel request)
        {

            if (request == null || request.BookingID <= 0 || string.IsNullOrEmpty(request.ActionType))
            {
                return BadRequest(new { Success = false, Message = "Invalid request" });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateTechnicianTracking", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BookingID", request.BookingID);
                        cmd.Parameters.AddWithValue("@ActionType", request.ActionType);
                        cmd.Parameters.AddWithValue("@BookingOTP", (object?)request.BookingOTP ?? DBNull.Value);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Success = true, Message = $"{request.ActionType} updated successfully." });

            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }

        }


        #endregion

    }
}
