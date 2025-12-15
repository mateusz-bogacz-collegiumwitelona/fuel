using DTO.Requests;
using Services.Event;
using Services.Event.Interfaces;
using Services.Interfaces;

namespace Services.Event.Handlers
{
    public class ProposalEmailNotificationHandler : IEventHandler<PriceProposalEvaluatedEvent>
    {
        private readonly IEmailSender _emailSender;

        public ProposalEmailNotificationHandler(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task HandleAsync(PriceProposalEvaluatedEvent @event)
        {
            var stationInfo = new FindStationRequest
            {
                BrandName = @event.Station.Brand.Name,
                Street = @event.Station.Address.Street,
                HouseNumber = @event.Station.Address.HouseNumber,
                City = @event.Station.Address.City
            };

            await _emailSender.SendPriceProposalStatusEmail(
                @event.User.Email,
                @event.User.UserName,
                @event.IsAccepted,
                stationInfo,
                @event.ProposedPrice
            );
        }
    }
}
