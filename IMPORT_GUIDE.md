# Bank Statement & Debt Review Import Guide

This guide specifically helps you import your bank statements, Hyphen PDA statements, and payslips into DoughDoodle.

## 📋 What You Need

1. **Bank Statements** (last 3 months) - PDF or Excel format from your bank
2. **Hyphen PDA Statements** (debt review) - Your monthly debt counseling statements
3. **Payslips** (last 3 months) - Your salary documentation

## 🔄 Step-by-Step Import Process

### Step 1: Convert Documents to CSV

#### For Bank Statements (PDF/Excel)

Most South African banks provide statements in PDF or Excel. You need to convert them to CSV:

**From Excel/Spreadsheet:**
1. Open the bank statement in Excel
2. Identify the columns: Date, Description, Amount, Balance
3. Create a new sheet with these columns:
   - Date (YYYY-MM-DD format)
   - Description 
   - Amount (negative for expenses, positive for income)
4. Copy your transactions to this format
5. Save as CSV (File → Save As → CSV)

**From PDF:**
1. Copy table data from PDF to Excel
2. Clean up the data
3. Format as above
4. Save as CSV

**Example conversion:**

Original bank statement:
```
Date          Description              Debit      Credit     Balance
15/01/2024    Salary                              15000.00   15000.00
16/01/2024    Pick n Pay               450.50                14549.50
17/01/2024    PDA Payment              5000.00               9549.50
```

Converted to CSV:
```csv
Date,Description,Amount
2024-01-15,Salary,15000.00
2024-01-16,Pick n Pay,-450.50
2024-01-17,PDA Payment,-5000.00
```

#### For Hyphen PDA Statements

Extract your debt review payments from the PDA statement:

```csv
Date,Description,Amount,Category
2024-01-20,Hyphen PDA Monthly Payment,-5000.00,PDA - Debt Review Payment
2024-01-20,PDA Administration Fee,-200.00,PDA - Debt Review Payment
```

#### For Payslips

Extract income information:

```csv
Date,Description,Amount
2024-01-25,Salary - [Your Company],-15000.00
2024-01-25,Overtime,500.00
2024-01-25,Allowances,300.00
```

### Step 2: Combine Files (Optional)

You can either:
- Import each month separately (3 files for bank statements)
- Combine all months into one CSV file
- Keep PDA and payslips in separate files

**Combined example:**
```csv
Date,Description,Amount,Category,Account
2024-01-25,Salary,15000.00,,Checking
2024-01-20,Hyphen PDA Payment,-5000.00,PDA - Debt Review Payment,Checking
2024-01-16,Pick n Pay Groceries,-450.50,Groceries & Food,Checking
2024-01-17,Engen Fuel,-600.00,Fuel/Petrol,Checking
2024-02-25,Salary,15000.00,,Checking
2024-02-20,Hyphen PDA Payment,-5000.00,PDA - Debt Review Payment,Checking
2024-03-25,Salary,15000.00,,Checking
2024-03-20,Hyphen PDA Payment,-5000.00,PDA - Debt Review Payment,Checking
```

### Step 3: Import into DoughDoodle

1. **Log into your account** at user@example.com
2. **Navigate to Import/Export** section
3. **Click "Import Transactions"**
4. **Select your CSV file**
5. **Click Upload**

The system will:
- ✅ Import all transactions
- ✅ Auto-detect transaction types (income vs expense)
- ✅ Auto-categorize based on description (see Auto-Categorization below)
- ✅ Show import summary

### Step 4: Review Imported Data

1. Go to **Transactions** page
2. Filter by date range to see your imported data
3. Check for any "Uncategorized" transactions
4. Manually assign categories as needed

## 🤖 Auto-Categorization

The import system automatically categorizes South African transactions based on merchant names:

| Merchant/Description | Auto-Assigned Category |
|---------------------|------------------------|
| PDA, Hyphen, Debt Review | PDA - Debt Review Payment |
| Pick n Pay, Checkers, Woolworths, Spar, Shoprite | Groceries & Food |
| Engen, Shell, BP, Caltex, Sasol | Fuel/Petrol |
| Eskom, Electricity | Electricity & Water |
| Discovery, Medshield, Medical Aid | Medical Aid/Health Insurance |
| Vodacom, MTN, Cell C, Telkom | Cellphone Contract |
| DSTV, Netflix, Showmax | DSTV/Streaming Services |
| Salary, Income, Bonus | Income |
| Rent, Bond, Mortgage | Rent/Bond Payment |

