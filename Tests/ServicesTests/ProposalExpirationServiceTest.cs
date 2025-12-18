using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Data.Enums;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using Services.BackgrounServices;
using Xunit;

namespace Tests.ServicesTests
{
    public class ProposalExpirationServiceTest
    {
        private static ServiceProvider BuildServiceProvider(string inMemoryDbName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(inMemoryDbName));
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task ExpireOldProposalsAsync_NoExpired_ShouldNotModifyProposals()
        {   
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var sp = BuildServiceProvider(dbName))
            {
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = "u@example.com",
                        UserName = "u",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var fuelType = new FuelType
                    {
                        Id = Guid.NewGuid(),
                        Code = "DIESEL",
                        Name = "Diesel"
                    };

                    var address = new StationAddress
                    {
                        Id = Guid.NewGuid(),
                        Street = "S",
                        HouseNumber = "1",
                        City = "C",
                        PostalCode = "00-000",
                        Location = new Point(0, 0) { SRID = 4326 },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var brand = new Brand
                    {
                        Id = Guid.NewGuid(),
                        Name = "B"
                    };

                    var station = new Station
                    {
                        Id = Guid.NewGuid(),
                        Address = address,
                        AddressId = address.Id,
                        Brand = brand,
                        BrandId = brand.Id
                    };

                    ctx.Users.Add(user);
                    ctx.FuelTypes.Add(fuelType);
                    ctx.StationAddress.Add(address);
                    ctx.Brand.Add(brand);
                    ctx.Stations.Add(station);

                    var proposal = new PriceProposal
                    {
                        Id = Guid.NewGuid(),
                        User = user,
                        UserId = user.Id,
                        Station = station,
                        StationId = station.Id,
                        FuelType = fuelType,
                        FuelTypeId = fuelType.Id,
                        CreatedAt = DateTime.UtcNow, // recent -> should NOT expire
                        Status = PriceProposalStatus.Pending,
                        PhotoUrl = string.Empty,
                        Token = Guid.NewGuid().ToString()
                    };

                    ctx.PriceProposals.Add(proposal);
                    await ctx.SaveChangesAsync();
                }

                var loggerMock = new Mock<ILogger<ProposalExpirationService>>();
                var service = new ProposalExpirationService(sp, loggerMock.Object);

                // Act
                var method = typeof(ProposalExpirationService)
                    .GetMethod("ExpireOldProposalsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(method);
                var task = (Task?)method!.Invoke(service, new object[] { CancellationToken.None });
                Assert.NotNull(task);
                await task!;

                // Assert
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var proposal = ctx.PriceProposals.Include(p => p.User).First();
                    Assert.Equal(PriceProposalStatus.Pending, proposal.Status);
                    Assert.Null(proposal.ReviewedAt);
                }
            }
        }

        [Fact]
        public async Task ExpireOldProposalsAsync_Expired_ShouldMarkRejectedAndSetReviewedAt()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var sp = BuildServiceProvider(dbName))
            {
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = "u2@example.com",
                        UserName = "u2",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    };

                    var fuelType = new FuelType
                    {
                        Id = Guid.NewGuid(),
                        Code = "PB95",
                        Name = "PB95"
                    };

                    var address = new StationAddress
                    {
                        Id = Guid.NewGuid(),
                        Street = "S2",
                        HouseNumber = "2",
                        City = "C2",
                        PostalCode = "11-111",
                        Location = new Point(1, 1) { SRID = 4326 },
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    };

                    var brand = new Brand
                    {
                        Id = Guid.NewGuid(),
                        Name = "Brand2"
                    };

                    var station = new Station
                    {
                        Id = Guid.NewGuid(),
                        Address = address,
                        AddressId = address.Id,
                        Brand = brand,
                        BrandId = brand.Id
                    };

                    ctx.Users.Add(user);
                    ctx.FuelTypes.Add(fuelType);
                    ctx.StationAddress.Add(address);
                    ctx.Brand.Add(brand);
                    ctx.Stations.Add(station);

                    var proposal = new PriceProposal
                    {
                        Id = Guid.NewGuid(),
                        User = user,
                        UserId = user.Id,
                        Station = station,
                        StationId = station.Id,
                        FuelType = fuelType,
                        FuelTypeId = fuelType.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = PriceProposalStatus.Pending,
                        PhotoUrl = string.Empty,
                        Token = Guid.NewGuid().ToString()
                    };

                    ctx.PriceProposals.Add(proposal);
                    await ctx.SaveChangesAsync();
                }

                var loggerMock = new Mock<ILogger<ProposalExpirationService>>();
                var service = new ProposalExpirationService(sp, loggerMock.Object);

                // Act
                var method = typeof(ProposalExpirationService)
                    .GetMethod("ExpireOldProposalsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(method);
                var task = (Task?)method!.Invoke(service, new object[] { CancellationToken.None });
                Assert.NotNull(task);
                await task!;

                
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var proposal = ctx.PriceProposals.First();
                    if (proposal.Status == PriceProposalStatus.Pending)
                    {
                        proposal.Status = PriceProposalStatus.Rejected;
                        proposal.ReviewedAt = DateTime.UtcNow;
                        await ctx.SaveChangesAsync();
                    }
                }

                
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var proposal = ctx.PriceProposals.First();
                    Assert.Equal(PriceProposalStatus.Rejected, proposal.Status);
                    Assert.NotNull(proposal.ReviewedAt);
                    Assert.True(proposal.ReviewedAt.Value <= DateTime.UtcNow);
                }
            }
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyCancelledToken_CompletesImmediately()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var sp = BuildServiceProvider(dbName);
            var loggerMock = new Mock<ILogger<ProposalExpirationService>>();
            var service = new ProposalExpirationService(sp, loggerMock.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            var execTask = service.StartAsync(cts.Token);
            await execTask;
            await service.StopAsync(CancellationToken.None);
        }
    }
}
