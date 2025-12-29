namespace DTO.Responses
{
    public class GetPriceHistoryResponse
    {
        public string FuelType { get; set; }
        public string FuelCode { get; set; }
        public List<DateTime> ValidFrom { get; set; } = new List<DateTime>();
        public List<DateTime?> ValidTo { get; set; } = new List<DateTime?>();
        public List<decimal> Price { get; set; } = new List<decimal>();
    }
}
