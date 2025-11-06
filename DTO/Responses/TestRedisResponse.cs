namespace DTO.Responses
{
    public class TestRedisResponse
    {
        public int Status { get; set; }
        public List<string>? Messages { get; set; }
        public string? ResponseTime { get; set; }
        public List<string>? Endpoints { get; set; }
    }
}
