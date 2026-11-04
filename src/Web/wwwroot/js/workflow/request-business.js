window.requestBusiness = { isReady: false, definition: null };
$(async function () {
    const form = document.getElementById('requestForm');
    if (!form || !window.requestBusinessConfig) return;
    const state = window.requestBusiness;
    const config = window.requestBusinessConfig;
    const container = document.getElementById('businessFields');
    const lookupError = document.getElementById('businessLookupError');
    const cache = new Map();
    let lookups;
    let selectedCode;
    let leaveQuoteTimer, leaveQuoteEpoch = 0;
    const key = name => name.charAt(0).toLowerCase() + name.slice(1);
    function formatMoney(value) {
        const parts = String(value ?? '').split('.');
        if (!parts[0]) return '';
        const integer = parts[0].replace(/^0+(?=\d)/, '').replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        return integer + (parts.length > 1 ? ',' + parts[1] : '');
    }
    state.formatMoney = formatMoney;
    const el = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    };
    function fieldControl(name) { return form.elements.namedItem('BusinessData.' + name); }
    function optionsFor(lookup, parent) {
        if (lookup === 'employees') return lookups.employees.map(x => ({ ...x, name: x.name + (x.empId ? ' (' + x.empId + ')' : '') }));
        if (lookup === 'drivers') return (lookups.drivers || []).map(x => ({ ...x, name: x.name + (x.empId ? ' (' + x.empId + ')' : '') }));
        if (lookup === 'departments') return lookups.departments.filter(x => parent === undefined || String(x.siteId) === String(parent));
        if (lookup === 'sites') return lookups.sites;
        if (lookup === 'sessions') return [{ id: 'AM', name: 'Buổi sáng' }, { id: 'PM', name: 'Buổi chiều' }];
        return lookups.masterItems.filter(x => x.category === lookup && (parent === undefined || String(x.parentId) === String(parent)));
    }
    function populate(select, lookup, value, parent) {
        select.replaceChildren();
        if (lookup === 'City' || lookup === 'Vehicle' || (lookup === 'departments' && parent !== undefined)) select.disabled = !parent;
        if (!select.multiple) select.append(new Option('Chọn ' + (select.dataset.label || 'dữ liệu').toLowerCase(), ''));
        optionsFor(lookup, parent).forEach(item => {
            if (item.isActive === false && String(item.id) !== String(value)) return;
            const option = new Option(item.name + (item.isActive === false ? ' [Ngừng sử dụng]' : ''), item.id);
            option.disabled = item.isActive === false;
            if (Array.isArray(value)) option.selected = value.includes(String(item.id));
            else option.selected = String(value ?? '') === String(item.id);
            select.append(option);
        });
        if (value && !Array.isArray(value) && ![...select.options].some(x => x.value === String(value))) {
            const option = new Option('Dữ liệu đã ngừng sử dụng — vui lòng chọn lại', value, true, true);
            option.disabled = true;
            select.append(option);
        }
    }
    function capture() {
        if (!state.definition) return {};
        const data = {};
        state.definition.fields.forEach(field => { data[key(field.name)] = fieldControl(field.name)?.value || ''; });
        data.participantIds = [...(document.getElementById('businessParticipants')?.selectedOptions || [])].map(x => x.value);
        data.items = [...container.querySelectorAll('[data-business-row]')].map(row => {
            const item = {};
            row.querySelectorAll('[data-item-field]').forEach(control => { item[control.dataset.itemField] = control.value; });
            return item;
        });
        data.commonAmount = form.elements.Amount.value;
        return data;
    }
    function reindexRows() {
        container.querySelectorAll('[data-business-row]').forEach((row, index) => {
            row.querySelector('[data-line-number]').textContent = index + 1;
            row.querySelectorAll('[data-item-field]').forEach(control => {
                const name = control.dataset.itemField;
                control.name = 'BusinessData.Items[' + index + '].' + name.charAt(0).toUpperCase() + name.slice(1);
            });
        });
    }
    function addLine(data = {}) {
        const definition = state.definition;
        const row = el('tr'); row.dataset.businessRow = '';
        const number = el('td', 'wf-line-number'); number.dataset.lineNumber = ''; row.append(number);
        const addCell = control => { const cell = el('td'); cell.append(control); row.append(cell); return control; };
        const select = el('select'); select.dataset.itemField = 'catalogItemId'; select.dataset.label = definition.lineCategory === 'ExpenseCategory' ? 'khoản mục' : 'hàng hóa / thiết bị';
        select.setAttribute('aria-label', select.dataset.label); select.dataset.requiredForSend = 'true';
        populate(select, definition.lineCategory, data.catalogItemId); addCell(select);
        const description = el('input'); description.dataset.itemField = 'description'; description.maxLength = 500; description.placeholder = 'Quy cách / Diễn giải'; description.value = data.description || ''; description.setAttribute('aria-label', 'Quy cách / Diễn giải'); addCell(description);
        const unit = el('input'); unit.dataset.itemField = 'unit'; unit.readOnly = true; unit.value = data.unit || ''; unit.setAttribute('aria-label', 'Đơn vị tính'); addCell(unit);
        const quantity = el('input'); quantity.type = 'number'; quantity.min = definition.code === 'EQUIPMENT' ? '1' : '.01'; quantity.max = '1000000'; quantity.step = definition.code === 'EQUIPMENT' ? '1' : '.01'; quantity.value = data.quantity ?? 1; quantity.dataset.itemField = 'quantity'; quantity.dataset.requiredForSend = 'true'; quantity.setAttribute('aria-label', 'Số lượng'); addCell(quantity);
        if (definition.pricedLines) {
            const price = el('input'); price.type = 'hidden'; price.value = data.unitPrice ?? '0'; price.dataset.itemField = 'unitPrice';
            const display = el('input'); display.type = 'text'; display.inputMode = 'decimal'; display.autocomplete = 'off'; display.value = formatMoney(price.value); display.dataset.requiredForSend = 'true'; display.setAttribute('aria-label', 'Đơn giá'); display.placeholder = '0';
            display.pattern = '(?=(?:[0-9]\\.?){1,13}(?:,[0-9]{1,2})?$)[0-9]{1,3}(?:\\.[0-9]{3})*(?:,[0-9]{1,2})?';
            const cell = el('td'); cell.append(display, price); row.append(cell);
            display.addEventListener('input', () => {
                const before = display.value.slice(0, display.selectionStart ?? display.value.length).replace(/\./g, '').length;
                const raw = display.value.replace(/[^0-9,]/g, '');
                const parts = raw.split(',');
                const whole = (parts[0] || '').replace(/^0+(?=\d)/, '');
                const decimal = parts.slice(1).join('');
                price.value = whole ? whole + (parts.length > 1 && decimal ? '.' + decimal : '') : '';
                display.value = formatMoney(whole + (parts.length > 1 ? '.' + decimal : ''));
                let position = 0, seen = 0;
                while (position < display.value.length && seen < before) { if (display.value[position] !== '.') seen++; position++; }
                display.setSelectionRange(position, position);
                display.setCustomValidity(parts.length > 2 || decimal.length > 2 || whole.length > 13 ? 'Đơn giá tối đa 13 chữ số và 2 chữ số thập phân.' : '');
            });
            const total = el('td', 'wf-line-total'); total.dataset.lineTotal = ''; row.append(total);
        }
        const note = el('input'); note.dataset.itemField = 'note'; note.maxLength = 1000; note.placeholder = 'Ghi chú'; note.value = data.note || ''; note.setAttribute('aria-label', 'Ghi chú dòng chi tiết'); addCell(note);
        const remove = el('button', 'wf-button wf-button-icon', '×'); remove.type = 'button'; remove.setAttribute('aria-label', 'Xóa dòng chi tiết'); addCell(remove);
        remove.addEventListener('click', () => { row.remove(); reindexRows(); updateComputed(); });
        const updateUnit = () => {
            const master = lookups.masterItems.find(x => String(x.id) === select.value);
            unit.value = master?.unit || '';
        };
        select.addEventListener('change', () => { updateUnit(); updateComputed(); });
        row.addEventListener('input', updateComputed);
        document.getElementById('businessLineBody').append(row); updateUnit(); reindexRows(); updateComputed();
    }
    function render() {
        const option = form.elements.RequestTypeId.selectedOptions[0];
        const definition = config.definitions.find(x => x.code === option?.dataset.template);
        if (selectedCode && state.definition) cache.set(selectedCode, capture());
        container.replaceChildren(); state.definition = definition || null; selectedCode = definition?.code;
        const summary = document.getElementById('requestTemplateSummary'); summary.hidden = !definition;
        if (!definition) return;
        const initial = cache.get(selectedCode) || (String(option.value) === String(config.initialRequestTypeId) ? config.initial : null) || {};
        document.getElementById('requestFormHeading').textContent = (config.editing ? 'Chỉnh sửa: ' : '') + option.textContent;
        document.getElementById('requestGuideHeading').textContent = definition.name;
        document.getElementById('requestFormIcon').className = 'mdi mdi-' + definition.icon;
        document.getElementById('businessSectionHeading').textContent = '2. ' + definition.name;
        document.getElementById('requestContentLabel').textContent = definition.contentLabel;
        summary.textContent = definition.name + ' · ' + definition.fields.filter(x => x.required).length + ' thông tin nghiệp vụ bắt buộc' + (definition.lineLabel ? ' · ' + definition.lineLabel : '');
        const sections = new Map();
        definition.fields.forEach(field => {
            if (!sections.has(field.section)) {
                const section = el('section', 'wf-business-section'); section.append(el('h3', 'wf-business-heading', field.section));
                const grid = el('div', 'wf-fields'); section.append(grid); container.append(section); sections.set(field.section, grid);
            }
            const wrapper = el('div', 'wf-field' + (field.input === 'textarea' ? ' wf-field-full' : ''));
            const label = el('label', '', field.label + ' '); label.htmlFor = 'business-' + field.name;
            if (field.required) label.append(el('em', '', '*'));
            const control = el(field.input === 'select' ? 'select' : field.input === 'textarea' ? 'textarea' : 'input');
            control.id = 'business-' + field.name; control.name = 'BusinessData.' + field.name; control.dataset.requiredForSend = String(field.required);
            control.dataset.businessLabel = field.label;
            if (field.input === 'select') {
                control.dataset.label = field.label;
                let value = initial[key(field.name)];
                if (!value && field.name === 'EmployeeId') value = lookups.employees.some(x => x.id === lookups.currentEmployeeId) ? lookups.currentEmployeeId : '';
                if (!value && field.name === 'TargetDepartmentId') value = lookups.currentDepartmentId;
                if (!value && field.name === 'WorkSiteId') value = lookups.departments.find(x => String(x.id) === String(initial.targetDepartmentId))?.siteId || lookups.currentSiteId;
                if (!value && field.name === 'CurrencyId') value = lookups.masterItems.find(x => x.category === 'Currency' && x.code === 'VND' && x.isActive)?.id;
                if (!value && field.name === 'StartSession') value = 'AM';
                if (!value && field.name === 'EndSession') value = 'PM';
                const parent = field.lookup === 'City' ? (initial.countryId || '') : field.lookup === 'Vehicle' ? (initial.vehicleTypeId || '') : field.lookup === 'departments' && fieldControl('WorkSiteId') ? fieldControl('WorkSiteId').value : undefined;
                populate(control, field.lookup, value, parent);
            } else {
                if (field.input !== 'textarea') control.type = field.input;
                if (field.input === 'number') { control.min = field.name === 'PassengerCount' ? '1' : '.000001'; control.max = field.name === 'PassengerCount' ? '100' : '1000000000'; control.step = field.name === 'PassengerCount' ? '1' : '.000001'; }
                else if (field.input === 'text' || field.input === 'textarea') control.maxLength = field.name === 'ExpectedBenefit' ? 2000 : field.name === 'ContactPhone' ? 30 : field.name === 'BankAccount' || field.name === 'InvoiceNumber' ? 100 : field.name === 'BankName' || field.name === 'Payee' ? 200 : 500;
                const value = initial[key(field.name)];
                control.value = value == null ? '' : field.input === 'date' ? String(value).slice(0, 10) : field.input === 'datetime-local' ? String(value).slice(0, 16) : value;
                if (field.input === 'textarea') control.rows = 3;
            }
            wrapper.append(label, control); sections.get(field.section).append(wrapper);
        });
        if (definition.hasPeriod) { const duration = el('p', 'wf-info-note'); duration.id = 'businessDuration'; container.append(duration); }
        if (definition.code === 'LEAVE') { const balance = el('div', 'wf-info-note', 'Chọn nhân viên, loại nghỉ và khoảng thời gian để xem phép khả dụng.'); balance.id = 'businessLeaveBalance'; balance.setAttribute('role', 'status'); container.append(balance); }
        if (definition.code === 'VEHICLE') container.append(el('p', 'wf-info-note', 'Xe và tài xế tại đây là đề xuất. Sau khi duyệt xong, điều phối viên sẽ kiểm tra lịch và phân công thực tế tại mục Điều phối xe.'));
        if (definition.hasParticipants) {
            const section = el('section', 'wf-business-section'); section.append(el('h3', 'wf-business-heading', definition.code === 'OVERTIME' ? 'Nhân viên làm thêm' : 'Nhân viên tham gia'));
            const select = el('select', 'wf-participant-select'); select.multiple = true; select.size = 5; select.id = 'businessParticipants'; select.name = 'BusinessData.ParticipantIds'; select.setAttribute('aria-label', 'Nhân viên tham gia');
            if (definition.code === 'OVERTIME') select.dataset.requiredForSend = 'true';
            const ids = initial.participantIds || (definition.code === 'OVERTIME' && lookups.employees.some(x => x.id === lookups.currentEmployeeId) ? [lookups.currentEmployeeId] : []);
            populate(select, 'employees', ids);
            section.append(select, el('p', 'wf-help', definition.code === 'VEHICLE' ? 'Giữ Ctrl (hoặc Command) để chọn đủ các hành khách. Số hành khách phải bằng số nhân viên đã chọn.' : 'Giữ Ctrl (hoặc Command) để chọn nhiều người. Người đi chính được tự động tính vào danh sách.')); container.append(section);
        }
        if (definition.lineCategory) {
            const section = el('section', 'wf-business-section'); section.append(el('h3', 'wf-business-heading', definition.lineLabel + (definition.requireLines ? ' *' : '')));
            const scroll = el('div', 'wf-table-scroll'); const table = el('table', 'wf-business-lines'); const head = el('thead'); const header = el('tr');
            ['#', definition.lineCategory === 'ExpenseCategory' ? 'Khoản mục chi phí' : 'Hàng hóa / Thiết bị', 'Quy cách / Diễn giải', 'ĐVT', 'Số lượng', ...(definition.pricedLines ? ['Đơn giá', 'Thành tiền'] : []), 'Ghi chú', ''].forEach(text => header.append(el('th', '', text)));
            head.append(header); const body = el('tbody'); body.id = 'businessLineBody'; table.append(head, body); scroll.append(table); section.append(scroll);
            const add = el('button', 'wf-button wf-button-outline', '+ Thêm dòng'); add.id = 'businessAddLine'; add.type = 'button'; add.addEventListener('click', () => addLine()); section.append(add);
            if (!optionsFor(definition.lineCategory).some(x => x.isActive)) section.append(el('p', 'wf-help', 'Danh mục hiện chưa có dữ liệu. Vui lòng liên hệ quản trị viên để bổ sung trước khi gửi.'));
            if (definition.pricedLines) { const total = el('p', 'wf-business-total'); total.id = 'businessTotal'; section.append(total); }
            container.append(section); (initial.items || []).forEach(addLine);
        }
        const date = form.elements.RequestedDate;
        document.getElementById('commonDateField').hidden = definition.hasPeriod; date.disabled = definition.hasPeriod;
        document.getElementById('requestDateLabel').textContent = definition.dateLabel || 'Ngày yêu cầu';
        const amount = form.elements.Amount;
        let formattedAmount = document.getElementById('requestAmountFormatted');
        if (!formattedAmount) { formattedAmount = el('input'); formattedAmount.id = 'requestAmountFormatted'; formattedAmount.readOnly = true; formattedAmount.setAttribute('aria-label', 'Tổng tiền quy đổi (VND)'); amount.insertAdjacentElement('afterend', formattedAmount); }
        amount.hidden = definition.pricedLines; formattedAmount.hidden = !definition.pricedLines;
        document.getElementById('commonAmountField').hidden = !definition.pricedLines && definition.code !== 'INTERNAL';
        amount.disabled = !definition.pricedLines && definition.code !== 'INTERNAL'; amount.readOnly = definition.pricedLines;
        document.getElementById('requestAmountLabel').textContent = definition.pricedLines ? 'Tổng tiền quy đổi (VND)' : 'Ngân sách dự kiến (VND)';
        if (definition.code === 'INTERNAL') amount.value = initial.commonAmount ?? (String(option.value) === String(config.initialRequestTypeId) ? config.initialAmount : '') ?? '';
        updateCurrency(); updateComputed(); scheduleLeaveQuote();
    }
    function updateCurrency() {
        const currency = fieldControl('CurrencyId'); const rate = fieldControl('ExchangeRate');
        if (!currency || !rate) return;
        const vnd = lookups.masterItems.find(x => String(x.id) === currency.value)?.code === 'VND';
        rate.readOnly = vnd;
        if (vnd) rate.value = '1';
    }
    function updateComputed() {
        const definition = state.definition;
        if (!definition) return;
        const start = fieldControl('StartAt'); const end = fieldControl('EndAt');
        if (start && end) {
            end.min = start.value;
            const duration = document.getElementById('businessDuration');
            if (start.value && end.value && end.value >= start.value) {
                let value = (new Date(end.value) - new Date(start.value)) / 3600000;
                const hours = ['OVERTIME', 'VEHICLE'].includes(definition.code);
                if (!hours) { value = value / 24 + 1; if (definition.code === 'LEAVE') { if (fieldControl('StartSession').value === 'PM') value -= .5; if (fieldControl('EndSession').value === 'AM') value -= .5; } }
                duration.textContent = hours ? 'Thời lượng: ' + value.toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' giờ.' : 'Số ngày theo lịch: ' + value.toLocaleString('vi-VN') + '. Chưa loại trừ ngày nghỉ, ngày lễ theo chính sách của nhà trường.';
            } else duration.textContent = 'Chọn thời gian bắt đầu và kết thúc để tính thời lượng.';
        }
        if (definition.pricedLines) {
            let total = 0;
            container.querySelectorAll('[data-business-row]').forEach(row => {
                const value = Math.round((Number(row.querySelector('[data-item-field="quantity"]').value) * Number(row.querySelector('[data-item-field="unitPrice"]').value) + Number.EPSILON) * 100) / 100;
                row.querySelector('[data-line-total]').textContent = value.toLocaleString('vi-VN', { maximumFractionDigits: 2 }); total += value;
            });
            const rate = Number(fieldControl('ExchangeRate')?.value);
            form.elements.Amount.value = rate > 0 ? (Math.round(total * rate * 100) / 100).toFixed(2) : '';
            const formattedAmount = document.getElementById('requestAmountFormatted');
            if (formattedAmount) formattedAmount.value = formatMoney(form.elements.Amount.value);
            const currencyName = fieldControl('CurrencyId')?.selectedOptions[0]?.textContent || '';
            document.getElementById('businessTotal').textContent = 'Tổng cộng: ' + total.toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' · ' + currencyName;
        }
    }
    state.reviewEntries = () => {
        if (!state.definition) return [];
        const entries = state.definition.fields.map(field => { const control = fieldControl(field.name); return [field.label, control.tagName === 'SELECT' ? control.selectedOptions[0]?.textContent || '' : control.value]; });
        const participants = document.getElementById('businessParticipants');
        if (participants) entries.push(['Nhân viên tham gia', [...participants.selectedOptions].map(x => x.textContent).join('\n')]);
        if (state.definition.lineCategory) entries.push([state.definition.lineLabel, [...container.querySelectorAll('[data-business-row]')].map(row => { const catalog = row.querySelector('select'); return (catalog.selectedOptions[0]?.textContent || '') + ' · SL: ' + row.querySelector('[data-item-field="quantity"]').value + (state.definition.pricedLines ? ' · Đơn giá: ' + row.querySelector('[data-item-field="unitPrice"]').value : ''); }).join('\n')]);
        return entries;
    };
    function scheduleLeaveQuote() {
        clearTimeout(leaveQuoteTimer); const epoch = ++leaveQuoteEpoch;
        if (state.definition?.code !== 'LEAVE') return;
        const target = document.getElementById('businessLeaveBalance');
        const values = { employeeId: fieldControl('EmployeeId')?.value, leaveTypeId: fieldControl('LeaveTypeId')?.value,
            startAt: fieldControl('StartAt')?.value, endAt: fieldControl('EndAt')?.value,
            startSession: fieldControl('StartSession')?.value, endSession: fieldControl('EndSession')?.value };
        if (!Object.values(values).every(Boolean)) { target.textContent = 'Chọn nhân viên, loại nghỉ và khoảng thời gian để xem phép khả dụng.'; return; }
        if (document.getElementById('requestId').value) values.requestId = document.getElementById('requestId').value;
        leaveQuoteTimer = setTimeout(async () => {
            target.textContent = 'Đang kiểm tra phép khả dụng...';
            try {
                const response = await fetch('/api/operations/leave/quote?' + new URLSearchParams(values));
                const data = await response.json();
                if (epoch !== leaveQuoteEpoch || !target.isConnected) return;
                if (!response.ok) throw new Error(data.message || 'Chưa thể xem phép tồn.');
                target.replaceChildren();
                if (!data.years.length) { target.textContent = 'Khoảng nghỉ không có ngày làm việc theo lịch đã thiết lập.'; return; }
                data.years.forEach(q => { const b = q.balance;
                    const text = `Năm ${q.year}: đề nghị ${q.days} ngày làm việc. ` + (!b.policyConfigured ? 'Loại nghỉ chưa được HR thiết lập hạn mức.' : !b.deductsBalance ? 'Không trừ phép tồn.' : !b.configured ? 'HR chưa cấp sổ phép.' : `Phép khả dụng: ${b.availableDays} ngày${b.availableDays < q.days ? ' — không đủ phép để gửi.' : '.'}`) + (b.unclassifiedRequests ? ' Có dữ liệu cũ cần HR đối soát.' : '');
                    target.append(el('div', '', text));
                });
                if (!data.workingCalendarConfigured) target.append(el('small', '', 'Chưa cấu hình lịch làm việc: số ngày đang tính theo ngày lịch.'));
            } catch (error) { if (epoch === leaveQuoteEpoch && target.isConnected) target.textContent = error.message; }
        }, 250);
    }
    state.prepare = forSend => {
        if (!state.isReady) { lookupError.textContent = 'Chưa tải xong danh mục. Vui lòng thử lại sau hoặc tải lại trang.'; lookupError.hidden = false; return false; }
        container.querySelectorAll('[data-business-row]').forEach(row => {
            if (!row.querySelector('select').value && !row.querySelector('[data-item-field="description"]').value.trim() && !row.querySelector('[data-item-field="note"]').value.trim() && Number(row.querySelector('[data-item-field="quantity"]').value) === 1 && (!state.definition.pricedLines || Number(row.querySelector('[data-item-field="unitPrice"]').value) === 0)) row.remove();
        });
        reindexRows(); updateComputed();
        if (forSend && state.definition?.code === 'VEHICLE') {
            const count = fieldControl('PassengerCount');
            const selected = document.getElementById('businessParticipants')?.selectedOptions.length || 0;
            if (count?.value && Number(count.value) !== selected) {
                const error = document.getElementById('requestFormError');
                error.textContent = `Số hành khách (${count.value}) phải bằng số nhân viên tham gia đã chọn (${selected}).`;
                error.hidden = false;
                return false;
            }
        }
        if (forSend && state.definition?.requireLines && !container.querySelector('[data-business-row]')) {
            const error = document.getElementById('requestFormError'); error.textContent = 'Vui lòng thêm ít nhất một dòng ' + state.definition.lineLabel.toLowerCase() + '.'; error.hidden = false; return false;
        }
        return true;
    };
    container.addEventListener('change', event => {
        const name = event.target.name;
        if (name === 'BusinessData.CountryId') populate(fieldControl('CityId'), 'City', '', event.target.value);
        if (name === 'BusinessData.VehicleTypeId') populate(fieldControl('VehicleId'), 'Vehicle', '', event.target.value);
        if (name === 'BusinessData.WorkSiteId' && fieldControl('TargetDepartmentId')) populate(fieldControl('TargetDepartmentId'), 'departments', '', event.target.value);
        if (name === 'BusinessData.CurrencyId') { fieldControl('ExchangeRate').value = ''; updateCurrency(); }
        updateComputed();
        scheduleLeaveQuote();
    });
    container.addEventListener('input', () => { updateComputed(); scheduleLeaveQuote(); });
    $('#requestType').on('change', () => { if (state.isReady) render(); });
    try {
        const response = await fetch('/api/request-forms/lookups');
        if (!response.ok) throw new Error('Không thể tải danh mục nhập liệu. Vui lòng tải lại trang hoặc đăng nhập lại.');
        lookups = await response.json(); state.isReady = true; render();
    } catch (error) { lookupError.textContent = error.message; lookupError.hidden = false; }
});

