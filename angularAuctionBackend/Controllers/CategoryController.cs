using angularAuctionBackend.Model.ModelDto;
using angularAuctionBackend.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace angularAuctionBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration _configuration;
    
        public CategoryController(IConfiguration config)
        {
            _configuration = config;
 
        }
        [HttpGet]
        [Route("getcategories")]
        public IActionResult GetCategories()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))

            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM Category", con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<CategoryDTO> categories = new List<CategoryDTO>();
                        while (reader.Read())
                        {
                            CategoryDTO category = new CategoryDTO
                            {
                                CategoryName = reader["CategoryName"].ToString()
                            };
                            categories.Add(category);
                        }
                        return Ok(categories);
                    }
                }
            }
        }

    }
}
