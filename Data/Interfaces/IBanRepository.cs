using Data.Models;
using DTO.Requests;
using DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IBanRepository
    {
        Task<bool> AddBanRecordAsync(ApplicationUser user, ApplicationUser admin, SetLockoutForUserRequest request);
        Task DeactivateActiveBansAsync(Guid userId, Guid unbannedByAdminId);
        Task<ReviewUserBanResponses> GetUserBanInfoAsync(string email);
    }
}
