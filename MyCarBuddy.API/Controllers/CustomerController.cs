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
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomerController> _logger;
        private readonly IWebHostEnvironment _env;

        public CustomerController(IConfiguration configuration, ILogger<CustomerController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env=env;
        }

        #region InsertCustomer


        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromForm] string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return BadRequest(new { Success = false, Message = "Phone number is required" });

                if (phoneNumber.Length == 10)
                    phoneNumber = "91" + phoneNumber;
                else if (!phoneNumber.StartsWith("91") || phoneNumber.Length != 12)
                    return BadRequest(new { Success = false, Message = "Invalid phone number" });

                string otp = new Random().Next(100000, 999999).ToString();

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_SaveCustomerOTPTemp", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@LoginId", phoneNumber);
                        cmd.Parameters.AddWithValue("@OTP", otp);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                string apiKey = "00aaa0bb-62dc-11f0-a562-0200cd936042";
                string senderId = "GLANSA";
                string templateName = "MycarbuddySMS";
                string apiUrl = $"https://2factor.in/API/R1?module=TRANS_SMS" +
                                $"&apikey={apiKey}" +
                                $"&to={phoneNumber}" +
                                $"&from={senderId}" +
                                $"&templatename={templateName}" +
                                $"&var1={phoneNumber}" +
                                $"&var2={otp}";

                using HttpClient client = new HttpClient();
                var response = await client.GetAsync(apiUrl);
                var result = await response.Content.ReadAsStringAsync();

                using var json = JsonDocument.Parse(result);
                var status = json.RootElement.GetProperty("Status").GetString();

                if (status != "Success")
                    return StatusCode(500, new { Success = false, Message = "OTP send failed", APIResponse = result });

                return Ok(new { Success = true, Message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }



        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromForm] string phoneNumber, [FromForm] string otp)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(otp))
                return BadRequest(new { Success = false, Message = "Phone number and OTP required" });

            using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            using SqlCommand cmd = new SqlCommand("SP_VerifyCustomerOTPTemp", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@LoginId", phoneNumber);
            cmd.Parameters.AddWithValue("@OTP", otp);

            var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Unauthorized(new { Success = false, Message = "Invalid or expired OTP" });

            reader.Close();

            using SqlCommand markCmd = new SqlCommand("SP_MarkCustomerOTPUsedTemp", conn);
            markCmd.CommandType = CommandType.StoredProcedure;
            markCmd.Parameters.AddWithValue("@LoginId", phoneNumber);
            markCmd.Parameters.AddWithValue("@OTP", otp);
            markCmd.ExecuteNonQuery();

            return Ok(new { Success = true, Message = "OTP verified. You can proceed to register." });
        }


        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromForm] CustomerModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.Email))
                    return BadRequest(new { Success = false, Message = "Required fields missing" });

                string profileImagePath = null;
                if (model.ProfileImageFile != null)
                {
                    var folder = Path.Combine(_env.WebRootPath, "Images", "Customer");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    var fileName = Path.GetFileNameWithoutExtension(model.ProfileImageFile.FileName);
                    var ext = Path.GetExtension(model.ProfileImageFile.FileName);
                    string unique = $"{fileName}_{Guid.NewGuid():N}{ext}";
                    string fullPath = Path.Combine(folder, unique);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await model.ProfileImageFile.CopyToAsync(stream);

                    profileImagePath = Path.Combine("Customer", unique).Replace("\\", "/");
                }

                int newCustId;
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCustomerDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                        cmd.Parameters.AddWithValue("@AlternateNumber", (object?)model.AlternateNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@ProfileImage", (object?)profileImagePath ?? DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        newCustId = Convert.ToInt32(result);
                    }
                }

                return Ok(new { Success = true, Message = "Customer registered", CustID = newCustId });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        #endregion


        [HttpPost("update-customer")]
        public async Task<IActionResult> UpdateCustomer([FromForm] CustomerModel model)
        {
            try
            {
                if (model.CustID <= 0)
                    return BadRequest(new { Success = false, Message = "Invalid customer ID" });

                if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.Email))
                    return BadRequest(new { Success = false, Message = "Required fields missing" });

                string profileImagePath = null;
                if (model.ProfileImageFile != null)
                {
                    var folder = Path.Combine(_env.WebRootPath, "Images", "Customer");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    var fileName = Path.GetFileNameWithoutExtension(model.ProfileImageFile.FileName);
                    var ext = Path.GetExtension(model.ProfileImageFile.FileName);
                    string unique = $"{fileName}_{Guid.NewGuid():N}{ext}";
                    string fullPath = Path.Combine(folder, unique);

                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await model.ProfileImageFile.CopyToAsync(stream);

                    profileImagePath = Path.Combine("Customer", unique).Replace("\\", "/");
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateCustomerDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CustID", model.CustID);
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                        cmd.Parameters.AddWithValue("@AlternateNumber", (object?)model.AlternateNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@ProfileImage", (object?)profileImagePath ?? DBNull.Value);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0)
                            return Ok(new { Success = true, Message = "Customer updated successfully" });
                        else
                            return NotFound(new { Success = false, Message = "Customer not found or no changes made" });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        #region Get Customer List

        [HttpGet]

        public IActionResult GetListCustomers()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListCustomerDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    var Data = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            dict[col.ColumnName] = row[col];
                        }
                        Data.Add(dict);
                    }
                    return Ok(new { status = true, Data });
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Customers.", error = ex.Message });

            }
        }

        #endregion


        #region GetCustomersById


        [HttpGet("Id")]

        public IActionResult GetCustomersById(int Id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetCustomerDetailsByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CustID", Id);
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
                    return NotFound(new { message = "Customers not found" });
                }
                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    Data.Add(dict);
                }
                return Ok(Data.Count == 1 ? Data[0] : Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Customers.", error = ex.Message });

            }

        }

        #endregion



    }
}
