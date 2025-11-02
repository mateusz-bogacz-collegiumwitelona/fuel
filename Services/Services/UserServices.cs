using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserServices> _logger;

        public UserServices(
            IUserRepository userRepository,
            ILogger<UserServices> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> ChangeUserNameAsync(string email, string userName) 
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName))
                {
                    _logger.LogWarning("Invalid input: email or userName is null or empty.");
                    return Result<bool>.Bad(
                        "Email and UserName must be provided.", 
                        StatusCodes.Status400BadRequest);
                }

                var isChanged = await _userRepository.ChangeUserNameAsync(email, userName);

                if (!isChanged)
                {
                    _logger.LogWarning("Failed to change UserName for user with email {Email}.", email);
                    return Result<bool>.Bad(
                        "Failed to change UserName.", 
                        StatusCodes.Status500InternalServerError);
                }

                return Result<bool>.Good(
                    "UserName changed successfully.", 
                    StatusCodes.Status200OK, isChanged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing UserName for user with email {Email}.", email);
                return Result<bool>.Bad("An unexpected error occurred.", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
