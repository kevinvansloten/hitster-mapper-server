using hitster_mapper_server.Configuration;
using hitster_mapper_server.Service.Navidrome;
using hitster_mapper_server.Service.Spotify;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<NavidromeService>();
builder.Services.AddSingleton<SpotifyService>();

var cfgSection = builder.Configuration.GetSection("ConnectionConfiguration");
builder.Services.Configure<ConnectionConfiguration>(cfgSection);

// Fix for CS1061: Use AddDbContext for MySQL configuration
var conString = builder.Configuration.GetConnectionString("Database") ??
     throw new InvalidOperationException("Connection string 'Database'" +
    " not found.");
builder.Services.AddDbContext<HitsterContext>(options => options.UseMySQL(conString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

