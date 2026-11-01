using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Quiniela.BusinessLogic;
using Quiniela.Model;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("QuinielaDb")
	?? throw new InvalidOperationException(
		"Set the PostgreSQL connection string using ConnectionStrings:QuinielaDb.");

builder.Services.AddDbContext<QuinielaDbContext>(options => options.UseNpgsql(
	connectionString,
	npgsql => npgsql.MapEnum<SportType>("sport_type", "user_08")));
builder.Services.AddScoped<ForecastCrudService>();
builder.Services.AddScoped<MatchCrudService>();
builder.Services.AddScoped<TeamCrudService>();
builder.Services.AddScoped<TournamentCrudService>();
builder.Services.AddScoped<TournamentTeamCrudService>();
builder.Services.AddScoped<UserCrudService>();
builder.Services.AddControllers()
	.AddJsonOptions(options =>
		options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

var app = builder.Build();

app.MapControllers();

app.Run();
