using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using loginAPI.Data;
using loginAPI.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authorization;

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
            // untuk mencari user yang ada didalam database
            var user = _context.Users.SingleOrDefault(u => u.Username == request.Username);
            if (user == null) 
            return BadRequest(new { message = "Username Doesn't Exists, Please Register" });

            // untuk memverifikasi password apakah sudah sesuai dengan yang ada di database
            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword("", user.Password, request.Password);
            if (result == PasswordVerificationResult.Failed) 
            return BadRequest(new { message = "Password Incorrect, Try again" });

            //JWT KEY
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "fallback-secret");
            
            // untuk menentukan masa berlaku token dan kredensial token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            // untuk generate token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // return token ke client jika berhasil login
            return Ok(new { token = tokenHandler.WriteToken(token), message = "Login successfully, Welcome"} );
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
                Password = hashedPassword,
                Role = request.Role ?? "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully" });
        }

        [Authorize]
        [HttpDelete("username")]
        public IActionResult Delete([FromBody] DeleteRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Username.ToString()))
                return BadRequest(new { message = "Username cannot be empty!" });

            var userId = int.Parse(userIdClaim);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return Unauthorized();

            // pastikan username yg dikirim adalah milik user login
           if (!string.Equals(user.Username, request.Username, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Username does not match logged-in user" });
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new
            {
                message = "User deleted successfully. Please logout."
            });
        }


        [Authorize]
        [HttpPut("password")]
        public IActionResult UpdatePassword([FromBody] UpdateRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var user = _context.Users.Find(userId);

            if (user == null)
                return Unauthorized();

            if (string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Password cannot be empty" });

            var hasher = new PasswordHasher<string>();
            user.Password = hasher.HashPassword("", request.Password);

            _context.SaveChanges();

            return Ok(new { message = "Password updated successfully" });
        }

      
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == request.Username);
            if (user == null)
                return BadRequest(new { message = "User Doesn't Exists, Please Register!!!" });

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpired = DateTime.UtcNow.AddMinutes(2);

            _context.SaveChanges();

            return Ok(new
            {
                message = "Token Reset Created", 
                resetToken = user.ResetToken
            });
        }

        [HttpPut("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = _context.Users.SingleOrDefault(u =>
                u.ResetToken == request.Token &&
                u.ResetTokenExpired > DateTime.UtcNow);

            if (user == null)
                return BadRequest(new { message = "Token invalid or Expired" });

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Password cannot be empty!" });

            var hasher = new PasswordHasher<string>();
            user.Password = hasher.HashPassword("", request.NewPassword);

            // invalidate token
            user.ResetToken = null;
            user.ResetTokenExpired = null;

            _context.SaveChanges();

            return Ok(new
            {
                message = "Reset Password Successfully"
            });
        }
    }


    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

}
