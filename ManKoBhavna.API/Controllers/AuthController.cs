using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ManKoBhavna.API.Data;
using ManKoBhavna.API.Models;
using ManKoBhavna.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ManKoBhavna.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext db, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Error = "All fields are required." });
            }

            if (!request.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Error = "Only Gmail addresses (@gmail.com) are allowed." });
            }

            if (!request.Password.Any(char.IsUpper))
            {
                return BadRequest(new { Error = "Password must contain at least one uppercase letter." });
            }

            if (!request.Password.Any(c => !char.IsLetterOrDigit(c)))
            {
                return BadRequest(new { Error = "Password must contain at least one special character." });
            }

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);
            if (existingUser != null)
            {
                return BadRequest(new { Error = "Username or Email already exists." });
            }

            var user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = HashPassword(request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Send welcome email in the background to not block registration API response
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; background-color: #f0f2f5; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f0f2f5; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""max-width: 600px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08);"">
                    
                    <!-- Header with Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 48px 40px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: 700; color: #ffffff; letter-spacing: -0.5px;"">✨ ManKoBhavna</h1>
                            <p style=""margin: 8px 0 0 0; font-size: 14px; color: rgba(255,255,255,0.85); letter-spacing: 1px; text-transform: uppercase;"">Your Personal Journal</p>
                        </td>
                    </tr>

                    <!-- Welcome Message -->
                    <tr>
                        <td style=""padding: 48px 40px 24px 40px; text-align: center;"">
                            <div style=""width: 72px; height: 72px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 50%; margin: 0 auto 24px auto; line-height: 72px; font-size: 32px;"">👋</div>
                            <h2 style=""margin: 0 0 8px 0; font-size: 24px; font-weight: 700; color: #1a1a2e;"">Welcome, {user.Username}!</h2>
                            <p style=""margin: 0; font-size: 16px; color: #6b7280; line-height: 1.6;"">Thank you for registering with ManKoBhavna.<br>Your secure personal journal is now ready to use.</p>
                        </td>
                    </tr>

                    <!-- Divider -->
                    <tr>
                        <td style=""padding: 0 40px;"">
                            <hr style=""border: none; height: 1px; background-color: #e5e7eb; margin: 0;"">
                        </td>
                    </tr>

                    <!-- Features Section -->
                    <tr>
                        <td style=""padding: 32px 40px;"">
                            <h3 style=""margin: 0 0 20px 0; font-size: 16px; font-weight: 600; color: #1a1a2e; text-align: center;"">What you can do with ManKoBhavna</h3>
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""width: 44px; height: 44px; background-color: #ede9fe; border-radius: 10px; text-align: center; vertical-align: middle; font-size: 20px;"">📝</td>
                                                <td style=""padding-left: 16px;"">
                                                    <p style=""margin: 0; font-size: 15px; font-weight: 600; color: #1a1a2e;"">Write Journal Entries</p>
                                                    <p style=""margin: 4px 0 0 0; font-size: 13px; color: #6b7280;"">Capture your thoughts, feelings, and experiences daily.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""width: 44px; height: 44px; background-color: #fce7f3; border-radius: 10px; text-align: center; vertical-align: middle; font-size: 20px;"">😊</td>
                                                <td style=""padding-left: 16px;"">
                                                    <p style=""margin: 0; font-size: 15px; font-weight: 600; color: #1a1a2e;"">Track Your Mood</p>
                                                    <p style=""margin: 4px 0 0 0; font-size: 13px; color: #6b7280;"">Monitor your emotional well-being with mood tracking.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""width: 44px; height: 44px; background-color: #dbeafe; border-radius: 10px; text-align: center; vertical-align: middle; font-size: 20px;"">📊</td>
                                                <td style=""padding-left: 16px;"">
                                                    <p style=""margin: 0; font-size: 15px; font-weight: 600; color: #1a1a2e;"">View Analytics</p>
                                                    <p style=""margin: 4px 0 0 0; font-size: 13px; color: #6b7280;"">Gain insights into your journaling habits and trends.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- CTA Button -->
                    <tr>
                        <td style=""padding: 8px 40px 40px 40px; text-align: center;"">
                            <a href=""#"" style=""display: inline-block; padding: 14px 36px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 10px; font-size: 15px; font-weight: 600; letter-spacing: 0.3px;"">Open ManKoBhavna</a>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f9fafb; padding: 28px 40px; text-align: center; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; color: #9ca3af;"">Thank you for choosing ManKoBhavna ❤️</p>
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.UtcNow.Year} ManKoBhavna — Your Secure Personal Journal</p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Welcome to ManKoBhavna! ✨",
                        emailBody
                    );
                }
                catch
                {
                    // Failures are already logged inside EmailService
                }
            });

            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Error = "Email and Password are required." });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (user == null || !VerifyPassword(user.PasswordHash, request.Password))
            {
                return Unauthorized(new { Error = "Incorrect email or password." });
            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                Token = token,
                User = new { user.Id, user.Email, user.Username }
            });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (request.Username != user.Username)
            {
                var exists = await _db.Users.AnyAsync(u => u.Username == request.Username && u.Id != userId);
                if (exists) return BadRequest(new { Error = "Username already taken." });
            }

            if (request.Email != user.Email)
            {
                var exists = await _db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId);
                if (exists) return BadRequest(new { Error = "Email already taken." });
            }

            user.Username = request.Username;
            user.Email = request.Email;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { user.Id, user.Email, user.Username });
        }

        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!VerifyPassword(user.PasswordHash, request.CurrentPassword))
            {
                return BadRequest(new { Error = "Current password is incorrect." });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { Error = "New password cannot be empty." });
            }

            if (!request.NewPassword.Any(char.IsUpper))
            {
                return BadRequest(new { Error = "New password must contain at least one uppercase letter." });
            }

            if (!request.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
            {
                return BadRequest(new { Error = "New password must contain at least one special character." });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
            {
                throw new InvalidOperationException("JWT Key (Jwt:Key) is missing from configuration or is less than 32 bytes long.");
            }
            var jwtIssuer = _config["Jwt:Issuer"] ?? "ManKoBhavna.API";
            var jwtAudience = _config["Jwt:Audience"] ?? "ManKoBhavna.Client";
            var expireMinutes = Convert.ToInt32(_config["Jwt:ExpireMinutes"] ?? "1440");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static readonly PasswordHasher<User> _passwordHasher = new();

        private static string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null!, password);
        }

        private static bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
            return result != PasswordVerificationResult.Failed;
        }
    }

    public record RegisterRequest(string Email, string Username, string Password);
    public record LoginRequest(string Email, string Password);
    public record UpdateProfileRequest(string Username, string Email);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
