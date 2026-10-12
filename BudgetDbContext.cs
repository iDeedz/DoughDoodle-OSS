using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DoughDoodle.Models;

namespace DoughDoodle.Data;

public class BudgetDbContext : IdentityDbContext<ApplicationUser>
{
  // Core budgeting entities
  public DbSet<Category> Categories { get; set; }
  public DbSet<Transaction> Transactions { get; set; }
  public DbSet<MonthlyBudget> MonthlyBudgets { get; set; }
  
  // Account management
  public DbSet<Account> Accounts { get; set; }
  
  // Enhanced transaction features
  public DbSet<TransactionSplit> TransactionSplits { get; set; }
  public DbSet<TransactionTag> TransactionTags { get; set; }
  public DbSet<TransactionTagAssignment> TransactionTagAssignments { get; set; }
  
  // Automation
  public DbSet<ScheduledTransaction> ScheduledTransactions { get; set; }
  public DbSet<TransactionRule> TransactionRules { get; set; }
  
  // Budgeting features
  public DbSet<Income> Incomes { get; set; }
  public DbSet<CategoryRollover> CategoryRollovers { get; set; }
  public DbSet<BudgetSettings> BudgetSettings { get; set; }
  
  // Reporting and tracking
  public DbSet<SavedReport> SavedReports { get; set; }
  public DbSet<UserAction> UserActions { get; set; }
  public DbSet<ImportSession> ImportSessions { get; set; }
  
  // Enhanced features
  public DbSet<FinancialGoal> FinancialGoals { get; set; }
  public DbSet<Notification> Notifications { get; set; }
  public DbSet<NotificationSettings> NotificationSettings { get; set; }
  public DbSet<SecurityLog> SecurityLogs { get; set; }
  public DbSet<MonthlyIncome> MonthlyIncomes { get; set; }
  
  // Bill tracking
  public DbSet<Bill> Bills { get; set; }
  public DbSet<BillPayment> BillPayments { get; set; }
  
  // Net Worth tracking
  public DbSet<NetWorthItem> NetWorthItems { get; set; }
  public DbSet<NetWorthHistory> NetWorthHistories { get; set; }
  
  // Stripe webhook idempotency
  public DbSet<ProcessedStripeEvent> ProcessedStripeEvents { get; set; }
  
  // Investment tracking
  public DbSet<InvestmentAccount> InvestmentAccounts { get; set; }
  public DbSet<PortfolioHolding> PortfolioHoldings { get; set; }
  
  // Automated transfers
  public DbSet<ScheduledTransfer> ScheduledTransfers { get; set; }
  
  // Tax optimization
  public DbSet<TaxOptimizationSuggestion> TaxOptimizationSuggestions { get; set; }

  // Roadmap clearance (phases 1–20 + extras)
  public DbSet<BankConnection> BankConnections { get; set; }
  public DbSet<ExchangeRate> ExchangeRates { get; set; }
  public DbSet<ExpenseProject> ExpenseProjects { get; set; }
  public DbSet<CollaborativeComment> CollaborativeComments { get; set; }
  public DbSet<SharedEnvelope> SharedEnvelopes { get; set; }
  public DbSet<ImportColumnMapping> ImportColumnMappings { get; set; }
  
  public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure User relationships
    modelBuilder.Entity<ApplicationUser>()
        .HasOne(u => u.ParentUser)
        .WithMany(u => u.SubAccounts)
        .HasForeignKey(u => u.ParentUserId)
        .OnDelete(DeleteBehavior.Restrict);

