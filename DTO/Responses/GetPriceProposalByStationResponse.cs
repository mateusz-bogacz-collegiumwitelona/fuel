namespace DTO.Responses
{
    public class GetPriceProposalByStationResponse
    {
        public string UserName { get; set; } 

        public string FuelCode { get; set; }
        public decimal ProposedPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
