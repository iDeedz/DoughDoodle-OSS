using System.Security.Claims;
using DoughDoodle.Data;
using DoughDoodle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoughDoodle.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly BudgetDbContext _db;
    public CommentsController(BudgetDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> For(string entityType, int entityId)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return Unauthorized();
        // Ownership: only show comments if user owns entity or is household member of owner
        if (!await UserCanAccessEntity(uid, entityType, entityId))
            return Forbid();

        var comments = await _db.CollaborativeComments
            .Include(c => c.User)
            .Where(c => c.EntityType == entityType && c.EntityId == entityId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        ViewBag.EntityType = entityType;
        ViewBag.EntityId = entityId;
        return PartialView("_CommentThread", comments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string entityType, int entityId, string body, string? returnUrl = null)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        if (string.IsNullOrWhiteSpace(body)) { TempData["Error"] = "Comment cannot be empty."; return Redirect(returnUrl ?? "/"); }
        if (!await UserCanAccessEntity(uid, entityType, entityId)) return Forbid();

        _db.CollaborativeComments.Add(new CollaborativeComment
        {
            UserId = uid,
            EntityType = entityType,
            EntityId = entityId,
            Body = body.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Comment added.";
        return Redirect(returnUrl ?? Url.Action("Index", "Goals")!);
    }

    private async Task<bool> UserCanAccessEntity(string uid, string entityType, int entityId)
    {
        return entityType switch
        {
            "Goal" => await _db.FinancialGoals.AnyAsync(g => g.Id == entityId && (g.UserId == uid || (g.IsShared && g.SharedWithUserIds != null && g.SharedWithUserIds.Contains(uid)))),
            "Budget" => await _db.MonthlyBudgets.AnyAsync(b => b.Id == entityId && _db.Categories.Any(c => c.Id == b.CategoryId && c.UserId == uid)),
            "Category" => await _db.Categories.AnyAsync(c => c.Id == entityId && c.UserId == uid),
            _ => false
        };
    }
}
