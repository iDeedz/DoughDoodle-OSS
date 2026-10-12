// DoughDoodle Help System
// Provides contextual help tooltips and help mode functionality

class HelpSystem {
    constructor() {
        this.helpMode = localStorage.getItem('helpMode') === 'true';
        this.init();
    }

    init() {
        this.setupHelpTooltips();
        this.setupHelpButtons();
        this.setupHelpMode();
        this.addHelpIndicators();
    }

    setupHelpTooltips() {
        // Help tooltips for common elements
        const helpData = {
            // Main dashboard elements
            '.monthly-overview-card': {
                title: 'Monthly Overview',
                content: 'This shows your total monthly income and how much you have left to allocate to budget categories (envelopes).'
            },
            '.income-card': {
                title: 'Monthly Income',
                content: 'Set your monthly take-home pay here. This is the total amount you have to distribute across all your budget categories.'
            },
            '.budget-available-card': {
                title: 'Available to Budget',
                content: 'This is money you haven\'t assigned to any envelope yet. Your goal is to get this to zero by allocating all income.'
            },
            '.budget-table': {
                title: 'Envelope Categories',
                content: 'These are your spending categories (envelopes). Each shows how much you\'ve budgeted, spent, and have remaining.'
            },
            '.budget-progress-bar': {
                title: 'Spending Progress',
                content: 'Visual indicator of how much you\'ve spent. Green = good, yellow = warning (80%+), red = overspent.'
            },
            
            // Navigation elements
            '.nav-link[href*="Dashboard"]': {
                title: 'Dashboard',
                content: 'View detailed analytics, charts, and insights about your spending patterns and financial health.'
            },
            '.nav-link[href*="Goals"]': {
                title: 'Financial Goals',
                content: 'Set and track savings goals like emergency funds, vacations, or debt payoff targets.'
            },
            '.nav-link[href*="AIAdvisor"]': {
                title: 'AI Budget Advisor',
                content: 'Get personalized recommendations for debt management and savings strategies powered by AI.'
            },
            '.nav-link[href*="Recurring"]': {
                title: 'Recurring Transactions',
                content: 'Set up automatic entries for regular bills and income to save time on data entry.'
            },
            '.nav-link[href*="ImportExport"]': {
                title: 'Import/Export',
                content: 'Import transactions from your bank (CSV files) or export your budget data for backup.'
            },
            
            // Form elements
            'input[type="number"]': {
                title: 'Amount Entry',
                content: 'Enter amounts without a currency symbol. Use decimals for cents (e.g., 123.45).'
            },
            'select': {
                title: 'Category Selection',
                content: 'Choose which envelope (category) this transaction belongs to.'
            }
        };

        // Apply tooltips based on help data
        Object.keys(helpData).forEach(selector => {
            const elements = document.querySelectorAll(selector);
            elements.forEach(element => {
                if (!element.hasAttribute('data-help-added')) {
                    this.addTooltip(element, helpData[selector]);
                    element.setAttribute('data-help-added', 'true');
                }
            });
        });
    }

    addTooltip(element, helpInfo) {
        // Only add if help mode is enabled or if explicitly requested
        if (!this.helpMode && !element.hasAttribute('data-force-help')) {
            return;
        }

        // Create help icon
        const helpIcon = document.createElement('span');
        helpIcon.className = 'help-icon';
        helpIcon.innerHTML = '<i class="bi bi-question-circle-fill text-primary"></i>';
        helpIcon.style.cssText = `
            margin-left: 5px;
            cursor: help;
            font-size: 0.8em;
            opacity: 0.7;
            transition: opacity 0.2s ease;
        `;

        // Add hover effect
        helpIcon.addEventListener('mouseenter', () => {
            helpIcon.style.opacity = '1';
        });
        helpIcon.addEventListener('mouseleave', () => {
            helpIcon.style.opacity = '0.7';
        });

        // Initialize Bootstrap tooltip
        helpIcon.setAttribute('data-bs-toggle', 'tooltip');
        helpIcon.setAttribute('data-bs-placement', 'top');
        helpIcon.setAttribute('data-bs-html', 'true');
        helpIcon.setAttribute('title', `<strong>${helpInfo.title}</strong><br>${helpInfo.content}`);

        // Insert help icon
        if (element.tagName === 'TH' || element.tagName === 'LABEL') {
            element.appendChild(helpIcon);
        } else {
            // For other elements, try to find a good spot
            const parent = element.parentElement;
            if (parent) {
                parent.insertBefore(helpIcon, element.nextSibling);
            }
        }

        // Initialize the tooltip
        new bootstrap.Tooltip(helpIcon);
    }

