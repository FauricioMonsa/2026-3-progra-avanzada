using Microsoft.EntityFrameworkCore;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.BusinessLogic.Tests;

public class MatchQueryTests
{
    private static QuinielaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QuinielaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QuinielaDbContext(options);
    }

    private static async Task SeedSampleDataAsync(QuinielaDbContext context)
    {
        var tournament = new Tournament
        {
            TournamentId = 1,
            Name = "Copa Apertura 2026",
            SeasonYear = 2026,
            Sport = SportType.Soccer,
            TimeZone = "America/Costa_Rica"
        };

        var tournament2 = new Tournament
        {
            TournamentId = 2,
            Name = "Liga Clausura 2026",
            SeasonYear = 2026,
            Sport = SportType.Soccer,
            TimeZone = "America/Costa_Rica"
        };

        var team1 = new Team { TeamId = 10, Name = "Deportivo Saprissa", Sport = SportType.Soccer };
        var team2 = new Team { TeamId = 20, Name = "Liga Deportiva Alajuelense", Sport = SportType.Soccer };
        var team3 = new Team { TeamId = 30, Name = "Club Sport Herediano", Sport = SportType.Soccer };

        var match1 = new Match
        {
            MatchId = 101,
            Kind = MatchKind.Competition,
            TournamentId = 1,
            Sport = SportType.Soccer,
            HomeTeamId = 10,
            AwayTeamId = 20,
            KickoffAt = new DateTime(2026, 3, 10, 18, 0, 0, DateTimeKind.Utc),
            MatchDay = new DateOnly(2026, 3, 10),
            Status = MatchStatus.Finished,
            HomeScore = 2,
            AwayScore = 1,
            Stats = "{}",
            TimeZone = "UTC"
        };

        var match2 = new Match
        {
            MatchId = 102,
            Kind = MatchKind.Competition,
            TournamentId = 1,
            Sport = SportType.Soccer,
            HomeTeamId = 30,
            AwayTeamId = 10,
            KickoffAt = new DateTime(2026, 3, 15, 20, 0, 0, DateTimeKind.Utc),
            MatchDay = new DateOnly(2026, 3, 15),
            Status = MatchStatus.Finished,
            HomeScore = 0,
            AwayScore = 3,
            Stats = "{}",
            TimeZone = "UTC"
        };

        var match3Friendly = new Match
        {
            MatchId = 103,
            Kind = MatchKind.Friendly,
            TournamentId = null,
            Sport = SportType.Soccer,
            HomeTeamId = 10,
            AwayTeamId = 30,
            KickoffAt = new DateTime(2026, 3, 20, 15, 0, 0, DateTimeKind.Utc),
            MatchDay = new DateOnly(2026, 3, 20),
            Status = MatchStatus.Scheduled,
            HomeScore = null,
            AwayScore = null,
            Stats = "{}",
            TimeZone = "UTC"
        };

        var match4OutsideInterval = new Match
        {
            MatchId = 104,
            Kind = MatchKind.Competition,
            TournamentId = 2,
            Sport = SportType.Soccer,
            HomeTeamId = 10,
            AwayTeamId = 20,
            KickoffAt = new DateTime(2026, 6, 1, 19, 0, 0, DateTimeKind.Utc),
            MatchDay = new DateOnly(2026, 6, 1),
            Status = MatchStatus.Scheduled,
            HomeScore = null,
            AwayScore = null,
            Stats = "{}",
            TimeZone = "UTC"
        };

        var match5DifferentTeams = new Match
        {
            MatchId = 105,
            Kind = MatchKind.Competition,
            TournamentId = 1,
            Sport = SportType.Soccer,
            HomeTeamId = 20,
            AwayTeamId = 30,
            KickoffAt = new DateTime(2026, 3, 12, 19, 0, 0, DateTimeKind.Utc),
            MatchDay = new DateOnly(2026, 3, 12),
            Status = MatchStatus.Finished,
            HomeScore = 1,
            AwayScore = 1,
            Stats = "{}",
            TimeZone = "UTC"
        };

        context.Tournaments.AddRange(tournament, tournament2);
        context.Teams.AddRange(team1, team2, team3);
        context.Matches.AddRange(match1, match2, match3Friendly, match4OutsideInterval, match5DifferentTeams);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Query1_ReturnsMatches_WhereTeamIsHomeOrAway_WithinTimeInterval_ChronologicallyAscending()
    {
        using QuinielaDbContext context = CreateContext();
        await SeedSampleDataAsync(context);
        var queryService = new MatchQueryService(context);

        DateTime start = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = new(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);

        List<MatchDetails> results = await queryService.GetMatchesByTeamAsync(10, start, end);

        Assert.Equal(3, results.Count);

        // Verification of chronological ascending order
        Assert.Equal(101, results[0].MatchId);
        Assert.Equal(102, results[1].MatchId);
        Assert.Equal(103, results[2].MatchId);

        // First match: team 10 is HomeTeam in a tournament competition match
        MatchDetails firstMatch = results[0];
        Assert.Equal("Copa Apertura 2026", firstMatch.TournamentName);
        Assert.Equal((short)2026, firstMatch.TournamentSeason);
        Assert.Equal("Soccer", firstMatch.TournamentSportName);
        Assert.Equal(101, firstMatch.MatchId);
        Assert.Equal(MatchKind.Competition, firstMatch.MatchKind);
        Assert.Equal("Deportivo Saprissa", firstMatch.HomeTeamName);
        Assert.Equal("Liga Deportiva Alajuelense", firstMatch.AwayTeamName);
        Assert.Equal(new DateTime(2026, 3, 10, 18, 0, 0, DateTimeKind.Utc), firstMatch.DatePlayed);
        Assert.Equal("Finished", firstMatch.StatusName);
        Assert.Equal(2, firstMatch.HomeScore);
        Assert.Equal(1, firstMatch.AwayScore);

        // Second match: team 10 is AwayTeam
        MatchDetails secondMatch = results[1];
        Assert.Equal("Club Sport Herediano", secondMatch.HomeTeamName);
        Assert.Equal("Deportivo Saprissa", secondMatch.AwayTeamName);
        Assert.Equal(0, secondMatch.HomeScore);
        Assert.Equal(3, secondMatch.AwayScore);

        // Third match: friendly match without tournament
        MatchDetails thirdMatch = results[2];
        Assert.Null(thirdMatch.TournamentName);
        Assert.Null(thirdMatch.TournamentSeason);
        Assert.Null(thirdMatch.TournamentSportName);
        Assert.Equal(MatchKind.Friendly, thirdMatch.MatchKind);
        Assert.Equal("Scheduled", thirdMatch.StatusName);
        Assert.Null(thirdMatch.HomeScore);
        Assert.Null(thirdMatch.AwayScore);
    }

    [Fact]
    public async Task Query1_ThrowsArgumentException_WhenStartDateIsGreaterThanEndDate()
    {
        using QuinielaDbContext context = CreateContext();
        var queryService = new MatchQueryService(context);

        DateTime start = new(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(() => queryService.GetMatchesByTeamAsync(10, start, end));
    }

    [Fact]
    public async Task Query1_DateOnlyOverload_FiltersCorrectly()
    {
        using QuinielaDbContext context = CreateContext();
        await SeedSampleDataAsync(context);
        var queryService = new MatchQueryService(context);

        DateOnly startDate = new(2026, 3, 10);
        DateOnly endDate = new(2026, 3, 15);

        List<MatchDetails> results = await queryService.GetMatchesByTeamAsync(10, startDate, endDate);

        Assert.Equal(2, results.Count);
        Assert.Equal(101, results[0].MatchId);
        Assert.Equal(102, results[1].MatchId);
    }

    [Fact]
    public async Task Query2_ReturnsMatchesOfTournament_ChronologicallyAscending()
    {
        using QuinielaDbContext context = CreateContext();
        await SeedSampleDataAsync(context);
        var queryService = new MatchQueryService(context);

        List<MatchDetails> results = await queryService.GetMatchesByTournamentAsync(1);

        // Matches 101, 102, 105 belong to tournament 1
        Assert.Equal(3, results.Count);

        // Sorted chronologically ascending
        Assert.Equal(101, results[0].MatchId);
        Assert.Equal(105, results[1].MatchId);
        Assert.Equal(102, results[2].MatchId);

        foreach (MatchDetails match in results)
        {
            Assert.Equal("Copa Apertura 2026", match.TournamentName);
            Assert.Equal((short)2026, match.TournamentSeason);
            Assert.Equal("Soccer", match.TournamentSportName);
            Assert.Equal(MatchKind.Competition, match.MatchKind);
        }
    }

    [Fact]
    public async Task Query2_ReturnsEmptyList_WhenTournamentDoesNotExist()
    {
        using QuinielaDbContext context = CreateContext();
        await SeedSampleDataAsync(context);
        var queryService = new MatchQueryService(context);

        List<MatchDetails> results = await queryService.GetMatchesByTournamentAsync(999);

        Assert.Empty(results);
    }

    [Fact]
    public async Task BothQueries_ReturnSameType_MatchDetails()
    {
        using QuinielaDbContext context = CreateContext();
        await SeedSampleDataAsync(context);
        var queryService = new MatchQueryService(context);

        List<MatchDetails> query1Result = await queryService.GetMatchesByTeamAsync(
            10,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));

        List<MatchDetails> query2Result = await queryService.GetMatchesByTournamentAsync(1);

        Assert.IsType<List<MatchDetails>>(query1Result);
        Assert.IsType<List<MatchDetails>>(query2Result);
        Assert.IsType<MatchDetails>(query1Result[0]);
        Assert.IsType<MatchDetails>(query2Result[0]);
    }

    [Fact]
    public async Task MatchCrudService_AlsoExposesBothQueries()
    {
        using QuinielaDbContext context = CreateContext();
        await SeedSampleDataAsync(context);
        var crudService = new MatchCrudService(context);

        List<MatchDetails> query1Result = await crudService.GetMatchesByTeamAsync(
            10,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc));

        List<MatchDetails> query2Result = await crudService.GetMatchesByTournamentAsync(1);

        Assert.Equal(3, query1Result.Count);
        Assert.Equal(3, query2Result.Count);
    }
}
