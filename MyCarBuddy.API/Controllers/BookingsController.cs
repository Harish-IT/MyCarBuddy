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
using System.Net.Http;
using System.Net.Http.Headers;
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

        //[HttpPost("insert-booking")]
        //public async Task<IActionResult> InsertBooking([FromForm] BookingInsertDTO model)
        //{
        //    decimal priceWithGst = 0;
        //    try
        //    {
        //        int bookingId = 0;

        //        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            await conn.OpenAsync();

        //            // 1. Generate BookingTrackID
        //            string month = DateTime.Now.ToString("MM");
        //            string year = DateTime.Now.ToString("yyyy");
        //            string prefix = $"MYCAR{month}{year}";
        //            string lastTrackId = null;
        //            int nextSequence = 1;

        //            using (SqlCommand cmdCheck = new SqlCommand("SELECT TOP 1 BookingTrackID FROM Bookings WHERE BookingTrackID LIKE @Prefix + '%' ORDER BY BookingID DESC", conn))
        //            {
        //                cmdCheck.Parameters.AddWithValue("@Prefix", prefix);
        //                var result = await cmdCheck.ExecuteScalarAsync();
        //                if (result != null)
        //                {
        //                    lastTrackId = result.ToString();
        //                    string lastSeqStr = lastTrackId.Substring(prefix.Length);
        //                    if (int.TryParse(lastSeqStr, out int lastSeq))
        //                        nextSequence = lastSeq + 1;
        //                }
        //            }

        //            string newTrackId = $"{prefix}{nextSequence:D3}";
        //            model.BookingTrackID = newTrackId;



        //            // GST Calculation (18%) and coupon deduction
        //            decimal couponAmount = Convert.ToDecimal(model.CouponAmount);
        //            decimal gstAmount = Convert.ToDecimal((model.TotalPrice-model.CouponAmount) * 0.18m);
        //            decimal finalPrice = Convert.ToDecimal(model.TotalPrice-model.CouponAmount + gstAmount);
        //            //decimal couponAmount = model.CouponAmount ?? 0;


        //            //decimal  = priceWithGst - couponAmount;
        //            if (finalPrice < 0)
        //               finalPrice = 0;
        //           // model.TotalPrice = finalPrice;
        //            model.GSTAmount = gstAmount;

        //            // 2. Insert Booking
        //            using (SqlCommand cmd = new SqlCommand("SP_InsertBookings", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@BookingTrackID", model.BookingTrackID ?? "");
        //                cmd.Parameters.AddWithValue("@CustID", model.CustID);
        //                cmd.Parameters.AddWithValue("@TechFullName", model.TechFullName ?? "");
        //                cmd.Parameters.AddWithValue("@TechPhoneNumber", model.TechPhoneNumber ?? "");
        //                cmd.Parameters.AddWithValue("@CustFullName", model.CustFullName ?? "");
        //                cmd.Parameters.AddWithValue("@CustPhoneNumber", model.CustPhoneNumber ?? "");
        //                cmd.Parameters.AddWithValue("@CustEmail", model.CustEmail ?? "");
        //                cmd.Parameters.AddWithValue("@StateID", model.StateID);
        //                cmd.Parameters.AddWithValue("@CityID", model.CityID);
        //                cmd.Parameters.AddWithValue("@Pincode", model.Pincode);
        //                cmd.Parameters.AddWithValue("@FullAddress", model.FullAddress ?? "");
        //                cmd.Parameters.AddWithValue("@BookingStatus", model.BookingStatus ?? "Pending");
        //                cmd.Parameters.AddWithValue("@Longitude", model.Longitude ?? "");
        //                cmd.Parameters.AddWithValue("@Latitude", model.Latitude ?? "");
        //                cmd.Parameters.AddWithValue("@PackageIds", model.PackageIds ?? "");
        //                cmd.Parameters.AddWithValue("@PackagePrice", model.PackagePrice ?? "");
        //                cmd.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);

        //                cmd.Parameters.AddWithValue("@GSTAmount", model.GSTAmount);

        //                cmd.Parameters.AddWithValue("@CouponCode", model.CouponCode ?? "");
        //                cmd.Parameters.AddWithValue("@CouponAmount", model.CouponAmount);
        //                cmd.Parameters.AddWithValue("@BookingFrom", model.BookingFrom ?? "App");
        //                cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod ?? "");
        //                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
        //                cmd.Parameters.AddWithValue("@BookingDate", model.BookingDate);
        //                cmd.Parameters.AddWithValue("@TimeSlot", model.TimeSlot ?? "");
        //                cmd.Parameters.AddWithValue("@IsOthers", model.IsOthers);
        //                cmd.Parameters.AddWithValue("@OthersFullName", model.OthersFullName ?? "");
        //                cmd.Parameters.AddWithValue("@OthersPhoneNumber", model.OthersPhoneNumber ?? "");
        //                cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
        //                cmd.Parameters.AddWithValue("@VechicleID", model.VechicleID);

        //                var outputId = new SqlParameter("@BookingID", SqlDbType.Int)
        //                {
        //                    Direction = ParameterDirection.Output
        //                };
        //                cmd.Parameters.Add(outputId);

        //                await cmd.ExecuteNonQueryAsync();
        //                bookingId = Convert.ToInt32(outputId.Value);
        //            }

        //            // 3. Save Booking Images
        //            if (model.Images != null && model.Images.Count > 0)
        //            {
        //                foreach (var image in model.Images)
        //                {
        //                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
        //                    var savePath = Path.Combine("wwwroot", "BookingImages", fileName);

        //                    using (var stream = new FileStream(savePath, FileMode.Create))
        //                        await image.CopyToAsync(stream);

        //                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO BookingImages 
        //        (BookingID, ImageURL, UploadedBy, UploadedAt, CustID, TechID, ImageUploadType)
        //        VALUES (@BookingID, @ImageURL, @UploadedBy, @UploadedAt, @CustID, @TechID, 'Customer')", conn))
        //                    {
        //                        cmd.Parameters.AddWithValue("@BookingID", bookingId);
        //                        cmd.Parameters.AddWithValue("@ImageURL", "/BookingImages/" + fileName);
        //                        cmd.Parameters.AddWithValue("@UploadedBy", model.CreatedBy);
        //                        cmd.Parameters.AddWithValue("@UploadedAt", DateTime.Now);
        //                        cmd.Parameters.AddWithValue("@CustID", model.CustID);
        //                        cmd.Parameters.AddWithValue("@TechID", model.TechID);

        //                        await cmd.ExecuteNonQueryAsync();
        //                    }
        //                }
        //            }
        //        }

        //        // 4. Only create Razorpay order if not Cash On Service
        //        object razorpayOrderInfo = null;
        //        if (!string.Equals(model.PaymentMethod, "COS", StringComparison.OrdinalIgnoreCase))
        //        {
        //            string key = _configuration["Razorpay:Key"];
        //            string secret = _configuration["Razorpay:Secret"];
        //            RazorpayClient client = new RazorpayClient(key, secret);


        //            Dictionary<string, object> options = new Dictionary<string, object>
        //    {
        //        { "amount",priceWithGst * 100 }, // in paise
        //        { "currency", "INR" },
        //        { "receipt", bookingId.ToString() },
        //        { "payment_capture", 1 }
        //    };

        //            Razorpay.Api.Order order = client.Order.Create(options);

        //            razorpayOrderInfo = new
        //            {
        //                OrderID = order["id"].ToString(),
        //                Key = key
        //            };
        //        }

        //        // 5. Notification (optional)
        //        await SendBookingConfirmationSMS(model.CustID, bookingId);
        //        await SendBookingConfirmationEmail(model.CustID, bookingId);

        //        // 6. Final Response
        //        return Ok(new
        //        {
        //            Success = true,
        //            BookingID = bookingId,
        //            BookingTrackID = model.BookingTrackID,
        //            Razorpay = razorpayOrderInfo,
        //            Message = model.PaymentMethod == "COS"
        //                ? "Booking created successfully. Payment will be collected on service."
        //                : "Booking created and Razorpay order generated successfully."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Success = false, Message = ex.Message });
        //    }
        //}

        //private Task SendBookingConfirmationSMS(int custId, int bookingId)
        //{
        //    // Get customer phone & send SMS
        //    return Task.CompletedTask;
        //}

        //private Task SendBookingConfirmationEmail(int custId, int bookingId)
        //{
        //    // Get customer email & send message
        //    return Task.CompletedTask;
        //}

        //[HttpPost("update-booking")]
        //public async Task<IActionResult> UpdateBooking([FromBody] BookingUpdateDTO model)
        //{
        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            await conn.OpenAsync();

        //            // Call SP to update booking and insert payment record if missing
        //            using (SqlCommand cmd = new SqlCommand("SP_UpdateBookingAndCreateOrder", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@BookingID", model.BookingID);
        //                cmd.Parameters.AddWithValue("@CustID", model.CustID);
        //                cmd.Parameters.AddWithValue("@TechID", model.TechID);
        //                cmd.Parameters.AddWithValue("@BookingStatus", model.BookingStatus ?? "Pending");
        //                cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod ?? "");
        //                cmd.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);
        //                cmd.Parameters.AddWithValue("@ModifiedBy", model.ModifiedBy);

        //                await cmd.ExecuteNonQueryAsync();
        //            }

        //            // Create Razorpay order if online payment and order not created
        //            if (model.PaymentMethod != null && model.PaymentMethod != "COS")
        //            {
        //                string key = _configuration["Razorpay:Key"];
        //                string secret = _configuration["Razorpay:Secret"];
        //                RazorpayClient client = new RazorpayClient(key, secret);

        //                Dictionary<string, object> options = new Dictionary<string, object>
        //        {
        //            { "amount", model.TotalPrice * 100 }, // in paise
        //            { "currency", "INR" },
        //            { "receipt", model.BookingID.ToString() },
        //            { "payment_capture", 1 }
        //        };

        //                Razorpay.Api.Order order = client.Order.Create(options);

        //                // Update Payments table with Razorpay OrderID
        //                using (SqlCommand cmdUpdate = new SqlCommand(
        //                    "UPDATE Payments SET TransactionID = @TransactionID WHERE BookingID = @BookingID AND PaymentMode = 'Razorpay'", conn))
        //                {
        //                    cmdUpdate.Parameters.AddWithValue("@TransactionID", order["id"].ToString());
        //                    cmdUpdate.Parameters.AddWithValue("@BookingID", model.BookingID);
        //                    await cmdUpdate.ExecuteNonQueryAsync();
        //                }

        //                return Ok(new
        //                {
        //                    Success = true,
        //                    Message = "Booking updated and Razorpay order created.",
        //                    Razorpay = new
        //                    {
        //                        OrderID = order["id"].ToString(),
        //                        Key = key
        //                    }
        //                });
        //            }

        //            return Ok(new { Success = true, Message = "Booking updated without Razorpay order." });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Success = false, Message = ex.Message });
        //    }
        //}

        //[HttpPost("confirm-Payment")]
        //public IActionResult ConfirmPayment([FromBody] RazorpayPaymentRequest paymentRequest)
        //{
        //    string secret = _configuration["Razorpay:Secret"];
        //    string expectedSignature = Utils.GetHash($"{paymentRequest.RazorpayOrderId}|{paymentRequest.RazorpayPaymentId}", secret);

        //    if (expectedSignature != paymentRequest.RazorpaySignature)
        //    {
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = "Signature verification failed"
        //        });
        //    }

        //    try
        //    {
        //        string invoiceNumber;
        //        string fileUrl;

        //        // Prepare invoice file path
        //        string invoicesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Invoices");
        //        if (!Directory.Exists(invoicesFolder))
        //        {
        //            Directory.CreateDirectory(invoicesFolder);
        //        }

        //        // File path in server
        //        string filePath = Path.Combine(invoicesFolder, "TEMP.pdf"); // Will rename after SP returns invoice number

        //        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            conn.Open();
        //            using (SqlCommand cmd = new SqlCommand("SP_InsertPaymentDetails", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@BookingID", paymentRequest.BookingID);
        //                cmd.Parameters.AddWithValue("@AmountPaid", paymentRequest.AmountPaid);
        //                cmd.Parameters.AddWithValue("@PaymentMode", "Razorpay");
        //                cmd.Parameters.AddWithValue("@TransactionID", paymentRequest.RazorpayPaymentId);
        //                cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@IsRefunded", 0);

        //                // Placeholder path (will update with real invoice number)
        //                string tempPath = $"{Request.Scheme}://{Request.Host}/Invoices/TEMP.pdf";
        //                cmd.Parameters.AddWithValue("@FolderPath", tempPath);

        //                SqlParameter outputInvoice = new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 50)
        //                {
        //                    Direction = ParameterDirection.Output
        //                };
        //                cmd.Parameters.Add(outputInvoice);

        //                int result = cmd.ExecuteNonQuery();

        //                if (result <= 0)
        //                {
        //                    return StatusCode(500, new
        //                    {
        //                        success = false,
        //                        message = "Failed to save payment details"
        //                    });
        //                }

        //                invoiceNumber = outputInvoice.Value.ToString();
        //            }
        //        }

        //        // Now update file path with invoice number
        //        string newFilePath = Path.Combine(invoicesFolder, $"{invoiceNumber}.pdf");
        //        fileUrl = $"{Request.Scheme}://{Request.Host}/Invoices/{invoiceNumber}.pdf";

        //        // Generate PDF
        //        iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);
        //        PdfWriter.GetInstance(doc, new FileStream(newFilePath, FileMode.Create));
        //        doc.Open();
        //        doc.Add(new Paragraph("Invoice"));
        //        doc.Add(new Paragraph($"Invoice Number: {invoiceNumber}"));
        //        doc.Add(new Paragraph($"Booking ID: {paymentRequest.BookingID}"));
        //        doc.Add(new Paragraph($"Transaction ID: {paymentRequest.RazorpayPaymentId}"));
        //        doc.Add(new Paragraph($"Amount Paid: {paymentRequest.AmountPaid:C}"));
        //        doc.Add(new Paragraph($"Payment Mode: Razorpay"));
        //        doc.Add(new Paragraph($"Payment Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
        //        doc.Close();

        //        // Update folder path in DB for this invoice
        //        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            conn.Open();
        //            using (SqlCommand cmd = new SqlCommand("UPDATE Payments SET FolderPath = @Path WHERE InvoiceNumber = @InvoiceNumber", conn))
        //            {
        //                cmd.Parameters.AddWithValue("@Path", fileUrl);
        //                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Payment confirmed, invoice generated successfully",
        //            data = new
        //            {
        //                bookingId = paymentRequest.BookingID,
        //                transactionId = paymentRequest.RazorpayPaymentId,
        //                amountPaid = paymentRequest.AmountPaid,
        //                paymentMode = "Razorpay",
        //                paymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //                invoiceNumber = invoiceNumber,
        //                invoiceUrl = fileUrl
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
                    cmd.Parameters.AddWithValue("@ExpectedAmount", model.TotalPrice);
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
                await SendBookingConfirmationSMS(model.CustID, bookingId);
                await SendBookingConfirmationEmail(model.CustID, bookingId);

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
                    cmd.Parameters.AddWithValue("@AmountPaid", req.AmountPaid);
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
                    cmd.Parameters.AddWithValue("@AmountPaid", req.AmountPaid);
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
                cmd.Parameters.AddWithValue("@AmountPaid", req.AmountPaid);
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


        private Task SendBookingConfirmationSMS(int custId, int bookingId) => Task.CompletedTask;
        private Task SendBookingConfirmationEmail(int custId, int bookingId) => Task.CompletedTask;


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






        //[HttpPost("confirm-Payment")]
        //public IActionResult ConfirmPayment([FromBody] RazorpayPaymentRequest paymentRequest)
        //{
        //    string secret = _configuration["Razorpay:Secret"];

        //    string expectedSignature = Utils.GetHash($"{paymentRequest.RazorpayOrderId}|{paymentRequest.RazorpayPaymentId}", secret);

        //    if (expectedSignature != paymentRequest.RazorpaySignature)
        //    {
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = "Signature verification failed"
        //        });
        //    }

        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            conn.Open();
        //            using (SqlCommand cmd = new SqlCommand("SP_InsertPaymentDetails", conn))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@BookingID", paymentRequest.BookingID);
        //                cmd.Parameters.AddWithValue("@AmountPaid", paymentRequest.AmountPaid);
        //                cmd.Parameters.AddWithValue("@PaymentMode", "Razorpay");
        //                cmd.Parameters.AddWithValue("@TransactionID", paymentRequest.RazorpayPaymentId);
        //                cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@IsRefunded", 0);

        //                int result = cmd.ExecuteNonQuery();

        //                if (result <= 0)
        //                {
        //                    return StatusCode(500, new
        //                    {
        //                        success = false,
        //                        message = "Failed to save payment details"
        //                    });
        //                }
        //            }
        //        }

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Payment confirmed and saved successfully",
        //            data = new
        //            {
        //                bookingId = paymentRequest.BookingID,
        //                transactionId = paymentRequest.RazorpayPaymentId,
        //                amountPaid = paymentRequest.AmountPaid,
        //                paymentMode = "Razorpay",
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
