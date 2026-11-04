$(async function () {
    'use strict';
    const el = id => document.getElementById(id);
    const root = el('workflowAdmin');
    if (!root) return;
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const kinds = [
        { id: 4, name: 'Trưởng Bộ phận/Bộ môn người yêu cầu' },
        { id: 0, name: 'Trưởng Phòng/Khoa/Trung tâm người yêu cầu' },
        { id: 1, name: 'Trưởng Phòng/Khoa/Trung tâm được chỉ định' },
        { id: 2, name: 'Người dùng cụ thể' },
        { id: 3, name: 'Người dùng theo vai trò' }
    ];
    const icons = { LEAVE: 'calendar-check', PURCHASE: 'cart-outline', PAYMENT: 'cash-multiple', EQUIPMENT: 'laptop', OVERTIME: 'clock-outline', INTERNAL: 'lightbulb-outline', BUSINESS_TRIP: 'airplane', VEHICLE: 'car' };
    let forms = [], lookups, selectedId, data, draft, original, saving = false, loading = false, dirty = false;
    let undoAction, pendingAction, draggedIndex, loadEpoch = 0, toastTimer;
    const widgets = [];
    async function json(url, options) {
        const response = await fetch(url, options);
        const body = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(body.message || Object.values(body.errors || {}).flat().join(' ') || (response.status === 401 || response.status === 403 ? 'Bạn không có quyền quản trị cấu hình phê duyệt.' : 'Không thể tải hoặc lưu cấu hình. Vui lòng thử lại.'));
        return body;
    }
    function error(message) { el('workflowError').textContent = message || ''; el('workflowError').hidden = !message; }
    function toast(message, undo) {
        clearTimeout(toastTimer); undoAction = undo;
        el('workflowToast').querySelector('span').textContent = message;
        el('workflowUndo').hidden = !undo; el('workflowToast').hidden = false;
        toastTimer = setTimeout(() => { el('workflowToast').hidden = true; undoAction = null; }, undo ? 15000 : 6000);
    }
    function payload() {
        return { ...draft, name: el('workflowName').value.trim(), description: el('workflowDescription').value.trim(),
            isActive: el('workflowActive').checked,
            steps: draft.steps.map((step, i) => ({ ...step, stepOrder: i + 1 })) };
    }
    function changed() {
        dirty = JSON.stringify(payload()) !== original;
        el('workflowSaveStatus').textContent = saving ? 'Đang công bố phiên bản...' : dirty ? 'Có thay đổi chưa lưu' : `Đã đồng bộ · Phiên bản ${draft.revision}`;
        el('workflowReview').disabled = !dirty || saving || loading;
        el('workflowReset').disabled = saving || loading;
        el('workflowAddStep').disabled = saving || draft.steps.length >= 100;
    }
    function setTab(history) {
        el('workflowDesign').hidden = history; el('workflowHistory').hidden = !history;
        ['workflowDesignTab', 'workflowHistoryTab'].forEach((id, i) => {
            el(id).setAttribute('aria-selected', String(history === !!i)); el(id).tabIndex = history === !!i ? 0 : -1;
        });
    }
    function renderForms() {
        const term = el('workflowFormSearch').value.toLocaleLowerCase('vi').trim();
        const rows = forms.filter(f => `${f.name} ${f.code}`.toLocaleLowerCase('vi').includes(term));
        el('workflowFormCount').textContent = forms.length;
        el('workflowFormList').innerHTML = rows.length ? rows.map(f => `<button type="button" class="wf-form-option" data-form-id="${f.id}" aria-pressed="${f.id === selectedId}" ${saving || loading ? 'disabled' : ''}><span class="wf-form-icon"><i class="mdi mdi-${icons[f.templateCode] || 'file-document-outline'}" aria-hidden="true"></i></span><span><strong>${escape(f.name)}</strong><small>${f.stepCount} tầng · v${f.revision}${!f.isActive || !f.workflowActive ? ' · Tạm dừng' : ''}</small></span></button>`).join('') : '<p class="wf-empty">Không tìm thấy loại form phù hợp.</p>';
    }
    function assignee(step) {
        if (step.approverKind === 0) return 'Trưởng Phòng/Khoa/Trung tâm người yêu cầu';
        if (step.approverKind === 4) return 'Người duyệt chính của Bộ phận/Bộ môn người yêu cầu';
        if (step.approverKind === 1) return lookups.departments.find(d => d.id === step.approverDepartmentId)?.name || 'Chưa chọn đơn vị';
        if (step.approverKind === 2) {
            const user = lookups.users.find(u => u.userName === step.approverUserName);
            return user ? `${user.fullName} (${user.userName})` : step.approverUserName || 'Chưa chọn người duyệt';
        }
        if (step.approverKind === 3) return lookups.roles.find(r => r.name === step.approverRoleName)?.displayName || step.approverRoleName || 'Chưa chọn vai trò';
        return 'Quy tắc cũ · cần chọn lại khi chỉnh sửa';
    }
    function previewNode(title, subtitle, number, terminal) {
        return `<div class="wf-preview-node ${terminal ? 'wf-terminal' : ''}"><span class="wf-preview-dot">${number}</span><div><strong>${escape(title)}</strong><small>${escape(subtitle)}</small></div></div>`;
    }
    function renderPreview() {
        el('workflowPreview').innerHTML = previewNode('Người yêu cầu', 'Tạo và gửi form', '<i class="mdi mdi-account-outline"></i>', true) +
            draft.steps.map((s, i) => previewNode(s.name || `Tầng ${i + 1}`, assignee(s), i + 1, false)).join('') +
            previewNode('Hoàn thành', 'Sau khi tất cả các tầng đồng ý', '<i class="mdi mdi-check"></i>', true);
        el('workflowStepCount').textContent = `${draft.steps.length} tầng`;
    }
    function selectBox(container, options) {
        const widget = $(container).dxSelectBox({ searchEnabled: true, searchMode: 'contains', placeholder: 'Chọn...',
            noDataText: 'Không có dữ liệu phù hợp', showClearButton: false, width: '100%',
            dropDownOptions: { maxHeight: 320 }, ...options }).dxSelectBox('instance');
        widgets.push(widget); return widget;
    }
    function focusStep(index) {
        el('workflowSteps').querySelectorAll('.wf-step-name')[index]?.focus();
    }
    function move(from, to) {
        if (saving || to < 0 || to >= draft.steps.length || from === to) return;
        const [step] = draft.steps.splice(from, 1); draft.steps.splice(to, 0, step);
        renderSteps(); changed(); focusStep(to); toast(`Đã chuyển tầng đến vị trí ${to + 1}.`);
    }
    function renderSteps() {
        while (widgets.length) widgets.pop().dispose();
        const container = el('workflowSteps'); container.replaceChildren();
        if (!draft.steps.length) container.innerHTML = '<div class="wf-empty">Luồng chưa có tầng phê duyệt.<br />Thêm ít nhất một tầng trước khi công bố.</div>';
        draft.steps.forEach((step, index) => {
            const card = document.createElement('article'); card.className = 'wf-step'; card.dataset.index = index;
            const titleId = `wf-step-${index}-name`, kindId = `wf-step-${index}-kind`, assigneeId = `wf-step-${index}-assignee`;
            card.innerHTML = `<div class="wf-step-head"><button type="button" class="wf-grip" draggable="true" aria-label="Kéo để di chuyển tầng ${index + 1}" title="Kéo để đổi thứ tự"><i class="mdi mdi-drag-vertical" aria-hidden="true"></i></button><span class="wf-step-number">${index + 1}</span><strong>Tầng phê duyệt ${index + 1}</strong><button type="button" class="wf-icon-btn wf-up" aria-label="Chuyển tầng ${index + 1} lên trên" title="Chuyển lên" ${index === 0 ? 'disabled' : ''}>↑</button><button type="button" class="wf-icon-btn wf-down" aria-label="Chuyển tầng ${index + 1} xuống dưới" title="Chuyển xuống" ${index + 1 === draft.steps.length ? 'disabled' : ''}>↓</button><button type="button" class="wf-icon-btn wf-delete" aria-label="Xóa tầng ${index + 1}" title="Xóa tầng"><i class="mdi mdi-trash-can-outline" aria-hidden="true"></i></button></div><div class="wf-step-content"><div class="wf-field"><label for="${titleId}">Tên tầng phê duyệt *</label><input class="wf-step-name" id="${titleId}" type="text" maxlength="200" required placeholder="Ví dụ: Trưởng bộ phận xác nhận" value="${escape(step.name)}" /></div><div class="wf-field"><label id="${kindId}-label">Cách xác định người duyệt *</label><div class="wf-kind-select"></div></div><div class="wf-field wf-step-assignee" hidden><label id="${assigneeId}-label"></label><div class="wf-assignee-select"></div></div><div class="wf-step-hint"></div></div>`;
            container.append(card);
            card.querySelector('input').addEventListener('input', e => { step.name = e.target.value; renderPreview(); changed(); });
            card.querySelector('.wf-up').onclick = () => move(index, index - 1);
            card.querySelector('.wf-down').onclick = () => move(index, index + 1);
            card.querySelector('.wf-delete').onclick = () => {
                const formId = selectedId;
                draft.steps.splice(index, 1); renderSteps(); changed();
                toast(`Đã xóa tầng “${step.name || index + 1}”.`, () => {
                    if (selectedId !== formId || saving) return;
                    draft.steps.splice(Math.min(index, draft.steps.length), 0, step); renderSteps(); changed(); focusStep(index);
                });
            };
            const grip = card.querySelector('.wf-grip');
            grip.addEventListener('dragstart', e => { draggedIndex = index; e.dataTransfer.effectAllowed = 'move'; e.dataTransfer.setData('text/plain', String(index)); card.classList.add('dragging'); });
            grip.addEventListener('dragend', () => { draggedIndex = null; document.querySelectorAll('.wf-step').forEach(c => c.classList.remove('dragging', 'drop-target')); });
            card.addEventListener('dragover', e => { if (draggedIndex != null) { e.preventDefault(); e.dataTransfer.dropEffect = 'move'; card.classList.add('drop-target'); } });
            card.addEventListener('dragleave', e => { if (!card.contains(e.relatedTarget)) card.classList.remove('drop-target'); });
            card.addEventListener('drop', e => { e.preventDefault(); card.classList.remove('drop-target'); if (draggedIndex != null) move(draggedIndex, index); draggedIndex = null; });
            selectBox(card.querySelector('.wf-kind-select'), { dataSource: kinds, valueExpr: 'id', displayExpr: 'name', value: step.approverKind,
                inputAttr: { 'aria-labelledby': kindId + '-label' },
                onValueChanged: e => { if (e.value === step.approverKind) return; step.approverKind = e.value;
                    step.approverUserName = null; step.approverDepartmentId = null; step.approverRoleName = null;
                    renderSteps(); changed(); focusStep(index); }
            });
            const target = card.querySelector('.wf-step-assignee'), hint = card.querySelector('.wf-step-hint');
            const hints = [
                'Lấy Người duyệt chính của Phòng/Khoa/Trung tâm người tạo yêu cầu, không lấy người duyệt cấp Bộ phận/Bộ môn.',
                'Lấy Người duyệt chính của Phòng/Khoa/Trung tâm được chọn trong danh mục.',
                'Chỉ người dùng được chọn tại tầng này được quyền phê duyệt.',
                'Các user đang hoạt động thuộc vai trò này sẽ được gán khi gửi. Một người trong nhóm phê duyệt để hoàn tất tầng.',
                'Lấy Người duyệt chính trong danh mục Bộ phận/Bộ môn của người tạo tại thời điểm gửi. Không tự thay bằng trưởng khoa hay người duyệt phụ.'
            ];
            hint.innerHTML = `<i class="mdi mdi-information-outline" aria-hidden="true"></i><span>${escape(hints[step.approverKind] || 'Cấu hình cũ có cách gán chưa hỗ trợ trong trình thiết kế. Hãy chọn lại cách xác định người duyệt trước khi lưu.')}</span>`;
            let options, field;
            if (step.approverKind === 1) {
                field = 'approverDepartmentId'; target.querySelector('label').textContent = 'Đơn vị phê duyệt *';
                options = { dataSource: lookups.departments, valueExpr: 'id', displayExpr: 'name', searchExpr: ['name'], placeholder: 'Tìm và chọn đơn vị...' };
            } else if (step.approverKind === 2) {
                field = 'approverUserName'; target.querySelector('label').textContent = 'Người phê duyệt *';
                options = { dataSource: lookups.users, valueExpr: 'userName', displayExpr: u => u ? `${u.fullName} · ${u.userName}${u.department ? ' · ' + u.department : ''}` : '', searchExpr: ['fullName', 'userName', 'department'], placeholder: 'Tìm họ tên, tài khoản, đơn vị...' };
            } else if (step.approverKind === 3) {
                field = 'approverRoleName'; target.querySelector('label').textContent = 'Vai trò phê duyệt *';
                options = { dataSource: lookups.roles, valueExpr: 'name', displayExpr: r => r ? (r.displayName || r.name) : '', searchExpr: ['name', 'displayName'], placeholder: 'Tìm và chọn vai trò...' };
            }
            if (options) { target.hidden = false;
                selectBox(target.querySelector('.wf-assignee-select'), { ...options, value: step[field], inputAttr: { 'aria-labelledby': assigneeId + '-label' }, onValueChanged: e => { step[field] = e.value; renderPreview(); changed(); } });
            }
        });
        renderPreview();
    }
    function renderHistory() {
        const formId = selectedId;
        el('workflowHistoryList').innerHTML = data.versions.map((v, i) => {
            const date = new Date(v.publishedAt.endsWith('Z') ? v.publishedAt : v.publishedAt + 'Z');
            return `<article class="wf-history-card"><button type="button" class="wf-history-toggle" data-revision="${v.revision}" aria-expanded="false" aria-controls="wf-history-${v.revision}"><span><strong>Phiên bản ${v.revision} · ${escape(v.name)}</strong><small>${escape(date.toLocaleString('vi-VN'))} · ${escape(v.publishedBy || 'Cấu hình ban đầu')} · ${v.stepCount} tầng</small></span>${i === 0 ? '<span class="wf-badge wf-badge-green">Hiện tại</span>' : ''}<i class="mdi mdi-chevron-down" aria-hidden="true"></i></button><div class="wf-history-detail" id="wf-history-${v.revision}" hidden></div></article>`;
        }).join('') || '<div class="wf-empty">Chưa có lịch sử phiên bản.</div>';
        el('workflowHistoryList').querySelectorAll('button').forEach(button => button.addEventListener('click', async () => {
            const detail = el('wf-history-' + button.dataset.revision), open = button.getAttribute('aria-expanded') !== 'true';
            button.setAttribute('aria-expanded', String(open)); detail.hidden = !open;
            if (!open || detail.dataset.loaded) return;
            detail.textContent = 'Đang tải phiên bản...';
            try {
                const version = await json(`/api/admin/workflows/forms/${formId}/versions/${button.dataset.revision}`);
                if (formId !== selectedId) return;
                detail.innerHTML = `<ol>${version.steps.map(s => `<li><strong>${escape(s.name)}</strong><small>${escape(assignee(s))}</small></li>`).join('')}</ol>`;
                detail.dataset.loaded = 'true';
            } catch (e) { detail.textContent = e.message; }
        }));
    }
    async function loadForm(id) {
        const epoch = ++loadEpoch;
        loading = true; el('workflowEditor').inert = true; root.setAttribute('aria-busy', 'true'); error(''); renderForms();
        try {
            const next = await json('/api/admin/workflows/forms/' + id);
            if (epoch !== loadEpoch) return;
            selectedId = id; data = next; draft = structuredClone(data.workflow); undoAction = null; el('workflowToast').hidden = true;
            el('workflowFormName').textContent = data.form.name; el('workflowFormCode').textContent = `${data.form.code} · Cấu hình áp dụng riêng cho loại form này`;
            el('workflowRevision').textContent = `Phiên bản ${draft.revision}`;
            el('workflowActiveBadge').textContent = draft.isActive && data.form.isActive ? 'Đang áp dụng' : 'Tạm dừng yêu cầu mới';
            el('workflowActiveBadge').className = 'wf-badge ' + (draft.isActive && data.form.isActive ? 'wf-badge-green' : 'wf-badge-amber');
            el('workflowName').value = draft.name; el('workflowDescription').value = draft.description || ''; el('workflowActive').checked = draft.isActive;
            el('workflowVersionCount').textContent = `(${data.versions.length})`;
            el('workflowProtectionNote').textContent = `${data.existingRequests} yêu cầu đang mở được giữ nguyên luồng, bao gồm bản nháp. ${data.sharedFormCount > 1 ? 'Luồng hiện được dùng chung; lần lưu này sẽ tạo cấu hình riêng, các form khác giữ nguyên.' : 'Mỗi lần lưu tạo phiên bản mới chỉ áp dụng cho yêu cầu tạo sau đó.'}`;
            renderSteps(); renderHistory(); setTab(false); original = JSON.stringify(payload());
            el('workflowLoading').hidden = true; el('workflowEditor').hidden = false;
        } catch (e) { error(e.message); el('workflowLoading').hidden = true; }
        finally { if (epoch === loadEpoch) { loading = false; el('workflowEditor').inert = false; root.setAttribute('aria-busy', 'false'); renderForms(); if (draft) changed(); } }
    }
    function afterDiscard(action) {
        if (saving || loading) return;
        if (!dirty) return action();
        pendingAction = action; el('workflowDiscardDialog').showModal();
    }
    function validate() {
        error('');
        if (!el('workflowName').reportValidity()) return false;
        if (!draft.steps.length) { error('Hãy thêm ít nhất một tầng phê duyệt.'); el('workflowAddStep').focus(); return false; }
        for (let i = 0; i < draft.steps.length; i++) {
            const s = draft.steps[i]; let message;
            if (!s.name.trim()) message = 'Nhập tên tầng phê duyệt.';
            else if (!kinds.some(k => k.id === s.approverKind)) message = 'Chọn cách xác định người duyệt.';
            else if (s.approverKind === 1 && !lookups.departments.some(d => d.id === s.approverDepartmentId)) message = 'Chọn đơn vị phê duyệt.';
            else if (s.approverKind === 2 && !lookups.users.some(u => u.userName === s.approverUserName)) message = 'Chọn một người dùng đang hoạt động trong danh mục.';
            else if (s.approverKind === 3 && !lookups.roles.some(r => r.name === s.approverRoleName)) message = 'Chọn vai trò phê duyệt.';
            if (message) { error(`Tầng ${i + 1}: ${message}`); focusStep(i); return false; }
        }
        return true;
    }
    el('workflowFormList').addEventListener('click', e => {
        const button = e.target.closest('[data-form-id]');
        if (button && Number(button.dataset.formId) !== selectedId) afterDiscard(() => loadForm(Number(button.dataset.formId)));
    });
    el('workflowFormSearch').addEventListener('input', renderForms);
    el('workflowAddStep').onclick = () => {
        if (saving || draft.steps.length >= 100) return;
        draft.steps.push({ stepOrder: draft.steps.length + 1, name: '', approverKind: 4, approverDepartmentId: null, approverUserName: null, approverRoleName: null });
        renderSteps(); changed(); focusStep(draft.steps.length - 1);
    };
    ['workflowName', 'workflowDescription', 'workflowActive'].forEach(id => el(id).addEventListener('input', changed));
    el('workflowReset').onclick = () => afterDiscard(() => loadForm(selectedId));
    el('workflowDesignTab').onclick = () => setTab(false); el('workflowHistoryTab').onclick = () => setTab(true);
    document.querySelectorAll('.wf-tabs [role=tab]').forEach(tab => tab.addEventListener('keydown', e => {
        if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key)) {
            e.preventDefault(); const history = e.key === 'End' || (e.key !== 'Home' && el('workflowDesignTab').getAttribute('aria-selected') === 'true');
            setTab(history); el(history ? 'workflowHistoryTab' : 'workflowDesignTab').focus();
        }
    }));
    el('workflowReview').onclick = () => {
        if (saving || !dirty) return; setTab(false); if (!validate()) return;
        el('workflowPublishSummary').textContent = `${data.form.name} · Phiên bản ${draft.revision + 1} · ${draft.steps.length} tầng${el('workflowActive').checked ? '' : ' · Tạm dừng tạo yêu cầu mới'}`;
        el('workflowPublishScope').textContent = `${data.existingRequests} yêu cầu đang mở tiếp tục dùng luồng cũ. ${data.sharedFormCount > 1 ? 'Các loại form khác đang dùng chung luồng sẽ giữ nguyên cấu hình.' : 'Không thay đổi người duyệt hoặc tiến độ của các yêu cầu cũ.'}`;
        el('workflowReviewList').innerHTML = draft.steps.map((s, i) => `<li><span class="wf-step-number">${i + 1}</span><div><strong>${escape(s.name)}</strong><small>${escape(assignee(s))}</small></div></li>`).join('');
        el('workflowPublishDialog').showModal();
    };
    el('workflowPublishCancel').onclick = () => el('workflowPublishDialog').close();
    el('workflowPublishDialog').addEventListener('cancel', e => { if (saving) e.preventDefault(); });
    el('workflowPublishConfirm').onclick = async () => {
        if (saving || !validate()) return;
        saving = true; changed(); el('workflowPublishConfirm').disabled = true; el('workflowPublishCancel').disabled = true;
        try {
            const saved = await json('/api/admin/workflows/forms/' + selectedId, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload()) });
            dirty = false; el('workflowPublishDialog').close();
            // Do not let a failed refresh make a successful publication look like a failed save.
            original = JSON.stringify(payload());
            forms = await json('/api/admin/workflows/forms').catch(() => forms.map(f => f.id === selectedId ? { ...f, revision: saved.revision, stepCount: draft.steps.length, workflowActive: payload().isActive } : f));
            await loadForm(selectedId); toast(`Đã công bố phiên bản ${saved.revision}. Yêu cầu cũ giữ nguyên luồng.`);
        } catch (e) { el('workflowPublishDialog').close(); error(e.message); }
        finally { saving = false; el('workflowPublishConfirm').disabled = false; el('workflowPublishCancel').disabled = false; renderForms(); changed(); }
    };
    el('workflowDiscardCancel').onclick = () => { pendingAction = null; el('workflowDiscardDialog').close(); };
    el('workflowDiscardConfirm').onclick = () => { const action = pendingAction; pendingAction = null; el('workflowDiscardDialog').close(); action?.(); };
    el('workflowDiscardDialog').addEventListener('cancel', () => { pendingAction = null; });
    el('workflowUndo').onclick = () => { const action = undoAction; undoAction = null; el('workflowToast').hidden = true; action?.(); };
    el('workflowToastClose').onclick = () => { el('workflowToast').hidden = true; undoAction = null; };
    window.addEventListener('beforeunload', e => { if (dirty) { e.preventDefault(); e.returnValue = ''; } });
    try {
        [forms, lookups] = await Promise.all([json('/api/admin/workflows/forms'), json('/api/admin/workflows/lookups')]);
        renderForms();
        const requested = Number(new URLSearchParams(location.search).get('form'));
        if (forms.length) await loadForm(forms.some(f => f.id === requested) ? requested : forms[0].id);
        else { el('workflowLoading').textContent = 'Chưa có loại form. Vào Quản lý loại yêu cầu để tạo form trước.'; root.setAttribute('aria-busy', 'false'); }
    } catch (e) { error(e.message); el('workflowLoading').hidden = true; root.setAttribute('aria-busy', 'false'); }
});
