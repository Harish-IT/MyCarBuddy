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

        [HttpPost("InsertCustomer")]

        public async Task<IActionResult> InsertCustomer([FromForm] CustomerModel model)
        {
            try
            {
                var missingFields = new List<string>();
                if (string.IsNullOrWhiteSpace(model.FullName))
                    missingFields.Add("FullName");
                if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                    missingFields.Add("PhoneNumber");
                if (string.IsNullOrWhiteSpace(model.Email))
                    missingFields.Add("Email");

                if (missingFields.Any())
                {
                    return BadRequest($"The following fields are required: {string.Join(", ", missingFields)}");
                }

                string profileImagePath = null;
                if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
                {
                   // var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(),  "Images", "Customer");
                    var imagesFolder = Path.Combine(_env.WebRootPath, "Images", "Customer");
                    if (!Directory.Exists(imagesFolder))
                        Directory.CreateDirectory(imagesFolder);

                    var originalFileName = Path.GetFileName(model.ProfileImageFile.FileName);
                    var filePath = Path.Combine(imagesFolder, originalFileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                        filePath = Path.Combine(imagesFolder, uniqueFileName);
                        originalFileName = uniqueFileName;
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImageFile.CopyToAsync(stream);
                    }

                    profileImagePath = Path.Combine("Customer", originalFileName).Replace("\\", "/");
                }
                int newCustId;
                using (SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_InsertCustomerDetails",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", model.PhoneNumber);
                        cmd.Parameters.AddWithValue("@AlternateNumber", (object?)model.AlternateNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@ProfileImage", (object?)profileImagePath ?? DBNull.Value);
                       // cmd.Parameters.AddWithValue("@StateID", model.StateID);
                      //  cmd.Parameters.AddWithValue("@CityID", model.CityID);

                        await conn.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        newCustId = Convert.ToInt32(result);
                    }
                }
                return Ok(new { status = true , message="Customer record is inserted successfully...", CustID = newCustId });

            }
            catch(Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status=false, message = "An error occurred while inserting the record.", error = ex.Message });


            }


        }

        #endregion


    }
}
