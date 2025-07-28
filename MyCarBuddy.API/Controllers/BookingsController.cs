using GSF.ErrorManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using MyCarBuddy.API.Utilities;
using GSF;
using Braintree;
using Microsoft.AspNetCore.Authorization;


namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingsController> _logger;
        private readonly IWebHostEnvironment _env;

        public BookingsController(IConfiguration configuration, ILogger<BookingsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }


        [HttpPost("insert-booking")]
        public async Task<IActionResult> InsertBooking([FromForm] Bookings model)
        {
            try
            {
                int bookingId = 0;

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_InsertBookings", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CustID", model.CustID);
                        cmd.Parameters.AddWithValue("@VehicleID", model.VehicleID);
                        cmd.Parameters.AddWithValue("@PricingID", model.PricingID);
                        cmd.Parameters.AddWithValue("@AddressID", model.AddressID);
                        cmd.Parameters.AddWithValue("@ScheduledDate", model.ScheduledDate);
                        cmd.Parameters.AddWithValue("@BookingPrice", model.BookingPrice);
                        cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                        cmd.Parameters.AddWithValue("@OTPForCompletion", model.OTPForCompletion);
                        cmd.Parameters.AddWithValue("@CouponID", model.CouponID);

                        var outputId = new SqlParameter("@BookingID", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputId);

                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        bookingId = Convert.ToInt32(outputId.Value);
                    }

                    // Insert Images (Optional)
                    if (model.Images != null && model.Images.Count > 0)
                    {
                        foreach (var image in model.Images)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                            var savePath = Path.Combine("wwwroot", "BookingImages", fileName);
                            using (var stream = new FileStream(savePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            using (SqlCommand cmd = new SqlCommand("INSERT INTO BookingImages (BookingID, ImageURL, UploadedBy, UploadedAt, CustID, TechID, ImageUploadType) VALUES (@BookingID, @ImageURL, @UploadedBy, @UploadedAt, @CustID, NULL, 'Customer')", conn))
                            {
                                cmd.Parameters.AddWithValue("@BookingID", bookingId);
                                cmd.Parameters.AddWithValue("@ImageURL", "/BookingImages/" + fileName);
                                cmd.Parameters.AddWithValue("@UploadedBy", model.CustID);
                                cmd.Parameters.AddWithValue("@UploadedAt", DateTime.Now);
                                cmd.Parameters.AddWithValue("@CustID", model.CustID);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // ✅ Send SMS & Email here (pseudo method)
                await SendBookingConfirmationSMS(model.CustID, bookingId);
                await SendBookingConfirmationEmail(model.CustID, bookingId);

                return Ok(new { Success = true, BookingID = bookingId, Message = "Booking created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        private Task SendBookingConfirmationSMS(int custId, int bookingId)
        {
            // Get customer phone & send SMS
            return Task.CompletedTask;
        }

        private Task SendBookingConfirmationEmail(int custId, int bookingId)
        {
            // Get customer email & send message
            return Task.CompletedTask;
        }

        [HttpPut("assign-technician")]
        public async Task<IActionResult> AssignTechnician([FromBody] AssignTechnicianModel model)
        {
            if (model.BookingID == null || model.TechID == null)
            {
                return BadRequest(new { Success = false, Message = "BookingID and TechID are required." });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_AssignTechnician", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BookingID", model.BookingID);
                        cmd.Parameters.AddWithValue("@TechID", model.TechID);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { Success = true, Message = "Technician assigned successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning technician.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }
        }


        #region GetListAllBookings

        [HttpGet]

        public IActionResult GetListAllBookings()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllBookings", conn))
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

                _logger.LogError(ex, "Error retrieving   Bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });


              //  _logger.LogError(ex, HttpContext, _configuration, _logger);
               // return StatusCode(500, new { message = "An error occurred while retrieving the times slot.", error = ex.Message });

            }
        }

        #endregion

        #region GetBookingsById

        [HttpGet("Id")]

        public IActionResult GetBookingsById(int id )
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllBookingsById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BookingID", id);
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
                    return NotFound(new { message = "Bookings not found" });
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
                _logger.LogError(ex, "Error retrieving   Bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }

        }

        #endregion

    }
}
