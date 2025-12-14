using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IPriceProposalServices
    {
        Task<Result<string>> AddNewProposalAsync(string email, AddNewPriceProposalRequest request);
        Task<Result<GetPriceProposalResponse>> GetPriceProposal(string photoToken);
        Task<Result<PagedResult<GetStationPriceProposalResponse>>> GetAllPriceProposal(GetPaggedRequest pagged, TableRequest request);
        Task<Result<bool>> ChangePriceProposalStatus(string adminEmail, bool isAccepted, string photoToken);
        Task<Result<GetPriceProposalStaisticResponse>> GetPriceProposalStaisticAsync();
    }
}
