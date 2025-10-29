using Data.Context;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
namespace Data.Repositories
{
    public class PriceProposalRepository : IPriceProposalRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PriceProposalRepository> _logger;
        private readonly S3ApiHelper _s3ApiHelper;
        private readonly IConfiguration _config;

        public PriceProposalRepository(
            ApplicationDbContext context,
            ILogger<PriceProposalRepository> logger,
            S3ApiHelper s3ApiHelper,
            IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _s3ApiHelper = s3ApiHelper;
            _config = config;
        }

        public async Task<bool> AddNewPriceProposalAsync(
            ApplicationUser user,
            Station station,
            FuelType fuelType,
            decimal priceProposal,
            IFormFile photo,
            string extension)
        {
            var stopwatch = Stopwatch.StartNew();
            string? photoUrl = null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string photoToken = GeneratePhotoToken();

                var proposal = new PriceProposal
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    User = user,
                    StationId = station.Id,
                    Station = station,
                    ProposedPrice = priceProposal,
                    FuelTypeId = fuelType.Id,
                    FuelType = fuelType,
                    CreatedAt = DateTime.UtcNow,
                    Status = Data.Enums.PriceProposalStatus.Pending,
                    PhotoToken = photoToken
                };

                var contentType = extension.ToLower() switch
                {
                    ".jpeg" => "image/jpeg",
                    ".jpg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => throw new InvalidOperationException($"Unsupported file extension: {extension}")
                };

                string fileName = $"{proposal.Id}{extension}";
                string subPath = $"fuel-prices/{DateTime.UtcNow:yyyy/MM/dd}";
                
                string? bucketName = _config["MinIO:fuel-prices"];  

                await using var photoStream = photo.OpenReadStream();

                photoUrl = await _s3ApiHelper.UploadFileAsync(
                    photoStream,
                    fileName,
                    contentType,
                    bucketName, 
                    subPath);

                if (string.IsNullOrEmpty(photoUrl))
                {
                    _logger.LogError("Failed to upload photo for price proposal {ProposalId}", proposal.Id);
                    throw new InvalidOperationException("Photo upload failed");
                }

                proposal.PhotoUrl = photoUrl; 
                _context.PriceProposals.Add(proposal);
                var savedCount = await _context.SaveChangesAsync();

                if (savedCount <= 0) throw new InvalidOperationException("Failed to save price proposal to database");


                await transaction.CommitAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    "Price proposal {ProposalId} added successfully. Time: {ElapsedMilliseconds}ms",
                    proposal.Id,
                    stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await transaction.RollbackAsync();

                if (!string.IsNullOrEmpty(photoUrl))
                {
                    string? bucketName = _config["MinIO:PriceProposalsBucket"];
                    var deleted = await _s3ApiHelper.DeleteFileAsync(photoUrl, bucketName);

                    if (deleted)
                    {
                        _logger.LogInformation("Successfully cleaned up orphaned photo: {PhotoUrl}", photoUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to cleanup orphaned photo: {PhotoUrl}", photoUrl);
                    }
                }

                _logger.LogError(
                    ex,
                    "Failed to add price proposal. Time: {ElapsedMilliseconds}ms",
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }

        private string GeneratePhotoToken()
        {
            byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public async Task<GetPriceProposalResponse> GetPriceProposal(string photoToken)
        {
            var proposal = await _context.PriceProposals
                .Include(pp => pp.User)
                .Include(pp => pp.Station)
                    .ThenInclude(s => s.Brand)
                .Include(b => b.Station)
                    .ThenInclude(s => s.Address)
                .Include(pp => pp.FuelType)
                .FirstOrDefaultAsync(pp => pp.PhotoToken == photoToken);

            if (proposal == null)
            {
                _logger.LogWarning("Price proposal with photo token {PhotoToken} not found.", photoToken);
                return null;
            }

            string bucketName = _config["MinIO:BucketName"] ?? "fuel-prices";
            string path = proposal.PhotoUrl;

            string photoUrl = await _s3ApiHelper.GetPresignedUrlAsync(path, bucketName);

            return new GetPriceProposalResponse
            {
                Email = proposal.User.Email,
                BrandName = proposal.Station.Brand.Name,
                Street = proposal.Station.Address.Street,
                HouseNumber = proposal.Station.Address.HouseNumber,
                City = proposal.Station.Address.City,
                PostalCode = proposal.Station.Address.PostalCode,
                FuelType = proposal.FuelType.Name,
                ProposedPrice = proposal.ProposedPrice,
                PhotoUrl = photoUrl,
                CreatedAt = proposal.CreatedAt
            };
        }
    }
}