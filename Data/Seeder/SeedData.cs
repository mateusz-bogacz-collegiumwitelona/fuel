using Data.Context;
using Data.Enums;
using Data.Helpers;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

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
            if (!await _context.ProposalStatistics.AnyAsync()) await SeedProposalStatisticsAsync();
            if (!await _context.Brand.AnyAsync()) await SeedBrandsAsync();
            if (!await _context.Stations.AnyAsync()) await SeedStationsAsync();
            if (!await _context.FuelTypes.AnyAsync()) await SeedFuelTypesAsync();
            if (!await _context.FuelPrices.AnyAsync()) await SeedFuelPriceAsync();
            if (!await _context.PriceProposals.AnyAsync()) await SeedPriceProposals();
            
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

                string adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
                string adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

                await CreateUserIfNotExists("Admin", adminEmail, adminPass, "Admin");

                for (int i = 1; i <= 10; i++)
                {
                    string userName = $"User{i}";
                    string userEmail = $"user{i}@example.pl";
                    string pass = GenerateSecurePassword();
                    await CreateUserIfNotExists(userName, userEmail, pass, "User");
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
                    IsDeleted = false
                };

                var result = await _userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, role);
                    Console.WriteLine($"✓ User {userName} created successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create {userName}:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error.Code}: {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"User {userName} already exists - skipping");
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
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                var query = @"
            [out:json][timeout:300];
            area[""ISO3166-1""=""PL""]->.poland;
            (
              node[""amenity""=""fuel""](area.poland);
              way[""amenity""=""fuel""](area.poland);
              relation[""amenity""=""fuel""](area.poland);
            );
            out center;
        ";

                Console.WriteLine("Fetching stations from Overpass API...");
                var content = new StringContent($"data={query}", Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await httpClient.PostAsync("https://overpass-api.de/api/interpreter", content);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonString);

                var elements = jsonDoc.RootElement.GetProperty("elements").EnumerateArray().ToList();
                Console.WriteLine($"Found {elements.Count} potential stations from Overpass API");

                var existingStations = await _context.Stations
                    .Include(s => s.Address)
                    .ToListAsync();

                var existingLocations = new HashSet<string>();
                foreach (var station in existingStations)
                {
                    if (station.Address?.Location != null)
                    {
                        var key = $"{Math.Round(station.Address.Location.Y, 6)}_{Math.Round(station.Address.Location.X, 6)}";
                        existingLocations.Add(key);
                    }
                }

                Console.WriteLine($"Found {existingLocations.Count} existing stations in database");

                var batch = new List<Station>();
                int processedCount = 0;
                int skippedCount = 0;
                int addedCount = 0;

                foreach (var element in elements)
                {
                    processedCount++;

                    var tags = element.TryGetProperty("tags", out var t) ? t : default;

                    double lat = element.TryGetProperty("lat", out var latEl)
                        ? latEl.GetDouble()
                        : element.GetProperty("center").GetProperty("lat").GetDouble();
                    double lon = element.TryGetProperty("lon", out var lonEl)
                        ? lonEl.GetDouble()
                        : element.GetProperty("center").GetProperty("lon").GetDouble();

                    var locationKey = $"{Math.Round(lat, 6)}_{Math.Round(lon, 6)}";
                    if (existingLocations.Contains(locationKey))
                    {
                        skippedCount++;
                        continue;
                    }

                    var street = tags.TryGetProperty("addr:street", out var s) ? s.GetString() ?? "" : "";
                    var number = tags.TryGetProperty("addr:housenumber", out var n) ? n.GetString() ?? "" : "";
                    var city = tags.TryGetProperty("addr:city", out var c) ? c.GetString() ?? "" : "";
                    var postal = tags.TryGetProperty("addr:postcode", out var p) ? p.GetString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(street) ||
                        string.IsNullOrWhiteSpace(number) ||
                        string.IsNullOrWhiteSpace(city) ||
                        string.IsNullOrWhiteSpace(postal))
                    {
                        skippedCount++;
                        continue;
                    }

                    string brandName = GetBrandName(tags);
                    var brand = await _context.Brand.FirstOrDefaultAsync(b => b.Name.ToLower() == brandName.ToLower());

                    if (brand == null)
                    {
                        brand = new Brand
                        {
                            Id = Guid.NewGuid(),
                            Name = brandName,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Brand.Add(brand);
                        await _context.SaveChangesAsync();
                    }

                    var stationAddress = new StationAddress
                    {
                        Id = Guid.NewGuid(),
                        Street = street,
                        HouseNumber = number,
                        City = city,
                        PostalCode = postal,
                        Location = new NetTopologySuite.Geometries.Point(lon, lat) { SRID = GeoConstants.SRID_VALUE },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Add(stationAddress);
                    await _context.SaveChangesAsync();

                    var station = new Station
                    {
                        Id = Guid.NewGuid(),
                        BrandId = brand.Id,
                        AddressId = stationAddress.Id,
                        Address = stationAddress,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    batch.Add(station);
                    existingLocations.Add(locationKey); 
                    addedCount++;

                    if (batch.Count >= 100)
                    {
                        _context.Stations.AddRange(batch);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Progress: {processedCount}/{elements.Count} processed, {addedCount} added, {skippedCount} skipped");
                        batch.Clear();
                    }
                }

                if (batch.Any())
                {
                    _context.Stations.AddRange(batch);
                    await _context.SaveChangesAsync();
                }

                Console.WriteLine($"Stations seeding completed!");
                Console.WriteLine($"Total processed: {processedCount}");
                Console.WriteLine($"Added: {addedCount}");
                Console.WriteLine($"Skipped (duplicates): {skippedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while seeding stations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
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
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
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
                                UpdatedAt = DateTime.UtcNow,
                                ValidFrom = DateTime.UtcNow.AddDays(-_random.Next(0, 30))
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

        public async Task SeedPriceProposals()
        {
            try
            {
                if (await _context.PriceProposals.AnyAsync())
                {
                    Console.WriteLine("PriceProposals already exist - skipping seeding");
                    return;
                }

                var stations = await _context.Stations.ToListAsync();
                var users = await _userManager.GetUsersInRoleAsync("User");
                var fuelTypes = await _context.FuelTypes.ToListAsync();

                if (stations.Count == 0 || users.Count == 0 || fuelTypes.Count == 0)
                {
                    throw new Exception($"Missing data - Stations: {stations.Count}, Users: {users.Count}, FuelTypes: {fuelTypes.Count}. Please seed them first.");
                }

                var proposals = new List<PriceProposal>();
                int totalToGenerate = 100; 

                for (int i = 0; i < totalToGenerate; i++)
                {
                    var station = stations[_random.Next(stations.Count)];
                    var user = users[_random.Next(users.Count)];
                    var fuelType = fuelTypes[_random.Next(fuelTypes.Count)];

                    var proposal = new PriceProposal
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        StationId = station.Id,
                        FuelTypeId = fuelType.Id,
                        ProposedPrice = Math.Round((decimal)(_random.NextDouble() * (7.0 - 4.0) + 4.0), 2),
                        PhotoUrl = $"proposals/{Guid.NewGuid()}.jpg",
                        Token = Guid.NewGuid().ToString("N"),
                        Status = PriceProposalStatus.Pending,
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 30))
                    };

                    proposals.Add(proposal);
                }

                _context.PriceProposals.AddRange(proposals);
                int saved = await _context.SaveChangesAsync();

                Console.WriteLine($"Successfully seeded {saved} price proposals");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding price proposals: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        public async Task SeedProposalStatisticsAsync()
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                foreach (var user in users)
                {
                    if (!await _context.ProposalStatistics.AnyAsync(ps => ps.UserId == user.Id))
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
                            Points = approved,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _context.ProposalStatistics.AddAsync(poposal);
                    }
                }

                int result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    Console.WriteLine("Error during save changes");
                }
                else
                {
                    Console.WriteLine("Save success");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} | {ex.InnerException}");
            }
        }

        private string GetBrandName(JsonElement tags)
        {
            if (tags.ValueKind == JsonValueKind.Undefined)
                return "Unknown";

            if (tags.TryGetProperty("brand", out var brandEl))
                return brandEl.GetString() ?? "Unknown";

            if (tags.TryGetProperty("name", out var nameEl))
                return nameEl.GetString() ?? "Unknown";

            return "Unknown";
        }


        private string GenerateSecurePassword(int length = 16)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var password = new char[length];

            password[0] = lowercase[_random.Next(lowercase.Length)];
            password[1] = uppercase[_random.Next(uppercase.Length)];
            password[2] = digits[_random.Next(digits.Length)];
            password[3] = special[_random.Next(special.Length)];

            string allChars = lowercase + uppercase + digits + special;
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[_random.Next(allChars.Length)];
            }

            for (int i = length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}
