// File: Hubs/ProductHub.cs
using angularAuctionBackend.Model.ModelDto;
using Microsoft.AspNetCore.SignalR;

public class ProductHub : Hub
{
    // Broadcast new product information to all connected clients
    public async Task AddProduct(AddProductDTO product)
    {
        await Clients.All.SendAsync("ReceiveProduct", product);
    }
}
