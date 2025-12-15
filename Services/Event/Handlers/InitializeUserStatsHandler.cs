using Data.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;

namespace Services.Event.Handlers
{
    public class InitializeUserStatsHandler : IEventHandler<UserRegisteredEvent>
    {
        private readonly IProposalStatisticRepository _proposalStatisticRepo;
        private readonly ILogger<InitializeUserStatsHandler> _logger;

        public InitializeUserStatsHandler(IProposalStatisticRepository proposalStatisticRepo, ILogger<InitializeUserStatsHandler> logger)
        {
            _proposalStatisticRepo = proposalStatisticRepo;
            _logger = logger;
        }

        public async Task HandleAsync(UserRegisteredEvent @event)
        {
            var result = await _proposalStatisticRepo.AddProposalStatisticRecordAsync(@event.User);

            if (result)
            {
                _logger.LogInformation("Initialized stats for new user {Email}", @event.User.Email);
            }
            else
            {
                _logger.LogWarning("Failed to initialize stats for new user {Email}", @event.User.Email);
            }
        }
    }
}
