namespace angularAuctionBackend.Model
{
    public class User
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? flag { get; set; }
        public string? Role { get; set; }

        public IFormFile? ImageURL { get; set; }
    }
}
