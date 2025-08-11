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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyCarBuddy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AfterServiceLeadsController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<AfterServiceLeadsController> _logger;
        private readonly IWebHostEnvironment _env;

        public AfterServiceLeadsController(IConfiguration configuration, ILogger<AfterServiceLeadsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpPost]
        public IActionResult InsertReason([FromBody] AfterServiceLeadsModel leads)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_InsertReason", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Reason", leads.Reason);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { status = true, message = "Reason inserted successfully." });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the Reason.", error = ex.Message });
            }
        }

        #region GetListReasons

        [HttpGet]
        public IActionResult GetListReasons()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllReasons", conn))
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
                        return NotFound(new { message = "Reasons not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Reasons.", error = ex.Message });

            }
        }

        #endregion

        #region GetReasonsListById

        [HttpGet("id")]

        public IActionResult GetReasonsListById(int Id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllReasonsById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID", Id);
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
                    return NotFound(new { message = "Reasons not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Reasons.", error = ex.Message });

            }

        }

        #endregion
        [HttpPost("InsertServiceLeads")]
        public IActionResult InsertServiceLeads([FromBody] List<ServiceLeadRequest> serviceLeadsList)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    foreach (var lead in serviceLeadsList)
                    {
                        foreach (var package in lead.Packages)
                        {
                            foreach (var include in package.Includes)
                            {
                                using (SqlCommand cmd = new SqlCommand("Sp_InsertServiceLead", conn))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@BookingID", lead.BookingID);
                                    cmd.Parameters.AddWithValue("@PackageID", package.PackageID);
                                    cmd.Parameters.AddWithValue("@IncludeID", include.IncludeID);
                                    cmd.Parameters.AddWithValue("@IncludeName", include.IncludeName ?? "");
                                    cmd.Parameters.AddWithValue("@Status", include.Status);
                                    cmd.Parameters.AddWithValue("@Reasons", lead.Reasons ?? "");
                                    cmd.Parameters.AddWithValue("@TechID", lead.TechID ?? (object)DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }

                return Ok(new { status = true, message = "Service leads inserted successfully." });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "Error inserting service leads", error = ex.Message });
            }
        }


    }
}
