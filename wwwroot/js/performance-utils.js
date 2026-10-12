// Performance Optimization Utilities
// Part of "Performance Optimizations" - Faster page loads and smoother interactions

/**
 * Lazy load images when they come into viewport
 */
class LazyLoadManager {
    constructor() {
        this.images = document.querySelectorAll('img[data-src]');
        this.init();
    }

    init() {
        if ('IntersectionObserver' in window) {
            this.observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        this.loadImage(entry.target);
                    }
                });
            }, {
                rootMargin: '50px 0px',
                threshold: 0.01
            });

            this.images.forEach(img => this.observer.observe(img));
        } else {
            // Fallback for older browsers
            this.images.forEach(img => this.loadImage(img));
        }
    }

    loadImage(img) {
        const src = img.getAttribute('data-src');
        if (src) {
            img.src = src;
            img.removeAttribute('data-src');
            img.classList.add('loaded');
        }
        if (this.observer) {
            this.observer.unobserve(img);
        }
    }
}

/**
 * Debounce function to limit function execution rate
 */
function debounce(func, wait = 300) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Throttle function to ensure function doesn't execute more than once per interval
 */
function throttle(func, limit = 300) {
    let inThrottle;
    return function(...args) {
        if (!inThrottle) {
            func.apply(this, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

/**
 * Optimize scroll events with passive listeners
 */
function addOptimizedScrollListener(callback, options = {}) {
    const { useThrottle = true, delay = 100 } = options;
    const handler = useThrottle ? throttle(callback, delay) : debounce(callback, delay);
    
    window.addEventListener('scroll', handler, { passive: true });
    
    return () => window.removeEventListener('scroll', handler);
}

/**
 * Prefetch resources for faster navigation
 */
class ResourcePrefetcher {
    constructor() {
        this.prefetchedUrls = new Set();
    }

    prefetch(url, type = 'fetch') {
        if (this.prefetchedUrls.has(url)) return;

        const link = document.createElement('link');
        link.rel = type === 'fetch' ? 'prefetch' : 'preload';
        if (type === 'preload') {
            link.as = 'fetch';
        }
        link.href = url;
        document.head.appendChild(link);

        this.prefetchedUrls.add(url);
    }

    preconnect(url) {
        if (this.prefetchedUrls.has(url)) return;

        const link = document.createElement('link');
        link.rel = 'preconnect';
        link.href = url;
        document.head.appendChild(link);

        this.prefetchedUrls.add(url);
    }
}

/**
 * Request Idle Callback polyfill
 */
window.requestIdleCallback = window.requestIdleCallback || function (cb) {
    const start = Date.now();
    return setTimeout(function () {
        cb({
            didTimeout: false,
            timeRemaining: function () {
                return Math.max(0, 50 - (Date.now() - start));
            }
        });
    }, 1);
};

window.cancelIdleCallback = window.cancelIdleCallback || function (id) {
    clearTimeout(id);
};

/**
 * Defer non-critical operations until browser is idle
 */
function runWhenIdle(callback, options = {}) {
    const { timeout = 2000 } = options;
    
    if ('requestIdleCallback' in window) {
        requestIdleCallback(callback, { timeout });
    } else {
        setTimeout(callback, 0);
    }
}

/**
 * Initialize critical CSS loading
 */
function loadCriticalCSS() {
    const criticalStyles = document.querySelectorAll('link[data-critical]');
    criticalStyles.forEach(link => {
        link.media = 'all';
    });
}

/**
 * Web font loading optimization
 */
class FontLoader {
    constructor() {
        this.fonts = [];
    }

    async loadFont(fontFamily, options = {}) {
        const { weight = '400', style = 'normal' } = options;
        
        try {
            const font = new FontFace(fontFamily, `local('${fontFamily}')`, {
                weight,
                style
            });
            
            await font.load();
            document.fonts.add(font);
            return true;
        } catch (error) {
            console.warn(`Failed to load font: ${fontFamily}`, error);
            return false;
        }
    }
}

/**
 * Performance monitoring utilities
 */
class PerformanceMonitor {
    constructor() {
        this.metrics = {};
    }

    mark(name) {
        if ('performance' in window && performance.mark) {
            performance.mark(name);
        }
    }

    measure(name, startMark, endMark) {
        if ('performance' in window && performance.measure) {
            try {
                performance.measure(name, startMark, endMark);
                const measure = performance.getEntriesByName(name)[0];
                this.metrics[name] = measure.duration;
                return measure.duration;
            } catch (error) {
                console.warn('Performance measurement failed:', error);
            }
        }
        return null;
    }

    getMetrics() {
        return { ...this.metrics };
    }

    logMetrics() {
        console.table(this.metrics);
    }
}

/**
 * Smart table rendering for large datasets
 */
function optimizeTableRendering() {
    const tables = document.querySelectorAll('table[data-optimize]');
    
    tables.forEach(table => {
        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        const rows = Array.from(tbody.querySelectorAll('tr'));
        const rowHeight = rows[0]?.offsetHeight || 40;
        const visibleRows = Math.ceil(window.innerHeight / rowHeight) + 5;
        
        if (rows.length <= visibleRows * 2) return; // Skip optimization for small tables

        let scrollTimeout;
        const container = table.parentElement;
        
        const renderVisibleRows = throttle(() => {
            const scrollTop = container.scrollTop;
            const startIndex = Math.floor(scrollTop / rowHeight);
            const endIndex = Math.min(startIndex + visibleRows, rows.length);

            rows.forEach((row, index) => {
                if (index >= startIndex && index <= endIndex) {
                    row.style.display = '';
                } else {
                    row.style.display = 'none';
                }
            });
        }, 50);

        container.addEventListener('scroll', renderVisibleRows, { passive: true });
        renderVisibleRows(); // Initial render
    });
}

// Initialize performance optimizations
document.addEventListener('DOMContentLoaded', () => {
    // Initialize lazy loading
    new LazyLoadManager();
    
    // Load critical CSS
    runWhenIdle(() => loadCriticalCSS());
    
    // Initialize resource prefetcher
    window.resourcePrefetcher = new ResourcePrefetcher();
    
    // Initialize performance monitor
    window.perfMonitor = new PerformanceMonitor();
    
    // Optimize table rendering
    runWhenIdle(() => optimizeTableRendering());
    
    // Preconnect to common CDNs
    const cdns = [
        'https://cdn.jsdelivr.net',
        'https://cdnjs.cloudflare.com'
    ];
    
    cdns.forEach(cdn => {
        runWhenIdle(() => window.resourcePrefetcher?.preconnect(cdn));
    });
});

// Export utilities
window.performance_utils = {
    debounce,
    throttle,
    addOptimizedScrollListener,
    runWhenIdle,
    LazyLoadManager,
    ResourcePrefetcher,
    FontLoader,
    PerformanceMonitor
};
