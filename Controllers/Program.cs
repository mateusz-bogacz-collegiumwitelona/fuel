using Azure.Storage.Blobs;
using Data.Config;
using Data.Context;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using Data.Reopsitories;
using Data.Repositories;
using Data.Seeder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.PeriodicBatching;
using Services.BackgroundServices;
using Services.BackgrounServices;
using Services.Commands;
using Services.Helpers;
using Services.Interfaces;
using Services.Services;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

//log configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File($"logs/log{DateTime.UtcNow}.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) =>
{
    config
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Sink(
        new PeriodicBatchingSink(
            new EfCoreSinkConfig(services),
            new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = 50,
                Period = TimeSpan.FromSeconds(2),
                EagerlyEmitFirstEvent = false,
                QueueLimit = 10000
            }))
    .Enrich.FromLogContext();
});

//CORS policy
builder.Services.AddCors(op =>
{
    op.AddPolicy("AllowClient", p =>
    {
        p.WithOrigins("http://localhost:4000")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.AddFixedWindowLimiter("auth", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });


    options.AddFixedWindowLimiter("upload", options => { 
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        TimeSpan? retryAfter = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
        {
            retryAfter = retry;
            context.HttpContext.Response.Headers.RetryAfter = retry.TotalSeconds.ToString();
        }

        var result = Result<object>.Bad(
            message: "Too many requests. Please try again later.",
            statusCode: StatusCodes.Status429TooManyRequests,
            errors: retryAfter.HasValue
                ? new List<string> { $"Retry after {retryAfter.Value.TotalSeconds} seconds" }
                : new List<string> { "Rate limit exceeded" }
        );

        await context.HttpContext.Response.WriteAsJsonAsync(result, cancellationToken);
    };
});

//jwt configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];
                Console.WriteLine($"JWT: Token received from cookie");
            }
            else if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"JWT: Token received from Authorization header");
                }
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT: Token validated successfully");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT: Authentication FAILED: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
})
.AddFacebook("Facebook", options =>
{
    options.AppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID");
    options.AppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET");
    options.Fields.Add("name");
    options.Fields.Add("email");
    options.SaveTokens = true;
    options.CallbackPath = "/api/auth/facebook/callback";
});

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
        );
});

//register identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// add connection to redis
var redisHost = builder.Configuration["Redis:Host"] ?? "redis";
var redisPort = builder.Configuration["Redis:Port"] ?? "6379";

var options = new ConfigurationOptions
{
    EndPoints = { $"{redisHost}:{redisPort}" },
    AbortOnConnectFail = false
};

var redis = ConnectionMultiplexer.Connect(options);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

//register blob client
builder.Services.AddHostedService<BlobInitializer>();
builder.Services.Configure<BlobConfig>(builder.Configuration.GetSection("Blob"));
builder.Services.AddSingleton(sp =>
{
    var blobSettings = sp.GetRequiredService<IOptions<BlobConfig>>().Value;
    return new BlobServiceClient(blobSettings.ConnectionString);
});


//register repo 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<IProposalStatisticRepository, ProposalStatisticRepository>();
builder.Services.AddScoped<IPriceProposalRepository, PriceProposalRepository>();
builder.Services.AddScoped<IFuelTypeRepository, FuelTypeRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IReportRepositry, ReportRepositry>();
builder.Services.AddScoped<IBanRepository, BanRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

//register services
builder.Services.AddScoped<ILoginRegisterServices, LoginRegisterServices>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IStationServices, StationServices>();
builder.Services.AddScoped<IProposalStatisticServices, ProposalStatisticServices>();
builder.Services.AddScoped<IPriceProposalServices, PriceProposalServices>();
builder.Services.AddScoped<IFuelTypeServices, FuelTypeServices>();
builder.Services.AddScoped<IBrandServices, BrandServices>();
builder.Services.AddScoped<IBanService, BanService>();
builder.Services.AddScoped<IReportService, ReportService>();

//register helpers
builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<EmailBodys>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenFactory, TokenFactory>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<IStorage, BlobApiHelper>();

//register background services
builder.Services.AddHostedService<BanExpirationService>();
builder.Services.AddHostedService<ProposalExpirationService>();

builder.Services.AddControllers(op =>
{
    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

    op.Filters.Add(new AuthorizeFilter(policy));
})
.ConfigureApiBehaviorOptions(op =>
{
    op.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
        .Where(e => e.Value.Errors.Count > 0)
        .SelectMany(e => e.Value.Errors)
        .Select(e => e.ErrorMessage)
        .ToList();

        var response = new
        {
            success = false,
            message = "Validation error",
            errors = errors
        };

        return new BadRequestObjectResult(response);
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
if (cliArgs.Length > 0)
{
    var commandRunner = new CommandRunner(app.Services);
    await commandRunner.RunAsync(cliArgs);
    Environment.Exit(0);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//auto migration and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await dbContext.Database.MigrateAsync();

        var seeder = new SeedData(roleManager, userManager, dbContext);
        await seeder.InicializeAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"error during migration: {ex.Message} | {ex.InnerException}");
    }
}

//middleware

//app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
