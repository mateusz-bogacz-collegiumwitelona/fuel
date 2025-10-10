using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Data.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Data.Context;
using System.Runtime.ConstrainedExecution;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Data.Enums;

namespace Data.Seeder
{
    public class SeedData
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private ApplicationDbContext _context;

        private Random _random = new Random();

        public SeedData(
            RoleManager<IdentityRole<Guid>> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task InicializeAsync()
        {
            if (!await _roleManager.Roles.AnyAsync()) await SeedRolesAsync();
            if (!await _userManager.Users.AnyAsync()) await SeedUsersAsync();
            if (!await _context.Brand.AnyAsync()) await SeedBrandsAsync();
            if (!await _context.Stations.AnyAsync()) await SeedStationsAsync();
            if (!await _context.FuelTypes.AnyAsync()) await SeedFuelTypesAsync();
            if (!await _context.FuelPrices.AnyAsync()) await SeedFuelPriceAsync();
            if (!await _context.PriceProposals.AnyAsync()) await SeedPriceProposials();
            if (!await _context.ProposalStatisicts.AnyAsync()) await SeedPriceProposial();

            Console.WriteLine("Database seeding completed.");
        }

        public async Task SeedRolesAsync()
        {
            try
            {
                string[] rolesName = { "Admin", "User" };

                foreach (var roleName in rolesName)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName, NormalizedName = roleName.ToUpper() });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding roles: {ex.Message} | {ex.InnerException}");
            }
        }

        public async Task SeedUsersAsync()
        {
            try
            {
                if (!await _roleManager.RoleExistsAsync("Admin") || !await _roleManager.RoleExistsAsync("User"))
                {
                    await SeedRolesAsync();
                }

                await CreateUserIfNotExists("Admin", "admin@example.pl", "Admin123!", "Admin");
                await CreateUserIfNotExists("User", "user@example.pl", "User123!", "User");

                for (int i = 1; i <= 10; i++)
                {
                    string userName = $"User{i}";
                    string userEmail = $"user{i}@example.pl";
                    await CreateUserIfNotExists(userName, userEmail, "User123!", "User");
                }

                Console.WriteLine("All users seeded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding users: {ex.Message} | {ex.InnerException}");
            }
        }
        private async Task CreateUserIfNotExists(string userName, string email, string password, string role)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var newUser = new ApplicationUser
                {
                    Email = email,
                    NormalizedEmail = email.ToUpper(),
                    UserName = userName,
                    NormalizedUserName = userName.ToUpper(),
                    EmailConfirmed = true,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Points = 0
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, role);
                }
                else
                {
                    throw new Exception($"Failed to create {userName}");
                }
            }
        }

        public async Task SeedBrandsAsync()
        {
            try
            {
                string[] brandsName = { "Orlen", "Lotos", "Shell", "BP", "Moya", "Circle K" };

                foreach (var brandName in brandsName)
                {
                    if (!_context.Brand.Any(b => b.Name == brandName))
                    {
                        var brand = new Brand
                        {
                            Id = Guid.NewGuid(),
                            Name = brandName,
                            LogoUrl = " ",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Brand.Add(brand);
                    }
                    else
                    {
                        Console.WriteLine($"Brand {brandName} already exists.");
                    }
                }

                int result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    throw new Exception("Failed to seed Brands");
                }
                else
                {
                    Console.WriteLine("Brands seeded successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding brands: {ex.Message} | {ex.InnerException}");
            }
        }

        public async Task SeedStationsAsync()
        {
            try
            {
                var brands = await _context.Brand.ToListAsync();

                foreach (var brand in brands)
                {
                    bool isExist = await _context.Stations.AnyAsync(s => s.BrandId == brand.Id);

                    if (!isExist)
                    {
                        double minLat = 49.0, maxLat = 54.8;
                        double minLon = 14.1, maxLon = 24.2;

                        double latitude = minLat + (maxLat - minLat) * _random.NextDouble();
                        double longitude = minLon + (maxLon - minLon) * _random.NextDouble();

                        var station = new Station
                        {
                            Id = Guid.NewGuid(),
                            BrandId = brand.Id,
                            Brand = brand,
                            Address = $"Sample Address for {brand.Name}",
                            Location = new NetTopologySuite.Geometries.Point(longitude, latitude) { SRID = 4326 },
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Stations.Add(station);
                    }
                    else
                    {
                        Console.WriteLine($"Station for brand {brand.Name} already exists.");
                    }
                }

                int result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    throw new Exception("Failed to seed Stations");
                }
                else
                {
                    Console.WriteLine("Stations seeded successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding stations: {ex.Message} | {ex.InnerException}");
            }
        }

        public async Task SeedFuelTypesAsync()
        {
            try
            {
                var fuels = new[]
                {
                    new { Name = "PB95", Code = "PB95" },
                    new { Name = "PB98", Code = "PB98" },
                    new { Name = "ON", Code = "ON" },
                    new { Name = "LPG", Code = "LPG" },
                    new { Name = "E85", Code = "E85" }
                };


                foreach (var fuel in fuels)
                {
                    if (!await _context.FuelTypes.AnyAsync(ft => ft.Name == fuel.Name))
                    {
                        _context.FuelTypes.Add(new FuelType
                        {
                            Id = Guid.NewGuid(),
                            Name = fuel.Name,
                            Code = fuel.Code,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        Console.WriteLine($"FuelType {fuel.Name} already exists.");
                    }
                }

                int result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    throw new Exception("Failed to seed FuelTypes");
                }
                else
                {
                    Console.WriteLine("FuelTypes seeded successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding fuel types: {ex.Message} | {ex.InnerException}");
            }
        }

        public async Task SeedFuelPriceAsync()
        {
            try
            {
                var stations = await _context.Stations.ToListAsync();
                var fuelTypes = await _context.FuelTypes.ToListAsync();

                foreach (var station in stations)
                {
                    foreach (var fuelType in fuelTypes)
                    {
                        bool isExist = await _context.FuelPrices.AnyAsync(fp => fp.StationId == station.Id && fp.FuelTypeId == fuelType.Id);
                        if (!isExist)
                        {
                            var fuelPrice = new FuelPrice
                            {
                                Id = Guid.NewGuid(),
                                StationId = station.Id,
                                FuelTypeId = fuelType.Id,
                                Price = Math.Round((decimal)(_random.NextDouble() * (7.0 - 4.0) + 4.0), 2),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.FuelPrices.Add(fuelPrice);
                        }
                    }
                }

                int result = await _context.SaveChangesAsync();


                if (result <= 0)
                {
                    throw new Exception("Failed to seed FuelPrices");
                }
                else
                {
                    Console.WriteLine("FuelPrices seeded successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding fuel prices: {ex.Message} | {ex.InnerException}");
            }
        }

        public async Task SeedPriceProposials()
        {
            try
            {
                var stations = await _context.Stations.ToListAsync();
                var users = await _userManager.GetUsersInRoleAsync("User");
                var fuelsTypes = await _context.FuelTypes.ToListAsync();

                if (stations.Count == 0 || users.Count == 0 || fuelsTypes.Count == 0)
                {
                    throw new Exception("Stations, Users or FuelTypes data is missing. Please seed them first.");
                }

                foreach (var station in stations)
                {
                    if (!_context.PriceProposals.Any(pp => pp.StationId == station.Id))
                    {
                        var user = users[_random.Next(users.Count)];
                        var priceProposal = new PriceProposal
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            StationId = station.Id,
                            PhotoUrl = " ",
                            FuelTypeId = fuelsTypes[_random.Next(fuelsTypes.Count)].Id,
                            ProposedPrice = Math.Round(Math.Abs((decimal)(_random.NextDouble() * (7.0 - 4.0) + 4.0)), 2),
                            Status = PriceProposalStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.PriceProposals.Add(priceProposal);
                    }
                    else
                    {
                        Console.WriteLine($"PriceProposial for station {station.Id} already exists.");

                    }
                }

                int result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    throw new Exception("Failed to seed PriceProposials");
                }
                else
                {
                    Console.WriteLine("PriceProposials seeded successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding price proposials: {ex.Message} | {ex.InnerException}");
            }
        }

        public async Task SeedPriceProposial()
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                foreach (var user in users)
                {
                    if (!await _context.ProposalStatisicts.AnyAsync(ps => ps.UserId == user.Id))
                    {
                        int total = _random.Next(1, 20);
                        int approved = _random.Next(1, total);
                        int rejected = total - approved;
                        int rate = (int)(((double)approved / total) * 100);

                        var poposal = new ProposalStatistic
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            User = user,
                            TotalProposals = total,
                            ApprovedProposals = approved,
                            RejectedProposals = rejected,
                            AcceptedRate = rate,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _context.ProposalStatisicts.AddAsync(poposal);
                    }
                }

                int result = await _context.SaveChangesAsync();

                if (result <= 0) {
                    Console.WriteLine("Error during save changes");
                } else
                {
                    Console.WriteLine("Save success");
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} | {ex.InnerException}");
            }
        }
    }
}
