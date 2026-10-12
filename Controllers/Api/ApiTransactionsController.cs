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
[Route("api/transactions")]
public class ApiTransactionsController : ControllerBase
{
    private readonly BudgetDbContext _db;
    public ApiTransactionsController(BudgetDbContext db) => _db = db;
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var accountIds = await _db.Accounts.Where(a => a.UserId == uid).Select(a => a.Id).ToListAsync();
        var catIds = await _db.Categories.Where(c => c.UserId == uid).Select(c => c.Id).ToListAsync();
        var q = _db.Transactions.Where(t =>
            (t.AccountId != null && accountIds.Contains(t.AccountId.Value)) ||
            (t.CategoryId != null && catIds.Contains(t.CategoryId.Value)));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.Date).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new { t.Id, t.Description, t.Amount, t.Date, t.TransactionType, t.CategoryId, t.AccountId })
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }
}
