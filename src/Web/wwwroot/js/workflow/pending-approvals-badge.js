(() => {
    const badge = document.getElementById('pendingApprovalsBadge');
    const link = document.getElementById('pendingApprovalsLink');
    if (!badge || !link) return;
    let running = false;
    async function refresh() {
        if (running || document.visibilityState === 'hidden') return;
        running = true;
        try {
            const response = await fetch('/api/requests/pending-count', { cache: 'no-store' });
            if (!response.ok) throw new Error('Cannot load pending approvals');
            const { count } = await response.json();
            if (!Number.isSafeInteger(count) || count < 0) throw new Error('Invalid count');
            badge.textContent = count > 99 ? '99+' : String(count);
            badge.hidden = count === 0;
            link.setAttribute('aria-label', count ? `Chờ tôi phê duyệt, ${count} yêu cầu` : 'Chờ tôi phê duyệt');
        } catch {
            badge.hidden = true;
            link.setAttribute('aria-label', 'Chờ tôi phê duyệt');
        } finally { running = false; }
    }
    window.refreshPendingApprovals = refresh;
    document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'visible') refresh(); });
    window.addEventListener('focus', refresh);
    window.setInterval(refresh, 60000);
    refresh();
})();
