namespace Data.Models
{
    public class FuelType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<PriceProposal> PriceProposals { get; set; } = new List<PriceProposal>();
        public ICollection<FuelPrice> FuelPrice { get; set; } = new List<FuelPrice>();
    }
}
