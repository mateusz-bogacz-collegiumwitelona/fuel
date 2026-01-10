using Data.Context;
using Data.Models;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Services.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tests.ControllerTests;

namespace Tests.ControllerTest.Admin
{
    [Collection("IntegrationTests")]
    public class PriceProposalControllerTest : IAsyncLifetime
    {
        private  HttpClient _client;
        private  CustomAppFact _factory;

        public async Task InitializeAsync()
        {
            _factory = new CustomAppFact();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-admin-token");
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task GetAllPriceProposalTest_401()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var url = "api/admin/proposal/list?PageNumber=1&amp;PageSize=10&amp";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllPriceProposalTest_200OK()
        {
            //Arrange
            var url = "/api/admin/proposal/list?PageNumber=1&PageSize=10";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<PagedResult<GetPriceProposalResponse>>(content, options);
            Assert.NotNull(content);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetPriceProposalAsync_200OK()
        {
            //Arrange
            var token = "token1";
            var url = $"/api/admin/proposal?token={token}";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPriceProposalAsync_404()
        {
            //Arrange
            var token = "badToken";
            var url = $"/api/admin/proposal?token={token}";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPriceProposalAsync_400()
        {
            //Arrange
            var token = " ";
            var url = $"/api/admin/proposal?token={token}";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePriceProposalStatus_200OK()
        {
            //Arrange
            var token = "token1";
            var url = $"/api/admin/proposal/change-status?token={token}&amp;isAccepted=true";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode );
        }

        [Fact]
        public async Task ChangePriceProposalStatus_400()
        {
            //Arrange
            var token = " ";
            var url = $"/api/admin/proposal/change-status?token={token}&amp;isAccepted=";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePriceProposalStatus_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var token = "token1";
            var url = $"/api/admin/proposal/change-status?token={token}&amp;isAccepted=";

            //Act
            var response = await unauthClient.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePriceProposalStatus_409()
        {
            //Arrange
            var token = "token3";
            var url = $"/api/admin/proposal/change-status?token={token}&amp;isAccepted=true";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ChangePriceProposalStatus_404()
        {
            // Arrange

            var token = "tokenBad";
            var url = $"/api/admin/proposal/change-status?token={token}&isAccepted=true";

            // Act
            var response = await _client.PatchAsync(url, null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
