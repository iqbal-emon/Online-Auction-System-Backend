namespace angularAuctionBackend.Model.ModelDto
{
    public class AddProductDTO
    {
        public int? UserID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal? ReservePrice { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public IFormFile? ImageURL { get; set; }
        public IFormFile? ImageURL1 { get; set; }
        public IFormFile? ImageURL2 { get; set; }
    }
}
