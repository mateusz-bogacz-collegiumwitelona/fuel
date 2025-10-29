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
            ILogger<UserRepository> logger
            )
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<IdentityResult> RegisterNewUser(RegisterNewUserRequest request)
        {
            try
            {
                var isEmailExist = await _userManager.FindByEmailAsync(request.Email);
                if (isEmailExist != null)
                {
                    _logger.LogWarning("Attempt to register with existing email: {Email}", request.Email);
                    return IdentityResult
                        .Failed(new IdentityError
                        {
                            Description = $"User with this email: {request.Email} already exists"
                        });
                }

                var isUserNameExist = await _userManager.FindByNameAsync(request.UserName);
                if (isUserNameExist != null)
                {
                    _logger.LogWarning("Attempt to register with existing username: {UserName}", request.UserName);
                    return IdentityResult
                        .Failed(new IdentityError
                        {
                            Description = $"User with this username: {request.UserName} already exists"
                        });
                }

                var newUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = request.UserName,
                    NormalizedUserName = request.UserName.ToUpper(),
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpper(),
                    EmailConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Points = 0
                };

                var creatUser = await _userManager.CreateAsync(newUser, request.Password);

                if (!creatUser.Succeeded)
                {
                    _logger.LogError("User creation failed for {Email}. Errors: {Errors}",
                        request.Email,
                        string.Join(", ", creatUser.Errors.Select(e => e.Description)));

                    return IdentityResult
                        .Failed(creatUser.Errors.ToArray());
                }

                string defaultRole = "User";

                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    _logger.LogError("Default role '{Role}' does not exist.", defaultRole);
                    return IdentityResult
                        .Failed(new IdentityError
                        {
                            Description = $"Role '{defaultRole}' does not exist"
                        });
                }

                var addUserToRole = await _userManager.AddToRoleAsync(newUser, defaultRole);

                if (!addUserToRole.Succeeded)
                {
                    var errors = string.Join(", ", addUserToRole.Errors.Select(e => e.Description));

                    _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}",
                        defaultRole,
                        request.Email,
                        errors);

                    return IdentityResult
                        .Failed(new IdentityError
                        {
                            Description = $"Failed to assign role '{defaultRole}' to user. Errors: {errors}"
                        });
                }

                bool isHaveProposalRecord = await _proposalStatistic.AddProposalStatisticRecordAsunc(request.Email);

                if (!isHaveProposalRecord)
                {
                    _logger.LogError("Failed to create proposal statistic record for user {Email}", request.Email);
                    return IdentityResult
                            .Failed(new IdentityError
                            {
                                Description = $"Failed to add Proposal record for {request.Email}"
                            });
                }

                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message} | throw: {ex.InnerException}");

                return IdentityResult
                    .Failed(new IdentityError
                    {
                        Description = $"An error occurred: {ex.Message} | throw: {ex.InnerException}"
                    });
            }
        }

        public async Task<string> GenerateConfirEmailTokenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return null;
                _logger.LogWarning("Attempt to generate email confirmation token for non-existing email: {Email}", email);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            return token;
        }

        public async Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Attempt to confirm email for non-existing email: {Email}", request.Email);

                    return IdentityResult.Failed(
                        new IdentityError
                        {
                            Description = $"User with email '{request.Email}' not found."
                        });
                }

                var decodedToken = Uri.UnescapeDataString(request.Token);
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

                if (!result.Succeeded)
                {
                    _logger.LogError("Email confirmation failed for {Email}. Errors: {Errors}",
                        request.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));

                    return IdentityResult.Failed(
                        new IdentityError
                        {
                            Description = $"Email confirmation failed for user with email '{request.Email}'.",
                        });
                }
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message} | throw: {ex.InnerException}");

                return IdentityResult
                    .Failed(new IdentityError
                    {
                        Description = $"An error occurred: {ex.Message} | throw: {ex.InnerException}"
                    });
            }
        }

        public async Task<string> GeneratePasswordResetToken(string email)
        {

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Attempt to generate password reset token for non-existing email: {Email}", email);
                return null;
            }

            string token = await _userManager.GeneratePasswordResetTokenAsync(user);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to generate password reset token for email: {Email}", email);
                return null;
            }

            return token;
        }

        
    }
}