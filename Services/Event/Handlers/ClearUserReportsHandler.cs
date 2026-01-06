using Data.Interfaces;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;

namespace Services.Event.Handlers
{
    public class ClearUserReportsHandler : IEventHandler<UserBannedEvent>
    {
        private readonly IReportRepositry _reportRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClearUserReportsHandler> _logger;

        public ClearUserReportsHandler(
            IReportRepositry reportRepo,
            UserManager<ApplicationUser> userManager,
            ILogger<ClearUserReportsHandler> logger
            ) 
        {
            _reportRepo = reportRepo;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task HandleAsync(UserBannedEvent @event)
        {
            var admin = await _userManager.FindByIdAsync(@event.AdminId.ToString());

            if (admin != null)
            {
                await _reportRepo.ClearReports(@event.User.Id, admin);
                _logger.LogInformation("Cleared pending reports for banned user {Email}", @event.User.Email);
            }
            else
            {
                _logger.LogWarning("Could not find admin with ID {AdminId} to clear reports.", @event.AdminId);
            }
        }
    }
}
