using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence;                // DbContext
using ShowroomCar.Infrastructure.Persistence.Entities;       // (nếu cần thực thể)
using ShowroomCar.Application;                               // Mapster mappings
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using ShowroomCar.Infrastructure.Auditing;                   // Audit interceptor
using ShowroomCar.Api.Services;                              // JwtTokenService (nếu dùng)

var builder = WebApplication.CreateBuilder(args);

// JSON: tránh vòng lặp & ẩn null
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// DI cơ bản
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditScope>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>(); // dùng cho AuthController

// DbContext + gắn Audit interceptor qua DI
builder.Services.AddDbContext<ShowroomDbContext>((sp, options) =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.UseMySql(conn, ServerVersion.AutoDetect(conn));
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});

// JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("ADMIN"));
    options.AddPolicy("RequireEmployee", p => p.RequireRole("EMPLOYEE", "ADMIN"));
});

// Mapster mapping
builder.Services.RegisterMappings();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pipeline
app.UseAuthentication();   // phải trước UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
