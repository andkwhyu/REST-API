using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using loginAPI.Data;
using loginAPI.Model;
using Microsoft.AspNetCore.Http.HttpResults;

namespace loginAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == request.Username);
            if (user == null) 
            return Unauthorized();

            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword("", user.Password, request.Password);

            if (result == PasswordVerificationResult.Failed) 
            return Unauthorized();

            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "fallback-secret");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { token = tokenHandler.WriteToken(token) });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // untuk cek apakah username sudah ada di database
            if (_context.Users.Any(u => u.Username == request.Username))
                return BadRequest(new { message = "Username already exists" });


            //untuk hash password sebelum di simpan kedalam database
            var hasher = new PasswordHasher<string>();
            var hashedPassword = hasher.HashPassword("", request.Password);
            
            var user = new User
            {
                Username = request.Username,
                Password = request.Password,
                Role = request.Role ?? "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

}
