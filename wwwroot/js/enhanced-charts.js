// Enhanced Chart.js Configurations with Smooth Animations
// Part of "Remainder of 2025 - Polish & Small Improvements"

/**
 * Enhanced Chart.js animation configuration
 */
const chartAnimationConfig = {
    duration: 1000,
    easing: 'easeInOutQuart',
    delay: (context) => {
        let delay = 0;
        if (context.type === 'data' && context.mode === 'default') {
            delay = context.dataIndex * 100 + context.datasetIndex * 150;
        }
        return delay;
    }
};

/**
 * Common hover configuration for interactive charts
 */
const chartHoverConfig = {
    mode: 'index',
    intersect: false,
    animationDuration: 300
};

/**
 * Enhanced tooltip configuration
 */
const chartTooltipConfig = {
    enabled: true,
    backgroundColor: 'rgba(0, 0, 0, 0.8)',
    titleColor: '#ffffff',
    bodyColor: '#ffffff',
    borderColor: '#ffffff',
    borderWidth: 1,
    cornerRadius: 6,
    displayColors: true,
    padding: 12,
    titleFont: {
        size: 14,
        weight: 'bold'
    },
    bodyFont: {
        size: 13
    },
    animation: {
        duration: 300,
        easing: 'easeOutQuart'
    }
};

/**
 * Create enhanced line chart with smooth animations
 */
function createEnhancedLineChart(ctx, data, options = {}) {
    const defaultOptions = {
        responsive: true,
        maintainAspectRatio: false,
        animation: chartAnimationConfig,
        interaction: {
            ...chartHoverConfig,
            mode: 'nearest'
        },
        plugins: {
            legend: {
                display: true,
                position: 'top',
                labels: {
                    usePointStyle: true,
                    padding: 15,
                    font: {
                        size: 12,
                        weight: '500'
                    }
                }
            },
            tooltip: chartTooltipConfig
        },
        scales: {
            y: {
                beginAtZero: true,
                grid: {
                    color: 'rgba(0, 0, 0, 0.05)',
                    drawBorder: false
                },
                ticks: {
                    padding: 10,
                    font: {
                        size: 11
                    }
                }
            },
            x: {
                grid: {
                    display: false,
                    drawBorder: false
                },
                ticks: {
                    padding: 10,
                    font: {
                        size: 11
                    }
                }
            }
        },
        elements: {
            line: {
                tension: 0.4,
                borderWidth: 2
            },
            point: {
                radius: 4,
                hitRadius: 10,
                hoverRadius: 6,
                hoverBorderWidth: 2
            }
        }
    };

    const mergedOptions = { ...defaultOptions, ...options };
    return new Chart(ctx, {
        type: 'line',
        data: data,
        options: mergedOptions
    });
}

/**
 * Create enhanced doughnut chart with smooth animations
 */
function createEnhancedDoughnutChart(ctx, data, options = {}) {
    const defaultOptions = {
        responsive: true,
        maintainAspectRatio: false,
        animation: {
            ...chartAnimationConfig,
            animateRotate: true,
            animateScale: true
        },
        interaction: {
            mode: 'point',
            intersect: true
        },
        plugins: {
            legend: {
                display: true,
                position: 'bottom',
                labels: {
                    usePointStyle: true,
                    padding: 15,
                    font: {
                        size: 12,
                        weight: '500'
                    }
                }
            },
            tooltip: {
                ...chartTooltipConfig,
                callbacks: {
                    label: function(context) {
                        const label = context.label || '';
                        const value = context.parsed || 0;
                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                        const percentage = ((value / total) * 100).toFixed(1);
                        return `${label}: ${(options.currencySymbol || '')}${value.toLocaleString()} (${percentage}%)`;
                    }
                }
            }
        },
        cutout: '65%',
        elements: {
            arc: {
                borderWidth: 3,
                borderColor: '#fff',
                hoverBorderColor: '#fff',
                hoverBorderWidth: 4,
                hoverOffset: 8
            }
        }
    };

    const mergedOptions = { ...defaultOptions, ...options };
    return new Chart(ctx, {
        type: 'doughnut',
        data: data,
        options: mergedOptions
    });
}

/**
 * Create enhanced bar chart with smooth animations
 */
function createEnhancedBarChart(ctx, data, options = {}) {
    const defaultOptions = {
        responsive: true,
        maintainAspectRatio: false,
        animation: chartAnimationConfig,
        interaction: chartHoverConfig,
        plugins: {
            legend: {
                display: true,
                position: 'top',
                labels: {
                    usePointStyle: true,
                    padding: 15,
                    font: {
                        size: 12,
                        weight: '500'
                    }
                }
            },
            tooltip: chartTooltipConfig
        },
        scales: {
            y: {
                beginAtZero: true,
                grid: {
                    color: 'rgba(0, 0, 0, 0.05)',
                    drawBorder: false
                },
                ticks: {
                    padding: 10,
                    font: {
                        size: 11
                    }
                }
            },
            x: {
                grid: {
                    display: false,
                    drawBorder: false
                },
                ticks: {
                    padding: 10,
                    font: {
                        size: 11
                    }
                }
            }
        },
        elements: {
            bar: {
                borderRadius: 6,
                borderWidth: 0,
                hoverBorderWidth: 2,
                hoverBorderColor: 'rgba(0, 0, 0, 0.2)'
            }
        }
    };

    const mergedOptions = { ...defaultOptions, ...options };
    return new Chart(ctx, {
        type: 'bar',
        data: data,
        options: mergedOptions
    });
}

/**
 * Add smooth scroll reveal animation to chart containers
 */
function initChartScrollAnimations() {
    const chartContainers = document.querySelectorAll('.card:has(canvas)');
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });

    chartContainers.forEach(container => {
        container.style.opacity = '0';
        container.style.transform = 'translateY(20px)';
        container.style.transition = 'opacity 0.6s ease-out, transform 0.6s ease-out';
        observer.observe(container);
    });
}

// Initialize scroll animations when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initChartScrollAnimations);
} else {
    initChartScrollAnimations();
}
