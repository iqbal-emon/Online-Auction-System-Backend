using angularAuctionBackend.Model.ModelDto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace angularAuctionBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public StatusController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPut]
        [Route("blockseller/{userRole}/{userId}/{status}")]
        public IActionResult UpdateStatus(string? userRole,int? userId,int? status)
        {
            var query = userRole switch
            {
                "seller" => "UPDATE Users SET flag = @flag WHERE UserID = @UserID",
                "buyer" => "UPDATE Customers SET flag = @flag WHERE CustomerID = @UserID",
                _ => throw new ArgumentException("Invalid role.")
            };
            if (userRole == null || userId == null || status == null)
            {
                return BadRequest(new {Message= "User Role, User ID, and Status are required." });
            }
           
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {

                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@flag", status);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    var excute= cmd.ExecuteNonQuery();
                    if (excute > 0)
                    {
                        return Ok(new {Message="Status Updated Successfully" });
                    }
                    else
                    {
                        return BadRequest(new {Message= "Status Not Updated" } );
                    }
                }
            }

        }



        [HttpGet]
        [Route("getstatus/{userRole}")]
        public ActionResult<List<userDetailsDTO>> GetStatus(string? userRole)
        {
            if (string.IsNullOrEmpty(userRole))
            {
                return BadRequest(new { Message = "User Role is required." });
            }

            var query = userRole switch
            {
                "seller" => "SELECT * FROM Users",
                "buyer" => "SELECT * FROM Customers",
                _ => throw new ArgumentException("Invalid role.")
            };

            var users = new List<userDetailsDTO>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new userDetailsDTO
                            {
                                flag = reader["flag"]?.ToString(),
                                UserId = userRole=="seller"? Convert.ToInt32(reader["UserID"]) : Convert.ToInt32(reader["CustomerID"]),
                                Username = reader["UserName"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                ImageURL = reader["ImageURL"]?.ToString()
                            };
                            users.Add(user);
                        }
                    }
                }
            }

            return Ok(users);
        }


    }
}
