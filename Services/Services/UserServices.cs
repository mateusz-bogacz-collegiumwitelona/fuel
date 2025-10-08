using Data.Interfaces;
using DTO.Requests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Helpers;
using Microsoft.AspNetCore.Http;
using Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics;

namespace Services.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepository;

        public UserServices(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<IdentityResult>> RegisterNewUser(RegisterNewUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest, 
                        new List<string> { "UserName is required." });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return Result<IdentityResult>.Bad(
                        "Validation Error", 
                        StatusCodes.Status400BadRequest, 
                        new List<string> { "Password is required." });

                if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                    return Result<IdentityResult>.Bad(
                        "Validation Error", 
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ConfirmPassword is required." });

                if (request.Password != request.ConfirmPassword)
                {
                    return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Password and Confirm Password do not match." }
                        );
                }

                var result = await _userRepository.RegisterNewUser(request);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return Result<IdentityResult>.Bad(
                        "User registration failed",
                        StatusCodes.Status400BadRequest,
                        errors
                        );
                }

                return Result<IdentityResult>.Good(
                    "User registered successfully",
                    StatusCodes.Status201Created,
                    result
                    );
            }
            catch (Exception ex)
            {
                return Result<IdentityResult>.Bad(
                    "An error occurred while registering the user.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }
    }
}
