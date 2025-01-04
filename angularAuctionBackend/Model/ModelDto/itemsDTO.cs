namespace angularAuctionBackend.Model.ModelDto
{
    public class itemsDTO
    {
        public int? ItemID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal? ReservePrice { get; set; }
        public string? ImageURL { get; set; }
        public string? ImageURL1 { get; set; }
        public string? ImageURL2 { get; set; }


        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
     
    }
}
