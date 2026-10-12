#!/bin/bash

# DoughDoodle Personal Account Setup Script
# This script helps prepare CSV files for import

echo "=========================================="
echo "DoughDoodle Personal Account CSV Setup"
echo "=========================================="
echo ""

# Check if user wants to see help
if [ "$1" == "--help" ] || [ "$1" == "-h" ]; then
    echo "This script helps you prepare CSV files for import into DoughDoodle"
    echo ""
    echo "Usage: ./prepare-import.sh"
    echo ""
    echo "The script will:"
    echo "1. Validate CSV files in the current directory"
    echo "2. Check date formats"
    echo "3. Verify amount formats"
    echo "4. Suggest fixes for common issues"
    echo ""
    echo "Required CSV format:"
    echo "Date,Description,Amount,Category,Account"
    echo "2024-01-25,Salary,15000.00,Salary Income,Checking"
    echo ""
    exit 0
fi

# Function to validate CSV file
validate_csv() {
    local file=$1
    echo "Validating $file..."
    
    # Check if file exists
    if [ ! -f "$file" ]; then
        echo "  ❌ File not found: $file"
        return 1
    fi
    
    # Check if it's a CSV file
    if [[ ! "$file" =~ \.csv$ ]]; then
        echo "  ❌ Not a CSV file: $file"
        return 1
    fi
    
    # Read header
    header=$(head -n 1 "$file")
    echo "  Header: $header"
    
    # Check for required columns
    if [[ ! "$header" =~ "Date" ]] || [[ ! "$header" =~ "Description" ]] || [[ ! "$header" =~ "Amount" ]]; then
        echo "  ⚠️  Warning: Missing required columns (Date, Description, Amount)"
        echo "     Current header: $header"
        echo "     Expected: Date,Description,Amount,Category,Account"
    else
        echo "  ✅ Header looks good"
    fi
    
    # Count records (excluding header)
    record_count=$(($(wc -l < "$file") - 1))
    echo "  📊 Records: $record_count"
    
    # Check a few records for common issues
    echo "  🔍 Checking first few records..."
    
    # Skip header and check first 3 data rows
    tail -n +2 "$file" | head -n 3 | while IFS=',' read -r date desc amount rest; do
        # Check date format (should be YYYY-MM-DD)
        if [[ ! "$date" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ ]]; then
            echo "     ⚠️  Date format issue: '$date' (should be YYYY-MM-DD)"
        fi
        
        # Check amount format (should be numeric with optional decimal)
        if [[ ! "$amount" =~ ^-?[0-9]+\.?[0-9]*$ ]]; then
            echo "     ⚠️  Amount format issue: '$amount' (should be numeric, e.g., -450.50 or 1500.00)"
        fi
    done
    
    echo "  ✅ Validation complete for $file"
    echo ""
}

# Look for CSV files in current directory
csv_files=$(find . -maxdepth 1 -name "*.csv" -type f)

if [ -z "$csv_files" ]; then
    echo "❌ No CSV files found in current directory"
    echo ""
    echo "Please place your CSV files in this directory and run again."
    echo "Or use --help to see format requirements."
    exit 1
fi

echo "Found CSV files:"
echo "$csv_files"
echo ""

# Validate each CSV file
for file in $csv_files; do
    validate_csv "$file"
done

echo "=========================================="
echo "Summary & Next Steps"
echo "=========================================="
echo ""
echo "1. Review any warnings above"
echo "2. Fix any format issues in your CSV files"
echo "3. Log into DoughDoodle at http://localhost:5168"
echo "4. Navigate to Import/Export section"
echo "5. Upload your CSV files one by one"
echo ""
echo "📚 For detailed help, see:"
echo "   - IMPORT_GUIDE.md for step-by-step instructions"
echo "   - sample_import_template.csv for format reference"
echo ""
echo "✅ CSV validation complete!"
