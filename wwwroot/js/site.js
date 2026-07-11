// site.js — WA Blast helpers
document.addEventListener("DOMContentLoaded", function () {
    // Animate stat numbers on results page
    document.querySelectorAll(".stat-num").forEach(el => {
        const target = parseInt(el.textContent, 10);
        if (isNaN(target) || target === 0) return;
        let current = 0;
        const step = Math.max(1, Math.floor(target / 30));
        const timer = setInterval(() => {
            current = Math.min(current + step, target);
            el.textContent = current;
            if (current >= target) clearInterval(timer);
        }, 30);
    });
});
