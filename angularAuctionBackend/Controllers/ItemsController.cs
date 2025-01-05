using angularAuctionBackend.Model.ModelDto;
using angularAuctionBackend.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace angularAuctionBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ImageSave imageSave;
        private readonly IHubContext<ProductHub> _hubContext;
        public ItemsController(IConfiguration config, ImageSave imageSave, IHubContext<ProductHub> hubContext)
        {
            _configuration = config;
            this.imageSave = imageSave;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Route("activeProduct")]
        [Authorize(Roles = "buyer")]
        public IActionResult GetItems()
        {
            List<itemsDTO> itemList = new List<itemsDTO>();

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Items ", con))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            foreach (DataRow row in dt.Rows)
                            {
                                itemsDTO dto = new itemsDTO
                                {
                                    ItemID = Convert.ToInt32(row["ItemID"]),
                                    Title = Convert.ToString(row["Title"]),
                                    Description = Convert.ToString(row["Description"]),
                                    Category = Convert.ToString(row["Category"]),
                                    ReservePrice = Convert.ToDecimal(row["ReservePrice"]),
                                    StartTime = Convert.ToDateTime(row["StartTime"]),
                                    EndTime = Convert.ToDateTime(row["EndTime"]),
                                    ImageURL = row["ImageURL"].ToString(),
                                    ImageURL1 = row["ImageURL1"].ToString(),
                                    ImageURL2 = row["ImageURL2"].ToString()
                                };

                                itemList.Add(dto);
                            }
                        }
                        return Ok(itemList);
                    }
                }
            }
            catch
            {
                return StatusCode(500, "An error occurred while fetching user details.");
            }
        }

        [HttpGet]
        [Route("activeProduct/{id}")]
        [Authorize(Roles = "buyer")]
        public IActionResult GetProductById(int? id)
        {
            if (id == null)
            {
                return BadRequest("Product ID is required.");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Items WHERE ItemID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                itemsDTO items = new itemsDTO
                                {
                                    ItemID = Convert.ToInt32(reader["ItemID"]),
                                    Title = Convert.ToString(reader["Title"]),
                                    Description = Convert.ToString(reader["Description"]),
                                    Category = Convert.ToString(reader["Category"]),
                                    ReservePrice = Convert.ToDecimal(reader["ReservePrice"]),
                                    StartTime = Convert.ToDateTime(reader["StartTime"]),
                                    EndTime = Convert.ToDateTime(reader["EndTime"]),
                                    ImageURL = reader["ImageURL"].ToString(),
                                    ImageURL1 = reader["ImageURL1"].ToString(),
                                    ImageURL2 = reader["ImageURL2"].ToString()
                                };

                                return Ok(items);
                            }
                            else
                            {
                                return NotFound("Product not found.");
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"An error occurred while fetching product details.,{ex.Message}");
            }
        }




        [HttpPost]
        [Route("add-product")]
        public async Task<IActionResult> AddProduct([FromForm] AddProductDTO productRequest)
        {
            var imageURL = "";
            var imageURL1 = "";
            var imageURL2 = "";
            if (productRequest == null || string.IsNullOrEmpty(productRequest.Title) ||
                string.IsNullOrEmpty(productRequest.Description) ||
                string.IsNullOrEmpty(productRequest.Category) ||
                productRequest.ReservePrice <= 0 ||
                productRequest.StartTime == default ||
                productRequest.EndTime == default)
            {
                return BadRequest("All fields are required and must have valid values.");
            }
            if (productRequest.ImageURL != null)
            {
                imageURL = await imageSave.SaveImageToServer(productRequest.ImageURL);
            }
            if (productRequest.ImageURL1 != null)
            {
                imageURL1 = await imageSave.SaveImageToServer(productRequest.ImageURL1);
            }
            if (productRequest.ImageURL2 != null)
            {
                imageURL2 = await imageSave.SaveImageToServer(productRequest.ImageURL2);
            }
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    string query = @"
                INSERT INTO Items (UserID,Title, Description, Category, ReservePrice, StartTime, EndTime, ImageURL, ImageURL1, ImageURL2) 
                VALUES (@UserID,@Title, @Description, @Category, @ReservePrice, @StartTime, @EndTime, @ImageURL, @ImageURL1, @ImageURL2);
                ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserID", productRequest.UserID);
                        cmd.Parameters.AddWithValue("@Title", productRequest.Title);
                        cmd.Parameters.AddWithValue("@Description", productRequest.Description);
                        cmd.Parameters.AddWithValue("@Category", productRequest.Category);
                        cmd.Parameters.AddWithValue("@ReservePrice", productRequest.ReservePrice);
                        cmd.Parameters.AddWithValue("@StartTime", productRequest.StartTime);
                        cmd.Parameters.AddWithValue("@EndTime", productRequest.EndTime);
                        cmd.Parameters.AddWithValue("@ImageURL", imageURL);
                        cmd.Parameters.AddWithValue("@ImageURL1", imageURL1);
                        cmd.Parameters.AddWithValue("@ImageURL2", imageURL2);

                        var execute = cmd.ExecuteScalar();
                        // Notify all connected clients about the new product
                        var newProduct = new itemsDTO
                        {
                            Title = productRequest.Title,
                            Description = productRequest.Description,
                            Category = productRequest.Category,
                            ReservePrice = productRequest.ReservePrice,
                            StartTime = productRequest.StartTime,
                            EndTime = productRequest.EndTime,
                            ImageURL = imageURL,
                            ImageURL1 = imageURL1,
                            ImageURL2 = imageURL2
                        };

                        await _hubContext.Clients.All.SendAsync("ReceiveProduct", newProduct);

                        return Ok(new { Message = "Product added successfully." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding the product: {ex.Message}");
            }
        }
    }
}

