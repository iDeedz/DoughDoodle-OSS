namespace DoughDoodle.Models;

/// <summary>
/// Sample bank CSV column hints. Mapping is user-owned; this is not live bank sync.
/// </summary>
public sealed record BankImportPreset(
    string Key,
    string DisplayName,
    string DateColumn,
    string AmountColumn,
    string DescriptionColumn,
    string Notes);

public static class BankImportPresets
{
    public static IReadOnlyList<BankImportPreset> All { get; } =
    [
        new("generic-csv", "Generic / DoughDoodle sample", "Date", "Amount", "Description",
            "Date, Description, Amount, Category, Account. Expenses negative."),
        new("chase", "Chase", "Transaction Date", "Amount", "Description",
            "Typical Chase export: Transaction Date, Description, Amount."),
        new("bank-of-america", "Bank of America", "Date", "Amount", "Description",
            "Typical BofA export: Date, Description, Amount, Running Bal."),
        new("capital-one", "Capital One", "Transaction Date", "Debit / Credit", "Description",
            "Map Debit or Credit into Amount. Keep one signed Amount column for DoughDoodle."),
        new("ofx-default", "OFX statement", "DTPOSTED", "TRNAMT", "NAME",
            "OFX/QFX upload skips column mapping."),
        new("qfx-default", "QFX (Quicken)", "DTPOSTED", "TRNAMT", "NAME",
            "Same as OFX. Export from your bank, then import here.")
    ];

    /// <summary>Sample rows matching CsvTransactionRecord. Not live user data.</summary>
    public const string SampleCsvContent =
        """
        Date,Description,Amount,Category,Account
        2026-01-25,Paycheck deposit,15000.00,,Everyday Checking
        2026-01-20,Rent payment,-1200.00,Rent/Mortgage,Everyday Checking
        2026-01-15,Grocery store,-45.50,Groceries,Everyday Checking
        2026-01-16,Fuel station,-60.00,Transportation/Gas,Everyday Checking
        2026-01-17,Electric bill,-80.00,Utilities,Everyday Checking
        """;
}
