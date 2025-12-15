using EmployeeAttendanceSystem.Server.Context;
using EmployeeAttendanceSystem.Server.Domain;
using EmployeeAttendanceSystem.Server.Extensions;
using EmployeeAttendanceSystem.Server.Filters;
using EmployeeAttendanceSystem.Server.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString);

    options.AddInterceptors(
        serviceProvider.GetRequiredService<AuditInterceptor>());
});

builder.Services.AddScoped<AuditInterceptor>();


builder.Services.AddIdentity<Employee, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthenticationServices(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; 
    });

builder.Services.AddControllers();
builder.Services.AddScoped<AuditActionFilter>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger'a "Bearer" (JWT) kimlik doðrulamasýný (authentication) tanýmla
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                      "Token'ýnýzý 'Bearer ' (boþluk) ile birlikte girin.\r\n" +
                      "Örnek: \"Bearer 12345abcdef\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Swagger'ýn bu güvenlik tanýmýný kullanmasýný zorunlu kýl
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var allowedOrigins = builder.Configuration.GetSection("allowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" }; 

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();