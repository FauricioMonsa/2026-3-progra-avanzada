using Microsoft.EntityFrameworkCore;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.BusinessLogic.Tests;

public class CrudServiceTests
{
    [Fact]
    public async Task User_service_supports_insert_update_query_and_delete()
    {
        using QuinielaDbContext context = CreateContext();
        var service = new UserCrudService(context);
        var user = new User
        {
            UserId = 17,
            Username = "test-user",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        await service.InsertAsync(user);
        User? loadedUser = await service.GetByPrimaryKeyAsync(new object[] { 17 });

        Assert.NotNull(loadedUser);
        Assert.Equal("Test User", loadedUser.DisplayName);

        loadedUser.DisplayName = "Updated User";
        await service.UpdateAsync(loadedUser);
        Assert.Equal("Updated User", (await service.GetByPrimaryKeyAsync(new object[] { 17 }))?.DisplayName);

        Assert.True(await service.DeleteAsync(new object[] { 17 }));
        Assert.False(await service.DeleteAsync(new object[] { 17 }));
    }

    [Fact]
    public async Task Tournament_participant_service_queries_composite_primary_key()
    {
        using QuinielaDbContext context = CreateContext();
        var service = new TournamentTeamCrudService(context);
        var participant = new TournamentTeam
        {
            TournamentId = 7,
            TeamId = 12,
            Sport = SportType.Soccer
        };

        await service.InsertAsync(participant);
        TournamentTeam? loadedParticipant = await service.GetByPrimaryKeyAsync(new object[] { 7, 12 });

        Assert.NotNull(loadedParticipant);
        Assert.Equal(SportType.Soccer, loadedParticipant.Sport);
    }

    [Fact]
    public async Task Primary_key_query_loads_direct_navigations_only()
    {
        using QuinielaDbContext context = CreateContext();
        var user = new User
        {
            UserId = 21,
            Username = "forecast-user",
            Email = "forecast@example.com",
            DisplayName = "Forecast User"
        };
        user.Forecasts.Add(new Forecast
        {
            ForecastId = 31,
            MatchId = 41,
            UserId = user.UserId,
            HomeScore = 1,
            AwayScore = 0,
            SubmittedAt = DateTime.UtcNow,
            User = user
        });
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        User? loadedUser = await new UserCrudService(context).GetByPrimaryKeyAsync(new object[] { 21 });

        Assert.NotNull(loadedUser);
        Forecast loadedForecast = Assert.Single(loadedUser.Forecasts);
        Assert.False(context.Entry(loadedForecast).Reference(forecast => forecast.Match).IsLoaded);
    }

    private static QuinielaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QuinielaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QuinielaDbContext(options);
    }
}