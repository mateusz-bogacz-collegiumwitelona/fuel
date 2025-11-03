namespace DTO.Responses
{
    public class GetFuelPrivceAndCodeResponse
    {
        public string FuelCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime ValidFrom { get; set; }
    }
}
