using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid id);
        Task AddAsync(RefreshToken token);
        Task UpdateAsync(RefreshToken token);
        Task DeleteAsync(Guid id);
        Task RevokeAllAsync(Guid id);
        Task SaveChangesAsync();
    }
}
