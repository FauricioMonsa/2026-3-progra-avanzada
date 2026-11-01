using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.MinimalApi;

public static class CrudEndpointMappings
{
    public static RouteGroupBuilder MapCrudEndpoints<TEntity, TService>(
        this IEndpointRouteBuilder endpoints,
        string route,
        Func<TEntity, int> idSelector)
        where TEntity : class
        where TService : CrudService<TEntity>
    {
        RouteGroupBuilder group = endpoints.MapGroup(route);

        group.MapGet("", async (
            int? pageNumber,
            int? pageSize,
            TService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                List<TEntity> page = await service.GetAllAsync(
                    pageNumber ?? 1,
                    pageSize ?? 10,
                    cancellationToken);
                return Results.Ok(page);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        group.MapGet("/{id:int}", async (
            int id,
            TService service,
            CancellationToken cancellationToken) =>
        {
            TEntity? entity = await service.GetByPrimaryKeyAsync(new object[] { id }, cancellationToken);
            return entity is null ? Results.NotFound() : Results.Ok(entity);
        });

        group.MapPost("", async (
            TEntity entity,
            TService service,
            CancellationToken cancellationToken) =>
        {
            TEntity created = await service.InsertAsync(entity, cancellationToken);
            return Results.Created($"{route}/{idSelector(created)}", created);
        });

        group.MapPut("/{id:int}", async (
            int id,
            TEntity entity,
            TService service,
            CancellationToken cancellationToken) =>
        {
            if (idSelector(entity) != id)
            {
                return Results.BadRequest("The route key must match the entity key.");
            }

            object[] keyValues = { id };
            if (!await service.ExistsByPrimaryKeyAsync(keyValues, cancellationToken))
            {
                return Results.NotFound();
            }

            await service.UpdateAsync(entity, cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (
            int id,
            TService service,
            CancellationToken cancellationToken) =>
        {
            bool deleted = await service.DeleteAsync(new object[] { id }, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }

    public static RouteGroupBuilder MapTournamentParticipantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string route = "/api/tournament-participants";
        RouteGroupBuilder group = endpoints.MapGroup(route);

        group.MapGet("", async (
            int? pageNumber,
            int? pageSize,
            TournamentTeamCrudService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                List<TournamentTeam> page = await service.GetAllAsync(
                    pageNumber ?? 1,
                    pageSize ?? 10,
                    cancellationToken);
                return Results.Ok(page);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        group.MapGet("/{tournamentId:int}/{teamId:int}", async (
            int tournamentId,
            int teamId,
            TournamentTeamCrudService service,
            CancellationToken cancellationToken) =>
        {
            TournamentTeam? participant = await service.GetByPrimaryKeyAsync(
                new object[] { tournamentId, teamId }, cancellationToken);
            return participant is null ? Results.NotFound() : Results.Ok(participant);
        });

        group.MapPost("", async (
            TournamentTeam participant,
            TournamentTeamCrudService service,
            CancellationToken cancellationToken) =>
        {
            TournamentTeam created = await service.InsertAsync(participant, cancellationToken);
            return Results.Created(
                $"{route}/{created.TournamentId}/{created.TeamId}",
                created);
        });

        group.MapPut("/{tournamentId:int}/{teamId:int}", async (
            int tournamentId,
            int teamId,
            TournamentTeam participant,
            TournamentTeamCrudService service,
            CancellationToken cancellationToken) =>
        {
            object[] routeKeys = { tournamentId, teamId };
            object[] entityKeys = { participant.TournamentId, participant.TeamId };
            if (!routeKeys.SequenceEqual(entityKeys))
            {
                return Results.BadRequest("The route key must match the entity key.");
            }

            if (!await service.ExistsByPrimaryKeyAsync(routeKeys, cancellationToken))
            {
                return Results.NotFound();
            }

            await service.UpdateAsync(participant, cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{tournamentId:int}/{teamId:int}", async (
            int tournamentId,
            int teamId,
            TournamentTeamCrudService service,
            CancellationToken cancellationToken) =>
        {
            bool deleted = await service.DeleteAsync(
                new object[] { tournamentId, teamId }, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }
}