using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Models.ViewModels;
using System.Security.Claims;
using System.Globalization;
using CsvHelper;
using DoughDoodle.Services;
using DoughDoodle.Services.Bank;

namespace DoughDoodle.Controllers;

[Authorize]
public class ImportExportController : Controller
{
    private readonly BudgetDbContext _context;
    private readonly IOfxImportService _ofx;

    public ImportExportController(BudgetDbContext context, IOfxImportService ofx)
    {
        _context = context;
        _ofx = ofx;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var owned = _context.ImportSessions.Where(i => i.UserId == currentUserId);
        var recentImports = await owned
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .ToListAsync();

        var sessionCount = await owned.CountAsync();
        var recordsImported = await owned.SumAsync(i => (int?)i.SuccessfulRecords) ?? 0;
        var lastActivity = await owned
            .Select(i => i.CompletedAt ?? i.CreatedAt)
            .OrderByDescending(i => i)
            .Select(i => (DateTime?)i)
            .FirstOrDefaultAsync();

        var viewModel = new ImportExportViewModel
        {
            RecentImports = recentImports,
            ImportSessionCount = sessionCount,
            RecordsImported = recordsImported,
            ExportDownloadCount = 0,
            LastActivityAt = lastActivity,
            LastActivityLabel = lastActivity == null
                ? "—"
                : ImportSessionStats.FormatLastActivity(lastActivity, DateTime.UtcNow)
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ImportTransactions(IFormFile file)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to import.";
            return RedirectToAction(nameof(Index));
        }

        var allowedExtensions = new[] { ".csv", ".qif", ".ofx", ".qfx" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        
        if (!allowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Unsupported file format. Please use CSV, QIF, OFX, or QFX files.";
            return RedirectToAction(nameof(Index));
        }

        // Create import session
        var importSession = new ImportSession
        {
            FileName = file.FileName,
            FileType = extension.TrimStart('.').ToUpper(),
            Status = ImportStatus.Pending,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ImportSessions.Add(importSession);
        await _context.SaveChangesAsync();

        try
        {
            importSession.Status = ImportStatus.Processing;
            await _context.SaveChangesAsync();

            if (extension == ".csv")
            {
                await ProcessCsvImport(file, importSession, currentUserId);
            }
            else if (extension == ".ofx" || extension == ".qfx")
            {
                await using var stream = file.OpenReadStream();
                var (ok, fail) = await _ofx.ImportOfxOrQfxAsync(stream, file.FileName, importSession, currentUserId, null);
                importSession.SuccessfulRecords = ok;
                importSession.FailedRecords = fail;
            }
            else if (extension == ".qif")
            {
                await ProcessQifImport(file, importSession, currentUserId);
            }
            else
            {
                TempData["Error"] = $"{extension.ToUpper()} import is not yet implemented.";
                importSession.Status = ImportStatus.Failed;
                importSession.ErrorMessage = "File format not yet implemented";
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            importSession.Status = ImportStatus.Completed;
            importSession.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully imported {importSession.SuccessfulRecords} transactions from {file.FileName}";
        }
        catch (Exception ex)
        {
            importSession.Status = ImportStatus.Failed;
            importSession.ErrorMessage = ex.Message;
            importSession.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Import failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ProcessCsvImport(IFormFile file, ImportSession session, string userId)
    {
        using var reader = new StringReader(await new StreamReader(file.OpenReadStream()).ReadToEndAsync());
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<CsvTransactionRecord>().ToList();
        session.TotalRecords = records.Count;
        await _context.SaveChangesAsync();

        // Get user's categories for smart categorization
        var userCategories = await _context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var defaultCategory = userCategories.FirstOrDefault(c => c.Name == "Uncategorized");
        if (defaultCategory == null)
        {
            defaultCategory = new Category
            {
                Name = "Uncategorized",
                UserId = userId
            };
            _context.Categories.Add(defaultCategory);
            await _context.SaveChangesAsync();
            userCategories.Add(defaultCategory);
        }

        foreach (var record in records)
        {
            try
            {
                session.ProcessedRecords++;

                // Smart categorization based on description
                var categoryId = defaultCategory.Id;
                
                // If CSV has a category specified, try to match it
                if (!string.IsNullOrWhiteSpace(record.Category))
                {
                    var matchedCategory = userCategories.FirstOrDefault(c => 
                        c.Name.Equals(record.Category, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchedCategory != null)
                    {
                        categoryId = matchedCategory.Id;
                    }
                }
                
                // If no category match, use smart categorization based on description
                if (categoryId == defaultCategory.Id)
                {
                    categoryId = SmartCategorizeTransaction(record.Description ?? "", userCategories, defaultCategory.Id);
                }

                var transaction = new Transaction
                {
                    Description = record.Description ?? "Imported Transaction",
                    Amount = Math.Abs(record.Amount),
                    Date = record.Date,
                    TransactionType = record.Amount >= 0 ? TransactionType.Income : TransactionType.Expense,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                session.SuccessfulRecords++;
            }
            catch
            {
                session.FailedRecords++;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Smart categorization based on transaction description using keyword matching.
    /// This provides basic auto-categorization for South African transactions.
    /// </summary>
    private int SmartCategorizeTransaction(string description, List<Category> categories, int defaultCategoryId)
    {
        var descLower = description.ToLower();

        // Debt Review / PDA
        if (descLower.Contains("pda") || descLower.Contains("debt review") || descLower.Contains("hyphen"))
            return FindCategoryId(categories, new[] { "PDA - Debt Review Payment", "Debt Payment" }, defaultCategoryId);

        // Groceries
        if (descLower.Contains("pick n pay") || descLower.Contains("checkers") || descLower.Contains("woolworths") ||
            descLower.Contains("spar") || descLower.Contains("shoprite") || descLower.Contains("grocery") || 
            descLower.Contains("groceries"))
            return FindCategoryId(categories, new[] { "Groceries & Food", "Groceries" }, defaultCategoryId);

        // Fuel
        if (descLower.Contains("engen") || descLower.Contains("shell") || descLower.Contains("bp") ||
            descLower.Contains("caltex") || descLower.Contains("sasol") || descLower.Contains("fuel") || 
            descLower.Contains("petrol"))
            return FindCategoryId(categories, new[] { "Fuel/Petrol", "Transportation", "Fuel" }, defaultCategoryId);

        // Electricity
        if (descLower.Contains("eskom") || descLower.Contains("electricity") || descLower.Contains("prepaid power"))
            return FindCategoryId(categories, new[] { "Electricity & Water", "Utilities", "Electricity" }, defaultCategoryId);

        // Water
        if (descLower.Contains("water") || descLower.Contains("municipal"))
            return FindCategoryId(categories, new[] { "Electricity & Water", "Municipal Rates & Taxes", "Water" }, defaultCategoryId);

        // Medical
        if (descLower.Contains("discovery") || descLower.Contains("medshield") || descLower.Contains("medical aid") ||
            descLower.Contains("bonitas") || descLower.Contains("pharmacy") || descLower.Contains("clicks") || 
            descLower.Contains("dis-chem"))
            return FindCategoryId(categories, new[] { "Medical Aid/Health Insurance", "Medical Expenses", "Pharmacy & Medication" }, defaultCategoryId);

        // Cellphone
        if (descLower.Contains("vodacom") || descLower.Contains("mtn") || descLower.Contains("cell c") ||
            descLower.Contains("telkom") || descLower.Contains("cellphone"))
            return FindCategoryId(categories, new[] { "Cellphone Contract", "Communication" }, defaultCategoryId);

        // Internet/TV
        if (descLower.Contains("dstv") || descLower.Contains("internet") || descLower.Contains("wifi") ||
            descLower.Contains("multichoice") || descLower.Contains("showmax") || descLower.Contains("netflix"))
            return FindCategoryId(categories, new[] { "Internet & WIFI", "DSTV/Streaming Services", "Entertainment" }, defaultCategoryId);

        // Rent/Bond
        if (descLower.Contains("rent") || descLower.Contains("bond") || descLower.Contains("mortgage") ||
            descLower.Contains("lease"))
            return FindCategoryId(categories, new[] { "Rent/Bond Payment", "Rent/Mortgage" }, defaultCategoryId);

        // Income/Salary
        if (descLower.Contains("salary") || descLower.Contains("income") || descLower.Contains("payment received") ||
            descLower.Contains("transfer in") || descLower.Contains("bonus"))
            return FindCategoryId(categories, new[] { "Salary Income", "Income", "Bonus Income" }, defaultCategoryId);

        // Insurance
        if (descLower.Contains("insurance") || descLower.Contains("assurance"))
        {
            if (descLower.Contains("car") || descLower.Contains("vehicle"))
                return FindCategoryId(categories, new[] { "Car Insurance" }, defaultCategoryId);
            if (descLower.Contains("life"))
                return FindCategoryId(categories, new[] { "Life Insurance" }, defaultCategoryId);
            if (descLower.Contains("home") || descLower.Contains("house"))
                return FindCategoryId(categories, new[] { "Home Insurance" }, defaultCategoryId);
            if (descLower.Contains("funeral"))
                return FindCategoryId(categories, new[] { "Funeral Cover" }, defaultCategoryId);
            
            return FindCategoryId(categories, new[] { "Insurance", "Life Insurance" }, defaultCategoryId);
        }

        // Store accounts
        if (descLower.Contains("edgars") || descLower.Contains("woolworths account") || 
            descLower.Contains("truworths") || descLower.Contains("jet"))
            return FindCategoryId(categories, new[] { "Store Account Payment", "Clothing & Shoes" }, defaultCategoryId);

        // Dining/Restaurants
        if (descLower.Contains("restaurant") || descLower.Contains("kfc") || descLower.Contains("mcdonald") ||
            descLower.Contains("nando") || descLower.Contains("steers") || descLower.Contains("wimpy") ||
            descLower.Contains("pizza") || descLower.Contains("uber eats") || descLower.Contains("mr d"))
            return FindCategoryId(categories, new[] { "Dining Out & Takeaways", "Dining Out", "Entertainment" }, defaultCategoryId);

        return defaultCategoryId;
    }

    /// <summary>
    /// Helper method to find category ID by trying multiple name variations.
    /// </summary>
    private int FindCategoryId(List<Category> categories, string[] categoryNames, int defaultCategoryId)
    {
        foreach (var name in categoryNames)
        {
            var category = categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (category != null)
            {
                return category.Id;
            }
        }
        return defaultCategoryId;
    }

    [HttpGet]
    public async Task<IActionResult> ExportTransactions(DateTime? startDate = null, DateTime? endDate = null)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        var start = startDate ?? DateTime.Now.AddYears(-1);
        var end = endDate ?? DateTime.Now;

        var transactions = await _context.Transactions
            .Where(t => t.Category!.UserId == currentUserId && t.Date >= start && t.Date <= end)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .OrderBy(t => t.Date)
            .ToListAsync();

        var exportData = transactions.Select(t => new
        {
            Date = t.Date.ToString("yyyy-MM-dd"),
            Description = t.Description,
            Amount = t.TransactionType == TransactionType.Income ? t.Amount : -t.Amount,
            Category = t.Category?.Name ?? "Uncategorized",
            Account = t.Account?.Name ?? "Cash",
            Type = t.TransactionType.ToString()
        });

        // Create CSV
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(exportData);

        var csvContent = writer.ToString();
        var fileName = $"transactions_{start:yyyy-MM-dd}_to_{end:yyyy-MM-dd}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportBudgets(string monthYear)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");

        monthYear ??= DateTime.Now.ToString("yyyy-MM");

        var budgets = await _context.MonthlyBudgets
            .Where(b => b.Category.UserId == currentUserId && b.MonthYear == monthYear)
            .Include(b => b.Category)
            .OrderBy(b => b.Category.Name)
            .ToListAsync();

        var exportData = budgets.Select(b => new
        {
            Month = monthYear,
            Category = b.Category.Name,
            BudgetedAmount = b.Amount,
            SpentAmount = b.Category.Transactions
                .Where(t => t.Date.Year.ToString() + "-" + t.Date.Month.ToString("D2") == monthYear && 
                           t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount),
            RemainingAmount = b.Amount - b.Category.Transactions
                .Where(t => t.Date.Year.ToString() + "-" + t.Date.Month.ToString("D2") == monthYear && 
                           t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount)
        });

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(exportData);

        var csvContent = writer.ToString();
        var fileName = $"budget_{monthYear}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }

    private async Task ProcessQifImport(IFormFile file, ImportSession session, string userId)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var blocks = content.Split('^', StringSplitOptions.RemoveEmptyEntries);
        session.TotalRecords = blocks.Length;
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        int ok = 0, fail = 0;
        foreach (var block in blocks)
        {
            try
            {
                string? date = null, amount = null, payee = null;
                foreach (var line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (line.StartsWith("D")) date = line[1..].Trim();
                    else if (line.StartsWith("T")) amount = line[1..].Trim();
                    else if (line.StartsWith("P")) payee = line[1..].Trim();
                    else if (line.StartsWith("M") && payee == null) payee = line[1..].Trim();
                }
                if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(amount)) { fail++; continue; }
                if (!DateTime.TryParse(date, out var dt)) { fail++; continue; }
                if (!decimal.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt)) { fail++; continue; }
                _context.Transactions.Add(new Transaction
                {
                    Description = payee ?? "QIF import",
                    Amount = Math.Abs(amt),
                    Date = dt,
                    TransactionType = amt < 0 ? TransactionType.Expense : TransactionType.Income,
                    AccountId = account?.Id,
                    ImportSource = "QIF",
                    ImportId = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                ok++;
            }
            catch { fail++; }
        }
        session.SuccessfulRecords = ok;
        session.FailedRecords = fail;
        session.ProcessedRecords = ok + fail;
        await _context.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> MappingTemplates()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");
        var maps = await _context.ImportColumnMappings.Where(m => m.UserId == currentUserId).OrderBy(m => m.TemplateName).ToListAsync();
        ViewBag.BankTemplates = new[] { "generic-csv", "chase", "bank-of-america", "capital-one", "ofx-default", "qfx-default" };
        return View(maps);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMapping(string templateName, string bankTemplate, string dateColumn, string amountColumn, string descriptionColumn)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return RedirectToAction("Login", "Account");
        var json = System.Text.Json.JsonSerializer.Serialize(new { date = dateColumn, amount = amountColumn, description = descriptionColumn });
        _context.ImportColumnMappings.Add(new ImportColumnMapping
        {
            UserId = currentUserId,
            TemplateName = templateName,
            BankTemplate = bankTemplate,
            MappingJson = json
        });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Mapping template saved.";
        return RedirectToAction(nameof(MappingTemplates));
    }
}

public class CsvTransactionRecord
{
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public string? Account { get; set; }
}