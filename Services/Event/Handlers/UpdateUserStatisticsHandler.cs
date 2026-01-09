using Data.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;

namespace Services.Event.Handlers
{
    public class UpdateUserStatisticsHandler : IEventHandler<PriceProposalEvaluatedEvent>
    {
        private readonly IProposalStatisticRepository _statsRepo;
        private readonly ILogger<UpdateUserStatisticsHandler> _logger;

        public UpdateUserStatisticsHandler(
            IProposalStatisticRepository statsRepo,
            ILogger<UpdateUserStatisticsHandler> logger)
        {
            _statsRepo = statsRepo;
            _logger = logger;
        }

        public async Task HandleAsync(PriceProposalEvaluatedEvent @event)
        {
            var result = await _statsRepo.UpdateTotalProposalsAsync(@event.IsAccepted, @event.User.Id);

            if (result)
            {
                _logger.LogInformation("User {UserId} statistics updated after evaluating proposal {ProposalId}",
                    @event.User.Id, @event.ProposalId);
            }
            else
            {
                _logger.LogWarning("Failed to update user statistics for {UserId} after evaluating proposal {ProposalId}",
                    @event.User.Id, @event.ProposalId);
            }
        }
    }
}

