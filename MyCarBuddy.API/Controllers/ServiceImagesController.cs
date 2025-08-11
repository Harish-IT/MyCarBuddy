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
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;



namespace MyCarBuddy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceImagesController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceImagesController> _logger;
        private readonly IWebHostEnvironment _env;

        public ServiceImagesController(IConfiguration configuration, ILogger<ServiceImagesController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }


        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }




        [HttpPost("InsertServiceImages")]
        public async Task<IActionResult> InsertServiceImages([FromForm] ServiceImageModel serviceimages)
        {
            if (serviceimages.ImageURL1 == null || serviceimages.ImageURL1.Count == 0)
                return BadRequest("No files uploaded.");

            string uploadFolder = Path.Combine(_env.WebRootPath, "Images", "ServiceImages");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                foreach (var file in serviceimages.ImageURL1)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = $"{GetRandomAlphanumericString(8)}_{Path.GetFileName(file.FileName)}";
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        string imageUrl = $"ServiceImages/{uniqueFileName}";

                        using (SqlCommand cmd = new SqlCommand("sp_InsertServiceImages", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@BookingID", serviceimages.BookingID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ImageURL", imageUrl);
                            cmd.Parameters.AddWithValue("@UploadBy", serviceimages.UploadedBy ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TechID", serviceimages.TechID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ImageUploadType", serviceimages.ImageUploadType ?? "");
                            cmd.Parameters.AddWithValue("@ImageType", serviceimages.ImagesType ?? "");

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

            return Ok(new { message = "Images uploaded successfully" });
        }
    }
}
