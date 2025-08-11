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
        #region configuration
        private readonly IConfiguration _configuration;
        private readonly ILogger<CouponsController> _logger;
        private readonly IWebHostEnvironment _env;

        public CouponsController(IConfiguration configuration, ILogger<CouponsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion

        #region InsertCoupon

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
                        cmd.Parameters.AddWithValue("@MaxDisAmount", coupons.MaxDisAmount ?? 0);
                        cmd.Parameters.AddWithValue("@MinBookingAmount", coupons.MinBookingAmount ?? 0);


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

        #endregion

        #region UpdateCoupon

        [HttpPut]
        public IActionResult UpdateCoupon([FromBody] UpdateCoupon coupons)
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
                        cmd.Parameters.AddWithValue("@MaxDisAmount", coupons.MaxDisAmount.HasValue ? coupons.MaxDisAmount.Value : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MinBookingAmount", coupons.MinBookingAmount.HasValue ? coupons.MinBookingAmount.Value : (object)DBNull.Value);
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

        #endregion

        #region GetListCoupons

        [HttpGet]
        public IActionResult GetListCoupons()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllCoupons", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }



                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "Coupons not found" });
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

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Coupons.", error = ex.Message });

            }
        }

        #endregion


        #region GetCouponsListById

        [HttpGet("couponid")]

        public IActionResult GetCouponsListById(int couponid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListCouponsById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CouponID", couponid);
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
                    return NotFound(new { message = "Coupons not found" });
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
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Coupons.", error = ex.Message });

            }

        }

        #endregion

    }
}
