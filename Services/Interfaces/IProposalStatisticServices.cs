using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IProposalStatisticServices
    {
        Task<Result<GetProposalStatisticResponse>> GetUserProposalStatisticResponse(string email);
    }
}
