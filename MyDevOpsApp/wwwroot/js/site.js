// MyDevOpsApp Interactive Scripts & Theme Management
const THEME_STORAGE_KEY = 'app-theme-preference';

// Core Theme Helper Functions
function getStoredTheme() {
    return localStorage.getItem(THEME_STORAGE_KEY);
}

function getSystemTheme() {
    return (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) ? 'light' : 'dark';
}

function getActiveTheme() {
    return document.documentElement.getAttribute('data-theme') || getStoredTheme() || 'light';
}

function updateThemeUI(theme) {
    const isLight = theme === 'light';
    const toggleBtns = document.querySelectorAll('.theme-toggle-btn');
    
    toggleBtns.forEach(btn => {
        if (isLight) {
            btn.setAttribute('title', 'الوضع الحالي: فاتح ☀️ (انقر للتبديل إلى الداكن 🌙)');
            btn.setAttribute('aria-label', 'التبديل إلى الوضع الليلي');
        } else {
            btn.setAttribute('title', 'الوضع الحالي: داكن 🌙 (انقر للتبديل إلى الفاتح ☀️)');
            btn.setAttribute('aria-label', 'التبديل إلى الوضع النهاري');
        }
    });
}

function applyTheme(theme, persist = true) {
    document.documentElement.setAttribute('data-theme', theme);
    if (persist) {
        try {
            localStorage.setItem(THEME_STORAGE_KEY, theme);
        } catch (e) {
            console.warn('localStorage is not available for theme persistence:', e);
        }
    }
    updateThemeUI(theme);
}

let lastToggleTimestamp = 0;
function toggleTheme() {
    const now = Date.now();
    if (now - lastToggleTimestamp < 250) {
        return; // Prevent duplicate rapid/double-toggles
    }
    lastToggleTimestamp = now;

    const current = (document.documentElement.getAttribute('data-theme') || 'light').toLowerCase();
    const nextTheme = current === 'dark' ? 'light' : 'dark';
    applyTheme(nextTheme, true);
}

// Expose globally for inline onclick or console debugging
window.toggleTheme = toggleTheme;
window.applyTheme = applyTheme;
window.updateThemeUI = updateThemeUI;

document.addEventListener('DOMContentLoaded', () => {
    // 1. Initialize Theme UI to match active state
    updateThemeUI(getActiveTheme());

    // 3. Listen to system preference changes if user has not stored a manual preference
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', (e) => {
            if (!getStoredTheme()) {
                applyTheme(e.matches ? 'light' : 'dark', false);
            }
        });
    }

    // 4. Current Active Link Highlight
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link, .sidebar-menu-item a');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && href !== '#' && href !== '/' && currentPath.includes(href.toLowerCase())) {
            link.parentElement.classList.add('active');
        }
    });

    // 5. Clock-in / Attendance Toast Simulation
    const clockInBtn = document.getElementById('btn-clock-in');
    if (clockInBtn) {
        clockInBtn.addEventListener('click', (e) => {
            e.preventDefault();
            const timeStr = new Date().toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });
            clockInBtn.innerHTML = `✓ تم تسجيل الحضور (${timeStr})`;
            clockInBtn.style.background = 'var(--success)';
            clockInBtn.style.color = '#fff';
            clockInBtn.disabled = true;
        });
    }
});
