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
                // seed a non-expired pending proposal
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    ctx.PriceProposals.Add(new PriceProposal
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        Status = PriceProposalStatus.Pending
                    });
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
                    var proposal = ctx.PriceProposals.First();
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
                // seed an expired pending proposal (older than 24 hours)
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    ctx.PriceProposals.Add(new PriceProposal
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow.Subtract(TimeSpan.FromHours(25)),
                        Status = PriceProposalStatus.Pending
                    });
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
            cts.Cancel(); // already cancelled

            // Act & Assert: should return quickly without throwing
            var execTask = service.StartAsync(cts.Token);
            await execTask;
            await service.StopAsync(CancellationToken.None);
        }
    }
}
