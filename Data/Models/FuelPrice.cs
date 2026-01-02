namespace Data.Models
{
    public class FuelPrice
    {
        public Guid Id { get; set; }
        
        public Guid StationId { get; set; }
        public Station Station { get; set; }

        public Guid FuelTypeId { get; set; }
        public FuelType FuelType { get; set; }

        public decimal Price { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime CreatedAt{ get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
