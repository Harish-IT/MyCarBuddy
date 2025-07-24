using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<TechniciansDetailsController> _logger;
        private readonly IWebHostEnvironment _env;

        public BookingsController(IConfiguration configuration, ILogger<TechniciansDetailsController> logger, IWebHostEnvironment env)
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
                                cmd.Parameters.AddWithValue("@ImageURL", "/Uploads/" + fileName);
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


    }
}
