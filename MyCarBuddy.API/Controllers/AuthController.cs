using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Services;
using MyCarBuddy.API.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly JwtService _jwt;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AuthController> _logger;



        public AuthController(IConfiguration config, JwtService jwt, IWebHostEnvironment env)
        {
            _config = config;
            _jwt = jwt;
            _env = env;
        }

        [HttpPost("Technician-login")]
        public IActionResult TechnicianLogin([FromBody] LoginRequest request)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_TechnicianLogin", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
                cmd.Parameters.AddWithValue("@Password", request.Password); // Later use hashed password

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var techId = reader["TechID"].ToString();
                    string token = _jwt.GenerateToken(techId, "Technician");

                    return Ok(new
                    {

                        Success = true,
                        TechId = techId,
                        Token = token,
                        Name = reader["FullName"],
                        Email = reader["Email"]

                    });
                }
                else
                {
                    return Unauthorized(new { Success = false, Message = "Invalid logins" });
                }
            }
        }

        [HttpPost("Admin-login")]
        public IActionResult AdminLogin([FromBody] LoginRequest request)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_AdminLogin", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@Password", request.Password); // Hash if needed

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var userId = reader["ID"].ToString(); // Generic user ID
                    var userType = reader["UserType"].ToString(); // 'Admin', 'Distributor', or 'Dealer'
                    var fullName = reader["FullName"].ToString();
                    var email = reader["Email"].ToString();

                    string token = _jwt.GenerateToken(userId, userType); // Use userType in token

                    return Ok(new
                    {
                        Success = true,
                        Token = token,
                        Role = userType,
                        Name = fullName,
                        Email = email
                    });
                }
                else
                {
                    return Unauthorized(new { Success = false, Message = "Invalid login" });
                }
            }
        }


        [HttpPut("update-admin")]
        public async Task<IActionResult> UpdateAdmin([FromForm] AdminUpdate admin)
        {
            if (admin.AdminID == null)
                return BadRequest("AdminID is required.");

            string connectionString = _config.GetConnectionString("DefaultConnection");
            string? profileImagePath = null;

            if (admin.ProfileImage1 != null && admin.ProfileImage1.Length > 0)
            {
                string profileImageFileName = Guid.NewGuid() + Path.GetExtension(admin.ProfileImage1.FileName);
                var folderPath = Path.Combine(_env.WebRootPath, "Images", "AdminImages");
                var filePath = Path.Combine(folderPath, profileImageFileName);

                Directory.CreateDirectory(folderPath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await admin.ProfileImage1.CopyToAsync(stream);
                }

                // profileImagePath = Path.Combine("Images", "AdminImages", profileImageFileName)
                //  .Replace("\\", "/");
                profileImagePath = "/" + Path.Combine("Images", "AdminImages", profileImageFileName)
                      .Replace("\\", "/");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("Sp_UpdateAdminUsers", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@AdminID", admin.AdminID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FullName",
                        string.IsNullOrWhiteSpace(admin.FullName) ? DBNull.Value : admin.FullName);
                    cmd.Parameters.AddWithValue("@PasswordHash",
                        string.IsNullOrWhiteSpace(admin.PasswordHash) ? DBNull.Value : admin.PasswordHash);
                    cmd.Parameters.AddWithValue("@ProfileImage",
                        string.IsNullOrWhiteSpace(profileImagePath) ? DBNull.Value : profileImagePath);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { message = "Admin updated successfully" });
        }



        #region CategoryById


        [HttpGet("adminid")]

        public IActionResult GetCategoryById(int adminid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetAdminUsersById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AdminID", adminid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "Admin  not found" });
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
                ErrorLogger.LogToDatabase(ex, HttpContext, _config, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Admins.", error = ex.Message });

            }

        }

        #endregion





        //[HttpPost("send-otp")]
        //public IActionResult SendOtp([FromBody] CustomerLoginRequest request)
        //{
        //    // 1. Generate OTP
        //    string otp = new Random().Next(100000, 999999).ToString();

        //    // 2. Save OTP via Stored Procedure
        //    string connectionString = _config.GetConnectionString("DefaultConnection");
        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        SqlCommand cmd = new SqlCommand("SP_SaveCustomerOTP", conn);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.AddWithValue("@Email", request.Email);
        //        cmd.Parameters.AddWithValue("@OTP", otp);
        //        conn.Open();
        //        cmd.ExecuteNonQuery();
        //    }

        //    // 3. Send OTP Email (configure SMTP as needed)
        //    var smtpClient = new SmtpClient("smtp.gmail.com")
        //    {
        //        Port = 587,
        //        Credentials = new NetworkCredential("prudhviraj.glansa@gmail.com", "ujiajcwsczeghshr"),
        //        EnableSsl = true,
        //    };
        //    smtpClient.Send("harish@glansa.com", request.Email, "Your OTP Code", $"Your OTP is: {otp}");

        //    return Ok(new { Success = true, Message = "OTP sent to email." });
        //}


        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] CustomerLoginRequest request)
        {
            string loginId = request.LoginId?.Trim();
            if (string.IsNullOrEmpty(loginId) || loginId.ToLower() == "string")
                return BadRequest(new { Success = false, Message = "Please provide a valid Email or Phone Number." });

            string phoneNumber = loginId;
            string otp;
            bool sendOtpToUser = true;

            // Check if it's a phone number
            if (!loginId.Contains("@"))
            {
                if (phoneNumber.Length == 10)
                    phoneNumber = "91" + phoneNumber;
                else if (!phoneNumber.StartsWith("91") || phoneNumber.Length != 12)
                    return BadRequest(new { Success = false, Message = "Invalid phone number" });

                // Default OTP for test number
                if (phoneNumber == "919999999999")
                {
                    otp = "123456";
                    sendOtpToUser = false; // Skip SMS sending
                }
                else
                {
                    otp = new Random().Next(100000, 999999).ToString();
                }
            }
            else
            {
                // Email login -> always generate random OTP
                otp = new Random().Next(100000, 999999).ToString();
            }

            // Save OTP in DB
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SP_SaveCustomerOTP", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@LoginId", loginId);
                cmd.Parameters.AddWithValue("@OTP", otp);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // Email flow
            if (loginId.Contains("@"))
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("prudhviraj.glansa@gmail.com", "ujiajcwsczeghshr"),
                    EnableSsl = true,
                };
                smtpClient.Send("harish@glansa.com", loginId, "Your OTP Code", $"Your OTP is: {otp}");
            }
            else if (sendOtpToUser) // SMS flow only if not test number
            {
                string apiKey = "00aaa0bb-62dc-11f0-a562-0200cd936042";
                string templateName = "MycarbuddySMS";
                string senderId = "GLANSA";

                string apiUrl = $"https://2factor.in/API/R1?module=TRANS_SMS" +
                                $"&apikey={Uri.EscapeDataString(apiKey)}" +
                                $"&to={Uri.EscapeDataString(phoneNumber)}" +
                                $"&from={Uri.EscapeDataString(senderId)}" +
                                $"&templatename={Uri.EscapeDataString(templateName)}" +
                                $"&var1={Uri.EscapeDataString(phoneNumber)}" +
                                $"&var2={Uri.EscapeDataString(otp)}";

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    string result = await response.Content.ReadAsStringAsync();

                    var json = JsonDocument.Parse(result);
                    if (json.RootElement.GetProperty("Status").GetString() != "Success")
                        return StatusCode(500, new { Success = false, Message = "Failed to send SMS OTP", apiResponse = result });
                }
            }

            return Ok(new { Success = true, Message = "OTP sent successfully.", OTP = otp });
        }





        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] CustomerLoginRequest request)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            bool isValid = false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // 1. Check OTP via Stored Procedure
                SqlCommand cmd = new SqlCommand("SP_VerifyCustomerOTP", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@LoginId", request.LoginId);
                cmd.Parameters.AddWithValue("@OTP", request.OTP);
                conn.Open();
                var reader = cmd.ExecuteReader();
                string customerId = null;
                string customerName = null;
                string customerEmail = null;
                if (reader.Read())
                {
                    isValid = true;
                    customerId = reader["CustID"].ToString(); // Adjust field names as per your table
                    customerName = reader["FullName"].ToString();
                    customerEmail = reader["Email"].ToString();
                }
                reader.Close();

                if (isValid)
                {
                    // 2. Mark OTP as used
                    SqlCommand updateCmd = new SqlCommand("SP_MarkCustomerOTPUsed", conn);
                    updateCmd.CommandType = CommandType.StoredProcedure;
                    updateCmd.Parameters.AddWithValue("@LoginId", request.LoginId);
                    updateCmd.Parameters.AddWithValue("@OTP", request.OTP);
                    updateCmd.ExecuteNonQuery();

                    // 3. Insert DeviceToken
                    SqlCommand deviceCmd = new SqlCommand("SP_InsertDeviceToken", conn);
                    deviceCmd.CommandType = CommandType.StoredProcedure;
                    deviceCmd.Parameters.AddWithValue("@CustID", customerId);
                    deviceCmd.Parameters.AddWithValue("@DeviceToken", request.DeviceToken ?? (object)DBNull.Value);
                    deviceCmd.Parameters.AddWithValue("@DeviceType", request.DeviceId ?? (object)DBNull.Value);
                    // deviceCmd.Parameters.AddWithValue("@DeviceId", request.DeviceId ?? (object)DBNull.Value); // Optional
                    deviceCmd.ExecuteNonQuery();

                    // 4. Generate JWT token for customer
                    string token = _jwt.GenerateToken(customerId, "Customer");

                    return Ok(new
                    {
                        Success = true,
                        Message = "Login Success",
                        Token = token,
                        Name = customerName,
                        Email = customerEmail,
                        CustID = customerId


                    });
                }
                else
                {
                    return Unauthorized(new { Success = false, Message = "Invalid or expired OTP" });
                }
            }
        }

        [HttpPost("Techsend-otp")]
        public async Task<IActionResult> TechSendOtp([FromBody] TechLoginRequest request)
        {
            // 1. Generate OTP
            string otp = new Random().Next(100000, 999999).ToString();

            // 2. Save OTP via Stored Procedure
            string connectionString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_SaveOTP", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@OTP", otp);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // 3. Send OTP Email (configure SMTP as needed)
            //var smtpClient = new SmtpClient("smtp.gmail.com")
            //{
            //    Port = 587,
            //    Credentials = new NetworkCredential("prudhviraj.glansa@gmail.com", "ujiajcwsczeghshr"),
            //    EnableSsl = true,
            //};
            //smtpClient.Send("harish@glansa.com", request.Email, "Your OTP Code", $"Your OTP is: {otp}");

            //return Ok(new { Success = true, Message = "OTP sent to email." });

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.ToLower() != "string")
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("prudhviraj.glansa@gmail.com", "ujiajcwsczeghshr"),
                    EnableSsl = true,
                };
                smtpClient.Send("harish@glansa.com", request.Email, "Your OTP Code", $"Your OTP is: {otp}");
            }

            // Send SMS OTP
            else if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber.ToLower() != "string")
            {
                string apiKey = "00aaa0bb-62dc-11f0-a562-0200cd936042";
                string templateName = "MycarbuddySMS";
                string senderId = "GLANSA";

                string var1 = request.PhoneNumber.Trim();
                if (var1.Length == 10)
                    var1 = "91" + var1;
                else if (var1.Length != 12 || !var1.StartsWith("91"))
                    return BadRequest("Invalid phone number");

                string var2 = otp;

                // ✅ Use R1 endpoint
                string apiUrl = $"https://2factor.in/API/R1?module=TRANS_SMS" +
                                $"&apikey={Uri.EscapeDataString(apiKey)}" +
                                $"&to={Uri.EscapeDataString(var1)}" +
                                $"&from={Uri.EscapeDataString(senderId)}" +
                                $"&templatename={Uri.EscapeDataString(templateName)}" +
                                $"&var1={Uri.EscapeDataString(var1)}" +
                                $"&var2={Uri.EscapeDataString(var2)}";

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    string result = await response.Content.ReadAsStringAsync();

                    var json = JsonDocument.Parse(result);
                    if (json.RootElement.GetProperty("Status").GetString() != "Success")
                        return StatusCode(500, new { Success = false, Message = "Failed to send SMS OTP", apiResponse = result });

                    return Ok(new { Success = true, Message = "OTP sent successfully." });
                }
            }


            else
            {
                return BadRequest(new { Success = false, Message = "Please provide a valid Email or Phone Number." });
            }
            return Ok(new { Success = true, Message = "OTP sent successfully." });
        }


    }
}
