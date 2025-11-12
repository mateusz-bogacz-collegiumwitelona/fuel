using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Services.Commands
{
    public class PromoteUserCommand : BaseCommand
    {
        public override string Name => "user:promote";
        public override string Description => "Promote user to Admin role";

        public PromoteUserCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override async Task ExecuteAsync(string[] args)
        {
            Console.Write("Email: ");
            var email = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                PrintError("Email is required!");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                PrintError($"User with email '{email}' not found.");
                return;
            }

            string adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                Console.WriteLine("Admin role dosn't exist");
                return;
            }

            if (await userManager.IsInRoleAsync(user, adminRole))
            {
                PrintWarning($"User '{email}' is already an Admin.");
                return;
            }

            string userRole = "User";

            var removeResult = await userManager.RemoveFromRolesAsync(user, new[] { userRole });

            if (!removeResult.Succeeded)
            {
                PrintError("Failed to remove User role:");
                foreach (var error in removeResult.Errors)
                {
                    Console.WriteLine($"   - {error.Description}");
                }
                return;
            }

            var result = await userManager.AddToRoleAsync(user, adminRole);

            if (result.Succeeded)
            {
                var roles = await userManager.GetRolesAsync(user);
                PrintSuccess($"User '{email}' promoted to Admin!");
                PrintInfo($"Current roles: {string.Join(", ", roles)}");
            }
            else
            {
                PrintError("Failed to promote user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error.Description}");
                }
            }
        }
    }
}
