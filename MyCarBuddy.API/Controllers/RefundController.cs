using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class RefundController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefundController> _logger;
    private readonly IWebHostEnvironment _env;

    public RefundController(IConfiguration configuration, ILogger<RefundController> logger, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    [HttpPost("Refund")]
    public async Task<IActionResult> CreateRefund([FromBody] RefundRequest request)
    {
        if (string.IsNullOrEmpty(request.PaymentId) || request.Amount <= 0)
            return BadRequest("Invalid PaymentId or Amount");

        string key = _configuration["Razorpay:Key"];
        string secret = _configuration["Razorpay:Secret"];

        var url = $"https://api.razorpay.com/v1/payments/{request.PaymentId}/refund";

        var refundData = new
        {
            amount = request.Amount * 100 // Razorpay works in paise
        };

        var jsonData = JsonConvert.SerializeObject(refundData);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        using (var client = new HttpClient())
        {
            var byteArray = Encoding.ASCII.GetBytes($"{key}:{secret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var response = await client.PostAsync(url, content);
            var responseData = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Deserialize response into dynamic object
                var refundResponse = JsonConvert.DeserializeObject<dynamic>(responseData);

                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_InsertRefund", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@BookingID", request.BookingID);
                        command.Parameters.AddWithValue("@Amount", ((decimal)refundResponse.amount) / 100);
                        command.Parameters.AddWithValue("@RefundMethod", "Razorpay"); // you can set dynamic if needed
                        command.Parameters.AddWithValue("@TransactionRef",request.PaymentId);
                        command.Parameters.AddWithValue("@Status", (string)refundResponse.status);

                        await command.ExecuteNonQueryAsync();
                    }
                }


                // Return clean custom response
                return Ok(new
                {
                    Success = true,
                    RefundId = refundResponse.id,
                    PaymentId = refundResponse.payment_id,
                    Amount = ((decimal)refundResponse.amount) / 100, // back to INR
                    Currency = refundResponse.currency,
                    Status = refundResponse.status,
                    CreatedAt = refundResponse.created_at
                });
            }
            else
            {
                _logger.LogError("Refund failed: " + responseData);
                return StatusCode((int)response.StatusCode, new
                {
                    Success = false,
                    Message = responseData
                });
            }
        }
    }

}

public class RefundRequest
{
    public int BookingID {  get; set; }
    public string PaymentId { get; set; }
    public int Amount { get; set; } // in INR (not paise)
}
