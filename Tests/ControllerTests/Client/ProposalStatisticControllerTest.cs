using Data.Context;
using Data.Models;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Services.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Tests.ControllerTests;
using Xunit;

namespace Tests.ControllerTest.Client
{
    [Collection("IntegrationTests")]
    public class ProposalStatisticControllerTest : IAsyncLifetime
    {
        private HttpClient _client;
        private CustomAppFact _factory;

        public async Task InitializeAsync()
        {
            _factory = new CustomAppFact();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-user-token");
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task GetUserProposalStatisticResponseTest_200OK()
        {
            //Arrange
            //--

            // Act
            var response = await _client.GetAsync("/api/proposal-statistic");
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("totalProposals", out _));
            Assert.True(root.TryGetProperty("approvedProposals", out _));
            Assert.True(root.TryGetProperty("rejectedProposals", out _));
            Assert.True(root.TryGetProperty("acceptedRate", out _));
            Assert.True(root.TryGetProperty("updatedAt", out _));
        }

        [Fact]
        public async Task GetUserProposalStatisticResponseTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.GetAsync("/api/proposal-statistic");

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserProposalStatisticResponseTest_404()
        {
            //Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-user2-token");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "user2@test.com");
                Assert.NotNull(user);
                var statsForUser2 = await db.ProposalStatistics.Where(ps => ps.UserId == user.Id).ToListAsync();
                if (statsForUser2.Any())
                {
                    db.ProposalStatistics.RemoveRange(statsForUser2);
                    await db.SaveChangesAsync();
                }
            }
            //Act
            var response = await client.GetAsync("/api/proposal-statistic");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTopUserListAsyncTest_200OK()
        {
            //Arrange
            var requestUri = "/api/proposal-statistic/top-users?PageNumber=1&PageSize=10";

            //Act
            var response = await _client.GetAsync(requestUri);
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("items", out var itemsElement));
            Assert.Equal(JsonValueKind.Array, itemsElement.ValueKind);
            Assert.True(root.TryGetProperty("pageNumber", out var pageNumberElement));
            Assert.True(root.TryGetProperty("pageSize", out var pageSizeElement));
            Assert.Equal(1, pageNumberElement.GetInt32());
            Assert.Equal(10, pageSizeElement.GetInt32());
            Assert.True(root.TryGetProperty("totalCount", out _));
            Assert.True(root.TryGetProperty("totalPages", out _));
        }

        [Fact]
        public async Task GetTopUserListAsyncTest_400()
        {
            //Arrange
            var requestUri = "/api/proposal-statistic/top-users?PageNumber=0&PageSize=0";

            // Act
            var response = await _client.GetAsync(requestUri);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetTopUserListAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var requestUri = "/api/proposal-statistic/top-users?PageNumber=1&PageSize=10";

            //Act
            var response = await unauthClient.GetAsync(requestUri);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}