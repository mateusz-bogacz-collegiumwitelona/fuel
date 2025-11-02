using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Data.Reopsitories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(
            ApplicationDbContext context, 
            ILogger<UserRepository> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<bool> ChangeUserNameAsync(string email, string userName)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found.", email);
                return false;
            }

            user.UserName = userName;
            user.NormalizedUserName = userName.ToUpper();

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("UserName for user with email {Email} changed to {UserName}.", email, userName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to change UserName for user with email {Email}. Errors: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }

    }
}