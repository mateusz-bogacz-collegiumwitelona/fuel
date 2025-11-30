using Azure.Storage.Blobs;
using Data.Interfaces;

namespace Data.Helpers
{
    public class BlobApiHelper : IStorage
    {
        private readonly BlobContainerClient _blob;

        public BlobApiHelper(BlobServiceClient blobServiceClient, string containerName)
        {
            _blob = blobServiceClient.GetBlobContainerClient(containerName);
            _blob.CreateIfNotExists();
        }

        public async Task<string> UploadFileAsync(Stream photo, string fileName, string contentType, string? bucketName = null, string? subPath = null)
        {
            var blobName = string.IsNullOrWhiteSpace(subPath) ? fileName : $"{subPath.TrimEnd('/')}/{fileName}";
            var blobClient = _blob.GetBlobClient(blobName);
            await blobClient.UploadAsync(photo, overwrite: true);
            return blobName;
        }


        public async Task<bool> DeleteFileAsync(string objectPath, string? bucketName = null)
        {
            var blobClient = _blob.GetBlobClient(objectPath);
            var isGood = await blobClient.DeleteIfExistsAsync();
            return isGood;
        }

        public string GetPublicUrl(string objectPath, string? bucketName = null)
        {
            var blobClient = _blob.GetBlobClient(objectPath);
            return blobClient.Uri.ToString();
        }
    }
}
