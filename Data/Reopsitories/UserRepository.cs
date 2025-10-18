using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Reopsitories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;


        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager
            )
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IdentityResult> RegisterNewUser(RegisterNewUserRequest request)
        {
            try
            {
                var isEmailExist = await _userManager.FindByEmailAsync(request.Email);
                if (isEmailExist != null)
                {
                    return IdentityResult
                        .Failed(new IdentityError
                        {
                            Description = $"User with this email: {request.Email} already exists"
                        });
                }

                var isUserNameExist = await _userManager.FindByNameAsync(request.UserName);
                if (isUserNameExist != null)
                {
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
                    return IdentityResult
                        .Failed(creatUser.Errors.ToArray());
                }

                string defaultRole = "User";

                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
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

                    return IdentityResult
                        .Failed(new IdentityError
                        {
                            Description = $"Failed to assign role '{defaultRole}' to user. Errors: {errors}"
                        });
                }

                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
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

            if (user == null) return null;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            return token;
        }

        public async Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null) return IdentityResult.Failed(
                        new IdentityError
                        {
                            Description = $"User with email '{request.Email}' not found."
                        });

                var decodedToken = Uri.UnescapeDataString(request.Token);
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

                if (!result.Succeeded) return IdentityResult.Failed(
                    new IdentityError
                    {
                        Description = $"Email confirmation failed for user with email '{request.Email}'."
                    });

                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
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

            if (user == null) return null;


            string token = await _userManager.GeneratePasswordResetTokenAsync(user);

            if (string.IsNullOrEmpty(token)) return null;

            return token;
        }
    }
}