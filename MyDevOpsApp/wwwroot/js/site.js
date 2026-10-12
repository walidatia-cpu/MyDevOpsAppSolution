// MyDevOpsApp Interactive Scripts
document.addEventListener('DOMContentLoaded', () => {
    // Current Active Link Highlight
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link, .sidebar-menu-item a');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && href !== '#' && href !== '/' && currentPath.includes(href.toLowerCase())) {
            link.parentElement.classList.add('active');
        }
    });

    // Clock-in / Attendance Toast Simulation
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
