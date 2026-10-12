# How to Set Up Your Personal Account

This directory contains tools to help set up a personal DoughDoodle account with South African financial categories.

## ⚠️ CRITICAL SECURITY WARNING

**NEVER commit passwords or personal credentials to source code!**

The setup scripts in this project require you to provide the password at runtime, not in the code.

## Files in This Setup

1. **PersonalAccountSetup.cs** - Core functionality for creating accounts with SA categories
2. **PersonalAccountOneTimeSetup.cs** - One-time setup script
3. **PERSONAL_ACCOUNT_SETUP_GUIDE.md** - Complete setup and usage guide
4. **sample_import_template.csv** - Template for importing financial data

## Setup Methods

### Method 1: Via Application UI (Recommended for Production)

1. Run the DoughDoodle application
2. Navigate to the registration page
3. Create account with your email and password
4. The system will create the user with default categories
5. Follow the import guide to load your financial data

### Method 2: Via Command Line Setup (Development Only)

Add this code to your `Program.cs` file before `app.Run()`:

```csharp
// One-time setup for personal account
if (args.Contains("--setup-personal-account"))
{
    using var scope = app.Services.CreateScope();
    await PersonalAccountOneTimeSetup.SetupFromCommandLineAsync(scope.ServiceProvider);
    return;
}
```

Then run:
```bash
dotnet run --setup-personal-account
```

You'll be prompted to enter the password securely.

### Method 3: Via Database Seeding (Development Only)

**Only use this in development environments!**

Modify `SeedData.cs` to call the setup (but use environment variables for the password):

```csharp
var personalPassword = Environment.GetEnvironmentVariable("PERSONAL_ACCOUNT_PASSWORD");
if (!string.IsNullOrEmpty(personalPassword))
{
    await PersonalAccountSetup.CreatePersonalAccountAsync(
        context,
        userManager,
        "user@example.com",
        personalPassword,
        "User",
        "User"
    );
}
```

Then run with:
```bash
export PERSONAL_ACCOUNT_PASSWORD="YourSecurePassword"
dotnet ef database drop
dotnet ef database update
dotnet run
```

## After Setup: Importing Financial Data

Once your account is created, follow these steps:

### 1. Prepare Your CSV Files

Convert your financial documents to CSV format using the `sample_import_template.csv` as a guide.

#### Bank Statements
- Extract transactions from PDF/Excel bank statements
- Format as CSV with columns: Date, Description, Amount, Category, Account
- Negative amounts for expenses, positive for income

#### Hyphen PDA/Debt Review Statements
- Extract your monthly debt review payment
- Include in CSV with category: "PDA - Debt Review Payment"

#### Payslips
- Extract income information
- Include in CSV with positive amounts

### 2. Import via UI

1. Log into DoughDoodle
2. Go to **Import/Export** → **Import Transactions**
3. Upload each CSV file
4. System will automatically import and categorize

### 3. Review and Adjust

1. Check **Transactions** page for imported data
2. Review any "Uncategorized" transactions
3. Assign to appropriate categories
4. Set up monthly budgets based on your debt review plan

## South African Budget Categories

Your account includes these SA-specific categories:

### Debt Review (PDA)
- **PDA - Debt Review Payment** - Your Hyphen debt counseling payment
- Credit Card Payment
- Personal Loan Payment
- Store Account Payment

### Essential Living
- Groceries & Food
- Rent/Bond Payment
- Electricity & Water
- Municipal Rates & Taxes

### Transportation
- Car Payment/Installment
- Fuel/Petrol
- Car Insurance
- Toll Fees

### Insurance
- Medical Aid/Health Insurance
- Life Insurance
- Funeral Cover

### Communication
- Cellphone Contract
- Internet & WIFI
- DSTV/Streaming Services

[Full list in PersonalAccountSetup.cs]

## Sample Budget for Debt Review

Based on typical debt review scenarios:

```
Monthly Income: R15,000

Fixed/Priority:
├── PDA Debt Review Payment: R5,000 (33%)
├── Rent/Bond: R4,000 (27%)
├── Medical Aid: R1,200 (8%)
├── Car Insurance: R800 (5%)
└── Total Fixed: R11,000 (73%)

Variable/Essential:
├── Groceries & Food: R2,500 (17%)
├── Electricity & Water: R800 (5%)
├── Fuel/Petrol: R600 (4%)
└── Total Variable: R3,900 (26%)

Remaining for Emergency Fund: R100 (1%)
```

## Support

- See `PERSONAL_ACCOUNT_SETUP_GUIDE.md` for detailed instructions
- Check `USER_GUIDE.md` for general application help
- Review in-app help tooltips (? icon)

## Data Privacy

- All data is stored locally in your database
- No data is shared with third parties
- Export your data anytime via the Export feature
- Delete account through settings if needed
