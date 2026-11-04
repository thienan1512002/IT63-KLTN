$(async function () {
    const error = document.getElementById('reportError');
    let mode = 'requests'; let lastOptions = {}; let grid; let summaryEpoch = 0;
    const statuses = [{id:1,name:'Đang xử lý'},{id:2,name:'Đã hoàn thành'},{id:3,name:'Yêu cầu bổ sung'},{id:4,name:'Đã từ chối'},{id:5,name:'Bản nháp'},{id:6,name:'Đã hủy'}];
    const initialStatus = new URLSearchParams(location.search).get('status');
    if (initialStatus !== null && (initialStatus === '' || statuses.some(x => String(x.id) === initialStatus))) $('#reportStatus').val(initialStatus);
    const base = [
        {dataField:'applicationNo',caption:'Mã phiếu',fixed:true,cellTemplate:(cell,options)=>$('<a>').attr('href','/Requests/Details/'+(options.data.requestId || options.data.id)).text(options.value).appendTo(cell)},
        {dataField:'requestType',caption:'Loại yêu cầu'},
        {dataField:'workSite',caption:'Cơ sở'}, {dataField:'department',caption:'Đơn vị'},
        {dataField:'reportDate',caption:'Ngày báo cáo',dataType:'date',format:'dd/MM/yyyy',sortOrder:'desc'},
        {dataField:'status',caption:'Trạng thái',lookup:{dataSource:statuses,valueExpr:'id',displayExpr:'name'}}
    ];
    const number = (field,caption,templates) => ({dataField:field,caption,dataType:'number',format:{type:'fixedPoint',precision:2},templates});
    const requestColumns = [
        ...base, {dataField:'title',caption:'Tiêu đề'}, {dataField:'employee',caption:'Nhân viên chính'}, {dataField:'createdBy',caption:'Người tạo'},
        {dataField:'startAt',caption:'Bắt đầu',dataType:'datetime',format:'dd/MM/yyyy HH:mm'}, {dataField:'endAt',caption:'Kết thúc',dataType:'datetime',format:'dd/MM/yyyy HH:mm'},
        {dataField:'leaveType',caption:'Loại nghỉ phép',templates:['LEAVE']}, number('leaveDays','Ngày nghỉ (theo lịch)',['LEAVE']),
        number('leaveDebitedDays','Ngày phép đã trừ',['LEAVE']),number('leaveReservedDays','Ngày phép đang giữ chỗ',['LEAVE']),
        {dataField:'dispatchStatus',caption:'Trạng thái điều xe',templates:['VEHICLE'],lookup:{dataSource:[{id:0,name:'Chờ điều xe'},{id:1,name:'Đã phân công'},{id:2,name:'Đang thực hiện'},{id:3,name:'Hoàn thành'},{id:4,name:'Hủy chuyến'}],valueExpr:'id',displayExpr:'name'}},
        {dataField:'actualVehicle',caption:'Xe thực tế',templates:['VEHICLE']},{dataField:'actualDriver',caption:'Tài xế thực tế',templates:['VEHICLE']},
        {dataField:'purpose',caption:'Mục đích công tác',templates:['BUSINESS_TRIP']},{dataField:'country',caption:'Quốc gia',templates:['BUSINESS_TRIP']},{dataField:'city',caption:'Thành phố',templates:['BUSINESS_TRIP']},number('tripDays','Ngày công tác',['BUSINESS_TRIP']),
        {dataField:'partner',caption:'Đối tác / Nhà cung cấp',templates:['PURCHASE','PAYMENT','BUSINESS_TRIP']},
        {dataField:'overtimeType',caption:'Loại làm thêm',templates:['OVERTIME']},number('overtimeHours','Giờ làm thêm / người',['OVERTIME']),number('overtimePersonHours','Tổng giờ × số người',['OVERTIME']),{dataField:'participantCount',caption:'Số nhân viên tham gia',dataType:'number',templates:['BUSINESS_TRIP','OVERTIME','VEHICLE']},
        {dataField:'paymentMethod',caption:'Hình thức thanh toán',templates:['PAYMENT']},{dataField:'payee',caption:'Người nhận tiền',templates:['PAYMENT']},{dataField:'bank',caption:'Ngân hàng',templates:['PAYMENT']},{dataField:'invoiceNumber',caption:'Số hóa đơn',templates:['PAYMENT']},{dataField:'invoiceDate',caption:'Ngày hóa đơn',dataType:'date',format:'dd/MM/yyyy',templates:['PAYMENT']},
        {dataField:'proposalCategory',caption:'Nhóm đề xuất',templates:['INTERNAL']},{dataField:'expectedBenefit',caption:'Lợi ích kỳ vọng',templates:['INTERNAL']},
        {dataField:'vehicleType',caption:'Loại xe',templates:['VEHICLE']},{dataField:'vehicle',caption:'Xe được phân công',templates:['VEHICLE']},{dataField:'pickupLocation',caption:'Điểm đón',templates:['VEHICLE']},{dataField:'destination',caption:'Điểm đến',templates:['VEHICLE']},{dataField:'passengerCount',caption:'Số hành khách',dataType:'number',templates:['VEHICLE']},number('vehicleHours','Giờ sử dụng xe',['VEHICLE']),
        {dataField:'currency',caption:'Loại tiền',templates:['PURCHASE','PAYMENT','BUSINESS_TRIP']},number('exchangeRate','Tỷ giá đã lưu',['PURCHASE','PAYMENT','BUSINESS_TRIP']),number('originalAmount','Tổng tiền nguyên tệ',['PURCHASE','PAYMENT','BUSINESS_TRIP']),number('amountVnd','Tổng tiền (VND)',['PURCHASE','PAYMENT','BUSINESS_TRIP','INTERNAL']),
        {dataField:'hasBusinessData',caption:'Có thông tin nghiệp vụ',dataType:'boolean'}
    ];
    const itemColumns = [...base,{dataField:'partner',caption:'Đối tác / Nhà cung cấp'},{dataField:'catalogItem',caption:'Hàng hóa / Thiết bị / Khoản mục'},{dataField:'description',caption:'Quy cách / Diễn giải'},{dataField:'unit',caption:'ĐVT'},number('quantity','Số lượng'),number('unitPrice','Đơn giá'),number('lineTotal','Thành tiền nguyên tệ'),{dataField:'currency',caption:'Loại tiền'},number('exchangeRate','Tỷ giá đã lưu'),number('amountVnd','Thành tiền (VND)'),{dataField:'note',caption:'Ghi chú'}];
    function columns() {const template=$('#reportTemplate').val(); return (mode!=='items'?requestColumns:itemColumns).map(column=>({...column,...(column.dataField==='exchangeRate'?{format:{type:'fixedPoint',precision:6}}:{}),visible:!template || !column.templates || column.templates.includes(template)}));}
    function filterParams() {
        const params=new URLSearchParams();
        [['templateCode','reportTemplate'],['departmentId','reportDepartment'],['employeeId','reportEmployee'],['status','reportStatus'],['from','reportFrom'],['to','reportTo']].forEach(([name,id])=>{const value=document.getElementById(id).value;if(value)params.set(name,value)});
        if($('#reportTemplate').val()==='PURCHASE' && $('#reportPartner').val())params.set('partnerId',$('#reportPartner').val());
        return params;
    }
    async function load(options) {
        lastOptions=options;
        const params=filterParams();
        ['skip','take','requireTotalCount','requireGroupCount','sort','filter','group','groupSummary','totalSummary'].forEach(key=>{if(options[key]!==undefined&&options[key]!==null)params.set(key,JSON.stringify(options[key]))});
        const response=await fetch('/api/request-reports/'+(mode==='items'?'items':'requests')+'?'+params);
        if(!response.ok)throw new Error('Không thể tải báo cáo. Vui lòng kiểm tra bộ lọc hoặc đăng nhập lại.');
        return response.json();
    }
    function summary() {return {totalItems:(mode!=='items'?['amountVnd','leaveDays','leaveDebitedDays','leaveReservedDays','tripDays','overtimeHours','overtimePersonHours','vehicleHours']:['amountVnd']).map(field=>({column:field,summaryType:'sum',valueFormat:{type:'fixedPoint',precision:2},displayFormat:'Tổng: {0}'}))};}
    async function refreshPurchaseSummary() {
        const epoch=++summaryEpoch;
        if(mode!=='purchase')return;
        const params=filterParams();params.delete('templateCode');
        try {
            const response=await fetch('/api/request-reports/purchase-summary?'+params);
            if(!response.ok)throw new Error('Không thể tải tổng quan mua hàng.');
            const data=await response.json();if(epoch!==summaryEpoch)return;
            const fmt=n=>Number(n||0).toLocaleString('vi-VN',{maximumFractionDigits:2});
            $('#purchaseCount').text(fmt(data.count));$('#purchaseTotal').text(fmt(data.totalVnd));
            $('#purchaseCompletedCount').text(fmt(data.completedCount));$('#purchaseCompletedTotal').text(fmt(data.completedVnd));
            const target=document.getElementById('purchaseSuppliers');target.replaceChildren();
            data.suppliers.forEach(s=>{const tr=document.createElement('tr');[s.name,fmt(s.count),fmt(s.totalVnd)].forEach(v=>{const td=document.createElement('td');td.textContent=v;tr.append(td)});target.append(tr)});
            if(!data.suppliers.length){const tr=document.createElement('tr'),td=document.createElement('td');td.colSpan=3;td.textContent='Không có đề nghị mua hàng phù hợp.';tr.append(td);target.append(tr)}
        } catch(e){if(epoch===summaryEpoch){['purchaseCount','purchaseTotal','purchaseCompletedCount','purchaseCompletedTotal'].forEach(id=>document.getElementById(id).textContent='—');document.getElementById('purchaseSuppliers').replaceChildren();error.textContent=e.message;error.hidden=false;}}
    }
    try {
        const [definitions,lookups]=await Promise.all(['/api/request-forms','/api/request-forms/lookups'].map(async url=>{const response=await fetch(url);if(!response.ok)throw new Error('Không thể tải bộ lọc báo cáo.');return response.json()}));
        definitions.forEach(x=>$('#reportTemplate').append(new Option(x.name,x.code)));
        lookups.departments.forEach(x=>$('#reportDepartment').append(new Option(x.name,x.id)));
        lookups.masterItems.filter(x=>x.category==='Partner').forEach(x=>$('#reportPartner').append(new Option(x.name+(x.isActive?'':' (ngừng sử dụng)'),x.id)));
        lookups.employees.forEach(x=>$('#reportEmployee').append(new Option(x.name+(x.empId?' ('+x.empId+')':''),x.id)));
        grid=$('#businessReportGrid').dxDataGrid({
            dataSource:new DevExpress.data.CustomStore({key:'id',load}),remoteOperations:true,columns:columns(),columnAutoWidth:true,showBorders:false,showColumnLines:false,rowAlternationEnabled:true,
            searchPanel:{visible:true,placeholder:'Tìm trong báo cáo...'},filterRow:{visible:true},columnChooser:{enabled:true},groupPanel:{visible:true,emptyPanelText:'Kéo cột vào đây để nhóm số liệu'},
            paging:{pageSize:20},pager:{showInfo:true,showPageSizeSelector:true,allowedPageSizes:[20,50,100]},summary:summary(),noDataText:'Không có dữ liệu phù hợp với bộ lọc báo cáo.'
        }).dxDataGrid('instance');
        $('#reportTemplate,#reportDepartment,#reportEmployee,#reportPartner,#reportStatus,#reportFrom,#reportTo').on('change',()=>{
            const from=document.getElementById('reportFrom'),to=document.getElementById('reportTo');to.min=from.value;to.setCustomValidity(from.value&&to.value&&to.value<from.value?'Ngày kết thúc phải từ ngày bắt đầu trở đi.':'');if(!to.reportValidity())return;
            document.getElementById('reportPartnerFilter').hidden=$('#reportTemplate').val()!=='PURCHASE';
            error.hidden=true;
            grid.beginUpdate();grid.option('columns',columns());grid.option('summary',summary());grid.pageIndex(0);grid.endUpdate();grid.refresh();
            refreshPurchaseSummary();
        });
        $('[data-report-mode]').on('click',function(){mode=this.dataset.reportMode;if(mode==='purchase')$('#reportTemplate').val('PURCHASE');document.getElementById('reportTemplate').disabled=mode==='purchase';document.getElementById('reportPartnerFilter').hidden=$('#reportTemplate').val()!=='PURCHASE';document.getElementById('purchaseSummary').hidden=mode!=='purchase';$('[data-report-mode]').removeClass('is-active').attr('aria-pressed','false');$(this).addClass('is-active').attr('aria-pressed','true');grid.beginUpdate();grid.clearFilter();grid.clearGrouping();grid.option('columns',columns());grid.option('summary',summary());grid.pageIndex(0);grid.endUpdate();grid.refresh();refreshPurchaseSummary();});
        $('#reportExport').on('click',async function(){
            this.disabled=true;error.hidden=true;
            try {
                const exportMode=mode; const visibleColumns=grid.getVisibleColumns().filter(x=>x.dataField);
                const result=await load({...lastOptions,skip:0,take:10001,group:undefined,groupSummary:undefined,requireTotalCount:true});
                if(result.totalCount>10000)throw new Error('Báo cáo có hơn 10.000 dòng. Vui lòng thu hẹp bộ lọc trước khi xuất.');
                const text=value=>{let valueText=String(value??'');if(/^[\s]*[=+@-]/.test(valueText))valueText="'"+valueText;return '"'+valueText.replace(/"/g,'""')+'"'};
                const lines=[visibleColumns.map(x=>text(x.caption)).join(',')];
                result.data.forEach(row=>lines.push(visibleColumns.map(column=>{let value=row[column.dataField];if(column.dataField==='status')value=statuses.find(x=>x.id===value)?.name||value;if(column.dataType==='boolean')value=value?'Có':'Không';return text(value)}).join(',')));
                const url=URL.createObjectURL(new Blob(['\ufeff'+lines.join('\r\n')],{type:'text/csv;charset=utf-8;'}));const link=document.createElement('a');link.href=url;link.download='bao-cao-yeu-cau-'+exportMode+'.csv';link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
            }catch(exception){error.textContent=exception.message;error.hidden=false;}finally{this.disabled=false;}
        });
    } catch(exception){error.textContent=exception.message;error.hidden=false;}
});




