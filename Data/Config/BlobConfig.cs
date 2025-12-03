namespace Data.Config
{
    public class BlobConfig
    {
        public string ConnectionString { get; set; } = null!;
        public string ContainerName { get; set; } = null!;
        public string? PublicUrl { get; set; }
    }
}
