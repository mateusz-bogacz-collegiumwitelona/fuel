using Data.Context;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Services.Commands
{
    public class CreateUserCommand : BaseCommand
    {
        public override string Name => "user:create";
        public override string Description => "Create a new user with email confirmation";

        public CreateUserCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override async Task ExecuteAsync(string[] args)
        {

            Console.Write("Email: ");
            string email = Console.ReadLine()?.Trim();

            Console.Write("Username: ");
            string userName = Console.ReadLine()?.Trim();

            Console.Write("Password: ");
            string password = ReadPassword();

            Console.Write("Confirm Password: ");
            string confirmPassword = ReadPassword();

            if (
                string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(userName) || 
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(confirmPassword))
            {
                PrintError("All fields are required!");
                return;
            }

            if (password != confirmPassword)
            {
                PrintError("Passwords do not match!");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await userManager.FindByEmailAsync(email) != null)
            {
                PrintError($"User with email '{email}' already exists.");
                return;
            }

            if (await userManager.FindByNameAsync(userName) != null)
            {
                PrintError($"User with username '{userName}' already exists.");
                return;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                NormalizedUserName = userName.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                PrintError("Failed to create user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error.Description}");
                }
                return;
            }

            string defaultRole = "User";
            if (!await roleManager.RoleExistsAsync(defaultRole))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = defaultRole });
            }

            await userManager.AddToRoleAsync(user, defaultRole);

            var proposal = new ProposalStatistic
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                TotalProposals = 0,
                ApprovedProposals = 0,
                RejectedProposals = 0,
                AcceptedRate = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await context.ProposalStatistics.AddAsync(proposal);
            int isSaved = await context.SaveChangesAsync();

            if (isSaved <= 0)
            {
                Console.WriteLine("Failed to add proposal statistics for user.", user.Email);
                return;
            }

            PrintSuccess("User created successfully!");
            PrintInfo($"Email: {email}");
            PrintInfo($"Username: {userName}");
            PrintInfo($"ID: {user.Id}");
        }

        private string ReadPassword()
        {
            var password = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    password += keyInfo.KeyChar;
                    Console.Write("*");
                }
            } while (key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
}
