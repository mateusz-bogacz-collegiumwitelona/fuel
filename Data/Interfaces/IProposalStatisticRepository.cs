using DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IProposalStatisticRepository
    {
        Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(string email);
        Task<bool> UpdateTotalProposalsAsync(bool proposial, string email);
        Task<bool> AddProposalStatisticRecordAsunc(string email);
    }
}
