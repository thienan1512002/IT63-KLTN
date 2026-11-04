$(function () {
    const root = document.getElementById('requestApprovalPreview');
    if (!root) return;
    const type = document.getElementById('requestType');
    const state = document.getElementById('requestApprovalState');
    const list = document.getElementById('requestApprovalSteps');
    const note = document.getElementById('requestApprovalNote');
    const version = document.getElementById('requestApprovalVersion');
    const retry = document.getElementById('requestApprovalRetry');
    let sequence = 0, controller;
    function element(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text) node.textContent = text;
        return node;
    }
    function render(data) {
        const steps = data.steps || [];
        version.textContent = [steps.length ? steps.length + ' tầng' : '', data.revision ? 'Phiên bản ' + data.revision : '', data.isSnapshot ? 'Đã gán' : data.isPinned ? 'Đã gắn với bản nháp' : 'Dự kiến'].filter(Boolean).join(' · ');
        version.hidden = false;
        state.textContent = steps.length ? 'Yêu cầu sẽ đi qua ' + steps.length + ' tầng phê duyệt theo thứ tự dưới đây.' : data.message || 'Chưa có luồng phê duyệt cho loại yêu cầu này.';
        for (const step of steps) {
            const card = element('li', 'wf-route-step');
            if (step.warning) card.classList.add('wf-route-step-warning');
            const heading = element('div', 'wf-route-step-heading');
            heading.append(element('span', 'wf-route-number', String(step.stepOrder)), element('h3', '', step.name || 'Tầng phê duyệt'));
            card.append(heading, element('p', 'wf-route-assignment', step.assignment));
            const people = element('ul', 'wf-route-people');
            for (const person of step.approvers || []) {
                const row = element('li', 'wf-route-person');
                const name = person.fullName || person.userName || 'Người duyệt';
                const initials = name.trim().split(/\s+/).slice(-2).map(x => Array.from(x)[0]).join('').toLocaleUpperCase('vi-VN');
                const avatar = element('span', 'wf-route-avatar', initials);
                avatar.setAttribute('aria-hidden', 'true');
                const detail = element('div');
                detail.append(element('strong', '', name), element('small', '', [person.position, person.sectionName].filter(Boolean).join(' · ') || person.userName));
                row.append(avatar, detail); people.append(row);
            }
            card.append(people);
            if ((step.approvers || []).length > 1) card.append(element('p', 'wf-route-group-note', 'Một người trong danh sách duyệt để hoàn tất tầng.'));
            if (step.warning) card.append(element('p', 'wf-route-warning', step.warning));
            list.append(card);
        }
        list.hidden = !steps.length;
        note.textContent = steps.length ? data.message || '' : '';
        note.hidden = !note.textContent;
    }
    async function load() {
        const current = ++sequence;
        controller?.abort();
        controller = new AbortController();
        list.replaceChildren(); list.hidden = true; note.hidden = true; version.hidden = true; retry.hidden = true;
        if (!type.value) {
            root.setAttribute('aria-busy', 'false');
            state.textContent = 'Chọn loại yêu cầu để xem người duyệt dự kiến.';
            return;
        }
        state.textContent = 'Đang xác định người duyệt…';
        root.setAttribute('aria-busy', 'true');
        const query = new URLSearchParams({ requestTypeId: type.value });
        const id = document.getElementById('requestId').value;
        if (id) query.set('requestId', id);
        let failure = 'Không thể tải luồng phê duyệt. Vui lòng thử lại.';
        try {
            const response = await fetch('/api/requests/approval-preview?' + query, { signal: controller.signal, cache: 'no-store' });
            if (!response.ok) {
                if (response.status === 401) failure = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
                throw new Error(failure);
            }
            const data = await response.json();
            if (current === sequence) render(data);
        } catch (error) {
            if (current !== sequence || error.name === 'AbortError') return;
            state.textContent = failure;
            retry.hidden = false;
        } finally {
            if (current === sequence) root.setAttribute('aria-busy', 'false');
        }
    }
    $(type).on('change', load);
    retry.addEventListener('click', load);
    // Refresh against the newly saved/pinned draft if submission fails after saving.
    document.getElementById('requestForm').addEventListener('request-draft-saved', load);
    load();
});
