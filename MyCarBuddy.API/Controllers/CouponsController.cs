using Braintree;
using GSF;
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
using System.Data.Entity.Infrastructure;
using System.IO;
namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CouponsController> _logger;
        private readonly IWebHostEnvironment _env;

        public CouponsController(IConfiguration configuration, ILogger<CouponsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }
        [HttpPost]
        public IActionResult InsertCoupon([FromBody] CouponsModel coupons)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCoupons", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Code", coupons.Code ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", coupons.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DiscountType", coupons.DiscountType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DiscountValue", coupons.DiscountValue);
                        cmd.Parameters.AddWithValue("@ValidFrom", coupons.ValidFrom);
                        cmd.Parameters.AddWithValue("@ValidTill", coupons.ValidTill);
                        cmd.Parameters.AddWithValue("@MaxUsagePerUser", coupons.MaxUsagePerUser ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", coupons.IsActive ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy", coupons.CreatedBy ?? (object)DBNull.Value);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Coupon inserted successfully." });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the coupon.", error = ex.Message });
            }
        }


        [HttpPut]
        public IActionResult UpdateCoupon([FromBody] CouponsModel coupons)
        {

            try
            {
               
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_UpdateCoupons", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@CouponID", coupons.CouponID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Code", coupons.Code ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Description", coupons.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DiscountType", coupons.DiscountType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DiscountValue", coupons.DiscountValue);
                            cmd.Parameters.AddWithValue("@ValidFrom", coupons.ValidFrom);
                            cmd.Parameters.AddWithValue("@ValidTill", coupons.ValidTill);
                            cmd.Parameters.AddWithValue("@MaxUsagePerUser", coupons.MaxUsagePerUser ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsActive", coupons.IsActive ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ModifiedBy", coupons.ModifiedBy ?? (object)DBNull.Value);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }

                    return Ok(new { message = "Coupon updated successfully." });
                
               
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while updating the coupon.", error = ex.Message });
            }
        }


    }
}
