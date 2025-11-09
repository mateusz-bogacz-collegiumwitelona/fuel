namespace DTO.Responses
{
    public class GetStationPriceProposalResponse
    {
        public string UserName { get; set; }
        public string BrandName { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public string FuelName {  get; set; }
        public string FuelCode {  get; set; }
        public decimal ProposedPrice { get; set; }
        public string Status { get; set; }
        public string? PhotoToken { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
