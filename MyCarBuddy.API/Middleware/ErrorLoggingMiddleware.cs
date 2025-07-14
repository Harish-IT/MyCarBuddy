using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context); // Proceed to next middleware
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");

                // Save to DB
                LogErrorToDatabase(ex, context);

                // Return generic error response
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { error = "Internal server error", message = ex.Message });
                await context.Response.WriteAsync(result);
            }
        }

        private void LogErrorToDatabase(Exception ex, HttpContext context)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_LogError", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Message", ex.Message);
                        cmd.Parameters.AddWithValue("@Stack", ex.StackTrace ?? "");
                        cmd.Parameters.AddWithValue("@Path", context.Request.Path);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error while logging to the database");
            }

        }

    }
}
