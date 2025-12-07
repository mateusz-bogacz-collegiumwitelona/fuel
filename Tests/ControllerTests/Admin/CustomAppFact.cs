using Azure.Storage.Blobs;
using Data.Context;
using Data.Interfaces;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
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
            var identityAppDescriptor = services.SingleOrDefault(d => d.ServiceType.Name == "IConfigureOptions`1" && d.ImplementationType?.Name.Contains("IdentityCookieOptionsSetup") == true);
            if (identityAppDescriptor != null)
                services.Remove(identityAppDescriptor);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbDescriptor != null)
                services.Remove(dbDescriptor);
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
            var hostedServices = services.Where(s => s.ServiceType.Name.Contains("HostedService")).ToList();
            foreach (var hs in hostedServices)
                services.Remove(hs);

            var descriptor = services
               .SingleOrDefault(s => s.ServiceType == typeof(IConfigureOptions<FacebookOptions>));

            if (descriptor != null)
                services.Remove(descriptor);
            var redisMock = new Mock<IConnectionMultiplexer>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(redisMock.Object);
            services.RemoveAll<BlobServiceClient>();
            var blobMock = new Mock<BlobServiceClient>();
            services.AddSingleton(blobMock.Object);
            services.RemoveAll<IStorage>();
            var storageMock = new Mock<IStorage>();
            services.AddSingleton(storageMock.Object);

            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Headers.TryGetValue("Authorization", out var auth) &&
                            auth.ToString().Contains("test-admin-token"))
                        {
                            var identity = new ClaimsIdentity(new[]
                            {
                                    new Claim(ClaimTypes.Name, "TestAdmin"),
                                    new Claim(ClaimTypes.Role, "Admin")
                                }, "Test");
                            context.Principal = new ClaimsPrincipal(identity);
                            context.Success();
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            services.AddScoped<ApplicationDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                var ctx = new ApplicationDbContext(options);
                ctx.Database.EnsureCreated();

                return ctx;
            });
        });
    }
}
