using Braintree;
using GSF;
using GSF.ErrorManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using Razorpay.Api;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


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

                    // 1. Generate BookingTrackID
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

                    // 2. Insert Booking
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
                        cmd.Parameters.AddWithValue("@VechicleID", model.VechicleID);

                        var outputId = new SqlParameter("@BookingID", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputId);

                        await cmd.ExecuteNonQueryAsync();
                        bookingId = Convert.ToInt32(outputId.Value);
                    }

                    // 3. Save Booking Images
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

                // 4. Razorpay Order Creation
                string key = _configuration["Razorpay:Key"];
                string secret = _configuration["Razorpay:Secret"];
                RazorpayClient client = new RazorpayClient(key, secret);

                Dictionary<string, object> options = new Dictionary<string, object>
        {
            { "amount", model.TotalPrice * 100 },  // Convert to paise
            { "currency", "INR" },
            { "receipt", bookingId.ToString() },
            { "payment_capture", 1 }
        };

                Razorpay.Api.Order order = client.Order.Create(options);

                // 5. Notification (optional)
                await SendBookingConfirmationSMS(model.CustID, bookingId);
                await SendBookingConfirmationEmail(model.CustID, bookingId);

                // 6. Final Response
                return Ok(new
                {
                    Success = true,
                    BookingID = bookingId,
                    BookingTrackID = model.BookingTrackID,
                    Razorpay = new
                    {
                        OrderID = order["id"].ToString(),
                        Key = key
                    },
                    Message = "Booking created and order generated successfully."
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



        #region GetBookingsByCustomerIdId

        [HttpGet("{custId}")]
        public IActionResult GetBookingsByCustomer(int custId)
        {
            try
            {
                string jsonResult = null;

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_GetListAllBookingsById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CustID", custId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Combine multiple rows into one JSON array
                        var sb = new System.Text.StringBuilder();
                        //sb.Append("["); // start array

                        bool first = true;
                        while (reader.Read() && !reader.IsDBNull(0))
                        {
                            if (!first) sb.Append(",");
                            sb.Append(reader.GetString(0));
                            first = false;
                        }

                        // sb.Append("]"); // end array
                        jsonResult = sb.ToString();
                    }
                }

                if (string.IsNullOrWhiteSpace(jsonResult) || jsonResult == "[]")
                    return NotFound(new { message = "No bookings found for this Id" });

                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings.");
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
                string jsonResult = null;

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_GetTechnicianAssignedBookings_new", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TechID", Id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            jsonResult = reader.GetString(0);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(jsonResult))
                    return NotFound(new { message = "No bookings found for this technician" });

                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }
        }

        #endregion



        #region GetListBookingsById By Id


        [HttpGet("BookingId")]

        public IActionResult GetListBookingsById([FromQuery] int Id)
        {


            try
            {
                string jsonResult = null;

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("GetListBookingByBookingId", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BookingID", Id);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            jsonResult = reader.GetString(0);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(jsonResult))
                    return NotFound(new { message = "No bookings found for this Id" });

                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }



        }

        #endregion



        #region AssignedBookingsFetchByDate

        [HttpGet("GetAssignedBookingsByDate")]
        public IActionResult GetAssignedBookingsByDate([FromQuery] int TechID, [FromQuery] DateTime FromDate, [FromQuery] DateTime ToDate)
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



        public static class Utils
        {
            public static string GetHash(string message, string secret)
            {
                var encoding = new System.Text.UTF8Encoding();
                byte[] keyByte = encoding.GetBytes(secret);
                byte[] messageBytes = encoding.GetBytes(message);
                using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
                {
                    byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                    return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
                }
            }
        }


        //[HttpPost("create-order")]
        //public IActionResult CreateOrder([FromBody] RazorOrderRequest model)
        //{
        //    string key = _configuration["Razorpay:Key"];
        //    string secret = _configuration["Razorpay:Secret"];

        //    RazorpayClient client = new RazorpayClient(key, secret);

        //    Dictionary<string, object> options = new Dictionary<string, object>
        //{
        //    { "amount", model.Amount * 100 },
        //    { "currency", "INR" },
        //    { "receipt", model.BookingId.ToString() },
        //    { "payment_capture", 1 }
        //};

        //    Razorpay.Api.Order order = client.Order.Create(options);

        //    return Ok(new
        //    {
        //        success = true,
        //        orderId = order["id"].ToString(),
        //        key = key
        //    });
        //}

        [HttpPost("confirm-Payment")]
        public IActionResult ConfirmPayment([FromBody] RazorpayPaymentRequest paymentRequest)
        {
            string secret = _configuration["Razorpay:Secret"];

            string expectedSignature = Utils.GetHash($"{paymentRequest.RazorpayOrderId}|{paymentRequest.RazorpayPaymentId}", secret);

            if (expectedSignature != paymentRequest.RazorpaySignature)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Signature verification failed"
                });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SP_InsertPaymentDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@BookingID", paymentRequest.BookingID);
                        cmd.Parameters.AddWithValue("@AmountPaid", paymentRequest.AmountPaid);
                        cmd.Parameters.AddWithValue("@PaymentMode", "Razorpay");
                        cmd.Parameters.AddWithValue("@TransactionID", paymentRequest.RazorpayPaymentId);
                        cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@IsRefunded", 0);

                        int result = cmd.ExecuteNonQuery();

                        if (result <= 0)
                        {
                            return StatusCode(500, new
                            {
                                success = false,
                                message = "Failed to save payment details"
                            });
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment confirmed and saved successfully",
                    data = new
                    {
                        bookingId = paymentRequest.BookingID,
                        transactionId = paymentRequest.RazorpayPaymentId,
                        amountPaid = paymentRequest.AmountPaid,
                        paymentMode = "Razorpay",
                        paymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Server error",
                    error = ex.Message
                });
            }
        }


        #region Get GetTechBookingCounts

        [HttpGet("GetTechBookingCounts")]

        public IActionResult GetTechBookingCounts([FromQuery] int techId)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetTodayAndScheduledBookingsCount", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TechID", techId);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "No bookings found for the given technician" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Categories.", error = ex.Message });

            }

        }




        #endregion


        //[HttpPost("process-payment")]
        //public IActionResult ProcessPayment([FromBody] RazorCombinedPaymentRequest model)
        //{
        //    string key = _configuration["Razorpay:Key"];
        //    string secret = _configuration["Razorpay:Secret"];

        //    try
        //    {
        //        // Step 1: Create Razorpay Order
        //        RazorpayClient client = new RazorpayClient(key, secret);

        //        Dictionary<string, object> options = new Dictionary<string, object>
        //{
        //    { "amount", model.Amount * 100 }, // Convert to paise
        //    { "currency", "INR" },
        //    { "receipt", model.BookingID.ToString() },
        //    { "payment_capture", 1 }
        //};

        //        Razorpay.Api.Order order = client.Order.Create(options);

        //        // Step 2: If frontend has payment response, verify signature & save
        //        if (!string.IsNullOrEmpty(model.RazorpayPaymentId) && !string.IsNullOrEmpty(model.RazorpaySignature))
        //        {
        //            string expectedSignature = Utils.GetHash($"{order["id"]}|{model.RazorpayPaymentId}", secret);
        //            if (expectedSignature != model.RazorpaySignature)
        //            {
        //                return BadRequest(new
        //                {
        //                    success = false,
        //                    message = "Signature verification failed"
        //                });
        //            }

        //            // Step 3: Save to DB
        //            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //            {
        //                conn.Open();
        //                using (SqlCommand cmd = new SqlCommand("SP_InsertPaymentDetails", conn))
        //                {
        //                    cmd.CommandType = CommandType.StoredProcedure;

        //                    cmd.Parameters.AddWithValue("@BookingID", model.BookingID);
        //                    cmd.Parameters.AddWithValue("@AmountPaid", model.Amount);
        //                    cmd.Parameters.AddWithValue("@PaymentMode", "Razorpay");
        //                    cmd.Parameters.AddWithValue("@TransactionID", model.RazorpayPaymentId);
        //                    cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@IsRefunded", 0);

        //                    int result = cmd.ExecuteNonQuery();
        //                    if (result <= 0)
        //                    {
        //                        return StatusCode(500, new
        //                        {
        //                            success = false,
        //                            message = "Failed to save payment details"
        //                        });
        //                    }
        //                }
        //            }
        //        }

        //        // Step 4: Return response
        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Order created and payment processed successfully",
        //            data = new
        //            {
        //                orderId = order["id"].ToString(),
        //                key = key,
        //                bookingId = model.BookingID,
        //                transactionId = model.RazorpayPaymentId,
        //                amountPaid = model.Amount,
        //                paymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = "Server error",
        //            error = ex.Message
        //        });
        //    }
        //}

    }
}
