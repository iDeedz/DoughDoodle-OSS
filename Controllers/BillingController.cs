using DoughDoodle.Models;
using DoughDoodle.Services.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DoughDoodle.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStripeBillingService _billing;
    private readonly IDoughEntitlementService _entitlements;
    private readonly StripeOptions _options;

    public BillingController(
        UserManager<ApplicationUser> userManager,
        IStripeBillingService billing,
        IDoughEntitlementService entitlements,
        IOptions<StripeOptions> options)
    {
        _userManager = userManager;
        _billing = billing;
        _entitlements = entitlements;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? success = null, int? canceled = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var tier = _entitlements.GetTier(user);
        var packs = Enum.GetValues<DoughTier>().Select(t => new DoughPackCardViewModel
        {
            Tier = t,
            Name = _entitlements.GetPackDisplayName(t),
            Tagline = _entitlements.GetPackTagline(t),
            PriceLabel = _entitlements.GetPackPriceLabel(t),
            ImageSrc = _entitlements.GetTierImagePath(t),
            IsFree = t == DoughTier.Pocket,
            IsCurrent = t == tier,
            AccentClass = t switch
            {
                DoughTier.Pocket => "dd-pack-pocket",
                DoughTier.Bankroll => "dd-pack-bankroll",
                DoughTier.Stack => "dd-pack-stack",
                DoughTier.Vault => "dd-pack-vault",
                DoughTier.Prime => "dd-pack-prime",
                _ => ""
            },
            Features = FeaturesFor(t),
            MonthlyPriceId = t == DoughTier.Pocket ? null : _options.ResolveMonthlyPriceId(t.ToString()),
            YearlyPriceId = t == DoughTier.Pocket ? null : _options.ResolveYearlyPriceId(t.ToString())
        }).ToList();

        var vm = new BillingPageViewModel
        {
            CurrentTier = tier,
            CurrentPackName = _entitlements.GetPackDisplayName(tier),
            SubscriptionStatus = user.SubscriptionStatus,
            PeriodEnd = user.SubscriptionCurrentPeriodEnd,
            StripeConfigured = _billing.IsConfigured,
            HasCustomer = !string.IsNullOrWhiteSpace(user.StripeCustomerId),
            CheckoutSuccess = success == 1,
            CheckoutCanceled = canceled == 1,
            Packs = packs
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string priceId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!_billing.IsConfigured)
        {
            TempData["ErrorMessage"] = "Stripe is not configured yet. Add Stripe keys in appsettings (SecretKey, PublishableKey, Domain, and Price IDs). Checkout stays disabled until then — Pocket remains free.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var url = await _billing.CreateCheckoutSessionAsync(user, priceId);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Portal()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!_billing.IsConfigured)
        {
            TempData["ErrorMessage"] = "Stripe is not configured yet.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var url = await _billing.CreateCustomerPortalSessionAsync(user);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private static List<string> FeaturesFor(DoughTier t) => t switch
    {
        DoughTier.Pocket => new() { "10 envelopes", "2 accounts", "2 goals", "3-month history", "CSV import", "Dough left today" },
        DoughTier.Bankroll => new() { "25 envelopes", "5 accounts", "Household share", "12-month history", "Pace alerts", "Advanced reports" },
        DoughTier.Stack => new() { "50 envelopes", "AI insights", "Deep CSV history", "15 goals", "24-month history", "Priority tools" },
        DoughTier.Vault => new() { "100 envelopes", "Investments", "Net-worth depth", "10 household seats", "60-month history", "Vault analytics" },
        DoughTier.Prime => new() { "Unlimited envelopes", "Unlimited accounts", "Unlimited goals", "10-year history", "Full product access", "Priority support seat" },
        _ => new()
    };
}

public class BillingPageViewModel
{
    public DoughTier CurrentTier { get; set; }
    public string CurrentPackName { get; set; } = "Pocket";
    public string? SubscriptionStatus { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public bool StripeConfigured { get; set; }
    public bool HasCustomer { get; set; }
    public bool CheckoutSuccess { get; set; }
    public bool CheckoutCanceled { get; set; }
    public List<DoughPackCardViewModel> Packs { get; set; } = new();
}

public class DoughPackCardViewModel
{
    public DoughTier Tier { get; set; }
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string PriceLabel { get; set; } = "";
    public string ImageSrc { get; set; } = "";
    public string AccentClass { get; set; } = "";
    public bool IsFree { get; set; }
    public bool IsCurrent { get; set; }
    public List<string> Features { get; set; } = new();
    public string? MonthlyPriceId { get; set; }
    public string? YearlyPriceId { get; set; }
}
