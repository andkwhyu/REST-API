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


        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            //untuk mengecek apakah id ada di dalam database
            var ID = _context.Users.Find(id);
            if (ID == null)
                return NotFound(new{message = "User Not Found"});

            _context.Users.Remove(ID);
            _context.SaveChanges();

            return Ok(new{message = "User Deleted Successfully"});
        }


        [HttpPut("{id}/password")]
        public IActionResult UpdatePassword(int id, [FromBody] UpdateRequest request)
        {
            //untuk mengecek apakah id ada di dalam database
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            //untuk validasi kalau password tidak boleh kosong
            if (string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Password cannot be empty" });

            //untuk hash password sebelum di simpan kedalam database
            var hasher = new PasswordHasher<string>();
            var hashedPassword = hasher.HashPassword("", request.Password);

            user.Password = hashedPassword;

            _context.Users.Update(user);
            _context.SaveChanges();

            return Ok(new { message = "Password updated successfully" });
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

}
