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
[Route("api/budgets")]
public class ApiBudgetsController : ControllerBase
{
    private readonly BudgetDbContext _db;
    public ApiBudgetsController(BudgetDbContext db) => _db = db;
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? monthYear = null)
    {
        var uid = UserId;
        if (uid == null) return Unauthorized();
        monthYear ??= DateTime.UtcNow.ToString("yyyy-MM");
        var catIds = await _db.Categories.Where(c => c.UserId == uid).Select(c => c.Id).ToListAsync();
        var budgets = await _db.MonthlyBudgets
            .Where(b => catIds.Contains(b.CategoryId) && b.MonthYear == monthYear)
            .Select(b => new { b.Id, b.CategoryId, b.MonthYear, b.Amount })
            .ToListAsync();
        return Ok(budgets);
    }
}
