$(function () {
    const form = document.getElementById('requestForm');
    if (!form) return;
    const panels = [...form.querySelectorAll('[data-step-panel]')];
    const steps = [...form.querySelectorAll('[data-step]')];
    const error = document.getElementById('requestFormError');
    let current = 0;
    let busy = false;
    function updateType() {
        const option = form.elements.RequestTypeId.selectedOptions[0];
        [['RequestedDate', 'date'], ['Amount', 'amount'], ['Reason', 'reason']].forEach(([name, flag]) => {
            const required = option?.dataset[flag] === 'true';
            form.elements[name].dataset.requiredForSend = String(required);
            form.querySelector('[data-required-for="' + name + '"]').hidden = !required;
        });
        const allowFiles = option?.dataset.attachment !== 'false';
        form.elements.files.disabled = !allowFiles;
        document.getElementById('attachmentHint').textContent = allowFiles ? 'Bạn có thể chọn nhiều tệp trong một lần.' : 'Loại yêu cầu này không cho phép đính kèm tài liệu mới.';
    }
    $('#requestType').on('change', updateType);
    updateType();
    function review() {
        const target = document.getElementById('requestReview');
        target.replaceChildren();
        const values = [
            ['Loại yêu cầu', form.elements.RequestTypeId.selectedOptions[0]?.textContent],
            ['Mức độ ưu tiên', form.elements.Priority.selectedOptions[0]?.textContent],
            ['Tiêu đề', form.elements.Title.value],
            ...(window.requestBusiness?.reviewEntries?.() || []),
            ...(!form.elements.RequestedDate.disabled ? [[(document.getElementById('requestDateLabel')?.textContent || 'Ngày yêu cầu'), form.elements.RequestedDate.value ? form.elements.RequestedDate.value.split('-').reverse().join('/') : '']] : []),
            [(document.getElementById('requestContentLabel')?.textContent || 'Nội dung'), form.elements.Content.value],
            ...(!form.elements.Amount.disabled ? [[(document.getElementById('requestAmountLabel')?.textContent || 'Số tiền'), window.requestBusiness?.formatMoney?.(form.elements.Amount.value) || form.elements.Amount.value]] : []), ['Lý do', form.elements.Reason.value],
            ['Tài liệu mới', form.elements.files.disabled ? '' : [...form.elements.files.files].map(file => file.name).join('\n')]
        ];
        values.forEach(([label, value]) => {
            const dt = document.createElement('dt');
            const dd = document.createElement('dd');
            dt.textContent = label;
            dd.textContent = value || '—';
            dd.className = 'wf-multiline';
            target.append(dt, dd);
        });
    }
    function showStep(index, focus = false) {
        current = index;
        panels.forEach((panel, i) => { panel.hidden = i !== index; });
        steps.forEach((step, i) => {
            step.classList.toggle('is-active', i === index);
            step.classList.toggle('is-complete', i < index);
            if (i === index) step.setAttribute('aria-current', 'step');
            else step.removeAttribute('aria-current');
        });
        document.getElementById('requestPrevious').hidden = index === 0;
        document.getElementById('requestNext').hidden = index === 3;
        document.getElementById('requestSend').hidden = index !== 3;
        if (index === 3) review();
        if (focus) steps[index].focus();
    }
    function validate(index, forSend) {
        const fields = [...panels[index].querySelectorAll('input,select,textarea')];
        for (const field of fields) {
            if (field.disabled) continue;
            const required = field.required || (forSend && field.dataset.requiredForSend === 'true');
            field.setCustomValidity(required && !field.value.trim() ? 'Vui lòng điền thông tin này.' : '');
            if (!field.checkValidity()) {
                showStep(index);
                field.reportValidity();
                field.focus();
                return false;
            }
        }
        return true;
    }
    function moveTo(index) {
        if (busy) return;
        if (window.requestBusiness?.prepare && !window.requestBusiness.prepare(index > 1)) return;
        if (index > current) {
            for (let i = 0; i < index; i++) if (!validate(i, true)) return;
        }
        showStep(index, true);
    }
    steps.forEach((step, i) => step.addEventListener('click', () => moveTo(i)));
    $('#requestNext').on('click', () => moveTo(Math.min(3, current + 1)));
    $('#requestPrevious').on('click', () => moveTo(Math.max(0, current - 1)));
    $(form).on('input change', 'input,select,textarea', function () { this.setCustomValidity(''); });
    $('#requestFiles').on('change', function () {
        const list = document.getElementById('selectedFiles');
        list.replaceChildren();
        [...this.files].forEach(file => {
            const li = document.createElement('li');
            li.textContent = file.name + ' · ' + Math.ceil(file.size / 1024).toLocaleString('vi-VN') + ' KB';
            list.append(li);
        });
    });
    async function responseError(response) {
        try {
            const problem = await response.json();
            return Object.values(problem.errors || {}).flat().join('\n') || problem.message || problem.detail || problem.title || 'Không thể lưu yêu cầu. Vui lòng thử lại.';
        } catch { return 'Không thể lưu yêu cầu. Vui lòng thử lại hoặc đăng nhập lại.'; }
    }
    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        if (busy) return;
        const send = event.submitter?.value === 'send';
        if (window.requestBusiness?.prepare && !window.requestBusiness.prepare(send)) { if (window.requestBusiness.isReady) showStep(1); return; }
        for (let i = 0; i < panels.length; i++) if (!validate(i, send)) return;
        busy = true;
        error.hidden = true;
        const controls = [...form.querySelectorAll('button')];
        controls.forEach(button => { button.disabled = true; });
        let saved = false;
        try {
            let id = document.getElementById('requestId').value;
            const editing = Boolean(id);
            if (send && !await window.requestFeedback.confirm('submit', form.elements.Title.value.trim())) return;
            window.requestFeedback.loading(send ? 'Đang gửi yêu cầu…' : 'Đang lưu yêu cầu…');
            const response = await fetch(id ? '/api/requests/' + id : '/api/requests', { method: id ? 'PUT' : 'POST', body: new FormData(form) });
            if (!response.ok) throw new Error(await responseError(response));
            if (!id) {
                id = (await response.json()).id;
                document.getElementById('requestId').value = id;
                // Retrying a failed send must update the saved draft instead of creating another request.
                history.replaceState(null, '', '/Requests/Edit/' + id);
            }
            saved = true;
            form.dispatchEvent(new Event('request-draft-saved'));
            // Files have been saved; clear the input so retrying does not upload duplicates.
            form.elements.files.value = '';
            document.getElementById('selectedFiles').replaceChildren();
            if (send) {
                const submitted = await fetch('/api/requests/' + id + '/submit', { method: 'POST' });
                if (!submitted.ok) throw new Error(await responseError(submitted));
            }
            await window.requestFeedback.success(
                send ? (form.dataset.requestStatus === 'Returned' ? 'Đã gửi lại yêu cầu' : 'Gửi yêu cầu thành công') : (editing ? 'Đã lưu thay đổi' : 'Đã tạo bản nháp'),
                send ? 'Yêu cầu đã được chuyển đến người phê duyệt theo luồng đã gán.' : 'Thông tin đã được lưu. Bạn có thể tiếp tục chỉnh sửa và gửi phê duyệt sau.',
                'Xem yêu cầu');
            location.href = '/Requests/Details/' + id;
        } catch (exception) {
            error.textContent = (saved ? 'Đã lưu thông tin nhưng chưa gửi được phê duyệt. ' : '') + (exception.message || 'Không thể kết nối đến máy chủ.');
            error.hidden = false;
            await window.requestFeedback.error(error.textContent);
        } finally {
            busy = false;
            controls.forEach(button => { button.disabled = false; });
        }
    });
});


