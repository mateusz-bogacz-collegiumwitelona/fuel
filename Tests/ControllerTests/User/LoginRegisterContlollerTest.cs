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

namespace Tests.ControllerTest.User
{
    [Collection("IntegrationTests")]
    public class LoginRegisterContlollerTest : IAsyncLifetime
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
    }
}