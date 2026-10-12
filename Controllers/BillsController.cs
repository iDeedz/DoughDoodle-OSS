using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;

namespace DoughDoodle.Controllers;

[Authorize]
public class BillsController : Controller
{
    private readonly BudgetDbContext _context;

    public BillsController(BudgetDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var bills = await _context.Bills
            .Where(b => b.UserId == currentUserId && b.IsActive)
            .Include(b => b.Category)
            .OrderBy(b => b.DueDate)
            .ToListAsync();

        var upcomingBills = bills
            .Where(b => b.Status == BillStatus.Pending && b.DueDate <= DateTime.Now.AddDays(30))
            .OrderBy(b => b.DueDate)
            .ToList();

        var overdueBills = bills
            .Where(b => b.Status == BillStatus.Overdue || (b.Status == BillStatus.Pending && b.DueDate < DateTime.Now))
            .OrderBy(b => b.DueDate)
            .ToList();

        var totalMonthlyBills = bills
            .Where(b => b.IsRecurring && b.Recurrence == BillRecurrence.Monthly)
            .Sum(b => b.Amount);

        var viewModel = new BillsViewModel
        {
            AllBills = bills,
            UpcomingBills = upcomingBills,
            OverdueBills = overdueBills,
            TotalMonthlyAmount = totalMonthlyBills,
            TotalBillsCount = bills.Count,
            OverdueBillsCount = overdueBills.Count,
            UpcomingBillsCount = upcomingBills.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Bill bill)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        if (ModelState.IsValid)
        {
            bill.UserId = currentUserId;
            bill.CreatedAt = DateTime.UtcNow;
            bill.NextDueDate = CalculateNextDueDate(bill.DueDate, bill.Recurrence);

            // Update status based on due date
            if (bill.DueDate < DateTime.Now)
                bill.Status = BillStatus.Overdue;

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Bill '{bill.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(bill);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bill = await _context.Bills
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == currentUserId);

        if (bill == null) return NotFound();

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(bill);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Bill bill)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != bill.Id) return NotFound();

        var existingBill = await _context.Bills
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == currentUserId);
        
        if (existingBill == null) return NotFound();

        if (ModelState.IsValid)
        {
            existingBill.Name = bill.Name;
            existingBill.Description = bill.Description;
            existingBill.Amount = bill.Amount;
            existingBill.DueDate = bill.DueDate;
            existingBill.Recurrence = bill.Recurrence;
            existingBill.IsRecurring = bill.IsRecurring;
            existingBill.Payee = bill.Payee;
            existingBill.AccountNumber = bill.AccountNumber;
            existingBill.Website = bill.Website;
            existingBill.PhoneNumber = bill.PhoneNumber;
            existingBill.CategoryId = bill.CategoryId;
            existingBill.AutoPay = bill.AutoPay;
            existingBill.AutoPayAccount = bill.AutoPayAccount;
            existingBill.NotificationsEnabled = bill.NotificationsEnabled;
            existingBill.NotificationDaysBefore = bill.NotificationDaysBefore;
            existingBill.Notes = bill.Notes;
            existingBill.UpdatedAt = DateTime.UtcNow;
            existingBill.NextDueDate = CalculateNextDueDate(bill.DueDate, bill.Recurrence);

            // Update status based on due date
            if (existingBill.DueDate < DateTime.Now && existingBill.Status == BillStatus.Pending)
                existingBill.Status = BillStatus.Overdue;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Bill updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == currentUserId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(bill);
    }

    [HttpPost]
    public async Task<IActionResult> MarkPaid(int id, decimal? amountPaid, string paymentMethod = "Online", string? notes = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bill = await _context.Bills
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == currentUserId);

        if (bill == null) return NotFound();

        var payment = new BillPayment
        {
            BillId = bill.Id,
            AmountPaid = amountPaid ?? bill.Amount,
            PaymentDate = DateTime.Now,
            DueDateWhenPaid = bill.DueDate,
            PaymentMethod = paymentMethod,
            Notes = notes,
            IsLate = bill.DueDate < DateTime.Now,
            CreatedAt = DateTime.UtcNow
        };

        _context.BillPayments.Add(payment);

        // Update bill status and dates
        bill.Status = BillStatus.Paid;
        bill.LastPaidDate = DateTime.Now;
        
        if (bill.IsRecurring)
        {
            bill.DueDate = CalculateNextDueDate(bill.DueDate, bill.Recurrence);
            bill.NextDueDate = CalculateNextDueDate(bill.DueDate, bill.Recurrence);
            bill.Status = BillStatus.Pending; // Reset for next occurrence
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Bill '{bill.Name}' marked as paid!";
        return Json(new { success = true, message = "Bill marked as paid successfully!" });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bill = await _context.Bills
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == currentUserId);

        if (bill == null) return NotFound();

        // Soft delete
        bill.IsActive = false;
        bill.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Bill '{bill.Name}' deleted successfully!";
        return Json(new { success = true, message = "Bill deleted successfully!" });
    }

    public async Task<IActionResult> PaymentHistory(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bill = await _context.Bills
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == currentUserId);

        if (bill == null) return NotFound();

        var payments = await _context.BillPayments
            .Where(bp => bp.BillId == id)
            .OrderByDescending(bp => bp.PaymentDate)
            .ToListAsync();

        ViewBag.Bill = bill;
        return View(payments);
    }

    [HttpGet]
    public async Task<IActionResult> GetUpcomingBills()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Unauthorized();

        var upcomingBills = await _context.Bills
            .Where(b => b.UserId == currentUserId && b.IsActive 
                && b.Status == BillStatus.Pending 
                && b.DueDate <= DateTime.Now.AddDays(7))
            .Include(b => b.Category)
            .OrderBy(b => b.DueDate)
            .Take(5)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Amount,
                DueDate = b.DueDate.ToString("MMM dd"),
                CategoryName = b.Category != null ? b.Category.Name : "No Category",
                DaysUntilDue = (b.DueDate.Date - DateTime.Now.Date).Days
            })
            .ToListAsync();

        return Json(upcomingBills);
    }

    private DateTime CalculateNextDueDate(DateTime currentDueDate, BillRecurrence recurrence)
    {
        return recurrence switch
        {
            BillRecurrence.Daily => currentDueDate.AddDays(1),
            BillRecurrence.Weekly => currentDueDate.AddDays(7),
            BillRecurrence.BiWeekly => currentDueDate.AddDays(14),
            BillRecurrence.Monthly => currentDueDate.AddMonths(1),
            BillRecurrence.Quarterly => currentDueDate.AddMonths(3),
            BillRecurrence.SemiAnnually => currentDueDate.AddMonths(6),
            BillRecurrence.Annually => currentDueDate.AddYears(1),
            _ => currentDueDate
        };
    }
}