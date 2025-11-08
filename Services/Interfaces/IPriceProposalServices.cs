using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IPriceProposalServices
    {
        Task<Result<string>> AddNewProposalAsync(string email, AddNewPriceProposalRequest request);
        Task<Result<GetPriceProposalResponse>> GetPriceProposal(string photoToken);
        Task<Result<PagedResult<GetStationPriceProposalResponse>>> GetStationPriceProposalsAsync(GetPaggedRequest pagged, FindStationRequest request);
    }
}
