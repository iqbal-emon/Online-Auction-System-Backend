using angularAuctionBackend.Model;
using angularAuctionBackend.Model.ModelDto;
using angularAuctionBackend.Shared;
using Microsoft.AspNetCore.Identity;
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
        private readonly ImageSave _imageSave;
        private static readonly Dictionary<string, (string UserName, string Role)> RefreshTokens = new();

        public AuthenticationController(IConfiguration config,ImageSave imageSave)
        {
            _configuration = config;
            _imageSave = imageSave;
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
                string storedHashedPassword = null;
                var query = loginRequest.Role switch
                {
                    "seller" => "SELECT UserID AS UserId, Password FROM Users WHERE UserName = @UserName AND flag='1'",
                    "buyer" => "SELECT CustomerID AS UserId, Password FROM Customers WHERE UserName = @UserName AND flag='1'",
                    "admin" => "SELECT AdminID AS UserId, Password FROM Admins WHERE UserName = @UserName",

                    _ => throw new ArgumentException("Invalid role.")
                };

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserName", loginRequest.UserName);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new UserDTO
                                {
                                    UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : null
                                };
                                storedHashedPassword = reader["Password"].ToString();
                            }
                        }
                    }
                }

                if (user == null || string.IsNullOrEmpty(storedHashedPassword))
                {
                    return NotFound("User not found or password hash missing.");
                }

                var passwordHasher = new PasswordHasher<UserDTO>();

                var verificationResult = passwordHasher.VerifyHashedPassword(user, storedHashedPassword, loginRequest.Password);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    return Unauthorized("Invalid password.");
                }

                // Generate tokens
                var accessToken = GenerateAccessToken(loginRequest.UserName, loginRequest.Role, user.UserId);

                user.Token = accessToken;
                user.Role = loginRequest.Role;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost]
        [Route("sign-up")]
        public async Task<IActionResult> SignUp([FromForm] User user)
        {
            var imageURL = "";
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Username, password, and Image are required.");
            }
            try
            {
                if(user.ImageURL != null)
                {
                 imageURL=await _imageSave.SaveImageToServer(user.ImageURL);
                }
                var passwordHasher = new PasswordHasher<User>();

                var hashedPassword = passwordHasher.HashPassword(user, user.Password);

                var query = user.Role switch
                {
                    "seller" => "INSERT INTO Users (UserName, Password,Email, flag) VALUES (@UserName, @Password,@Email,'1')",
                    "buyer" => "INSERT INTO Customers (UserName, Password,Email,flag) VALUES (@UserName, @Password,@Email,'1')",
                    _ => throw new ArgumentException("Invalid role.")
                };
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserName", user.Username);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.Parameters.AddWithValue("@ImageURL", imageURL);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok("User created successfully.");
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