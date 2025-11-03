using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application;                    // Mapster mappings
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using ShowroomCar.Infrastructure.Auditing;        // Audit interceptor
using ShowroomCar.Api.Services;                   // JwtTokenService
using Microsoft.OpenApi.Models;                   // ✅ Swagger JWT
using ShowroomCar.Api.Middleware;                 // ✅ Error middleware (nếu bạn đã thêm file)

var builder = WebApplication.CreateBuilder(args);

// JSON
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// CORS (bật nếu có FE khác domain)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// DI cơ bản
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditScope>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// DbContext + Audit interceptor
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

// Mapster
builder.Services.RegisterMappings();

// Swagger + Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Hiển thị DateOnly là string 'date'
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập: Bearer {token}"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();                         // ✅ trước auth
app.UseGlobalExceptionHandling();      // ✅ nếu bạn đã thêm middleware ErrorHandlingMiddleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
