using System.Text;
using DoughDoodle.Data;
using DoughDoodle.Models;
using DoughDoodle.Services;
using DoughDoodle.Services.Analytics;
using DoughDoodle.Services.Bank;
using DoughDoodle.Services.Billing;
using DoughDoodle.Services.Currency;
using DoughDoodle.Services.Email;
using DoughDoodle.Services.Security;
using DoughDoodle.Services.Undo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BudgetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=budget.db"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<BudgetDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOpts = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Key));

// Identity already registered cookie auth; add JWT as an additional scheme for /api/*
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = signingKey
        };
    });
// Keep Identity cookie as default for MVC
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<PlaidOptions>(builder.Configuration.GetSection(PlaidOptions.SectionName));
builder.Services.Configure<FxApiOptions>(builder.Configuration.GetSection(FxApiOptions.SectionName));

builder.Services.AddScoped<IStripeBillingService, StripeBillingService>();
builder.Services.AddScoped<IDoughEntitlementService, DoughEntitlementService>();
builder.Services.AddScoped<ISafeToSpendService, SafeToSpendService>();
builder.Services.AddScoped<ISubscriptionHunterService, SubscriptionHunterService>();
builder.Services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();
builder.Services.AddScoped<IAlertDispatchService, AlertDispatchService>();
builder.Services.AddScoped<IOfxImportService, OfxImportService>();
builder.Services.AddScoped<IBankSyncProvider, ManualCsvBankSyncProvider>();
builder.Services.AddScoped<IBankSyncProvider, PlaidBankSyncProvider>();
builder.Services.AddScoped<IBankSyncOrchestrator, BankSyncOrchestrator>();
builder.Services.AddScoped<ICashflowForecastService, CashflowForecastService>();
builder.Services.AddScoped<IGoalAnalyticsService, GoalAnalyticsService>();
builder.Services.AddScoped<ITransactionUndoService, TransactionUndoService>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<ICurrencyRateService, CurrencyRateService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPerformanceCache, PerformanceCache>();

builder.Services.AddHttpClient<GrokAIProvider>();
builder.Services.AddHttpClient<OpenAIProvider>();
builder.Services.AddHttpClient<GeminiAIProvider>();
builder.Services.AddHttpClient<FallbackAIProvider>();

builder.Services.AddScoped<IAIProvider, GrokAIProvider>();
builder.Services.AddScoped<IAIProvider, OpenAIProvider>();
builder.Services.AddScoped<IAIProvider, GeminiAIProvider>();
builder.Services.AddScoped<IAIProvider, FallbackAIProvider>();
builder.Services.AddScoped<IAIProviderManager, AIProviderManager>();
builder.Services.AddScoped<IGrokBudgetAdvisorService, GrokBudgetAdvisorService>();
builder.Services.AddScoped<IMoneyTransferService, MoneyTransferService>();
builder.Services.AddScoped<IRolloverService, RolloverService>();
builder.Services.AddScoped<IDefaultCategoryService, DefaultCategoryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    context.Database.Migrate();

    var allowDemo = app.Configuration.GetValue("Seed:AllowDemoUsers", app.Environment.IsDevelopment());
    await SeedData.InitializeAsync(context, userManager, roleManager, allowDemoUsers: allowDemo);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
