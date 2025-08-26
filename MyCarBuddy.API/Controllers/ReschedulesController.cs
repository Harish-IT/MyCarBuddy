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
using System.Linq;
using System.Threading.Tasks;
    

namespace MyCarBuddy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReschedulesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReschedulesController> _logger;
        private readonly IWebHostEnvironment _env;

        public ReschedulesController(IConfiguration configuration, ILogger<ReschedulesController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpPost]
        public IActionResult InsertReschedule(ReschedulesModel reschedule)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertReschedules", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BookingID", reschedule.BookingID);
                        cmd.Parameters.AddWithValue("@OldSchedule", reschedule.OldSchedule);
                        cmd.Parameters.AddWithValue("@NewSchedule", reschedule.NewSchedule);

                        cmd.Parameters.AddWithValue("@Reason", reschedule.Reason);

                        cmd.Parameters.AddWithValue("@RequestedBy", reschedule.RequestedBy);
                        cmd.Parameters.AddWithValue("@Status", reschedule.Status);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Reschedule inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Reschedule not inserted." });
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

    }
}
