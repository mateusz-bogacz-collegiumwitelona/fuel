namespace Data.Interfaces
{
    public interface IS3ApiHelper
    {
        string GetPublicUrl(string objectPath, string? bucketName = null);
        Task<string> UploadFileAsync(Stream photo, string fileName, string contentType, string? bucketName = null, string? subPath = null);
        Task<bool> DeleteFileAsync(string objectPath, string? bucketName = null);
        Task<string> GetPresignedUrlAsync(string objectPath, string bucketName);
    }
}
