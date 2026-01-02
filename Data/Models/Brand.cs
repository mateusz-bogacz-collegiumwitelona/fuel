namespace Data.Models
{
    public class Brand
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Station> Station { get; set; } = new List<Station>();
    }
}
