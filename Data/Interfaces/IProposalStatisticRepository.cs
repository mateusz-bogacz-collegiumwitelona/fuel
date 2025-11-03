using DTO.Responses;

namespace Data.Interfaces
{
    public interface IProposalStatisticRepository
    {
        Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(string email);
        Task<bool> UpdateTotalProposalsAsync(bool proposial, string email);
        Task<bool> AddProposalStatisticRecordAsunc(string email);
    }
}
