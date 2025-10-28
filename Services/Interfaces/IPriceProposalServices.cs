using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IPriceProposalServices
    {
        Task<Result<string>> AddNewProposalAsync(AddNewPriceProposalRequest request);
        Task<Result<GetPriceProposalResponse>> GetPriceProposal(string photoToken);
    }
}