    setupHelpButtons() {
        // Add help buttons to key sections
        const helpButtons = [
            {
                selector: '.budget-table-container h1',
                position: 'after',
                content: 'Learn about envelope budgeting',
                link: '/Help/EnvelopeBudgeting'
            },
            {
                selector: '.monthly-overview-card h5',
                position: 'after',
                content: 'How to set up your budget',
                link: '/Help/GettingStarted'
            }
        ];

        helpButtons.forEach(buttonInfo => {
            const targetElement = document.querySelector(buttonInfo.selector);
            if (targetElement && !targetElement.hasAttribute('data-help-button-added')) {
                const helpButton = document.createElement('a');
                helpButton.href = buttonInfo.link;
                helpButton.className = 'btn btn-sm btn-outline-info ms-2 help-hint-button';
                helpButton.innerHTML = '<i class="bi bi-question-circle"></i>';
                helpButton.title = buttonInfo.content;
                helpButton.style.cssText = `
                    font-size: 0.7em;
                    padding: 0.25rem 0.4rem;
                    opacity: ${this.helpMode ? '1' : '0.6'};
                    transition: opacity 0.2s ease;
                `;

                if (buttonInfo.position === 'after') {
                    targetElement.appendChild(helpButton);
                } else {
                    targetElement.insertBefore(helpButton, targetElement.firstChild);
                }

                targetElement.setAttribute('data-help-button-added', 'true');
            }
        });
    }

    setupHelpMode() {
        // Update UI based on help mode state
        if (this.helpMode) {
            document.body.classList.add('help-mode-active');
            
            // Show all help elements
            document.querySelectorAll('.help-icon, .help-hint-button').forEach(el => {
                el.style.display = 'inline-block';
            });
        } else {
            document.body.classList.remove('help-mode-active');
            
            // Hide help elements
            document.querySelectorAll('.help-icon, .help-hint-button').forEach(el => {
                el.style.display = 'none';
            });
        }
    }

    addHelpIndicators() {
        // Add subtle help indicators to key interface elements
        const keyElements = [
            { selector: '.btn[onclick*="setBudget"]', hint: 'Click to set/update budget amount' },
            { selector: '.btn[onclick*="addTransaction"]', hint: 'Add a new expense or income' },
            { selector: '.btn[onclick*="setMonthlyIncome"]', hint: 'Set your monthly take-home pay' },
            { selector: '.nav-tabs .nav-link', hint: 'Filter categories by funding status' }
        ];

        keyElements.forEach(item => {
            const elements = document.querySelectorAll(item.selector);
            elements.forEach(element => {
                if (!element.hasAttribute('data-help-indicator-added') && this.helpMode) {
                    element.setAttribute('title', item.hint);
                    element.classList.add('help-enhanced');
                    element.setAttribute('data-help-indicator-added', 'true');
                }
            });
        });
    }

    toggleHelpMode() {
        this.helpMode = !this.helpMode;
        localStorage.setItem('helpMode', this.helpMode.toString());
        
        // Reinitialize help elements
        this.setupHelpTooltips();
        this.setupHelpButtons();
        this.setupHelpMode();
        this.addHelpIndicators();
        
        // Show notification
        this.showHelpModeNotification();
    }

