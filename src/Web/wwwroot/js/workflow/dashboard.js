$(function () {
    const page = document.getElementById('approvalDashboard');
    if (!page) return;
    const from = document.getElementById('dashboardFrom');
    const to = document.getElementById('dashboardTo');
    const period = document.getElementById('dashboardPeriod');
    const search = document.getElementById('dashboardSearch');
    const months = document.getElementById('dashboardTrendMonths');
    const error = document.getElementById('dashboardError');
    const format = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 1 });
    const countFormat = new Intl.NumberFormat('vi-VN');
    const statusInfo = {
        1: { name: 'Đang xử lý', color: '#ffbf28', badge: 'amber' },
        2: { name: 'Đã hoàn thành', color: '#10bc88', badge: 'green' },
        3: { name: 'Yêu cầu bổ sung', color: '#a45cf4', badge: 'purple' },
        4: { name: 'Đã từ chối', color: '#f95370', badge: 'red' },
        5: { name: 'Bản nháp', color: '#3995ff', badge: 'blue' },
        6: { name: 'Đã hủy', color: '#b5c1d4', badge: 'gray' }
    };
    const types = {
        PURCHASE: ['cart-outline', 'blue'], PAYMENT: ['credit-card', 'amber'], LEAVE: ['calendar-clock', 'purple'],
        EQUIPMENT: ['monitor', 'green'], OVERTIME: ['clock-outline', 'amber'], INTERNAL: ['lightbulb-outline', 'blue'],
        BUSINESS_TRIP: ['airplane', 'blue'], VEHICLE: ['car', 'green']
    };
    const palette = ['#248fff', '#15bc8d', '#ab53f4', '#ffb633', '#f47495', '#53c7d6', '#7a8cf2', '#64b48c'];
    let trendChart, statusChart, requestController, sequence = 0, searchTimer, lastData;
    const node = (tag, className, text) => { const item = document.createElement(tag); if (className) item.className = className; if (text !== undefined) item.textContent = text; return item; };
    const icon = name => { const item = node('i', 'mdi mdi-' + name); item.setAttribute('aria-hidden', 'true'); return item; };
    const number = value => value == null ? '—' : countFormat.format(value);
    const utcDate = value => new Date(/[zZ]$|[+-]\d{2}:\d{2}$/.test(value) ? value : value + 'Z');
    const dateInput = value => value.getFullYear() + '-' + String(value.getMonth() + 1).padStart(2, '0') + '-' + String(value.getDate()).padStart(2, '0');
    function applyPeriod() {
        const today = new Date(); today.setHours(0, 0, 0, 0);
        let start = new Date(today), end = new Date(today);
        if (period.value === 'month') start.setDate(1);
        else if (period.value === 'last-month') { start = new Date(today.getFullYear(), today.getMonth() - 1, 1); end = new Date(today.getFullYear(), today.getMonth(), 0); }
        else if (period.value !== 'custom') start.setDate(start.getDate() - Number(period.value) + 1);
        else return;
        from.value = dateInput(start); to.value = dateInput(end);
    }
    function empty(target, message = 'Chưa có dữ liệu trong kỳ này') {
        const holder = node('div', 'dash-empty'); holder.append(icon('chart-bar'), node('span', '', message)); target.replaceChildren(holder);
    }
    function setValue(name, value) { page.querySelector('[data-kpi="' + name + '"]').textContent = value; }
    function delta(key, current, previous, lowerIsBetter = false) {
        const target = page.querySelector('[data-delta="' + key + '"]'); target.replaceChildren();
        if (current == null) { target.textContent = 'Chưa có thời điểm hoàn thành'; return; }
        if (previous == null || previous === 0 && current !== 0) { target.textContent = 'Chưa có kỳ so sánh'; return; }
        const change = previous === 0 ? 0 : (current - previous) * 100 / previous;
        const tone = change === 0 || key === 'total' ? 'neutral' : (lowerIsBetter ? change < 0 : change > 0) ? 'good' : 'bad';
        const badge = node('b', 'is-' + tone); badge.append(icon(change > 0 ? 'arrow-up' : change < 0 ? 'arrow-down' : 'minus'), node('span', '', format.format(Math.abs(change)) + '%'));
        target.append(badge, node('span', '', 'so với kỳ trước'));
    }
    function charts(data) {
        if (!window.Highcharts) {
            empty(document.getElementById('dashboardTrend'), 'Không tải được thư viện biểu đồ. Vui lòng tải lại trang.');
            empty(document.getElementById('dashboardStatus'), 'Không tải được biểu đồ.');
            return;
        }
        trendChart?.destroy(); statusChart?.destroy();
        const chartBase = { chart: { backgroundColor: 'transparent', style: { fontFamily: 'Segoe UI, Arial, sans-serif' }, animation: false }, title: { text: null }, credits: { enabled: false }, exporting: { enabled: false }, legend: { enabled: false } };
        trendChart = Highcharts.chart('dashboardTrend', {
            ...chartBase, chart: { ...chartBase.chart, type: 'areaspline', height: 200, spacing: [4, 5, 8, 0] },
            accessibility: { description: 'Số yêu cầu tạo trong từng tháng, phân theo tổng số, đã hoàn thành và đang xử lý.' },
            xAxis: { categories: data.trend.map(x => 'T' + x.month + '/' + x.year), lineColor: '#e3ebf7', tickLength: 0, labels: { style: { color: '#8a9bb6', fontSize: '10px' } } },
            yAxis: { min: 0, allowDecimals: false, title: { text: null }, gridLineColor: '#edf2f9', labels: { style: { color: '#8a9bb6', fontSize: '10px' } } },
            tooltip: { shared: true, valueDecimals: 0, borderColor: '#e0e9f7', shadow: false },
            plotOptions: { series: { animation: false, lineWidth: 2, marker: { enabled: true, radius: 3.5, lineWidth: 1, lineColor: '#fff' }, fillOpacity: .07 } },
            series: [{ name: 'Tổng yêu cầu', color: '#248fff', data: data.trend.map(x => x.total) }, { name: 'Đã hoàn thành', color: '#13ba88', data: data.trend.map(x => x.completed) }, { name: 'Đang xử lý', color: '#ffbf28', data: data.trend.map(x => x.pending) }]
        });
        const visibleStatuses = [2, 1, 3, 4, 5, 6, 0].map(status => ({ status, count: data.statuses.find(x => x.status === status)?.count || 0 })).filter(x => x.count);
        statusChart = Highcharts.chart('dashboardStatus', {
            ...chartBase, chart: { ...chartBase.chart, type: 'pie', height: 220, spacing: [0, 0, 0, 0], events: { render() {
                const x = this.plotLeft + this.plotWidth / 2, y = this.plotTop + this.plotHeight / 2;
                if (!this.centerNumber) { this.centerNumber = this.renderer.text('', 0, 0).attr({ 'text-anchor': 'middle', zIndex: 5 }).css({ fontSize: '24px', fontWeight: '700', color: '#14284d' }).add(); this.centerCaption = this.renderer.text('yêu cầu', 0, 0).attr({ 'text-anchor': 'middle', zIndex: 5 }).css({ fontSize: '10px', color: '#7a8eac' }).add(); }
                this.centerNumber.attr({ text: number(data.current.total), x, y: y + 3 }); this.centerCaption.attr({ x, y: y + 20 });
            } } },
            accessibility: { description: 'Tỷ lệ các trạng thái yêu cầu trong kỳ đã chọn.' },
            tooltip: { enabled: data.current.total > 0, pointFormat: '<b>{point.y}</b> yêu cầu ({point.percentage:.1f}%)', valueDecimals: 0 },
            plotOptions: { pie: { innerSize: '73%', borderWidth: 2, borderColor: '#fff', dataLabels: { enabled: false }, states: { hover: { halo: { size: 3 } } }, animation: false } },
            series: [{ name: 'Yêu cầu', data: visibleStatuses.length ? visibleStatuses.map(x => ({ name: statusInfo[x.status]?.name || 'Khác', y: x.count, color: statusInfo[x.status]?.color || '#b7c5d9' })) : [{ name: 'Chưa có dữ liệu', y: 1, color: '#eef3fa' }], enableMouseTracking: data.current.total > 0 }]
        });
        const legend = document.getElementById('dashboardStatusLegend'); legend.replaceChildren();
        if (!visibleStatuses.length) { legend.append(node('span', 'dash-untracked', 'Chưa có yêu cầu trong kỳ')); return; }
        visibleStatuses.forEach(item => {
            const info = statusInfo[item.status] || { name: 'Khác', color: '#b7c5d9' };
            const row = node('div', 'dash-status-item'); const dot = node('b', 'dash-dot'); dot.style.background = info.color;
            const text = node('div'); text.append(node('span', '', info.name), node('strong', '', number(item.count) + ' (' + format.format(item.count * 100 / data.current.total) + '%)')); row.append(dot, text); legend.append(row);
        });
    }
    function bars(targetId, items, departments = false) {
        const target = document.getElementById(targetId); target.replaceChildren();
        if (!items.length) return empty(target);
        const maximum = Math.max(...items.map(x => x.count), 1);
        items.forEach((item, index) => {
            const type = departments ? ['office-building', ['amber', 'green', 'purple', 'blue'][index % 4]] : (types[item.templateCode] || ['file-document-outline', 'blue']);
            const row = node('div', 'dash-bar-row'); const badge = node('span', 'dash-bar-icon dash-' + type[1]); badge.append(icon(type[0]));
            const name = node('span', 'dash-bar-name', item.name); const track = node('div', 'dash-bar-track'); const fill = node('span', 'dash-bar-fill'); fill.style.width = Math.max(1, item.count * 100 / maximum) + '%'; fill.style.background = palette[index % palette.length]; track.append(fill);
            track.setAttribute('role', 'img'); track.setAttribute('aria-label', item.name + ': ' + item.count + ' yêu cầu');
            const count = node('span', 'dash-bar-count', number(item.count));
            if (departments) count.append(node('small', '', format.format(item.count ? item.completed * 100 / item.count : 0) + '% hoàn tất'));
            row.append(badge, name, track, count); target.append(row);
        });
    }
    function approvers(items) {
        const target = document.getElementById('dashboardApprovers'); target.replaceChildren();
        if (!items.length) { const item = node('li'); empty(item, 'Chưa có lượt phê duyệt trong kỳ'); target.append(item); return; }
        items.forEach((item, index) => {
            const row = node('li', 'dash-approver'); const rank = node('span', 'dash-rank dash-rank-' + (index + 1), index + 1);
            const initials = item.name.trim().split(/\s+/).slice(-2).map(x => x[0]).join('').toUpperCase();
            const avatar = node('span', 'dash-avatar', initials); avatar.setAttribute('aria-hidden', 'true');
            const info = node('div', 'dash-approver-info'); info.append(node('strong', '', item.name), node('span', '', item.position || 'Người phê duyệt'));
            row.append(rank, avatar, info, node('span', 'dash-approver-count', number(item.count))); target.append(row);
        });
    }
    function tableEmpty(target, span, message) { const row = node('tr'); const cell = node('td'); cell.colSpan = span; empty(cell, message); row.append(cell); target.replaceChildren(row); }
    function relative(value, waiting = false) {
        const elapsed = Math.max(0, (Date.now() - utcDate(value)) / 60000);
        if (elapsed < 1) return waiting ? 'Vừa gửi' : 'Vừa xong';
        if (elapsed < 60) return Math.floor(elapsed) + ' phút' + (waiting ? '' : ' trước');
        if (elapsed < 1440) return Math.floor(elapsed / 60) + ' giờ' + (waiting ? '' : ' trước');
        return Math.floor(elapsed / 1440) + ' ngày' + (waiting ? '' : ' trước');
    }
    function attention(items) {
        const target = document.getElementById('dashboardAttention'); target.replaceChildren();
        if (!items.length) return tableEmpty(target, 7, 'Không có yêu cầu cần chú ý trong kỳ');
        items.forEach(item => {
            const row = node('tr'); const add = child => { const cell = node('td'); if (typeof child === 'string') cell.textContent = child; else cell.append(child); row.append(cell); };
            const link = node('a', '', item.applicationNo); link.href = '/Requests/Details/' + item.id; add(link); add(item.requestType); add(item.createdBy);
            const status = statusInfo[item.status] || { badge: 'gray', name: 'Khác' }; add(node('span', 'dash-badge dash-badge-' + status.badge, status.name));
            add(node('span', 'dash-badge dash-badge-' + (item.priority >= 2 ? 'red' : item.priority === 1 ? 'amber' : 'green'), ['Thấp', 'Bình thường', 'Cao', 'Khẩn cấp'][item.priority] || '—'));
            const age = node('span', '', relative(item.sentAt, true)); age.title = utcDate(item.sentAt).toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok' }); add(age);
            const open = node('a', 'dash-view', 'Xem'); open.href = link.href; open.setAttribute('aria-label', 'Xem yêu cầu ' + item.applicationNo); add(open); target.append(row);
        });
    }
    function workflows(items) {
        const target = document.getElementById('dashboardWorkflows'); target.replaceChildren();
        if (!items.length) return tableEmpty(target, 3, 'Chưa có quy trình trong phạm vi xem');
        items.forEach(item => {
            const row = node('tr'); const name = node('td', '', item.name); name.append(node('small', '', item.requestTypes.join(' · ') || 'Chưa gán loại yêu cầu'));
            const state = node('td'); state.append(node('span', 'dash-badge dash-badge-' + (item.isActive ? 'green' : 'red'), item.isActive ? 'Đang hoạt động' : 'Tạm dừng'));
            row.append(name, node('td', '', number(item.stepCount)), state); target.append(row);
        });
    }
    function activities(items) {
        const target = document.getElementById('dashboardActivities'); target.replaceChildren();
        const info = { 0: ['file-document-outline', 'blue', 'tạo yêu cầu'], 1: ['send', 'blue', 'gửi phê duyệt'], 2: ['check', 'green', 'đã phê duyệt'], 3: ['undo-variant', 'amber', 'yêu cầu bổ sung'], 4: ['close', 'amber', 'đã từ chối'], 5: ['send', 'blue', 'gửi lại yêu cầu'], 6: ['close-circle', 'amber', 'hủy yêu cầu'], 7: ['pencil-outline', 'blue', 'cập nhật yêu cầu'] };
        if (!items.length) { const item = node('li'); empty(item, 'Chưa có hoạt động trong kỳ'); target.append(item); return; }
        items.forEach(item => {
            const type = info[item.action] || ['history', 'blue', 'xử lý yêu cầu']; const row = node('li', 'dash-activity'); const badge = node('span', 'dash-activity-icon dash-' + type[1]); badge.append(icon(type[0]));
            const text = node('div'); const link = node('a', '', item.applicationNo + ' · ' + relative(item.actionAt)); link.href = '/Requests/Details/' + item.requestId;
            text.append(node('strong', '', item.actor + ' ' + type[2]), link); row.append(badge, text); target.append(row);
        });
    }
    function render(data) {
        const current = data.current, previous = data.previous;
        setValue('total', number(current.total)); setValue('pending', number(current.pending)); setValue('average', current.averageHours == null ? '—' : format.format(current.averageHours) + ' giờ');
        setValue('automatic', number(data.automaticApprovals)); setValue('automation', data.automationRate == null ? '—' : format.format(data.automationRate) + '%'); setValue('sla', data.slaRate == null ? '—' : format.format(data.slaRate) + '%');
        delta('total', current.total, previous.total); delta('pending', current.pending, previous.pending, true); delta('average', current.averageHours, previous.averageHours, true);
        page.querySelector('[data-efficiency="completion"]').textContent = current.completionRate == null ? '—' : format.format(current.completionRate) + '%';
        page.querySelector('[data-efficiency="approvals"]').textContent = number(data.approvalActions); page.querySelector('[data-efficiency="workflows"]').textContent = number(data.activeWorkflows);
        charts(data); bars('dashboardTypes', data.categories); bars('dashboardDepartments', data.departments, true); approvers(data.approvers); attention(data.attention); workflows(data.workflows); activities(data.activities);
        document.getElementById('dashboardUpdated').textContent = 'Cập nhật: ' + utcDate(data.generatedAt).toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok', hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' });
    }
    async function refresh() {
        to.min = from.value;
        to.setCustomValidity(from.value && to.value && from.value > to.value ? 'Ngày kết thúc phải từ ngày bắt đầu trở đi.' : '');
        if (!from.value || !to.value || !to.reportValidity()) return;
        requestController?.abort(); requestController = new AbortController(); const ownSequence = ++sequence;
        page.setAttribute('aria-busy', 'true'); error.hidden = true;
        const params = new URLSearchParams({ from: from.value, to: to.value, trendMonths: months.value }); if (search.value.trim()) params.set('search', search.value.trim());
        try {
            const response = await fetch('/api/workflow-dashboard/overview?' + params, { credentials: 'same-origin', signal: requestController.signal });
            if (!response.ok) { let message = 'Không tải được dashboard. Vui lòng thử lại hoặc đăng nhập lại.'; try { const problem = await response.json(); message = problem.message || problem.detail || message; } catch {} throw new Error(message); }
            const data = await response.json(); if (ownSequence !== sequence) return;
            render(data); lastData = data;
        } catch (exception) {
            if (exception.name === 'AbortError' || ownSequence !== sequence) return;
            error.textContent = exception.message + (lastData ? ' Dữ liệu trên màn hình chưa được cập nhật theo bộ lọc mới.' : ''); error.hidden = false;
            document.getElementById('dashboardUpdated').textContent = 'Chưa cập nhật được dữ liệu';
        } finally { if (ownSequence === sequence) page.setAttribute('aria-busy', 'false'); }
    }
    period.addEventListener('change', () => { applyPeriod(); refresh(); });
    [from, to].forEach(input => input.addEventListener('change', () => { period.value = 'custom'; refresh(); }));
    months.addEventListener('change', refresh); document.getElementById('dashboardRefresh').addEventListener('click', refresh);
    search.addEventListener('input', () => { clearTimeout(searchTimer); searchTimer = setTimeout(refresh, 300); });
    applyPeriod(); refresh();
});