**Pro Tip:** Include keywords in your CSV description column to help auto-categorization!

## 📊 After Import: Set Up Your Budget

Once imported, set up your monthly budget:

### 1. Calculate Total Monthly Income
Based on your payslips:
```
Salary: R15,000
Allowances: R500
TOTAL: R15,500
```

### 2. Set Fixed Expenses (Debt Review Priority)
```
PDA Payment: R5,000 (non-negotiable)
Rent/Bond: R4,000
Medical Aid: R1,200
Car Insurance: R800
TOTAL FIXED: R11,000
```

### 3. Set Variable Expenses
```
Groceries: R2,500
Electricity: R800
Fuel: R600
Cellphone: R300
TOTAL VARIABLE: R4,200
```

### 4. Remaining Budget
```
Total Income: R15,500
Total Expenses: R15,200
Available for Savings/Emergency: R300
```

## ⚠️ Important Notes

### For Debt Review Clients (Hyphen PDA)

1. **PDA Payment is Priority**: Always ensure this category is fully funded
2. **Communicate Changes**: If income changes, contact Hyphen immediately
3. **Track All Payments**: Every creditor payment goes through PDA
4. **No New Debt**: Avoid creating new debt while under review
5. **Document Everything**: Keep all PDA statements for reference

### Data Quality Tips

1. **Date Format**: Always use YYYY-MM-DD (e.g., 2024-01-25)
2. **Amount Format**: 
   - No currency symbols (R, $)
   - Use negative for expenses: -450.50
   - Use positive for income: 15000.00
3. **Descriptions**: Include merchant name for better categorization
4. **Duplicates**: Check for duplicate transactions before importing

## 🔍 Troubleshooting

### Import Failed
- Check CSV format matches template
- Ensure dates are valid (YYYY-MM-DD)
- Remove special characters from amounts
- Check file encoding is UTF-8

### Wrong Categories
- Manually reassign in Transactions page
- Update description to include category keywords
- Reimport with Category column specified

### Missing Transactions
- Check date range filter
- Verify CSV has all rows
- Look in "Uncategorized" category

### Duplicate Transactions
- Use date + amount + description to identify duplicates
- Delete duplicates from Transactions page
- Filter by import session to find specific import

## 📱 Quick Reference: Common SA Bank CSV Formats

### Standard Chartered / FNB Format
```csv
Date,Description,Amount
25/01/2024,SALARY COMPANY XYZ,15000.00
20/01/2024,DEBIT ORDER - HYPHEN PDA,-5000.00
```

### Capitec Format  
```csv
Date,Description,Amount,Balance
2024-01-25,Salary,15000.00,15000.00
2024-01-20,PDA Payment,-5000.00,10000.00
```

### Nedbank Format
```csv
Transaction Date,Description,Debit,Credit
25 Jan 2024,SALARY CREDIT,,15000.00
20 Jan 2024,PDA PAYMENT,5000.00,
```

**Convert any format to DoughDoodle standard:**
```csv
Date,Description,Amount
2024-01-25,Salary,15000.00
2024-01-20,PDA Payment,-5000.00
```

## ✅ Final Checklist

Before importing:
- [ ] All PDFs converted to CSV
- [ ] Dates in YYYY-MM-DD format
- [ ] Amounts with correct positive/negative signs
- [ ] No duplicate transactions
- [ ] File saved as CSV (not Excel)

After importing:
- [ ] Review transaction count
- [ ] Check all categories assigned
- [ ] Verify income vs expense split
- [ ] Set monthly budgets
- [ ] Review PDA payment allocation

## 🆘 Need Help?

If you encounter issues:
1. Check this guide first
2. Review `sample_import_template.csv` for format reference
3. Use smaller test files first (1 month)
4. Contact support through the app

Remember: Your financial data is private and secure. Only you have access to your account.
