using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using Org.BouncyCastle.Asn1.Cms;
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
    public class SeometaController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<SeometaController> _logger;
        private readonly IWebHostEnvironment _env;

        public SeometaController(IConfiguration configuration, ILogger<SeometaController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }
        [HttpPost]

        public IActionResult SeoMeta(seometaModel model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_Insertseometa", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@page_slug", (object)model.page_slug ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_title", (object)model.seo_title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_description", (object)model.seo_description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_keywords", (object)model.seo_keywords ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@content", (object)model.content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_score", (object)model.seo_score ?? DBNull.Value);
                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true, message = "Seo record is inserted Succesfully.." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Record is not inserted.." });
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
        [HttpPut]
        public IActionResult UpdateSeometa(seometaModel model)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("Sp_UpdateSeometa",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@seo_id", model.seo_id);
                        cmd.Parameters.AddWithValue("@page_slug", (object)model.page_slug ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_title", (object)model.seo_title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_description", (object)model.seo_description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_keywords", (object)model.seo_keywords ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@content", (object)model.content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@seo_score", (object)model.seo_score ?? DBNull.Value);
                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true,message="Record is Updated.." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Record is not updated.." });
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

        #region GetSeoList

        [HttpGet]

        public IActionResult GetSeoList()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetListSeometa", conn))
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
                        return NotFound(new { message = "seo meta list not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the seo meta list .", error = ex.Message });

            }
        }

        #endregion


        #region GetSeoListById


        [HttpGet("seo_id")]

        public IActionResult GetSeoListById(int seoid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetListSeometabyId", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@seo_id", seoid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "seo meta list  not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the seo meta list .", error = ex.Message });

            }

        }

        #endregion


        #region GetSeoListByPageSlug


        [HttpGet("page_slug")]

        public IActionResult GetSeoListByPageSlug(string page_slug)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetListByPageSlug", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@page_slug", page_slug);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "seo meta list  not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the seo meta list .", error = ex.Message });

            }

        }

        #endregion
    }
}
