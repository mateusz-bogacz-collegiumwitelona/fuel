using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Data.Models
{
    public class Station
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public Brand Brand { get; set; }
        public string Address { get; set; }
        public Point Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<PriceProposal> PriceProposal { get; set; } = new List<PriceProposal>();
        public ICollection<FuelPrice> FuelPrice { get; set; } = new List<FuelPrice>(); 

    }
}
