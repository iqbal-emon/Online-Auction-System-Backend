using angularAuctionBackend.Model.ModelDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace angularAuctionBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static readonly Dictionary<string, (string UserName, string Role)> RefreshTokens = new();

        public AuthenticationController(IConfiguration config)
        {
            _configuration = config;
        }

        [HttpPost]
        [Route("login")]
        public IActionResult LoginDetails([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.UserName) || string.IsNullOrEmpty(loginRequest.Password) || string.IsNullOrEmpty(loginRequest.Role))
            {
                return BadRequest("Username, password, and role are required.");
            }

            try
            {
                UserDTO user = null;
                var query = loginRequest.Role switch
                {
                    "admin" => "SELECT AdminID AS UserId FROM Admins WHERE UserName = @UserName AND Password = @Password",
                    "seller" => "SELECT UserID AS UserId FROM Users WHERE UserName = @UserName AND Password = @Password",
                    "buyer" => "SELECT CustomerID AS UserId FROM Customers WHERE UserName = @UserName AND Password = @Password",
                    _ => throw new ArgumentException("Invalid role.")
                };

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserName", loginRequest.UserName);
                        cmd.Parameters.AddWithValue("@Password", loginRequest.Password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new UserDTO
                                {
                                    UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : null
                                };
                            }
                        }
                    }
                }

                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Generate tokens
                var accessToken = GenerateAccessToken(loginRequest.UserName, loginRequest.Role, user.UserId);
                //var refreshToken = GenerateRefreshToken();

                // Store refresh token with role
                //RefreshTokens[refreshToken] = (loginRequest.UserName, loginRequest.Role);

                user.Token = accessToken;
                //user.RefreshToken = refreshToken;
                user.Role = loginRequest.Role;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("refresh-token")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            if (RefreshTokens.TryGetValue(refreshTokenRequest.RefreshToken, out var tokenInfo))
            {
                // Generate a new access token with the correct role
                var accessToken = GenerateAccessToken(tokenInfo.UserName, tokenInfo.Role, null);

                return Ok(new { AccessToken = accessToken });
            }

            return Unauthorized("Invalid refresh token.");
        }

        private string GenerateAccessToken(string userName, string role, int? userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", userId?.ToString() ?? string.Empty)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Access token validity
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
    }

    public class LoginRequest
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}