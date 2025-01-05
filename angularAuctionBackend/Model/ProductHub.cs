using angularAuctionBackend.Model.ModelDto;
using Microsoft.AspNetCore.SignalR;

public class ProductHub : Hub
{
    public async Task AddProduct(AddProductDTO product)
    {
        await Clients.All.SendAsync("ReceiveProduct", product);
    }

    public async Task PlaceBid(BidsDTO bid)
    {
        await Clients.All.SendAsync("ReceiveBid", bid);
    }
}
