using Data.Context;
using Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.BackgrounServices
{
    public class ProposalExpirationService : BackgroundService 
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProposalExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _expirationTime = TimeSpan.FromHours(24);

        public ProposalExpirationService(
            IServiceProvider serviceProvider,
            ILogger<ProposalExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Proposal Expiration Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireOldProposalsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking expired proposals");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Proposal Expiration Service stopped");
        }

        private async Task ExpireOldProposalsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expirationThreshold = DateTime.UtcNow.Subtract(_expirationTime);

            var expiredProposals = await context.PriceProposals
                .Include(p => p.User)
                .Include(p => p.Station)
                    .ThenInclude(s => s.Address)
                .Include(p => p.FuelType)
                .Where(p => p.Status == PriceProposalStatus.Pending
                       && p.CreatedAt < expirationThreshold)
                .ToListAsync(stoppingToken);

            if (!expiredProposals.Any())
            {
                _logger.LogDebug("No expired proposals found");
                return;
            }

            foreach (var proposal in expiredProposals)
            {
                try
                {
                    proposal.Status = PriceProposalStatus.Rejected;
                    proposal.ReviewedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Auto-rejected expired proposal {ProposalId} for station {StationId}, created at {CreatedAt}",
                        proposal.Id,
                        proposal.StationId,
                        proposal.CreatedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error occurred while processing expired proposal {ProposalId}",
                        proposal.Id);
                }
            }

            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Successfully processed {Count} expired proposals", expiredProposals.Count);
        }
    }
}
