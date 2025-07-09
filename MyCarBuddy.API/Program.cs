using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyCarBuddy.API.Services;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Detect environment update
var isDevelopment = builder.Environment.IsDevelopment();

// Conditional URL binding
if (isDevelopment)
{
    builder.WebHost.UseUrls("https://localhost:5001");
    Console.WriteLine("Binding to https://localhost:5000 for local development.");
}
else
{
    builder.WebHost.UseUrls("https://0.0.0.0:443");
    Console.WriteLine("Binding to https://0.0.0.0:443 for server/production.");
}

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS policies
if (isDevelopment)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    });
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.WithOrigins(
                "https://mycarbuddy.glansadesigns.com",
                "https://api.mycarsbuddy.com",
                "http://localhost:5173" // <-- Add this line
            )
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    });
}

// Swagger setup (enabled only in development)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MyCarBuddy API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// JWT Setup
builder.Services.AddScoped<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger only in development
if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyCarBuddy API V1");
    });
    Console.WriteLine("Swagger enabled at /swagger");
}
else
{
    // Uncomment below if you want Swagger in production (not recommended)
    // app.UseSwagger();
    // app.UseSwaggerUI(options =>
    // {
    //     options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyCarBuddy API V1");
    // });
}

// Middleware
app.UseMiddleware<MyCarBuddy.API.Middleware.ErrorLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();