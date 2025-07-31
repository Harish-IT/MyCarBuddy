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
    public class CancellationsController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<CancellationsController> _logger;
        private readonly IWebHostEnvironment _env;



        public CancellationsController(IConfiguration configuration, ILogger<CancellationsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #region Insert Cancellations

        [HttpPost]
        public IActionResult InsertCancellation(CancellationsModel cancellation)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCancellations", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        cmd.Parameters.AddWithValue("@BookingID", cancellation.BookingID);
                        cmd.Parameters.AddWithValue("@CancelledBy", cancellation.CancelledBy);
                        cmd.Parameters.AddWithValue("@Reason", cancellation.Reason);
                        cmd.Parameters.AddWithValue("@RefundStaus", cancellation.RefundStatus);

                        conn.Open();
                        object result = cmd.ExecuteScalar();

                        if (result != null && int.TryParse(result.ToString(), out int cancellationId))
                        {
                            return Ok(new
                            {
                                status = true,
                                message = "Cancellation inserted successfully.",
                                CancellationID = cancellationId
                            });
                        }
                        else
                        {
                            return BadRequest(new
                            {
                                status = false,
                                message = "Cancellation not inserted or ID not returned."
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);

                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while inserting the cancellation.",
                    error = ex.Message
                });
            }
        }
        #endregion


        #region GetListCancellations

        [HttpGet]

        public IActionResult GetListCancellations()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllCancellations", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }

                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "Cancellations not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Cancellations.", error = ex.Message });

            }
        }

        #endregion

        #region Get Cancellation Details By CancelID and BookingID

        [HttpGet("cancellation/details")]
        public IActionResult GetCancellationDetails(int cancelId, int bookingId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_GetCancellationDetailsByID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CancelID", cancelId);
                    cmd.Parameters.AddWithValue("@BookingID", bookingId);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var result = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                                // Convert empty strings to null
                                if (value is string str && string.IsNullOrWhiteSpace(str))
                                {
                                    value = null;
                                }

                                result.Add(reader.GetName(i), value);
                            }

                            return Ok(new { status = true, data = result });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Cancellation details not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while retrieving the cancellation details.",
                    error = ex.Message
                });
            }
        }
        #endregion


    }
}
