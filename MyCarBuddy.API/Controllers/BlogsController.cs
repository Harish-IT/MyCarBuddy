using Braintree;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace MyCarBuddy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<BlogsController> _logger;
        private readonly IWebHostEnvironment _env;


        public BlogsController(IConfiguration configuration, ILogger<BlogsController> logger, IWebHostEnvironment env)
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
        

        [HttpPost("InsertBlog")]
        public async Task<ActionResult> Blogs([FromForm] BlogsModel blog)
        {
            string thumbnailPath = string.Empty;
            if (blog.Thumbnai1 != null && blog.Thumbnai1.Length > 0)
            {
                var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "Blogs");
                if (!Directory.Exists(thumbFolderPath))
                    Directory.CreateDirectory(thumbFolderPath);

                var originalThumbFileName = Path.GetFileNameWithoutExtension(blog.Thumbnai1.FileName);
                var originalFileNameWithoutSpaces = originalThumbFileName.Replace(" ", "");

                var thumbFileExt = Path.GetExtension(blog.Thumbnai1.FileName);
                var randomString = GetRandomAlphanumericString(8); // 8-character alphanumeric
                var thumbFileName = $"{originalFileNameWithoutSpaces}_{randomString}{thumbFileExt}";
                var thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);

                int counter = 1;
                while (System.IO.File.Exists(thumbFullPath))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(thumbFileName);
                    var fileExt = Path.GetExtension(thumbFileName);
                    thumbFileName = $"{fileNameWithoutExt}_{counter}{fileExt}";
                    thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);
                    counter++;
                }

                using (var stream = new FileStream(thumbFullPath, FileMode.Create))
                {
                    await blog.Thumbnai1.CopyToAsync(stream);
                }

                thumbnailPath = Path.Combine("Blogs", thumbFileName).Replace("\\", "/");
            }
            using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using(SqlCommand cmd=new SqlCommand("Sp_InsertBlogs",conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PostTitle", (object)blog.PostTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PostCategory", (object)blog.PostCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PostDescription", (object)blog.PostDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Thumbnail", (object)thumbnailPath ?? DBNull.Value);
                    conn.Open();
                    int row = cmd.ExecuteNonQuery();
                    if(row > 0)
                    {
                        return Ok(new { status = true, message = "Blog inserted sucessfully..." });
                    }
                    else
                    {
                        return BadRequest(new { status = false, message = "Blog not inserted.." });
                    }


                }
            }





        }





    }
}
