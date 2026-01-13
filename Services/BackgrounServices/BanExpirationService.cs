using Data.Context;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Email;

namespace Services.BackgroundServices
{
    public class BanExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BanExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

        public BanExpirationService(
            IServiceProvider serviceProvider,
            ILogger<BanExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ban Expiration Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiredBansAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking expired bans");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Ban Expiration Service stopped");
        }

        private async Task CheckExpiredBansAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var emailSender = scope.ServiceProvider.GetRequiredService<EmailSender>();

            var expiredBans = await context.BanRecords
                .Include(b => b.User)
                .Where(b => b.IsActive
                       && b.BannedUntil.HasValue
                       && b.BannedUntil.Value <= DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            if (!expiredBans.Any())
            {
                _logger.LogDebug("No expired bans found");
                return;
            }

            _logger.LogInformation("Found {Count} expired bans to process", expiredBans.Count);

            foreach (var ban in expiredBans)
            {
                try
                {
                    ban.IsActive = false;
                    ban.UnbannedAt = DateTime.UtcNow;

                    var user = ban.User;
                    if (user != null)
                    {
                        await userManager.SetLockoutEndDateAsync(user, null);
                        await userManager.ResetAccessFailedCountAsync(user);
                        await emailSender.SendAutoUnlockEmailAsync(
                            user.Email,
                            user.UserName,
                            ban.Reason,
                            ban.BannedAt,
                            ban.BannedUntil.Value
                        );

                        _logger.LogInformation(
                            "Automatically unlocked user {Email} after ban expiration",
                            user.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error occurred while processing expired ban for user {UserId}",
                        ban.UserId);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
