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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IProposalStatisticRepository _proposalStatistic;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<UserRepository> logger,
            IProposalStatisticRepository proposalStatistic
            )
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _proposalStatistic = proposalStatistic;
        }

        
        public async Task<string> RegisterNewUserAsync(RegisterNewUserRequest request)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                NormalizedUserName = request.UserName.ToUpper(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                Points = 0,
            };

            var createUser = await _userManager.CreateAsync(user, request.Password);
            if (!createUser.Succeeded)
            {
                _logger.LogError("Error occurred while creating user {UserName}: {Errors}", request.UserName, string.Join(", ", createUser.Errors.Select(e => e.Description)));
                return null;
            }

            string defaultRole = "User";

            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                _logger.LogError("Default role {Role} does not exist.", defaultRole);
                return null;
            }

            var addToRole = await _userManager.AddToRoleAsync(user, defaultRole);

            if (!addToRole.Succeeded)
            {
                var errors = string.Join(", ", addToRole.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}",
                    defaultRole, request.Email, errors);

                return null;
            }

            var isProposalStatAdded = await _proposalStatistic.AddProposalStatisticRecordAsync(request.Email);

            if (!isProposalStatAdded)
            {
                _logger.LogError("Failed to create proposal statistics record for user {Email}.", request.Email);
                return null;
            }

            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            if (string.IsNullOrEmpty(confirmToken))
            {
                _logger.LogError("Failed to generate email confirmation token for user {Email}.", request.Email);
                return null;
            }

            _logger.LogInformation("User {Email} registered successfully. Generate a confirm token", request.Email);

            return confirmToken;
        }

        public async Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Email confirmation attempt for non-existent email: {Email}", request.Email);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = $"User with email {request.Email} not found"
                });
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for user: {Email}", request.Email);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "EmailAlreadyConfirmed",
                    Description = "Email is already confirmed"
                });
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                string error = string.Join(", ", errors);

                _logger.LogWarning("Email confirmation failed for {Email}. Errors: {Errors}",
                    request.Email, error);

                return result; 
            }

            _logger.LogInformation("Email successfully confirmed for user: {Email}", request.Email);
            return IdentityResult.Success;
        }
    }
}