    showHelpModeNotification() {
        const notification = document.createElement('div');
        notification.className = `alert alert-${this.helpMode ? 'success' : 'info'} help-mode-notification`;
        notification.style.cssText = `
            position: fixed;
            top: 80px;
            right: 20px;
            z-index: 1050;
            max-width: 300px;
            animation: slideInRight 0.3s ease;
        `;
        
        notification.innerHTML = `
            <i class="bi bi-${this.helpMode ? 'lightbulb-fill' : 'lightbulb'}"></i>
            <strong>Help Mode ${this.helpMode ? 'Enabled' : 'Disabled'}</strong><br>
            ${this.helpMode ? 
                'Look for <i class="bi bi-question-circle-fill text-primary"></i> icons for helpful tips!' : 
                'Help hints are now hidden.'}
            <button type="button" class="btn-close" aria-label="Close"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto-dismiss after 4 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, 4000);
        
        // Manual dismiss
        notification.querySelector('.btn-close').addEventListener('click', () => {
            notification.remove();
        });
    }

    // Contextual help for specific pages
    addPageSpecificHelp() {
        const currentPath = window.location.pathname;
        
        if (currentPath === '/' || currentPath.includes('/Home')) {
            this.addBudgetingHelp();
        } else if (currentPath.includes('/Goals')) {
            this.addGoalsHelp();
        } else if (currentPath.includes('/Dashboard')) {
            this.addDashboardHelp();
        }
    }

    addBudgetingHelp() {
        // Add specific help for budgeting page
        const quickTips = document.createElement('div');
        quickTips.className = 'alert alert-info help-quick-tips';
        quickTips.innerHTML = `
            <h6><i class="bi bi-lightbulb"></i> Quick Tips:</h6>
            <ul class="mb-0">
                <li><strong>Green progress bar:</strong> You're doing great, stay within budget</li>
                <li><strong>Yellow progress bar:</strong> Warning - you've spent 80% of this envelope</li>
                <li><strong>Red progress bar:</strong> Overspent - consider transferring money from another envelope</li>
                <li><strong>Goal:</strong> Get "Available to Budget" to zero by allocating all your income</li>
            </ul>
        `;
        
        const tableContainer = document.querySelector('.budget-table-container');
        if (tableContainer && this.helpMode) {
            tableContainer.insertBefore(quickTips, tableContainer.querySelector('.budget-table'));
        }
    }

    addGoalsHelp() {
        // Add goals-specific help
        console.log('Adding goals help');
    }

    addDashboardHelp() {
        // Add dashboard-specific help
        console.log('Adding dashboard help');
    }
}

// Global function for toggling help mode (used by buttons)
function toggleHelpMode() {
    if (window.helpSystem) {
        window.helpSystem.toggleHelpMode();
    }
}

// Initialize help system when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.helpSystem = new HelpSystem();
    
    // Add page-specific help
    window.helpSystem.addPageSpecificHelp();
    
    // Reinitialize help on AJAX content updates
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length > 0) {
                // Small delay to ensure new content is fully rendered
                setTimeout(() => {
                    window.helpSystem.setupHelpTooltips();
                    window.helpSystem.setupHelpButtons();
                }, 100);
            }
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

// CSS for help mode
const helpCSS = `
<style>
@keyframes slideInRight {
    from { transform: translateX(100%); opacity: 0; }
    to { transform: translateX(0); opacity: 1; }
}

.help-mode-active .help-enhanced {
    position: relative;
}

.help-mode-active .help-enhanced::after {
    content: '?';
    position: absolute;
    top: -5px;
    right: -5px;
    background: #0d6efd;
    color: white;
    border-radius: 50%;
    width: 16px;
    height: 16px;
    font-size: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
}

.help-quick-tips {
    border-left: 4px solid #0dcaf0;
    background: linear-gradient(90deg, rgba(13, 202, 240, 0.1) 0%, rgba(13, 202, 240, 0.05) 100%);
}

.help-mode-notification {
    box-shadow: 0 4px 20px rgba(0,0,0,0.15);
    border: none;
}

.tooltip-inner {
    max-width: 250px;
    text-align: left;
}
</style>`;

// Inject CSS
document.head.insertAdjacentHTML('beforeend', helpCSS);