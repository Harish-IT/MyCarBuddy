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
    public class rolehaspermissionsController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<rolehaspermissionsController> _logger;
        private readonly IWebHostEnvironment _env;

        public rolehaspermissionsController(IConfiguration configuration, ILogger<rolehaspermissionsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpPost]
        public IActionResult rolehaspermissions(rolehaspermissionsModel userrole)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_Insertrole_has_permissions", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@permission_id", userrole.permission_id);
                        cmd.Parameters.AddWithValue("@role_id", userrole.role_id);

                        conn.Open();
                        int row = cmd.ExecuteNonQuery();
                        if (row > 0)
                        {
                            return Ok(new { status = true, message = "role has permissions is Inserted.." });
                        }
                        else
                        {
                            return NotFound(new { status = false, message = "role has permissions is not inserted.." });
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
        public IActionResult Updaterolehaspermissions(Updaterolehaspermissions role)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Updaterole_has_permissions", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id", role.id);
                        cmd.Parameters.AddWithValue("@permission_id", role.permission_id);
                        cmd.Parameters.AddWithValue("@role_id", role.role_id);

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

        #region GetListrolehaspermissions

        [HttpGet]

        public IActionResult GetListrolehaspermissions()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_GetListrole_has_permissions", conn))
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
                        return NotFound(new { message = "role has permissions not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the role has permissions.", error = ex.Message });

            }
        }

        #endregion


        #region Sp_Getrole_has_permissionsById


        [HttpGet("id")]

        public IActionResult GetPermissionById(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_Getrole_has_permissionsById", conn))
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
                        return NotFound(new { message = "role has permissions not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the role has permissions.", error = ex.Message });

            }

        }

        #endregion
    }
}
