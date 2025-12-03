using Azure.Storage.Blobs;
using Data.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.BackgrounServices
{
    public class BlobInitializer : IHostedService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IOptions<BlobConfig> _config;

        public BlobInitializer(BlobServiceClient blobServiceClient, IOptions<BlobConfig> config)
        {
            _blobServiceClient = blobServiceClient;
            _config = config;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var containerName = _config.Value.ContainerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(
                Azure.Storage.Blobs.Models.PublicAccessType.Blob,
                cancellationToken: cancellationToken
            );
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
