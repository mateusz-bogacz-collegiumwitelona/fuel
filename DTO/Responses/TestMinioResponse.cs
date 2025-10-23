namespace DTO.Responses
{
    public class TestMinioResponse
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public string? ResponseTime { get; set; }
        public bool CanConnect { get; set; }
        public string? Endpoint { get; set; }
        public bool IsBucketExist { get; set; }
        public string? BucketName { get; set; }
        public List<string>? AvalableBuckets { get; set; }
    }
}
