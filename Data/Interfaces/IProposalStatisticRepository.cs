using DTO.Responses;

namespace Data.Interfaces
{
    public interface IProposalStatisticRepository
    {
        Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(string email);
        Task<bool> UpdateTotalProposalsAsync(bool isAccepted, Guid userId);        
        Task<bool> AddProposalStatisticRecordAsync(string email);
        Task<List<TopUserResponse>> GetTopUserListAsync();
    }
}
