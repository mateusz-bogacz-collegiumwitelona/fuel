using Azure.Storage.Blobs;
using Data.Context;
using Data.Enums;
using Data.Interfaces;
using Data.Models;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using StackExchange.Redis;
using System.Security.Claims;

namespace Tests.ControllerTest;

public class CustomAppFact : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var fbDescriptors = services
                .Where(d =>
                    (d.ServiceType != null && d.ServiceType.FullName?.IndexOf("Facebook", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.ImplementationType != null && d.ImplementationType.FullName?.IndexOf("Facebook", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.ImplementationInstance != null && d.ImplementationInstance.GetType().FullName?.IndexOf("Facebook", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.ImplementationFactory != null && d.ImplementationFactory?.Method.DeclaringType?.FullName.IndexOf("Facebook", StringComparison.OrdinalIgnoreCase) >= 0)
                )
                .ToList();

            foreach (var d in fbDescriptors)
                services.Remove(d);
            services.RemoveAll<IConfigureOptions<FacebookOptions>>();
            services.RemoveAll<IPostConfigureOptions<FacebookOptions>>();
            var identityAppDescriptor = services.SingleOrDefault(d =>
                d.ServiceType.Name == "IConfigureOptions`1" &&
                d.ImplementationType?.Name.Contains("IdentityCookieOptionsSetup") == true);
            if (identityAppDescriptor != null)
                services.Remove(identityAppDescriptor);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // ✅ ZMIANA: Wyłączenie ostrzeżeń o transakcjach dla in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
                options.ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                );
            });

            var hostedServices = services.Where(s => s.ServiceType.Name.Contains("HostedService")).ToList();
            foreach (var hs in hostedServices)
                services.Remove(hs);

            var redisMock = new Mock<IConnectionMultiplexer>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(redisMock.Object);
            
            services.RemoveAll<BlobServiceClient>();
            var blobMock = new Mock<BlobServiceClient>();
            services.AddSingleton(blobMock.Object);

            services.RemoveAll<IStorage>();
            var storageMock = new Mock<IStorage>();
            services.AddSingleton(storageMock.Object);
            
            var adminId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Headers.TryGetValue("Authorization", out var auth))
                        {
                            var authValue = auth.ToString();
                            if (authValue.Contains("test-admin-token"))
                            {
                                var identity = new ClaimsIdentity(new[]
                                {
                                    new Claim(ClaimTypes.Email, "admin@test.com"),
                                    new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                                    new Claim(ClaimTypes.Name, "TestAdmin"),
                                    new Claim(ClaimTypes.Role, "Admin"),
                                }, "Test");
                                context.Principal = new ClaimsPrincipal(identity);
                                context.Success();
                            }
                            else if (authValue.Contains("test-user-token"))
                            {
                                var identity = new ClaimsIdentity(new[]
                                {
                                    new Claim(ClaimTypes.Email, "user@test.com"),
                                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                                    new Claim(ClaimTypes.Name, "TestUser"),
                                    new Claim(ClaimTypes.Role, "User"),
                                }, "Test");
                                context.Principal = new ClaimsPrincipal(identity);
                                context.Success();
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                db.RemoveRange(db.Set<Brand>());
                db.RemoveRange(db.Set<FuelType>());
                db.RemoveRange(db.Set<Station>());
                db.RemoveRange(db.Set<PriceProposal>());
                db.RemoveRange(db.Users);
                db.RemoveRange(db.Set<ApplicationUser>());
                
                var location = geometryFactory.CreatePoint(new Coordinate(10.0, 10.0));
                var user1 = new ApplicationUser { UserName = "TestUser", Id = userId, Email = "user@test.com", NormalizedUserName = "USER", NormalizedEmail = "USER@TEST.COM", SecurityStamp = Guid.NewGuid().ToString() };
                var admin = new ApplicationUser { UserName = "TestAdmin", Id = adminId, Email = "admin@test.com", NormalizedEmail = "ADMIN@TEST.COM", NormalizedUserName = "TESTADMIN", SecurityStamp = Guid.NewGuid().ToString() };
                var brand1 = new Brand { Name = "Orlen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                var brand2 = new Brand { Name = "Shell", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                var brand3 = new Brand { Name = "Test", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                var address1 = new StationAddress { City = "test", Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, HouseNumber = "1", PostalCode = "1", Street = "test", UpdatedAt = DateTime.UtcNow, Location = location };
                var station1 = new Station { Id = Guid.NewGuid(), Brand = brand1, BrandId = brand1.Id, CreatedAt = DateTime.UtcNow, Address = address1, AddressId = address1.Id };
                var ft1 = new FuelType { Name = "LPG gas", Code = "LPG", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Id = Guid.NewGuid() };
                var ft2 = new FuelType { Name = "Diesel", Code = "ON", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Id = Guid.NewGuid() };
                var ft3 = new FuelType { Name = "ZBenzyna 98", Code = "PB98", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Id = Guid.NewGuid() };
                var ft4 = new FuelType { Name = "YBenzyna 98", Code = "Y", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Id = Guid.NewGuid() };
                var ft5 = new FuelType { Name = "XDeleteBenzyna", Code = "X", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Id = Guid.NewGuid() };
                
                var pp1 = new PriceProposal { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, FuelType = ft2, FuelTypeId = ft2.Id, Token = "token1", ProposedPrice = 5.0m, PhotoUrl = "url1", Station = station1, StationId = station1.Id, User = user1, UserId = user1.Id, Status = PriceProposalStatus.Pending, ReviewedAt = null, ReviewedBy = null, Reviewer = null };
                var pp2 = new PriceProposal { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, FuelType = ft2, FuelTypeId = ft2.Id, Token = "token2", ProposedPrice = 4.0m, PhotoUrl = "url2", Station = station1, StationId = station1.Id, User = user1, UserId = user1.Id, Status = PriceProposalStatus.Rejected, ReviewedAt = null, ReviewedBy = null, Reviewer = null };
                var pp3 = new PriceProposal { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, FuelType = ft2, FuelTypeId = ft2.Id, Token = "token3", ProposedPrice = 4.0m, PhotoUrl = "url3", Station = station1, StationId = station1.Id, User = user1, UserId = user1.Id, Status = PriceProposalStatus.Pending, ReviewedAt = DateTime.UtcNow.AddDays(-1), ReviewedBy = admin.Id, Reviewer = admin };
                
                if (!db.Users.Any())
                {
                    db.Users.AddRange(admin, user1);
                    db.SaveChanges();
                }

                var adminRole = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                };
                
                var userRole = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    NormalizedName = "USER"
                };

                db.Roles.Add(adminRole);
                db.Roles.Add(userRole);
                db.SaveChanges();
                
                var adminUserRole = new IdentityUserRole<Guid>
                {
                    RoleId = adminRole.Id,
                    UserId = admin.Id
                };
                
                var normalUserRole = new IdentityUserRole<Guid>
                {
                    RoleId = userRole.Id,
                    UserId = user1.Id
                };
                
                db.UserRoles.Add(adminUserRole);
                db.UserRoles.Add(normalUserRole);
                db.SaveChanges();
                
                if (!db.Brand.Any())
                {
                    db.Brand.AddRange(new[] { brand1, brand2, brand3 });
                    db.SaveChanges();
                }
                
                if (!db.FuelTypes.Any())
                {
                    db.FuelTypes.AddRange(new[] { ft1, ft2, ft3, ft4, ft5 });
                    db.SaveChanges();
                }
                
                if (!db.PriceProposals.Any())
                {
                    db.PriceProposals.AddRange(pp1, pp2, pp3);
                    db.SaveChanges();
                }
            }
        });
    }
}
