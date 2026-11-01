using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/users")]
public sealed class UsersController : CrudController<User>
{
    public UsersController(UserCrudService crudService) : base(crudService) { }

    [HttpGet("{userId:int}")]
    public Task<ActionResult<User>> GetById(int userId, CancellationToken cancellationToken) =>
        GetByKeyAsync(new object[] { userId }, cancellationToken);

    [HttpPost]
    public Task<ActionResult<User>> Create(User user, CancellationToken cancellationToken) =>
        CreateAsync(user, nameof(GetById), new { userId = user.UserId }, cancellationToken);

    [HttpPut("{userId:int}")]
    public Task<IActionResult> Update(int userId, User user, CancellationToken cancellationToken) =>
        UpdateAsync(
            new object[] { userId },
            new object[] { user.UserId },
            user,
            cancellationToken);

    [HttpDelete("{userId:int}")]
    public Task<IActionResult> Delete(int userId, CancellationToken cancellationToken) =>
        DeleteAsync(new object[] { userId }, cancellationToken);
}