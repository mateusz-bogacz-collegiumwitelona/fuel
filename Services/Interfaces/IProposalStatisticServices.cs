using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IProposalStatisticServices
    {
        Task<Result<GetProposalStatisticResponse>> GetUserProposalStatisticResponse(string email);
        Task<Result<PagedResult<TopUserResponse>>> GetTopUserListAsync(GetPaggedRequest pagged);    }
}
