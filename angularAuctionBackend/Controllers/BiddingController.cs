using angularAuctionBackend.Model.ModelDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;

namespace angularAuctionBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BiddingController : ControllerBase
    {
        private readonly IHubContext<ProductHub> _hubContext;
        private readonly IConfiguration _configuration;
        public BiddingController(IHubContext<ProductHub> hubContext,IConfiguration configuration)
        {
            _hubContext = hubContext;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetBids/{itemID}")]
        [Authorize(Roles = "buyer")]
        public IActionResult GetBids(int? itemID)
        {
            if (itemID == null)
            {
                return BadRequest("Item ID is required.");
            }

            List<BidsDTO> bidList = new List<BidsDTO>();
            decimal maxBidAmount = 0;

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    // Retrieve all bids for the specified item
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Bids WHERE itemID=@itemID", con))
                    {
                        cmd.Parameters.AddWithValue("@itemID", itemID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                BidsDTO dto = new BidsDTO
                                {
                                    BidID = Convert.ToInt32(reader["BidID"]),
                                    ItemID = Convert.ToInt32(reader["ItemID"]),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    BidTime = Convert.ToDateTime(reader["BidTime"])
                                };
                                bidList.Add(dto);
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT MAX(Amount) AS MaxAmount FROM Bids WHERE itemID=@itemID", con))
                    {
                        cmd.Parameters.AddWithValue("@itemID", itemID);

                        maxBidAmount = (decimal)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok(new { Bids = bidList, MaxBidAmount = maxBidAmount });
        }



        [HttpPost]
        [Route("PlaceBid")]
        [Authorize(Roles = "buyer")]
        public async Task<IActionResult> PlaceBid(BidsDTO bid)
        {
            if (bid == null)
            {
                return BadRequest("Bid details are required.");
            }
            if (bid.ItemID == null)
            {
                return BadRequest("Item ID is required.");
            }
            if (bid.CustomerID == null)
            {
                return BadRequest("Customer ID is required.");
            }
            if (bid.Amount == null)
            {
                return BadRequest("Bid amount is required.");
            }
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Bids (ItemID, CustomerID, Amount, BidTime) VALUES (@ItemID, @CustomerID, @Amount, @BidTime)", con))
                    {
                        cmd.Parameters.AddWithValue("@ItemID", bid.ItemID);
                        cmd.Parameters.AddWithValue("@CustomerID", bid.CustomerID);
                        cmd.Parameters.AddWithValue("@Amount", bid.Amount);
                        cmd.Parameters.AddWithValue("@BidTime", bid.BidTime);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    await _hubContext.Clients.All.SendAsync("ReceiveBid", bid);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            return Ok(new {Message="Bid Added Successfully"});
        }


    }
}
