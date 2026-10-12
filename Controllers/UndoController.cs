using System.Security.Claims;
using DoughDoodle.Services.Undo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoughDoodle.Controllers;

[Authorize]
public class UndoController : Controller
{
    private readonly ITransactionUndoService _undo;
    public UndoController(ITransactionUndoService undo) => _undo = undo;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Undo(string? returnUrl = null)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var (ok, msg) = await _undo.UndoLastAsync(uid);
        TempData[ok ? "Success" : "Error"] = msg;
        return Redirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redo(string? returnUrl = null)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid == null) return RedirectToAction("Login", "Account");
        var (ok, msg) = await _undo.RedoLastAsync(uid);
        TempData[ok ? "Success" : "Error"] = msg;
        return Redirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
    }
}
