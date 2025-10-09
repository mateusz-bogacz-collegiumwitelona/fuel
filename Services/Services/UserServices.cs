using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailServices _emailServices;

        public UserServices(
            IUserRepository userRepository,
            IEmailServices emailServices)
        {
            _userRepository = userRepository;
            _emailServices = emailServices;
        }

        public async Task<Result<ConfirmEmailRequest>> RegisterNewUser(RegisterNewUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "UserName is required." });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Password is required." });

                if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ConfirmPassword is required." });

                if (request.Password != request.ConfirmPassword)
                {
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Password and Confirm Password do not match." }
                        );
                }

                var result = await _userRepository.RegisterNewUser(request);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return Result<ConfirmEmailRequest>.Bad(
                        "User registration failed",
                        StatusCodes.Status400BadRequest,
                        errors
                        );
                }

                string token = await _userRepository.GenerateConfirEmailTokenAsync(request.Email);

                if (string.IsNullOrWhiteSpace(token)) return Result<ConfirmEmailRequest>.Bad(
                        "Failed to generate email confirmation token.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "Token generation returned null or empty." }
                        );

                string encodedToken = Uri.EscapeDataString(token);

                if (string.IsNullOrWhiteSpace(encodedToken)) return Result<ConfirmEmailRequest>.Bad(
                        "Failed to encode email confirmation token.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "Encoded token is null or empty." }
                        );

                string confirmUrl = $"http://localhost:5000/api/auth/confirm-email?email={Uri.EscapeDataString(request.Email)}&token={encodedToken}";

                var sendEmail = await _emailServices.SendEmailConfirmationAsync(
                    request.Email,
                    request.UserName,
                    confirmUrl,
                    encodedToken
                    );

                if (!sendEmail.IsSuccess) return Result<ConfirmEmailRequest>.Bad(
                        "Failed to send email.",
                        StatusCodes.Status500InternalServerError
                        );

                var confirmEmailRequest = new ConfirmEmailRequest
                {
                    Email = request.Email,
                    Token = encodedToken
                };

                return Result<ConfirmEmailRequest>.Good(
                    "User registered successfully",
                    StatusCodes.Status201Created,
                    confirmEmailRequest
                    );
            }
            catch (Exception ex)
            {
                return Result<ConfirmEmailRequest>.Bad(
                    "An error occurred while registering the user.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<Result<IdentityResult>> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email)) return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Token)) return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Token is required." });

                var result = await _userRepository.ConfirmEmailAsync(request);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return Result<IdentityResult>.Bad(
                        "Email confirmation failed",
                        StatusCodes.Status400BadRequest,
                        errors
                        );
                }

                return Result<IdentityResult>.Good(
                    "Email confirmed successfully",
                    StatusCodes.Status200OK,
                    result
                    );
            }
            catch (Exception ex)
            {
                return Result<IdentityResult>.Bad(
                    "An error occurred while confirming the email.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }
    }
}
