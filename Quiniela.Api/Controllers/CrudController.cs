using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;

namespace Quiniela.Api.Controllers;

[ApiController]
public abstract class CrudController<TEntity> : ControllerBase where TEntity : class
{
    private readonly CrudService<TEntity> crudService;

    protected CrudController(CrudService<TEntity> crudService)
    {
        this.crudService = crudService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TEntity>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await crudService.GetAllAsync(pageNumber, pageSize, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    protected async Task<ActionResult<TEntity>> GetByKeyAsync(
        object[] keyValues,
        CancellationToken cancellationToken)
    {
        TEntity? entity = await crudService.GetByPrimaryKeyAsync(keyValues, cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    protected async Task<ActionResult<TEntity>> CreateAsync(
        TEntity entity,
        string getActionName,
        object routeValues,
        CancellationToken cancellationToken)
    {
        TEntity createdEntity = await crudService.InsertAsync(entity, cancellationToken);
        return CreatedAtAction(getActionName, routeValues, createdEntity);
    }

    protected async Task<IActionResult> UpdateAsync(
        object[] routeKeys,
        object[] entityKeys,
        TEntity entity,
        CancellationToken cancellationToken)
    {
        if (routeKeys.Length != entityKeys.Length || !routeKeys.SequenceEqual(entityKeys))
        {
            return BadRequest("The route key must match the entity key.");
        }

        if (!await crudService.ExistsByPrimaryKeyAsync(routeKeys, cancellationToken))
        {
            return NotFound();
        }

        await crudService.UpdateAsync(entity, cancellationToken);
        return NoContent();
    }

    protected async Task<IActionResult> DeleteAsync(
        object[] keyValues,
        CancellationToken cancellationToken)
    {
        bool deleted = await crudService.DeleteAsync(keyValues, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}