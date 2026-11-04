$(function () {
    const tabs = [...document.querySelectorAll('[data-detail-tab]')];
    function selectTab(tab) {
        tabs.forEach(item => {
            const active = item === tab;
            item.classList.toggle('is-active', active);
            item.setAttribute('aria-selected', String(active));
            item.tabIndex = active ? 0 : -1;
        });
        document.querySelectorAll('[data-detail-panel]').forEach(panel => { panel.hidden = panel.dataset.detailPanel !== tab.dataset.detailTab; });
    }
    tabs.forEach((tab, index) => {
        tab.addEventListener('click', () => selectTab(tab));
        tab.addEventListener('keydown', event => {
            let next;
            if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
            if (event.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length;
            if (event.key === 'Home') next = 0;
            if (event.key === 'End') next = tabs.length - 1;
            if (next !== undefined) { event.preventDefault(); selectTab(tabs[next]); tabs[next].focus(); }
        });
    });
    const commentField = document.getElementById('approvalComment');
    const error = document.getElementById('requestActionError');
    let busy = false;
    commentField?.addEventListener('input', () => {
        commentField.setCustomValidity('');
        document.getElementById('approvalCommentCount').textContent = commentField.value.length + '/2000';
    });
    $('.request-action').on('click', async function () {
        if (busy) return;
        const action = this.dataset.action;
        const comment = commentField?.value.trim() || null;
        if (this.dataset.comment && !comment) {
            commentField.setCustomValidity('Vui lòng nhập ý kiến khi từ chối hoặc yêu cầu bổ sung.');
            commentField.reportValidity();
            commentField.focus();
            return;
        }
        busy = true;
        error.hidden = true;
        $('.request-action').prop('disabled', true);
        try {
            const title = document.querySelector('.wf-detail-header h2')?.textContent?.trim();
            if (!await window.requestFeedback.confirm(action, title, comment)) return;
            window.requestFeedback.loading();
            const response = await fetch(`/api/requests/${window.requestId}/${action}`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ comment })
            });
            if (!response.ok) {
                let message = 'Không thể xử lý yêu cầu. Vui lòng tải lại trang hoặc thử lại.';
                try { const problem = await response.json(); message = Object.values(problem.errors || {}).flat().join('\n') || problem.message || problem.detail || problem.title || message; } catch { /* Use the readable fallback. */ }
                throw new Error(message);
            }
            window.refreshPendingApprovals?.();
            await window.requestFeedback.actionSuccess(action);
            location.reload();
        } catch (exception) {
            error.textContent = exception.message || 'Không thể kết nối đến máy chủ.';
            error.hidden = false;
            error.scrollIntoView({ block: 'nearest' });
            await window.requestFeedback.error(error.textContent);
        } finally {
            busy = false;
            $('.request-action').prop('disabled', false);
        }
    });
});

