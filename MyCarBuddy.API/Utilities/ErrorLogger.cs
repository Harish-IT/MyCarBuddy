using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;

namespace MyCarBuddy.API.Utilities
{
    public static class ErrorLogger
    {
        public static void LogToDatabase(Exception ex, HttpContext context, IConfiguration configuration, ILogger logger)
        {
            try
            {
                string connectionString = configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_LogError", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Message", ex.Message);
                        cmd.Parameters.AddWithValue("@Stack", ex.StackTrace ?? "");
                        cmd.Parameters.AddWithValue("@Path", context.Request.Path.ToString());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception dbEx)
            {
                logger.LogError(dbEx, "Error while logging to the database");
            }
        }
    }
}
