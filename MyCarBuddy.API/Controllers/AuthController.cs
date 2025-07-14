using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Services;
using System;
using System.Data;
using System.Net;
using System.Net.Mail;

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

        [HttpPost("technician-login")]
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


        [HttpPost("send-otp")]
        public IActionResult SendOtp([FromBody] CustomerLoginRequest request)
        {
            // 1. Generate OTP
            string otp = new Random().Next(100000, 999999).ToString();

            // 2. Save OTP via Stored Procedure
            string connectionString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_SaveCustomerOTP", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", request.Email);
                cmd.Parameters.AddWithValue("@OTP", otp);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // 3. Send OTP Email (configure SMTP as needed)
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("prudhviraj.glansa@gmail.com", "ujiajcwsczeghshr"),
                EnableSsl = true,
            };
            smtpClient.Send("harish@glansa.com", request.Email, "Your OTP Code", $"Your OTP is: {otp}");

            return Ok(new { Success = true, Message = "OTP sent to email." });
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




    }
}
