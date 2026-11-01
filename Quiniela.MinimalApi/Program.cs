using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Quiniela.MinimalApi;
using Quiniela.BusinessLogic;
using Quiniela.Model;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("QuinielaDb")
	?? throw new InvalidOperationException(
		"Set the PostgreSQL connection string using ConnectionStrings:QuinielaDb.");

builder.Services.AddDbContext<QuinielaDbContext>(options => options.UseNpgsql(
	connectionString,
	npgsql => npgsql
		.MapEnum<MatchKind>("match_kind", "user_08")
		.MapEnum<MatchStatus>("match_status", "user_08")
		.MapEnum<SportType>("sport_type", "user_08")));
builder.Services.AddScoped<ForecastCrudService>();
builder.Services.AddScoped<MatchCrudService>();
builder.Services.AddScoped<TeamCrudService>();
builder.Services.AddScoped<TournamentCrudService>();
builder.Services.AddScoped<TournamentTeamCrudService>();
builder.Services.AddScoped<UserCrudService>();
builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

var app = builder.Build();

app.MapCrudEndpoints<Forecast, ForecastCrudService>("/api/forecasts", forecast => forecast.ForecastId);
RouteGroupBuilder matches = app.MapCrudEndpoints<Match, MatchCrudService>(
	"/api/matches", match => match.MatchId);
app.MapCrudEndpoints<Team, TeamCrudService>("/api/teams", team => team.TeamId);
app.MapCrudEndpoints<Tournament, TournamentCrudService>(
	"/api/tournaments", tournament => tournament.TournamentId);
app.MapTournamentParticipantEndpoints();
app.MapCrudEndpoints<User, UserCrudService>("/api/users", user => user.UserId);

matches.MapGet("/by-team/{teamId:int}", async (
	int teamId,
	DateOnly startDate,
	DateOnly endDate,
	int? pageNumber,
	int? pageSize,
	MatchCrudService service,
	CancellationToken cancellationToken) =>
{
	try
	{
		List<MatchDetails> results = await service.GetMatchesByTeamAsync(
			teamId,
			startDate,
			endDate,
			pageNumber ?? 1,
			pageSize ?? 10,
			cancellationToken);
		return Results.Ok(results);
	}
	catch (ArgumentException exception)
	{
		return Results.BadRequest(exception.Message);
	}
});

matches.MapGet("/by-tournament/{tournamentId:int}", async (
	int tournamentId,
	int? pageNumber,
	int? pageSize,
	MatchCrudService service,
	CancellationToken cancellationToken) =>
{
	try
	{
		List<MatchDetails> results = await service.GetMatchesByTournamentAsync(
			tournamentId,
			pageNumber ?? 1,
			pageSize ?? 10,
			cancellationToken);
		return Results.Ok(results);
	}
	catch (ArgumentOutOfRangeException exception)
	{
		return Results.BadRequest(exception.Message);
	}
});

app.Run();
