using Data.Config;
using Data.Context;
using Data.Interfaces;
using Data.Models;
using Data.Reopsitories;
using Data.Seeder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.PeriodicBatching;
using Services.Helpers;
using Services.Interfaces;
using Services.Services;
using StackExchange.Redis;
using System.Reflection;
using System.Text;

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
        p.WithOrigins("https://localhost:4000")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
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

        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            Console.WriteLine($"JWT: Token received: {context.Token}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT: Token validated. Audience claims:");
            foreach (var claim in context.Principal.Claims.Where(c => c.Type == "aud"))
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT: Authentication FAILED: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };

});

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
        );
});

// add connection to redis
var redisHost = builder.Configuration["Redis:Host"] ?? "redis";
var redisPort = builder.Configuration["Redis:Port"] ?? "6379";

//register identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


var options = new ConfigurationOptions
{
    EndPoints = { $"{redisHost}:{redisPort}" },
    AbortOnConnectFail = false
};

var redis = ConnectionMultiplexer.Connect(options);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

//regiser repo 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<IProposalStatisticRepository, ProposalStatisticRepository>();
builder.Services.AddScoped<ITestRepository, TestRepository>();

//register services 
builder.Services.AddScoped<ILoginRegisterServices, LoginRegisterServices>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IStationServices, StationServices>();
builder.Services.AddScoped<IEmailServices, EmailServices>();
builder.Services.AddScoped<IProposalStatisticServices, ProposalStatisticServices>();
builder.Services.AddScoped<ITestServices, TestServices>();

//register helpers
builder.Services.AddScoped<IEmaliBody, EmailBodys>();

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
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
