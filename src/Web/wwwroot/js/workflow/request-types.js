$(async function () {
    const workflows = await fetch('/api/admin/workflows').then(r => r.json());
    const templates = await fetch('/api/request-forms').then(r => r.json());
    let rows = [];
    $('#requestTypeGrid').dxDataGrid({
        dataSource: new DevExpress.data.CustomStore({
            key: 'id',
            load: async () => rows = await fetch('/api/admin/request-types').then(r => r.json()),
            insert: values => save(null, values),
            update: (id, values) => save(id, { ...rows.find(r => r.id === id), ...values })
        }),
        showBorders: true, editing: { mode: 'popup', allowAdding: true, allowUpdating: true },
        columns: [
            { dataField: 'code', caption: 'Mã' },
            { dataField: 'templateCode', caption: 'Mẫu biểu nghiệp vụ', setCellValue: (data, value) => { data.templateCode = value; if (!templates.find(t => t.code === value)?.pricedLines && value !== 'INTERNAL') data.requireAmount = false; }, lookup: { dataSource: templates, valueExpr: 'code', displayExpr: 'name' }, validationRules: [{ type: 'required' }] }, { dataField: 'name', caption: 'Tên' },
            { dataField: 'description', caption: 'Mô tả' },
            { dataField: 'approvalCaseId', caption: 'Quy trình', lookup: { dataSource: workflows, valueExpr: 'caseId', displayExpr: 'name' } },
            { dataField: 'requireRequestedDate', caption: 'Cần ngày', dataType: 'boolean' },
            { dataField: 'requireAmount', caption: 'Cần số tiền', dataType: 'boolean' },
            { dataField: 'requireReason', caption: 'Cần lý do', dataType: 'boolean' },
            { dataField: 'allowAttachment', caption: 'Cho đính kèm', dataType: 'boolean' },
            { dataField: 'isActive', caption: 'Sử dụng', dataType: 'boolean' },
            { caption: 'Luồng phê duyệt', allowEditing: false, cellTemplate: (container, options) => {
                $('<a>').attr('href', '/Workflows?form=' + options.data.id).text('Thiết kế luồng').appendTo(container);
            } }
        ],
        onInitNewRow: e => Object.assign(e.data, { isActive: true, allowAttachment: true, requireRequestedDate: true, templateCode: 'INTERNAL' })
    });
    async function save(id, data) {
        const r = await fetch('/api/admin/request-types' + (id ? '/' + id : ''), {
            method: id ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data)
        }); if (!r.ok) throw new Error(await r.text());
    }
});


