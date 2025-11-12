namespace Data.Models
{
    public class Station
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public Brand Brand { get; set; }

        public Guid AddressId { get; set; }
        public StationAddress Address { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<PriceProposal> PriceProposal { get; set; } = new List<PriceProposal>();
        public ICollection<FuelPrice> FuelPrice { get; set; } = new List<FuelPrice>();
    }

}
