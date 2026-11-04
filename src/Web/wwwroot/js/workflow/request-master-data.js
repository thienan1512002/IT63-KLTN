$(async function () {
    const error = document.getElementById('masterError');
    try {
        async function read(url) { const response = await fetch(url); if (!response.ok) throw new Error('Không thể tải danh mục. Vui lòng thử lại.'); return response.json(); }
        const categories = await read('/api/admin/request-master-data/categories');
        categories.forEach(item => $('#masterCategory').append(new Option(item.name, item.code)));
        let rows = [];
        const grid = $('#masterGrid').dxDataGrid({
            dataSource: new DevExpress.data.CustomStore({ key: 'id', load: async () => rows = await read('/api/admin/request-master-data'), insert: values => save(null, values), update: (id, values) => save(id, { ...rows.find(row => row.id === id), ...values }) }),
            showBorders: false, columnAutoWidth: true, showColumnLines: false, rowAlternationEnabled: true,
            searchPanel: { visible: true, placeholder: 'Tìm mã hoặc tên danh mục...' }, paging: { pageSize: 20 }, pager: { showPageSizeSelector: true, allowedPageSizes: [20, 50, 100], showInfo: true },
            editing: { mode: 'popup', allowAdding: true, allowUpdating: true, popup: { title: 'Thông tin danh mục', showTitle: true, maxWidth: 700, height: 'auto' } },
            columns: [
                { dataField: 'category', caption: 'Nhóm danh mục', lookup: { dataSource: categories, valueExpr: 'code', displayExpr: 'name' }, validationRules: [{ type: 'required' }], setCellValue: (data, value) => { data.category = value; data.parentId = null; } },
                { dataField: 'code', caption: 'Mã', validationRules: [{ type: 'required' }, { type: 'stringLength', max: 50 }] },
                { dataField: 'name', caption: 'Tên hiển thị', validationRules: [{ type: 'required' }, { type: 'stringLength', max: 250 }] },
                { dataField: 'parentId', caption: 'Quốc gia / Loại xe', lookup: { dataSource: options => ({ store: rows, filter: ['category', '=', options.data?.category === 'City' ? 'Country' : options.data?.category === 'Vehicle' ? 'VehicleType' : '__none__'] }), valueExpr: 'id', displayExpr: 'name' } },
                { dataField: 'unit', caption: 'Đơn vị tính', validationRules: [{ type: 'stringLength', max: 50 }] },
                { dataField: 'capacity', caption: 'Sức chứa (hành khách)', dataType: 'number', validationRules: [{ type: 'range', min: 1, max: 100, ignoreEmptyValue: true }] },
                { dataField: 'isActive', caption: 'Sử dụng', dataType: 'boolean' }
            ],
            onInitNewRow: event => Object.assign(event.data, { isActive: true, category: $('#masterCategory').val() || undefined }),
            onEditorPreparing: event => {
                if (event.parentType !== 'dataRow') return;
                if (event.dataField === 'category' && !event.row.isNewRow) event.editorOptions.disabled = true;
                if (event.dataField === 'code' && !event.row.isNewRow) event.editorOptions.disabled = true;
                if (event.dataField === 'parentId') event.editorOptions.disabled = !event.row.isNewRow || !['City', 'Vehicle'].includes(event.row.data.category);
                if (event.dataField === 'capacity') event.editorOptions.disabled = !['Vehicle', 'VehicleType'].includes(event.row.data.category);
            }
        }).dxDataGrid('instance');
        $('#masterCategory').on('change', function () { if (this.value) grid.filter(['category', '=', this.value]); else grid.clearFilter(); });
        async function save(id, data) {
            const response = await fetch('/api/admin/request-master-data' + (id ? '/' + id : ''), { method: id ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
            if (!response.ok) { let message = 'Không thể lưu danh mục.'; try { const problem = await response.json(); message = problem.message || problem.detail || Object.values(problem.errors || {}).flat().join('\n') || message; } catch {} throw new Error(message); }
        }
    } catch (exception) { error.textContent = exception.message; error.hidden = false; }
});
