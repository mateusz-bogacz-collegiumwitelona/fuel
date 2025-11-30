using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Diagnostics;

namespace Data.Helpers
{
    public class S3ApiHelper: IS3ApiHelper
    {
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _config;
        private readonly ILogger<S3ApiHelper> _logger;
        private readonly string _publicUrl;

        public S3ApiHelper(
            IMinioClient minioClient,
            IConfiguration config,
            ILogger<S3ApiHelper> logger)
        {
            _minioClient = minioClient;
            _config = config;
            _logger = logger;
            _publicUrl = _config["MinIO:PublicUrl"] ?? "http://localhost:9000";
        }

        public string GetPublicUrl(string objectPath, string? bucketName = null)
        {
            var targetBucket = bucketName ?? _config["MinIO:BucketName"];
            return $"{_publicUrl}/{targetBucket}/{objectPath}";
        }

        public async Task<string> UploadFileAsync(
            Stream photo,
            string fileName,
            string contentType,
            string? bucketName = null,
            string? subPath = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var targetBucket = bucketName ?? _config["MinIO:BucketName"];

                if (string.IsNullOrEmpty(targetBucket))
                {
                    throw new InvalidOperationException("Bucket name must be provided or configured in MinIO:BucketName");
                }

                await EnsureBucketExistsAsync(targetBucket);

                var fullPath = string.IsNullOrWhiteSpace(subPath)
                    ? fileName
                    : $"{subPath.TrimEnd('/')}/{fileName}";

                if (photo.CanSeek) photo.Position = 0;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(targetBucket)
                    .WithObject(fullPath)
                    .WithStreamData(photo)
                    .WithObjectSize(photo.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Successfully uploaded file. Path: {FullPath}, Bucket: {BucketName}, Size: {Size} bytes, Time: {Time}ms",
                    fullPath,
                    targetBucket,
                    photo.Length,
                    stopwatch.ElapsedMilliseconds);

                return fullPath;
            }
            catch (BucketNotFoundException bnfex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    bnfex,
                    "Bucket not found while uploading file. Time: {Time}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"MinIO bucket not found: {bnfex.Message}", bnfex);
            }
            catch (InvalidObjectNameException ionex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ionex,
                    "Invalid file name while uploading file. FileName: {FileName}, Time: {Time}ms",
                    fileName,
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Invalid file name: {ionex.Message}", ionex);
            }
            catch (MinioException mex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    mex,
                    "MinIO error while uploading file. Time: {Time}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to upload file to MinIO: {mex.Message}", mex);
            }
            catch (IOException ioex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ioex,
                    "IO error while reading file stream. Time: {Time}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"IO error while uploading file: {ioex.Message}", ioex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Unexpected error while uploading file. Time: {Time}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
        public async Task<bool> DeleteFileAsync(string objectPath, string? bucketName = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrEmpty(objectPath))
                {
                    _logger.LogWarning("Cannot delete file: objectPath is null or empty");
                    return false;
                }

                var targetBucket = bucketName ?? _config["MinIO:BucketName"];

                if (string.IsNullOrEmpty(targetBucket))
                {
                    _logger.LogError("Bucket name must be provided or configured");
                    return false;
                }

                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(targetBucket)
                    .WithObject(objectPath);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Successfully deleted file. Path: {ObjectPath}, Bucket: {BucketName}, Time: {Time}ms",
                    objectPath,
                    targetBucket,
                    stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (MinioException minioEx)
            {
                stopwatch.Stop();
                _logger.LogError(minioEx, "MinIO error occurred while deleting object: {ObjectPath}, Time: {Time}ms",
                    objectPath, stopwatch.ElapsedMilliseconds);
                return false;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "An error occurred while deleting object from MinIO: {ObjectPath}, Time: {Time}ms",
                    objectPath, stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        private async Task EnsureBucketExistsAsync(string bucketName)
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);

                bool isBucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

                if (!isBucketExists)
                {
                    _logger.LogWarning("Bucket {BucketName} does not exist, creating it", bucketName);

                    try
                    {
                        var makeBucketArgs = new MakeBucketArgs()
                            .WithBucket(bucketName);
                        await _minioClient.MakeBucketAsync(makeBucketArgs);

                        _logger.LogInformation("Bucket {BucketName} created successfully", bucketName);
                    }
                    catch (MinioException ex) when (
                        ex.ServerMessage?.Contains("BucketAlreadyOwnedByYou") == true ||
                        ex.ServerMessage?.Contains("Your previous request to create the named bucket succeeded") == true ||
                        ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Bucket {BucketName} already exists (race condition handled). ServerMessage: {ServerMessage}",
                            bucketName, ex.ServerMessage ?? ex.Message);
                    }
                }
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "MinIO error while ensuring bucket exists: {BucketName}. ServerMessage: {ServerMessage}",
                    bucketName, ex.ServerMessage ?? ex.Message);
                throw;
            }
        }

        public async Task<string> GetPresignedUrlAsync(
            string objectPath,
            string bucketName
            )
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(objectPath))
                    throw new ArgumentException("Object path must be provided", nameof(objectPath));

                int expiryInSeconds = 3600;
                var targetBucket = bucketName ?? _config["MinIO:BucketName"];

                if (string.IsNullOrWhiteSpace(targetBucket))
                    throw new ArgumentException("Bucket name must be provided", nameof(targetBucket));

                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(targetBucket)
                    .WithObject(objectPath)
                    .WithExpiry(expiryInSeconds);

                var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Generated presigned URL. Path: {ObjectPath}, Bucket: {BucketName}, Expiry: {Expiry}s, Time: {Time}ms",
                    objectPath,
                    targetBucket,
                    expiryInSeconds,
                    stopwatch.ElapsedMilliseconds);

                return url;
            }
            catch (ArgumentException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (MinioException mex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    mex,
                    "MinIO error while generating presigned URL. Time: {Time}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to generate presigned URL from MinIO: {mex.Message}", mex);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Unexpected error while generating presigned URL. Time: {Time}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}