using Data.Context;
using Data.Enums;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Data.Repositories
{
    public class PriceProposalRepository : IPriceProposalRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PriceProposalRepository> _logger;
        private readonly IStorage _storage;
        private readonly IConfiguration _config;

        public PriceProposalRepository(
            ApplicationDbContext context,
            ILogger<PriceProposalRepository> logger,
            IStorage s3ApiHelper,
            IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _storage = s3ApiHelper;
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
                    Token = photoToken
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
                string subPath = $"{DateTime.UtcNow:yyyy/MM/dd}";
                string bucketName = _config["MinIO:BucketName"] ?? "fuel-prices";

                await using var photoStream = photo.OpenReadStream();

                photoUrl = await _storage.UploadFileAsync(
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
                    string bucketName = _config["MinIO:BucketName"] ?? "fuel-prices";  
                    var deleted = await _storage.DeleteFileAsync(photoUrl, bucketName);

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
                .FirstOrDefaultAsync(pp => pp.Token == photoToken);

            if (proposal == null)
            {
                _logger.LogWarning("Price proposal with photo token {PhotoToken} not found.", photoToken);
                return null;
            }

            string bucketName = _config["MinIO:BucketName"] ?? "fuel-prices";
            string path = proposal.PhotoUrl;

            string photoUrl = _storage.GetPublicUrl(path, bucketName);


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
                Token = proposal.Token,
                CreatedAt = proposal.CreatedAt
            };
        }

        public async Task<List<GetStationPriceProposalResponse>> GetAllPriceProposal(TableRequest request)
            => await new TableQueryBuilder<PriceProposal, GetStationPriceProposalResponse>(_context.PriceProposals
                    .Include(pp => pp.User)
                    .Include(pp => pp.FuelType)
                    .Include(pp => pp.Station)
                        .ThenInclude(s => s.Brand)
                    .Include(pp => pp.Station)
                        .ThenInclude(s => s.Address)
                    .Where(pp => pp.Status == Enums.PriceProposalStatus.Pending), request)
                .ApplySearch((q, search) =>
                    q.Where(pp =>
                        pp.User.UserName.ToLower().Contains(search) ||
                        pp.Station.Brand.Name.ToLower().Contains(search) ||
                        pp.Station.Address.Street.ToLower().Contains(search) ||
                        pp.Station.Address.HouseNumber.ToLower().Contains(search) ||
                        pp.Station.Address.City.ToLower().Contains(search) ||
                        pp.FuelType.Name.ToLower().Contains(search) ||
                        pp.FuelType.Code.ToLower().Contains(search) ||
                        pp.ProposedPrice.ToString().ToLower().Contains(search) ||
                        pp.CreatedAt.ToString().ToLower().Contains(search)
                    )
                )
                .ApplySort(
                    new Dictionary<string, System.Linq.Expressions.Expression<Func<PriceProposal, object>>>
                    {
                        { "username", pp => pp.User.UserName },
                        { "brandname", pp => pp.Station.Brand.Name },
                        { "street", pp => pp.Station.Address.Street },
                        { "housenumber", pp => pp.Station.Address.HouseNumber },
                        { "city", pp => pp.Station.Address.City },
                        { "fuelname", pp => pp.FuelType.Name },
                        { "fuelcode", pp => pp.FuelType.Code },
                        { "proposedprice", pp => pp.ProposedPrice },
                        { "createdat", pp => pp.CreatedAt }
                    },
                    pp => pp.CreatedAt
                )
                .ProjectAndExecuteAsync(pp => new GetStationPriceProposalResponse
                {
                    Token = pp.Token,
                    UserName = pp.User.UserName,
                    BrandName = pp.Station.Brand.Name,
                    Street = pp.Station.Address.Street,
                    HouseNumber = pp.Station.Address.HouseNumber,
                    City = pp.Station.Address.City,
                    FuelName = pp.FuelType.Name,
                    FuelCode = pp.FuelType.Code,
                    ProposedPrice = pp.ProposedPrice,
                    Status = pp.Status.ToString(),
                    CreatedAt = pp.CreatedAt,
                });

        public async Task<bool> ChangePriceProposalStatus(
            bool isAccepted,
            PriceProposal priceProposal,
            ApplicationUser admin)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                if (isAccepted)
                {
                    var stationFuelPrice = await _context.FuelPrices
                        .FirstOrDefaultAsync(fp =>
                            fp.StationId == priceProposal.StationId &&
                            fp.FuelTypeId == priceProposal.FuelTypeId);

                    if (stationFuelPrice == null)
                    {
                        _logger.LogInformation(
                            "Creating new fuel price for {Brand} in {City} - {FuelType}",
                            priceProposal.Station.Brand.Name,
                            priceProposal.Station.Address.City,
                            priceProposal.FuelType.Name);

                        stationFuelPrice = new FuelPrice
                        {
                            Id = Guid.NewGuid(),
                            StationId = priceProposal.StationId,
                            FuelTypeId = priceProposal.FuelTypeId,
                            Price = priceProposal.ProposedPrice,
                            ValidFrom = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.FuelPrices.Add(stationFuelPrice);
                    }
                    else
                    {
                        stationFuelPrice.Price = priceProposal.ProposedPrice;
                        stationFuelPrice.ValidFrom = DateTime.UtcNow;
                        stationFuelPrice.UpdatedAt = DateTime.UtcNow;
                    }

                    priceProposal.Status = Enums.PriceProposalStatus.Accepted;

                    _logger.LogInformation(
                        "Accepted price proposal {ProposalId}. New price: {Price} PLN for {FuelType} at {Brand} in {City}",
                        priceProposal.Id, priceProposal.ProposedPrice, priceProposal.FuelType.Name,
                        priceProposal.Station.Brand.Name, priceProposal.Station.Address.City);
                }
                else
                {
                    priceProposal.Status = Enums.PriceProposalStatus.Rejected;

                    _logger.LogInformation(
                        "Rejected price proposal {ProposalId} from user {UserId}",
                        priceProposal.Id, priceProposal.User.Id);
                }

                priceProposal.ReviewedBy = admin.Id;
                priceProposal.ReviewedAt = DateTime.UtcNow;

                var savedCount = await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return savedCount > 0;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Error changing price proposal status. PhotoToken: {PhotoToken}, UserId: {UserId}",
                    priceProposal.Token, priceProposal.User.Id);
                throw;
            }
        }

        public async Task<PriceProposal> FindPriceProposal(string token)
            =>  await  _context.PriceProposals
                    .Include(pp => pp.User)
                    .Include(pp => pp.Station.Brand)
                    .Include(pp => pp.Station.Address)
                    .Include(pp => pp.FuelType)
                    .FirstOrDefaultAsync(pp =>
                        pp.Token == token && pp.Status == Enums.PriceProposalStatus.Pending);


        public async Task<GetPriceProposalStaisticResponse> GetPriceProposalStaisticAsync()
        {
            var stats = await _context.PriceProposals
                .GroupBy(pp => pp.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return new GetPriceProposalStaisticResponse()
            {
                AcceptedRate = stats.FirstOrDefault(s => s.Status == PriceProposalStatus.Accepted)?.Count ?? 0,
                RejectedRate = stats.FirstOrDefault(s => s.Status == PriceProposalStatus.Rejected)?.Count ?? 0,
                PendingRate = stats.FirstOrDefault(s => s.Status == PriceProposalStatus.Pending)?.Count ?? 0,
            };
        }
    }
}