using Braintree;
using Microsoft.AspNetCore.Authorization;
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

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(IConfiguration configuration, ILogger<CategoryController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        #region Insert Category

        [HttpPost]

        public IActionResult Category(CategoryModel category)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_InsertServiceCategory",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                        cmd.Parameters.AddWithValue("@Description", category.Description);
                        cmd.Parameters.AddWithValue("@CreatedBy", category.CreatedBy);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        if(rows>0)
                        {
                            return Ok(new { status = true, message = "Category inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "category is not inserted." });

                        }

                    }
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }
        }

        #endregion
    }
}