    // Configure Category relationships
    modelBuilder.Entity<Category>()
        .HasOne(c => c.User)
        .WithMany(u => u.Categories)
        .HasForeignKey(c => c.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure MonthlyBudget relationships
    modelBuilder.Entity<MonthlyBudget>()
        .HasOne(mb => mb.Category)
        .WithMany(c => c.Budgets)
        .HasForeignKey(mb => mb.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Transaction relationships
    modelBuilder.Entity<Transaction>()
        .HasOne(t => t.Category)
        .WithMany(c => c.Transactions)
        .HasForeignKey(t => t.CategoryId)
        .OnDelete(DeleteBehavior.SetNull);
        
    modelBuilder.Entity<Transaction>()
        .HasOne(t => t.Account)
        .WithMany(a => a.Transactions)
        .HasForeignKey(t => t.AccountId)
        .OnDelete(DeleteBehavior.SetNull);

    // Configure Account relationships
    modelBuilder.Entity<Account>()
        .HasOne(a => a.User)
        .WithMany()
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Transaction Split relationships
    modelBuilder.Entity<TransactionSplit>()
        .HasOne(ts => ts.Transaction)
        .WithMany(t => t.Splits)
        .HasForeignKey(ts => ts.TransactionId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TransactionSplit>()
        .HasOne(ts => ts.Category)
        .WithMany()
        .HasForeignKey(ts => ts.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Transaction Tag relationships
    modelBuilder.Entity<TransactionTag>()
        .HasOne(tt => tt.User)
        .WithMany()
        .HasForeignKey(tt => tt.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TransactionTagAssignment>()
        .HasKey(tta => new { tta.TransactionId, tta.TagId });

    modelBuilder.Entity<TransactionTagAssignment>()
        .HasOne(tta => tta.Transaction)
        .WithMany(t => t.Tags)
        .HasForeignKey(tta => tta.TransactionId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<TransactionTagAssignment>()
        .HasOne(tta => tta.Tag)
        .WithMany(tt => tt.TransactionTags)
        .HasForeignKey(tta => tta.TagId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Scheduled Transaction relationships
    modelBuilder.Entity<ScheduledTransaction>()
        .HasOne(st => st.Category)
        .WithMany()
        .HasForeignKey(st => st.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<ScheduledTransaction>()
        .HasOne(st => st.Account)
        .WithMany()
        .HasForeignKey(st => st.AccountId)
        .OnDelete(DeleteBehavior.SetNull);

    modelBuilder.Entity<ScheduledTransaction>()
        .HasOne(st => st.User)
        .WithMany()
        .HasForeignKey(st => st.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Transaction Rule relationships
    modelBuilder.Entity<TransactionRule>()
        .HasOne(tr => tr.AssignCategory)
        .WithMany()
        .HasForeignKey(tr => tr.AssignCategoryId)
        .OnDelete(DeleteBehavior.SetNull);

    modelBuilder.Entity<TransactionRule>()
        .HasOne(tr => tr.AssignAccount)
        .WithMany()
        .HasForeignKey(tr => tr.AssignAccountId)
        .OnDelete(DeleteBehavior.SetNull);

    modelBuilder.Entity<TransactionRule>()
        .HasOne(tr => tr.User)
        .WithMany()
        .HasForeignKey(tr => tr.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Income relationships
    modelBuilder.Entity<Income>()
        .HasOne(i => i.User)
        .WithMany()
        .HasForeignKey(i => i.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Category Rollover relationships
    modelBuilder.Entity<CategoryRollover>()
        .HasOne(cr => cr.Category)
        .WithMany()
        .HasForeignKey(cr => cr.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Budget Settings relationships
    modelBuilder.Entity<BudgetSettings>()
        .HasOne(bs => bs.User)
        .WithOne()
        .HasForeignKey<BudgetSettings>(bs => bs.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Saved Report relationships
    modelBuilder.Entity<SavedReport>()
        .HasOne(sr => sr.User)
        .WithMany()
        .HasForeignKey(sr => sr.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure User Action relationships
    modelBuilder.Entity<UserAction>()
        .HasOne(ua => ua.User)
        .WithMany()
        .HasForeignKey(ua => ua.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Import Session relationships
    modelBuilder.Entity<ImportSession>()
        .HasOne(ims => ims.User)
        .WithMany()
        .HasForeignKey(ims => ims.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Bill relationships
    modelBuilder.Entity<Bill>()
        .HasOne(b => b.User)
        .WithMany()
        .HasForeignKey(b => b.UserId)
        .OnDelete(DeleteBehavior.Cascade);
        
    modelBuilder.Entity<Bill>()
        .HasOne(b => b.Category)
        .WithMany()
        .HasForeignKey(b => b.CategoryId)
        .OnDelete(DeleteBehavior.SetNull);

    // Configure Bill Payment relationships
    modelBuilder.Entity<BillPayment>()
        .HasOne(bp => bp.Bill)
        .WithMany()
        .HasForeignKey(bp => bp.BillId)
        .OnDelete(DeleteBehavior.Cascade);
        
    modelBuilder.Entity<BillPayment>()
        .HasOne(bp => bp.Transaction)
        .WithMany()
        .HasForeignKey(bp => bp.TransactionId)
        .OnDelete(DeleteBehavior.SetNull);

    // Configure Net Worth Item relationships
    modelBuilder.Entity<NetWorthItem>()
        .HasOne(nw => nw.User)
        .WithMany()
        .HasForeignKey(nw => nw.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Net Worth History relationships
    modelBuilder.Entity<NetWorthHistory>()
        .HasOne(nwh => nwh.NetWorthItem)
        .WithMany(nw => nw.History)
        .HasForeignKey(nwh => nwh.NetWorthItemId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Investment Account relationships
    modelBuilder.Entity<InvestmentAccount>()
        .HasOne(ia => ia.User)
        .WithMany()
        .HasForeignKey(ia => ia.UserId)
        .OnDelete(DeleteBehavior.Cascade);
        
    modelBuilder.Entity<PortfolioHolding>()
        .HasOne(ph => ph.InvestmentAccount)
        .WithMany(ia => ia.Holdings)
        .HasForeignKey(ph => ph.InvestmentAccountId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure Scheduled Transfer relationships
    modelBuilder.Entity<ScheduledTransfer>()
        .HasOne(st => st.User)
        .WithMany()
        .HasForeignKey(st => st.UserId)
        .OnDelete(DeleteBehavior.Cascade);
        
    modelBuilder.Entity<ScheduledTransfer>()
        .HasOne(st => st.FromCategory)
        .WithMany()
        .HasForeignKey(st => st.FromCategoryId)
        .OnDelete(DeleteBehavior.SetNull);
        
    modelBuilder.Entity<ScheduledTransfer>()
        .HasOne(st => st.ToCategory)
        .WithMany()
        .HasForeignKey(st => st.ToCategoryId)
        .OnDelete(DeleteBehavior.SetNull);

    // Configure Tax Optimization Suggestion relationships
    modelBuilder.Entity<TaxOptimizationSuggestion>()
        .HasOne(tos => tos.User)
        .WithMany()
        .HasForeignKey(tos => tos.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Configure indexes for performance
    modelBuilder.Entity<Category>()
        .HasIndex(c => c.UserId);

    modelBuilder.Entity<MonthlyBudget>()
        .HasIndex(mb => new { mb.CategoryId, mb.MonthYear })
        .IsUnique();

    modelBuilder.Entity<Transaction>()
        .HasIndex(t => new { t.CategoryId, t.Date });
        
    modelBuilder.Entity<Transaction>()
        .HasIndex(t => t.AccountId);
        
    modelBuilder.Entity<Transaction>()
        .HasIndex(t => t.Date);
        
    modelBuilder.Entity<Transaction>()
        .HasIndex(t => t.ImportId);

    modelBuilder.Entity<ApplicationUser>()
        .HasIndex(u => u.ParentUserId);
        
    modelBuilder.Entity<Account>()
        .HasIndex(a => a.UserId);
        
    modelBuilder.Entity<TransactionTag>()
        .HasIndex(tt => tt.UserId);
        
    modelBuilder.Entity<ScheduledTransaction>()
        .HasIndex(st => new { st.UserId, st.NextDueDate });
        
    modelBuilder.Entity<TransactionRule>()
        .HasIndex(tr => new { tr.UserId, tr.Priority });
        
    modelBuilder.Entity<Income>()
        .HasIndex(i => new { i.UserId, i.MonthYear });
        
    modelBuilder.Entity<CategoryRollover>()
        .HasIndex(cr => new { cr.CategoryId, cr.FromMonthYear, cr.ToMonthYear })
        .IsUnique();
        
    modelBuilder.Entity<UserAction>()
        .HasIndex(ua => new { ua.UserId, ua.CreatedAt });
        
    modelBuilder.Entity<ImportSession>()
        .HasIndex(ims => new { ims.UserId, ims.CreatedAt });
        
    modelBuilder.Entity<Bill>()
        .HasIndex(b => new { b.UserId, b.DueDate });
        
    modelBuilder.Entity<Bill>()
        .HasIndex(b => new { b.UserId, b.Status });
        
    modelBuilder.Entity<BillPayment>()
        .HasIndex(bp => new { bp.BillId, bp.PaymentDate });
        
    modelBuilder.Entity<NetWorthItem>()
        .HasIndex(nw => new { nw.UserId, nw.IsAsset });
        
    modelBuilder.Entity<NetWorthHistory>()
        .HasIndex(nwh => new { nwh.NetWorthItemId, nwh.RecordedAt });
        
    modelBuilder.Entity<InvestmentAccount>()
        .HasIndex(ia => new { ia.UserId, ia.IsActive });
        
    modelBuilder.Entity<PortfolioHolding>()
        .HasIndex(ph => new { ph.InvestmentAccountId, ph.Symbol });
        
    modelBuilder.Entity<ScheduledTransfer>()
        .HasIndex(st => new { st.UserId, st.NextTransferDate, st.IsActive });
        
    modelBuilder.Entity<TaxOptimizationSuggestion>()
        .HasIndex(tos => new { tos.UserId, tos.CreatedAt });

    modelBuilder.Entity<BankConnection>()
        .HasOne(bc => bc.User)
        .WithMany()
        .HasForeignKey(bc => bc.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    modelBuilder.Entity<BankConnection>()
        .HasIndex(bc => new { bc.UserId, bc.IsActive });

    modelBuilder.Entity<ExchangeRate>()
        .HasIndex(er => new { er.BaseCurrency, er.QuoteCurrency, er.AsOfUtc });

    modelBuilder.Entity<ExpenseProject>()
        .HasOne(ep => ep.User)
        .WithMany()
        .HasForeignKey(ep => ep.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CollaborativeComment>()
        .HasOne(cc => cc.User)
        .WithMany()
        .HasForeignKey(cc => cc.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    modelBuilder.Entity<CollaborativeComment>()
        .HasIndex(cc => new { cc.EntityType, cc.EntityId });

    modelBuilder.Entity<SharedEnvelope>()
        .HasOne(se => se.Category)
        .WithMany()
        .HasForeignKey(se => se.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);
    modelBuilder.Entity<SharedEnvelope>()
        .HasIndex(se => new { se.HouseholdOwnerId, se.CategoryId })
        .IsUnique();

    modelBuilder.Entity<ImportColumnMapping>()
        .HasIndex(m => new { m.UserId, m.TemplateName });
  }
}