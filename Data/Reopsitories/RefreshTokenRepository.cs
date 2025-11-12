using Data.Context;
using Data.Interfaces;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
            => await _context.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == token);

        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid id)
            => await _context.RefreshTokens.Where(r => r.UserId == id).ToListAsync();

        public async Task AddAsync(RefreshToken token)
            => await _context.RefreshTokens.AddAsync(token);

        public async Task UpdateAsync(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var token = await _context.RefreshTokens.FindAsync(id);

            if (token != null) _context.RefreshTokens.Remove(token);
        }

        public async Task RevokeAllAsync(Guid id)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == id && r.IsActive)
                .ToListAsync();

            foreach (var t in tokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }

            _context.RefreshTokens.UpdateRange(tokens);
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}