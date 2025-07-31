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
    public class SkillsController : ControllerBase
    {
        #region IConfiguration

        private readonly IConfiguration _configuration;
        private readonly ILogger<SkillsController> _logger;
        private readonly IWebHostEnvironment _env;


        public SkillsController(IConfiguration configuration, ILogger<SkillsController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion

        #region InsertSkills

        [HttpPost]

        public IActionResult InsertSkills(SkillsModel skill)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertSkill", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SkillName", skill.SkillName);
                        cmd.Parameters.AddWithValue("@Description", skill.Description);
                        cmd.Parameters.AddWithValue("@CreatedBy", skill.CreatedBy);
                        cmd.Parameters.AddWithValue("@IsActive", skill.IsActive);
                        
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Skill inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Insertion failed." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while inserting the record.", error = ex.Message });

            }
        }

        #endregion

        #region UpdateSkill

        [HttpPut]
        public IActionResult UpdateSkill(SkillsModel skill)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateSkill", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SkillID", skill.SkillID);
                        cmd.Parameters.AddWithValue("@SkillName", skill.SkillName);
                        cmd.Parameters.AddWithValue("@Description", skill.Description);
                        cmd.Parameters.AddWithValue("@ModifiedBy", skill.ModifiedBy);
                        cmd.Parameters.AddWithValue("@IsActive", skill.IsActive);



                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Skill updated successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Skill not updated." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while updating the record.", error = ex.Message });
            }
        }

        #endregion

        #region DeleteSkill

        [HttpDelete("skillid")]

        public IActionResult DeleteSkill(int skillid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteSkill", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SkillID", skillid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int resultCode = Convert.ToInt32(reader["ResultCode"]);
                                string message = reader["Message"].ToString();

                                if (resultCode == 1)
                                    return Ok(new { message });
                                else if (resultCode == -1)
                                    return BadRequest(new { message });
                                else
                                    return NotFound(new { message });
                            }
                        }
                        conn.Close();
                    }
                }
                return StatusCode(500, new { message = "Unknown error occurred." });
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while deleting the record.", error = ex.Message });

            }
        }

        #endregion

        #region GetAllSKills

        [HttpGet]

        public IActionResult GetAllSkills()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetAllSkills", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    var jsonResult = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            dict[col.ColumnName] = row[col];
                        }
                        jsonResult.Add(dict);
                    }
                    return Ok(jsonResult);
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the skills.", error = ex.Message });

            }
        }

        #endregion

        #region GetSkillById

        [HttpGet("skillid")]

        public IActionResult GetDealerById(int skillid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetSkillByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SkillID", skillid);
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
                    return NotFound(new { message = "Skills not found" });
                }
                var jsonResult = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    jsonResult.Add(dict);
                }
                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the skills.", error = ex.Message });

            }

        }

        #endregion

    }
}
