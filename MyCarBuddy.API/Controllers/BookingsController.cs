using Braintree;
using GSF;
using GSF.ErrorManagement;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
using Newtonsoft.Json;
using Razorpay.Api;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text;
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
        // ========== 1) INSERT-BOOKING (handles COS or Online) ==========


        decimal finalPrice;

        [HttpPost("insert-booking")]
        public async Task<IActionResult> InsertBooking([FromForm] BookingInsertDTO model)
        {
            try
            {
                // 1) Persist booking via SP
                var (bookingId, trackId) = await GenerateAndPersistBookingAsync(model);

                // 2) Payment initialization
                object razorpayOrderInfo = null;
                if (string.Equals(model.PaymentMethod, "COS", StringComparison.OrdinalIgnoreCase))
                {
                    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand("SP_InsertPendingCODPayment", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@BookingID", bookingId);
                    cmd.Parameters.AddWithValue("@ExpectedAmount", finalPrice);
                    await cmd.ExecuteNonQueryAsync();

                    await SetPaymentStatusAsync(bookingId, "Pending");
                }
                else
                {
                    // Online -> Razorpay order
                    string key = _configuration["Razorpay:Key"];
                    string secret = _configuration["Razorpay:Secret"];
                    var client = new RazorpayClient(key, secret);

                    var options = new Dictionary<string, object>
                    {
                        { "amount", Convert.ToInt64(finalPrice * 100) },
                        { "currency", "INR" },
                        { "receipt", bookingId.ToString() },
                        { "payment_capture", 1 }
                    };

                    Razorpay.Api.Order order = client.Order.Create(options);

                    razorpayOrderInfo = new
                    {
                        OrderID = order["id"].ToString(),
                        Key = key
                    };

                    await SetPaymentStatusAsync(bookingId, "Pending");
                }

                // Optional notifications
                await SendBookingConfirmationEmail(model.CustID, bookingId);
                await SendBookingConfirmationSMS(model.CustID, bookingId);
              

                return Ok(new
                {
                    Success = true,
                    BookingID = bookingId,
                    BookingTrackID = trackId,
                    Razorpay = razorpayOrderInfo,
                    Message = model.PaymentMethod?.ToUpper() == "COS"
                        ? "Booking created. Payment will be collected on service (COD)."
                        : "Booking created. Razorpay order generated."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

      
       // ========== 2) CONFIRM-PAYMENT ==========
       [HttpPost("confirm-payment")]
        public IActionResult ConfirmPayment([FromBody] PaymentConfirmRequest req)
        {
           

            try
            {
                string invoiceNumber;
                string fileUrl;

                string invoicesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Invoices");
                if (!Directory.Exists(invoicesFolder))
                    Directory.CreateDirectory(invoicesFolder);

                string tempUrl = $"{Request.Scheme}://{Request.Host}/Invoices/TEMP.pdf";

                if (req.PaymentMode?.Equals("Razorpay", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Verify Razorpay signature
                    string secret = _configuration["Razorpay:Secret"];
                    string expected = Utils.GetHash($"{req.RazorpayOrderId}|{req.RazorpayPaymentId}", secret);
                    if (!string.Equals(expected, req.RazorpaySignature, StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new { success = false, message = "Signature verification failed" });

                    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    conn.Open();
                    using var cmd = new SqlCommand("SP_InsertPaymentDetails", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@BookingID", req.BookingID);
                    cmd.Parameters.AddWithValue("@AmountPaid", finalPrice);
                    cmd.Parameters.AddWithValue("@PaymentMode", "Razorpay");
                    cmd.Parameters.AddWithValue("@TransactionID", req.RazorpayPaymentId);
                    cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@IsRefunded", 0);
                    cmd.Parameters.AddWithValue("@FolderPath", tempUrl);

                    var outInv = new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 50)
                    { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(outInv);

                    cmd.ExecuteNonQuery();
                    invoiceNumber = outInv.Value?.ToString() ?? throw new Exception("Invoice number not returned.");

                    SetPaymentStatus(req.BookingID, "Success");
                }
                else if (req.PaymentMode?.Equals("COS", StringComparison.OrdinalIgnoreCase) == true)
                {

                     

                    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    conn.Open();
                    using var cmd = new SqlCommand("SP_FinalizeCODPayment", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@BookingID", req.BookingID);
                    cmd.Parameters.AddWithValue("@AmountPaid", finalPrice);
                    cmd.Parameters.AddWithValue("@TransactionID", string.IsNullOrWhiteSpace(req.TransactionId) ? $"COS-{req.BookingID}-{DateTime.UtcNow:yyyyMMddHHmmss}" : req.TransactionId);
                    cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@FolderPath", tempUrl);

                    var outInv = new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 50)
                    { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(outInv);

                    cmd.ExecuteNonQuery();
                    invoiceNumber = outInv.Value?.ToString() ?? throw new Exception("Invoice number not returned.");

                    SetPaymentStatus(req.BookingID, "Success");
                }
                else
                {
                    return BadRequest(new { success = false, message = "Unsupported payment mode" });
                }

                // Generate PDF
                GenerateInvoicePDF(invoiceNumber, req);

                // Update final path in DB
                fileUrl = $"{Request.Scheme}://{Request.Host}/Invoices/{invoiceNumber}.pdf";
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    using var cmd = new SqlCommand("SP_UpdatePaymentFolderPath", conn)
                    { CommandType = CommandType.StoredProcedure };
                    cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                    cmd.Parameters.AddWithValue("@FolderPath", fileUrl);
                    cmd.ExecuteNonQuery();
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment confirmed and invoice generated",
                    data = new
                    {
                        bookingId = req.BookingID,
                        amountPaid = req.AmountPaid,
                        paymentMode = req.PaymentMode,
                        invoiceNumber,
                        invoiceUrl = fileUrl
                    }
                });
            }
            catch (Exception ex)
            {
                try { if (req?.BookingID > 0) SetPaymentStatus(req.BookingID, "Failed"); } catch { }
                return StatusCode(500, new { success = false, message = "Server error", error = ex.Message });
            }
        }

        // ========== HELPERS ==========

        private async Task<(int BookingId, string TrackId)> GenerateAndPersistBookingAsync(BookingInsertDTO model)
        {
            int bookingId;

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            string month = DateTime.Now.ToString("MM");
            string year = DateTime.Now.ToString("yyyy");
            string prefix = $"MYCAR{month}{year}";
            string trackId = $"{prefix}{Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper()}";
            model.BookingTrackID = trackId;

            //Generate OTP

            Random random=new Random();
            int otp = random.Next(100000, 999999);
            model.BookingOTP = otp;

            // GST Calculation (18%) and coupon deduction
            decimal couponAmount = Convert.ToDecimal(model.CouponAmount);
          
            finalPrice = Convert.ToDecimal(model.TotalPrice - model.CouponAmount + model.GSTAmount);
            //decimal couponAmount = model.CouponAmount ?? 0;
            //decimal  = priceWithGst - couponAmount;
            if (finalPrice < 0)
                finalPrice = 0;
            // model.TotalPrice = finalPrice;
            


            using var cmd = new SqlCommand("sp_InsertBookings", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookingTrackID", model.BookingTrackID ?? "");
            cmd.Parameters.AddWithValue("@CustID", model.CustID);
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
            cmd.Parameters.AddWithValue("@GSTAmount", model.GSTAmount);
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
            cmd.Parameters.AddWithValue("@BookingOTP", model.BookingOTP);


            var outId = new SqlParameter("@BookingID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outId);

            await cmd.ExecuteNonQueryAsync();
            bookingId = Convert.ToInt32(outId.Value);

            return (bookingId, trackId);
        }

        private async Task SetPaymentStatusAsync(int bookingId, string status)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SP_UpdateBookingPaymentStatus", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookingID", bookingId);
            cmd.Parameters.AddWithValue("@PaymentStatus", status);
            await cmd.ExecuteNonQueryAsync();
        }

        private void SetPaymentStatus(int bookingId, string status)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new SqlCommand("SP_UpdateBookingPaymentStatus", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookingID", bookingId);
            cmd.Parameters.AddWithValue("@PaymentStatus", status);
            cmd.ExecuteNonQuery();
        }

        private void GenerateInvoicePDF(string invoiceNumber, PaymentConfirmRequest req)
        {
            string invoicesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Invoices");
            string pdfPath = Path.Combine(invoicesFolder, $"{invoiceNumber}.pdf");

            iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4);
            using var fs = new FileStream(pdfPath, FileMode.Create);
            PdfWriter.GetInstance(doc, fs);
            doc.Open();
            doc.Add(new Paragraph("MyCarBuddy Invoice"));
            doc.Add(new Paragraph($"Invoice Number: {invoiceNumber}"));
            doc.Add(new Paragraph($"Booking ID: {req.BookingID}"));
            doc.Add(new Paragraph($"Payment Mode: {req.PaymentMode}"));
            doc.Add(new Paragraph($"Transaction ID: {(req.PaymentMode == "Razorpay" ? req.RazorpayPaymentId : req.TransactionId)}"));
            doc.Add(new Paragraph($"Amount Paid: {req.AmountPaid:C}"));
            doc.Add(new Paragraph($"Payment Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
            doc.Close();
        }



        [HttpPost("finalize-cash-payment")]
        public IActionResult FinalizeCashPayment([FromBody] CashPaymentFinalizeRequest req)
        {
            try
            {
                string invoiceNumber;
                string fileUrl;

                string invoicesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Invoices");
                if (!Directory.Exists(invoicesFolder))
                    Directory.CreateDirectory(invoicesFolder);

                string tempUrl = $"{Request.Scheme}://{Request.Host}/Invoices/TEMP.pdf";

                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();
                using var cmd = new SqlCommand("SP_FinalizeCODPayment", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@BookingID", req.BookingID);
                cmd.Parameters.AddWithValue("@AmountPaid", finalPrice);
                cmd.Parameters.AddWithValue("@TransactionID", string.IsNullOrWhiteSpace(req.TransactionId)
                    ? $"COS-{req.BookingID}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    : req.TransactionId);
                cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@FolderPath", tempUrl);

                var outInv = new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 50)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outInv);

                cmd.ExecuteNonQuery();
                invoiceNumber = outInv.Value?.ToString() ?? throw new Exception("Invoice number not returned.");

                SetPaymentStatus(req.BookingID, "Success");

                // Generate invoice PDF
                GenerateInvoicePDF(invoiceNumber, new PaymentConfirmRequest
                {
                    BookingID = req.BookingID,
                    AmountPaid = req.AmountPaid,
                    PaymentMode = "COS",
                    TransactionId = req.TransactionId
                });

                // Update folder path with final invoice
                fileUrl = $"{Request.Scheme}://{Request.Host}/Invoices/{invoiceNumber}.pdf";
                using (var conn2 = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn2.Open();
                    using var cmd2 = new SqlCommand("SP_UpdatePaymentFolderPath", conn2)
                    { CommandType = CommandType.StoredProcedure };
                    cmd2.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                    cmd2.Parameters.AddWithValue("@FolderPath", fileUrl);
                    cmd2.ExecuteNonQuery();
                }

                return Ok(new
                {
                    success = true,
                    message = "Cash payment finalized and invoice generated",
                    data = new
                    {
                        bookingId = req.BookingID,
                        amountPaid = req.AmountPaid,
                        paymentMode = "COS",
                        invoiceNumber,
                        invoiceUrl = fileUrl
                    }
                });
            }
            catch (Exception ex)
            {
                try { if (req?.BookingID > 0) SetPaymentStatus(req.BookingID, "Failed"); } catch { }
                return StatusCode(500, new { success = false, message = "Server error", error = ex.Message });
            }
        }


        //private Task SendBookingConfirmationSMS(int custId, int bookingId) => Task.CompletedTask;
        //private Task SendBookingConfirmationEmail(int custId, int bookingId) => Task.CompletedTask;

        // ✅ SMS confirmation (dummy template)
        private async Task SendBookingConfirmationSMS(int custId, int bookingId)
        {
            try
            {
                string phoneNumber = null;
                string custName = null;

                // 🔹 Fetch customer phone/email from DB
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand("sp_GetCustomerDetailsById", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CustID", custId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        phoneNumber = reader["CustPhoneNumber"]?.ToString();
                        custName = reader["CustFullName"]?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(phoneNumber)) return;

                // Dummy SMS template
                string message = $"Hi {custName ?? "Customer"}, your booking #{bookingId} has been confirmed. Thank you for choosing MyCarBuddy!";

                // 🔹 Example SMS API (replace with real provider)
                using var client = new HttpClient();
                var smsApiUrl = _configuration["SMS:ApiUrl"];
                var apiKey = _configuration["SMS:ApiKey"];

                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("apikey", apiKey),
            new KeyValuePair<string, string>("to", phoneNumber),
            new KeyValuePair<string, string>("message", message)
        });

                var response = await client.PostAsync(smsApiUrl, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS for booking {bookingId}");
            }
        }


        // ✅ Email confirmation (with invoice)
        private async Task SendBookingConfirmationEmail(int custId, int bookingId)
        {
            try
            {
                string email = null;
                string custName = null;
                string invoicePath = null;

                // 🔹 Fetch customer email & invoice from DB
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand("sp_GetCustomerDetailsById", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CustID", custId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        email = reader["CustEmail"]?.ToString();
                        custName = reader["CustFullName"]?.ToString();
                        invoicePath = reader["InvoicePath"]?.ToString(); // assuming you save invoice
                    }
                }

                if (string.IsNullOrEmpty(email)) return;

                string subject = "Booking Confirmation - MyCarBuddy";
                string body = $@"
        <h2>Dear {custName ?? "Customer"},</h2>
        <p>Your booking <b>#{bookingId}</b> has been successfully confirmed.</p>
        <p>Thank you for choosing <b>MyCarBuddy</b> 🚗✨</p>
        <p>Please find your invoice attached.</p>
        <br/>
        <p>Regards,<br/>Team MyCarBuddy</p>
        ";

                // ✅ Load Gmail SMTP details from config
                string smtpHost = _configuration["Email:SmtpHost"];
                int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                string smtpUser = _configuration["Email:Username"];
                string smtpPass = _configuration["Email:Password"];
                string fromEmail = _configuration["Email:From"];

                using var smtp = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mail = new MailMessage(fromEmail, email, subject, body)
                {
                    IsBodyHtml = true
                };

                // ✅ Add CC
                mail.CC.Add("haris@glansa.com");

                // attach invoice if available
                if (!string.IsNullOrEmpty(invoicePath) && System.IO.File.Exists(invoicePath))
                {
                    mail.Attachments.Add(new Attachment(invoicePath));
                }

                await smtp.SendMailAsync(mail);
                _logger.LogInformation($"Booking confirmation email sent to {email} (CC: haris@glansa.com) for booking {bookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send Email for booking {bookingId}");
            }
        }





        // ========== DTOs ==========

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

                return Ok (new { Success = true, Message = "Technician assigned successfully." });
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
                        return Ok (new { message = "Bookings not found" });
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

                    //using (SqlDataReader reader = cmd.ExecuteReader())
                    //{
                    //    // Combine multiple rows into one JSON array
                    //    var sb = new System.Text.StringBuilder();
                    //    //sb.Append("["); // start array

                    //    bool first = true;
                    //    while (reader.Read() && !reader.IsDBNull(0))
                    //    {
                    //        if (!first) sb.Append(",");
                    //        sb.Append(reader.GetString(0));
                    //        first = false;
                    //    }

                    //    // sb.Append("]"); // end array
                    //    jsonResult = sb.ToString();
                    //}
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            jsonResult = reader.GetString(0);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(jsonResult) || jsonResult == "[]")
                    return Ok (new { message = "No bookings found for this Id" });

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
                    return Ok (new { message = "No bookings found for this technician" });

                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings.");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }
        }

        #endregion


        #region GetTechniciansTodayBookings
        [HttpGet("GetTechTodayBookings")]

        public IActionResult GetTechniciansTodayBookings([FromQuery] int Id)
        {
            try
            {
                string jsonResult = null;

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("sp_GetTechniciansTodayBookings", conn))
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
                    return Ok (new { message = "No bookings found for this technician" });

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
                    return Ok (new { message = "No bookings found for this Id" });

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
    }
}
