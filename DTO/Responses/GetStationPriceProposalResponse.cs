namespace DTO.Responses
{
    public class GetStationPriceProposalResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FuelName {  get; set; }
        public string FuelCode {  get; set; }
        public decimal ProposedPrice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
