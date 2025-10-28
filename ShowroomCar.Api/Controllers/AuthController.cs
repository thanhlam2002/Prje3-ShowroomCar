using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using BCrypt.Net;
using ShowroomCar.Api.Services;

namespace ShowroomCar.Api.Controllers
{
    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        private readonly IJwtTokenService _jwt;

        public AuthController(ShowroomDbContext db, IJwtTokenService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == req.UsernameOrEmail || u.Email == req.UsernameOrEmail);

            // ✅ Sửa lỗi bool?
            if (user == null || user.Active == false)
                return Unauthorized("User not found or inactive.");

            // ✅ Sửa DbSet số ít
            var roles = await _db.Users
                .Where(u => u.UserId == user.UserId)
                .SelectMany(u => u.Roles.Select(r => r.Code))
                .ToListAsync();

            var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!ok)
                return Unauthorized("Invalid credentials.");

            var token = _jwt.CreateToken(user.UserId, user.Username, user.Email, roles, DateTime.UtcNow);

            return Ok(new LoginResponse
            {
                AccessToken = token,
                Username = user.Username,
                Roles = roles
            });
        }
    }
}
