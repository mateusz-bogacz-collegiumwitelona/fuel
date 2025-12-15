using Data.Models;
using Services.Event.Interfaces;

namespace Services.Event
{
    public class PriceProposalEvaluatedEvent : IEvent
    {
        public ApplicationUser User { get; }
        public Station Station { get; }
        public decimal ProposedPrice { get; }
        public bool IsAccepted { get; }
        public string? ProposalId { get; internal set; }

        public PriceProposalEvaluatedEvent(PriceProposal proposal, bool isAccepted)
        {
            User = proposal.User;
            Station = proposal.Station;
            ProposedPrice = proposal.ProposedPrice;
            IsAccepted = isAccepted;
        }
    }
}
