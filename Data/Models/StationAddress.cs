using NetTopologySuite.Geometries;

namespace Data.Models
{
    public class StationAddress
    {
        public Guid Id { get; set; }
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public Point Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt{ get; set; }

        public ICollection<Station> Stations { get; set; } = new List<Station>();
    }

}
