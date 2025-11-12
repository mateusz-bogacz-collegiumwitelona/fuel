using Data.Models;
using DTO.Responses;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IProposalStatisticRepository
    {
        Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(ApplicationUser user);
        Task<bool> UpdateTotalProposalsAsync(bool isAccepted, Guid userId);
        Task<bool> AddProposalStatisticRecordAsync(ApplicationUser user);
        Task<List<TopUserResponse>> GetTopUserListAsync();
    }
}
