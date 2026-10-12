using DoughDoodle.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly BudgetDbContext _db;

    public HealthController(BudgetDbContext db) => _db = db;

    [HttpGet]
    [HttpGet("")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        var payload = new
        {
            status = canConnect ? "Healthy" : "Unhealthy",
            service = "DoughDoodle",
            utc = DateTime.UtcNow,
            database = canConnect ? "up" : "down"
        };
        return canConnect ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }
}
