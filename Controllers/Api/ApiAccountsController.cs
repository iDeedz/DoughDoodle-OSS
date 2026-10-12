using System.Security.Claims;
using DoughDoodle.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
[Route("api/accounts")]
public class ApiAccountsController : ControllerBase
{
    private readonly BudgetDbContext _db;
    public ApiAccountsController(BudgetDbContext db) => _db = db;

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        var list = await _db.Accounts.Where(a => a.UserId == uid)
            .Select(a => new { a.Id, a.Name, a.Balance, a.AccountType, a.IsActive })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        var a = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        return a == null ? NotFound() : Ok(a);
    }
}
