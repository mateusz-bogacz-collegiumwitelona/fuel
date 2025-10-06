using Data.Context;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// add connection to redis
var redisHost = builder.Configuration["Redis:Host"] ?? "redis";
var redisPort = builder.Configuration["Redis:Port"] ?? "6379";

//var redisHost = builder.Configuration["Redis:Host"];
//var redisPort = builder.Configuration["Redis:Port"];
//var redisPort = "6379";

var options = new ConfigurationOptions
{
    EndPoints = { $"{redisHost}:{redisPort}" },
    AbortOnConnectFail = false
};
var redis = ConnectionMultiplexer.Connect(options);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
