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
using System.Linq;


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
        public async Task<IActionResult> InsertBooking([FromForm] BookingInsertDTO model)
        {
            try
            {
                int bookingId = 0;

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // Generate BookingTrackID
                    string month = DateTime.Now.ToString("MM");
                    string year = DateTime.Now.ToString("yyyy");
                    string prefix = $"MYCAR{month}{year}";
                    string lastTrackId = null;
                    int nextSequence = 1;

                    using (SqlCommand cmdCheck = new SqlCommand("SELECT TOP 1 BookingTrackID FROM Bookings WHERE BookingTrackID LIKE @Prefix + '%' ORDER BY BookingID DESC", conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@Prefix", prefix);
                        var result = await cmdCheck.ExecuteScalarAsync();
                        if (result != null)
                        {
                            lastTrackId = result.ToString();
                            string lastSeqStr = lastTrackId.Substring(prefix.Length);
                            if (int.TryParse(lastSeqStr, out int lastSeq))
                                nextSequence = lastSeq + 1;
                        }
                    }

                    string newTrackId = $"{prefix}{nextSequence:D3}";
                    model.BookingTrackID = newTrackId;

                    // Insert Booking
                    using (SqlCommand cmd = new SqlCommand("SP_InsertBookings", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@BookingTrackID", model.BookingTrackID ?? "");
                        cmd.Parameters.AddWithValue("@CustID", model.CustID);
                        cmd.Parameters.AddWithValue("@TechID", model.TechID);
                        cmd.Parameters.AddWithValue("@TechFullName", model.TechFullName ?? "");
                        cmd.Parameters.AddWithValue("@TechPhoneNumber", model.TechPhoneNumber ?? "");
                        cmd.Parameters.AddWithValue("@CustFullName", model.CustFullName ?? "");
                        cmd.Parameters.AddWithValue("@CustPhoneNumber", model.CustPhoneNumber ?? "");
                        cmd.Parameters.AddWithValue("@CustEmail", model.CustEmail ?? "");
                        cmd.Parameters.AddWithValue("@StateID", model.StateID);
                        cmd.Parameters.AddWithValue("@CityID", model.CityID);
                        cmd.Parameters.AddWithValue("@Pincode", model.Pincode);
                        cmd.Parameters.AddWithValue("@FullAddress", model.FullAddress ?? "");
                        cmd.Parameters.AddWithValue("@BookingStatus", model.BookingStatus ?? "Pending");
                        cmd.Parameters.AddWithValue("@Longitude", model.Longitude ?? "");
                        cmd.Parameters.AddWithValue("@Latitude", model.Latitude ?? "");
                        cmd.Parameters.AddWithValue("@PackageIds", model.PackageIds ?? "");
                        cmd.Parameters.AddWithValue("@PackagePrice", model.PackagePrice ?? "");
                        cmd.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);
                        cmd.Parameters.AddWithValue("@CouponCode", model.CouponCode ?? "");
                        cmd.Parameters.AddWithValue("@CouponAmount", model.CouponAmount);
                        cmd.Parameters.AddWithValue("@BookingFrom", model.BookingFrom ?? "App");
                        cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod ?? "");
                        cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                        cmd.Parameters.AddWithValue("@BookingDate", model.BookingDate);
                        cmd.Parameters.AddWithValue("@TimeSlot", model.TimeSlot ?? "");
                        cmd.Parameters.AddWithValue("@IsOthers", model.IsOthers);
                        cmd.Parameters.AddWithValue("@OthersFullName", model.OthersFullName ?? "");
                        cmd.Parameters.AddWithValue("@OthersPhoneNumber", model.OthersPhoneNumber ?? "");
                        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
                        //cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
                        //cmd.Parameters.AddWithValue("@ModifiedBy", model.ModifiedBy);
                        //cmd.Parameters.AddWithValue("@ModifiedDate", model.ModifiedDate);
                        //cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
                        cmd.Parameters.AddWithValue("@VechicleID", model.VechicleID);

                        var outputId = new SqlParameter("@BookingID", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputId);

                        await cmd.ExecuteNonQueryAsync();
                        bookingId = Convert.ToInt32(outputId.Value);
                    }

                    // Save Images
                    if (model.Images != null && model.Images.Count > 0)
                    {
                        foreach (var image in model.Images)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                            var savePath = Path.Combine("wwwroot", "BookingImages", fileName);

                            using (var stream = new FileStream(savePath, FileMode.Create))
                                await image.CopyToAsync(stream);

                            using (SqlCommand cmd = new SqlCommand(@"INSERT INTO BookingImages 
                        (BookingID, ImageURL, UploadedBy, UploadedAt, CustID, TechID, ImageUploadType)
                        VALUES (@BookingID, @ImageURL, @UploadedBy, @UploadedAt, @CustID, @TechID, 'Customer')", conn))
                            {
                                cmd.Parameters.AddWithValue("@BookingID", bookingId);
                                cmd.Parameters.AddWithValue("@ImageURL", "/BookingImages/" + fileName);
                                cmd.Parameters.AddWithValue("@UploadedBy", model.CreatedBy);
                                cmd.Parameters.AddWithValue("@UploadedAt", DateTime.Now);
                                cmd.Parameters.AddWithValue("@CustID", model.CustID);
                                cmd.Parameters.AddWithValue("@TechID", model.TechID);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // Notification
                await SendBookingConfirmationSMS(model.CustID, bookingId);
                await SendBookingConfirmationEmail(model.CustID, bookingId);

                return Ok(new
                {
                    Success = true,
                    BookingID = bookingId,
                    BookingTrackID = model.BookingTrackID,
                    Message = "Booking created successfully."
                });
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
            catch (SqlException sqlEx)
            {
                // This will capture RAISERROR messages from SQL Server
                _logger.LogError(sqlEx, "SQL Error assigning technician.");
                return BadRequest(new { Success = false, Message = sqlEx.Message });
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

                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "Bookings not found" });
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

                _logger.LogError(ex, "Error retrieving   Bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });


            }
        }

        #endregion

        #region GetBookingsById

        [HttpGet("Id")]

        public IActionResult GetBookingsById(int id)
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
                _logger.LogError(ex, "Error retrieving   Bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }

        }

        #endregion

        #region TechniciansBookings By Id


        [HttpGet("GetAssignedBookings")]

        public IActionResult GetAssignedBookings([FromQuery] int Id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetTechnicianAssignedBookings", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TechID", Id);
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
                    return NotFound(new { message = "Aggsign Bookings not found" });
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
                _logger.LogError(ex, "Error retrieving  Assigned Bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }

        }

        #endregion


        #region AssignedBookingsFetchByDate

        [HttpGet("GetAssignedBookingsByDate")]
        public IActionResult GetAssignedBookingsByDate( [FromQuery] int TechID,[FromQuery] DateTime FromDate,[FromQuery] DateTime ToDate)
        {
            try
            {
                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_AssingedBokingsFetchByDate", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TechID", TechID);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@ToDate", ToDate);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                    }
                }
                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { Success = false, Message = "Assigned bookings not found." });
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

                return Ok(new { Success = true, Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Assigned Bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }
        }

        #endregion


    }
}
