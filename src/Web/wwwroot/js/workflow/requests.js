$(function () {
    const page = document.querySelector('.wf-page[data-approval-list]');
    if (!page) return;
    const isApproval = page.dataset.approvalList === 'true';
    const error = document.getElementById('requestListError');
    const statuses = [
        { id: 1, name: 'Đang xử lý' }, { id: 2, name: 'Đã hoàn thành' },
        { id: 3, name: 'Yêu cầu bổ sung' }, { id: 4, name: 'Đã từ chối' },
        { id: 5, name: 'Bản nháp' }, { id: 6, name: 'Đã hủy' }
    ];
    const priorities = [{ id: 0, name: 'Thấp' }, { id: 1, name: 'Bình thường' }, { id: 2, name: 'Cao' }, { id: 3, name: 'Khẩn cấp' }];
    let priority = '';
    async function load(options) {
        const params = new URLSearchParams();
        ['skip', 'take', 'requireTotalCount', 'sort', 'filter', 'searchValue', 'searchExpr', 'searchOperation', 'group', 'groupSummary', 'totalSummary'].forEach(key => {
            if (options[key] !== undefined && options[key] !== null) params.set(key, JSON.stringify(options[key]));
        });
        const response = await fetch(window.requestGridUrl + '?' + params);
        if (!response.ok) throw new Error('Không thể tải yêu cầu. Vui lòng tải lại trang hoặc đăng nhập lại.');
        return response.json();
    }
    const grid = $('#requestGrid').dxDataGrid({
        dataSource: new DevExpress.data.CustomStore({ key: 'id', load }),
        remoteOperations: true, showBorders: false, showColumnLines: false,
        rowAlternationEnabled: true, hoverStateEnabled: true, columnAutoWidth: true,
        columnHidingEnabled: true, wordWrapEnabled: true,
        noDataText: isApproval ? 'Không có yêu cầu nào đang chờ bạn phê duyệt.' : 'Chưa có yêu cầu phù hợp. Hãy tạo yêu cầu mới hoặc thay đổi bộ lọc.',
        searchPanel: { visible: false, searchVisibleColumnsOnly: false },
        filterRow: { visible: false }, loadPanel: { text: 'Đang tải yêu cầu...' },
        paging: { pageSize: 10 },
        pager: { visible: true, showPageSizeSelector: true, allowedPageSizes: [10, 20, 50], showInfo: true, infoText: '{0} / {1} trang · {2} yêu cầu', showNavigationButtons: true },
        columns: [
            { dataField: 'applicationNo', caption: 'Mã phiếu', minWidth: 145, hidingPriority: 8, cellTemplate: (container, options) => $('<a>').attr('href', '/Requests/Details/' + options.data.id).text(options.value).appendTo(container) },
            { dataField: 'title', caption: 'Tiêu đề yêu cầu', minWidth: 200, hidingPriority: 9 },
            { dataField: 'requestType', caption: 'Loại yêu cầu', minWidth: 120, hidingPriority: 3, cellTemplate: (container, options) => $('<span>').addClass('wf-type-tag').text(options.value).appendTo(container) },
            { dataField: 'createdBy', caption: 'Người tạo', minWidth: 120, hidingPriority: 2 },
            { dataField: 'department', caption: 'Đơn vị', visible: false },
            { dataField: 'createdAt', caption: 'Ngày tạo', dataType: 'date', format: 'dd/MM/yyyy', sortOrder: 'desc', width: 110, hidingPriority: 1 },
            { dataField: 'status', caption: 'Trạng thái', minWidth: 120, hidingPriority: 7, lookup: { dataSource: statuses, valueExpr: 'id', displayExpr: 'name' }, cellTemplate: (container, options) => $('<span>').addClass('wf-badge wf-status-' + Number(options.value)).text(statuses.find(s => s.id === Number(options.value))?.name || 'Chưa xác định').appendTo(container) },
            { dataField: 'currentApprover', caption: 'Người đang xử lý', minWidth: 130, hidingPriority: 0, customizeText: options => options.value || '—' },
            { dataField: 'priority', caption: 'Ưu tiên', width: 100, hidingPriority: 4, lookup: { dataSource: priorities, valueExpr: 'id', displayExpr: 'name' }, cellTemplate: (container, options) => $('<span>').addClass('wf-badge wf-priority-' + Number(options.value)).text(priorities.find(p => p.id === Number(options.value))?.name || '—').appendTo(container) },
            { caption: '', width: 45, allowSorting: false, allowFiltering: false, allowSearch: false, hidingPriority: 6, cellTemplate: (container, options) => $('<a>').addClass('wf-grid-open').attr({ href: '/Requests/Details/' + options.data.id, title: 'Xem yêu cầu', 'aria-label': 'Xem yêu cầu ' + options.data.applicationNo }).append($('<i>').addClass('mdi mdi-chevron-right').attr('aria-hidden', 'true')).appendTo(container) }
        ]
    }).dxDataGrid('instance');

    function applyFilters() {
        const from = document.getElementById('requestFrom');
        const to = document.getElementById('requestTo');
        to.min = from.value;
        to.setCustomValidity(from.value && to.value && from.value > to.value ? 'Ngày kết thúc phải từ ngày bắt đầu trở đi.' : '');
        if (!to.reportValidity()) return;
        const filters = [];
        const type = $('#requestTypeFilter').val();
        const status = $('#requestStatusFilter').val();
        if (type) filters.push(['requestType', '=', type]);
        if (status) filters.push(['status', '=', Number(status)]);
        if (priority !== '') filters.push(['priority', '=', Number(priority)]);
        if (from.value) filters.push(['createdAt', '>=', new Date(from.value + 'T00:00:00').toISOString()]);
        if (to.value) {
            const end = new Date(to.value + 'T00:00:00');
            end.setDate(end.getDate() + 1);
            filters.push(['createdAt', '<', end.toISOString()]);
        }
        grid.beginUpdate();
        grid.option('filterValue', filters.length ? filters.reduce((result, entry) => result ? [result, 'and', entry] : entry, null) : null);
        grid.pageIndex(0);
        grid.endUpdate();
    }
    $('#requestTypeFilter, #requestStatusFilter, #requestFrom, #requestTo').on('change', applyFilters);
    let searchTimer;
    $('#requestSearch').on('input', function () {
        clearTimeout(searchTimer);
        const value = this.value;
        searchTimer = setTimeout(() => grid.searchByText(value), 300);
    });
    $('[data-priority]').on('click', function () {
        priority = this.dataset.priority;
        $('[data-priority]').removeClass('is-active').attr('aria-pressed', 'false');
        $(this).addClass('is-active').attr('aria-pressed', 'true');
        applyFilters();
    });
    $('[data-priority]').each(function () { this.setAttribute('aria-pressed', this.classList.contains('is-active').toString()); });
    $('#requestResetFilters').on('click', function () {
        clearTimeout(searchTimer);
        $('#requestSearch, #requestTypeFilter, #requestFrom, #requestTo').val('');
        $('#requestStatusFilter').val(isApproval ? '1' : '');
        priority = '';
        $('[data-priority]').removeClass('is-active').attr('aria-pressed', 'false').first().addClass('is-active').attr('aria-pressed', 'true');
        grid.searchByText('');
        applyFilters();
    });
    // Group counts are calculated by the API over the whole accessible list, not just the current page.
    Promise.allSettled([
        load({ group: [{ selector: 'status', isExpanded: false }], requireTotalCount: true }).then(result => {
            const groups = result.data || [];
            $('[data-stat]').each(function () {
                const value = this.dataset.stat === 'total' ? result.totalCount : (groups.find(g => Number(g.key) === Number(this.dataset.stat))?.count || 0);
                this.textContent = Number(value || 0).toLocaleString('vi-VN');
            });
        }),
        load({ group: [{ selector: 'requestType', isExpanded: false }] }).then(result => {
            (result.data || []).forEach(group => {
                if (group.key) $('<option>').val(group.key).text(group.key).appendTo('#requestTypeFilter');
            });
        })
    ]).then(results => {
        if (results.some(result => result.status === 'rejected')) {
            error.textContent = 'Chưa tải được thống kê hoặc danh sách loại yêu cầu. Vui lòng tải lại trang để thử lại.';
            error.hidden = false;
        }
    });
});
