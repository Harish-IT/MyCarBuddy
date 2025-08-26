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
    public class RolesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RolesController> _logger;
        private readonly IWebHostEnvironment _env;

        public RolesController(IConfiguration configuration, ILogger<RolesController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }
        [HttpPost]
        public IActionResult Role(RolesModel role)
        {
            try
            {
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("Sp_InsertRoles",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@name", role.name);
                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true, message = "Role is Inserted.." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Role is not inserted.." });
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
        public IActionResult UpdateRole(UpdateRole role)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_UpdateRoles", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id", role.id);
                        cmd.Parameters.AddWithValue("@name", role.name);
                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true, message = "Record is Updated.." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "Record is not updated" });
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


        #region GetListRoles

        [HttpGet]

        public IActionResult GetListRoles()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetListRoles", conn))
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
                        return NotFound(new { message = "Roles not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Roles.", error = ex.Message });

            }
        }

        #endregion


        #region GetRolesById


        [HttpGet("id")]

        public IActionResult GetRolesById(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetListRolesById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id", id);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    if (dt.Rows.Count == 0)
                    {
                        return NotFound(new { message = "Roles not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Roles.", error = ex.Message });

            }

        }

        #endregion

    }
}
