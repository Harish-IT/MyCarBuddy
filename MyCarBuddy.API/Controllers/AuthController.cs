using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Services;
using System;
using System.Data;
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

        public AuthController(IConfiguration config, JwtService jwt)
        {
            _config = config;
            _jwt = jwt;
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

            string otp = new Random().Next(100000, 999999).ToString();

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

            // Email or SMS based on format
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
            else
            {
                string apiKey = "00aaa0bb-62dc-11f0-a562-0200cd936042";
                string templateName = "MycarbuddySMS";
                string senderId = "GLANSA";

                if (loginId.Length == 10)
                    loginId = "91" + loginId;
                else if (loginId.Length != 12 || !loginId.StartsWith("91"))
                    return BadRequest("Invalid phone number");

                string apiUrl = $"https://2factor.in/API/R1?module=TRANS_SMS" +
                                $"&apikey={Uri.EscapeDataString(apiKey)}" +
                                $"&to={Uri.EscapeDataString(loginId)}" +
                                $"&from={Uri.EscapeDataString(senderId)}" +
                                $"&templatename={Uri.EscapeDataString(templateName)}" +
                                $"&var1={Uri.EscapeDataString(loginId)}" +
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

            return Ok(new { Success = true, Message = "OTP sent successfully." });
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
                cmd.Parameters.AddWithValue("@Email", request.Email);
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
                    updateCmd.Parameters.AddWithValue("@Email", request.Email);
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
                        Email = customerEmail
                    });
                }
                else
                {
                    return Unauthorized(new { Success = false, Message = "Invalid or expired OTP" });
                }
            }
        }

        [HttpPost("Techsend-otp")]
        public async Task<IActionResult> TechSendOtp([FromBody] CustomerLoginRequest request)
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
