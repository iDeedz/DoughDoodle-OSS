// Theme Management System
// Part of "Additional UI Themes" enhancement

class ThemeManager {
    constructor() {
        this.themes = ['default', 'ocean', 'forest', 'sunset', 'purple', 'dark', 'high-contrast'];
        this.currentTheme = this.getStoredTheme() || 'default';
        this.init();
    }

    init() {
        // Remove preload class to enable transitions
        window.addEventListener('load', () => {
            document.body.classList.remove('preload');
        });

        // Apply stored theme
        this.applyTheme(this.currentTheme);

        // Listen for theme changes
        this.setupEventListeners();
    }

    getStoredTheme() {
        const stored = localStorage.getItem('doughdoodle-theme');
        // Unknown/retired theme keys fall back to default
        if (stored && !this.themes.includes(stored)) {
            localStorage.setItem('doughdoodle-theme', 'default');
            return 'default';
        }
        return stored;
    }

    setStoredTheme(theme) {
        localStorage.setItem('doughdoodle-theme', theme);
    }

    applyTheme(theme) {
        // Validate theme
        if (!this.themes.includes(theme)) {
            theme = 'default';
        }

        // Remove all theme attributes
        document.documentElement.removeAttribute('data-theme');
        
        // Apply new theme if not default
        if (theme !== 'default') {
            document.documentElement.setAttribute('data-theme', theme);
        }

        this.currentTheme = theme;
        this.setStoredTheme(theme);

        // Update UI to reflect current theme
        this.updateThemeUI();

        // Dispatch custom event for other components
        window.dispatchEvent(new CustomEvent('themeChanged', { detail: { theme } }));
    }

    updateThemeUI() {
        // Update theme selector if it exists
        const themeSelectors = document.querySelectorAll('[data-theme-selector]');
        themeSelectors.forEach(selector => {
            selector.value = this.currentTheme;
        });

        // Update theme buttons if they exist
        const themeButtons = document.querySelectorAll('[data-theme-button]');
        themeButtons.forEach(button => {
            const buttonTheme = button.getAttribute('data-theme-button');
            if (buttonTheme === this.currentTheme) {
                button.classList.add('active');
            } else {
                button.classList.remove('active');
            }
        });
    }

    setupEventListeners() {
        // Listen for theme selector changes
        document.addEventListener('change', (e) => {
            if (e.target.matches('[data-theme-selector]')) {
                this.applyTheme(e.target.value);
            }
        });

        // Listen for theme button clicks
        document.addEventListener('click', (e) => {
            const themeButton = e.target.closest('[data-theme-button]');
            if (themeButton) {
                const theme = themeButton.getAttribute('data-theme-button');
                this.applyTheme(theme);
            }
        });
    }

    getNextTheme() {
        const currentIndex = this.themes.indexOf(this.currentTheme);
        const nextIndex = (currentIndex + 1) % this.themes.length;
        return this.themes[nextIndex];
    }

    toggleTheme() {
        const nextTheme = this.getNextTheme();
        this.applyTheme(nextTheme);
    }
}

// Initialize theme manager
const themeManager = new ThemeManager();

// Expose globally for console access
window.themeManager = themeManager;

// Quick theme toggle function
window.toggleTheme = () => themeManager.toggleTheme();

// Add preload class to prevent transitions on page load
document.documentElement.classList.add('preload');
