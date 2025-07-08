using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Services;
using System.Data;

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
                    return Unauthorized(new { Success = false, Message = "Invalid login Details" });
                }
            }
        }
    }
}
