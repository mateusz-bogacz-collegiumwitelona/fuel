namespace DTO.Requests
{
    public class GetStationsRequest
    {
        public List<string>? BrandName { get; set; }
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }
        public int? Distance { get; set; }
    }
}
