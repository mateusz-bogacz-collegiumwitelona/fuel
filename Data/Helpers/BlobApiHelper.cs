using Azure.Storage.Blobs;
using Data.Config;
using Data.Interfaces;
using Microsoft.Extensions.Options;

namespace Data.Helpers
{
    public class BlobApiHelper : IStorage
    {
        private readonly BlobContainerClient _blob;
        private readonly string _publicUrl;
        public BlobApiHelper(BlobServiceClient blobServiceClient, IOptions<BlobConfig> options)
        {
            var containerName = options.Value.ContainerName;
            _blob = blobServiceClient.GetBlobContainerClient(containerName);
            _blob.CreateIfNotExists();
            _publicUrl = options.Value.PublicUrl;
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

        public string GetPublicUrl(string objectPath, string? bucketName = null) => $"{_publicUrl}/{objectPath}";
    }
}
