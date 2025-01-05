namespace angularAuctionBackend.Model.ModelDto
{
    public class BidsDTO
    {
        public int? BidID { get; set; }
        public int? ItemID { get; set; }
        public int? CustomerID { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? BidTime { get; set; }= DateTime.Now;
    }
}
