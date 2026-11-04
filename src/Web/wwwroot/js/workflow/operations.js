$(async function () {
    'use strict';
    const $id = id => document.getElementById(id), app = $id('operationsApp'); if (!app) return;
    app.inert = true;
    const esc = x => String(x ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const today = new Date(Date.now() + 7 * 3600000).toISOString().slice(0, 10);
    const dateString = d => `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    const date = v => v ? String(v).slice(0,10).split('-').reverse().join('/') : '—';
    const stamp = v => v ? date(v) + (String(v).length > 10 ? ' ' + String(v).slice(11,16) : '') : '—';
    const utcStamp = v => v ? new Date(String(v).endsWith('Z') ? v : v+'Z').toLocaleString('vi-VN', {timeZone:'Asia/Bangkok'}) : '—';
    const number = v => v == null ? '—' : Number(v).toLocaleString('vi-VN', {maximumFractionDigits:2});
    const eventKinds = {1:'Công tác',2:'Nghỉ phép',3:'Chuyến xe tham gia',4:'Lái xe',5:'Làm thêm giờ'};
    const statusNames = ['Chờ điều xe','Đã phân công','Đang thực hiện','Hoàn thành','Đã hủy chuyến'];
    let lookups, settings, tab='calendar', calendar=[], dispatches=[], balances=[], agenda=false, modalSave, modalBusy=false, dialogWidgets=[], calendarEmployee, leaveEmployee;
    let settingsWidgets={}, calendarEpoch=0, dispatchEpoch=0, leaveEpoch=0, settingsEpoch=0, availabilityEpoch=0;
    const query = new URLSearchParams(location.search); let sourceRequest = Number(query.get('request')) || null;
    const params = object => new URLSearchParams(Object.entries(object).filter(([,v]) => v !== '' && v != null)).toString();
    async function api(path, method, body) {
        const r = await fetch('/api/operations/' + path, method ? {method, headers:{'Content-Type':'application/json'}, body: body === undefined ? undefined : JSON.stringify(body)} : undefined);
        const content = r.status === 204 ? null : await r.json().catch(()=>({}));
        if (!r.ok) throw new Error(content?.message || content?.detail || Object.values(content?.errors || {}).flat().join(' ') || 'Không thể thực hiện thao tác. Hãy tải lại và thử lại.');
        return content;
    }
    const showError = message => {$id('opError').textContent=message || '';$id('opError').hidden=!message;};
    const notice = message => {$id('opNotice').textContent=message || '';$id('opNotice').hidden=!message;};
    async function task(operation) {showError('');try{return await operation();}catch(e){showError(e.message);}}
    function select(container, options, registry) {
        const existing = DevExpress.ui.dxSelectBox.getInstance(container);
        if (existing) {existing.option(options);return existing;}
        // Native modal dialogs live in the browser top layer. Mount their dropdowns
        // inside the dialog so the list is not hidden behind the modal backdrop.
        const result=$(container).dxSelectBox({searchEnabled:true,noDataText:'Chưa có dữ liệu phù hợp',placeholder:'Tìm và chọn...',width:'100%',dropDownOptions:{maxHeight:300,...(registry ? {container:$id('opDialog')} : {})},...options}).dxSelectBox('instance');
        registry?.push(result);return result;
    }
    function kpis(id, rows) {$id(id).innerHTML=rows.map(r=>`<article class="op-kpi"><span class="op-kpi-icon ${r[3]||''}"><i class="mdi mdi-${r[2]}" aria-hidden="true"></i></span><div><small>${esc(r[0])}</small><strong>${number(r[1])}</strong></div></article>`).join('');}
    function monthBounds() {
        const value=$id('opMonth').value;
        if (!/^\d{4}-\d{2}$/.test(value)) throw new Error('Chọn tháng hợp lệ.');
        const [year,month]=value.split('-').map(Number); if(year<2000||year>2200)throw new Error('Chọn năm từ 2000 đến 2200.');
        const first=new Date(year,month-1,1),last=new Date(year,month,0),start=new Date(first),end=new Date(last);
        start.setDate(start.getDate()-((start.getDay()+6)%7));end.setDate(end.getDate()+(6-((end.getDay()+6)%7)));
        return {first,last,start,end};
    }
    function sourceLink(row) {return row.requestId && row.canViewRequest ? `<a class="op-button" href="/Requests/Details/${row.requestId}">${esc(row.applicationNo)} ↗</a>` : `<span class="op-status">${esc(row.applicationNo || 'Lịch nội bộ')}</span>`;}
    function renderCalendar() {
        const bounds=monthBounds(), rows=calendar.filter(e=>e.startAt.slice(0,10)<=dateString(bounds.last)&&e.endAt>dateString(bounds.first)+'T00:00:00');
        kpis('opCalendarKpis', [['Lịch công tác',rows.filter(e=>e.kind===1).length,'airplane'],['Lịch nghỉ phép',rows.filter(e=>e.kind===2).length,'calendar-check','purple'],['Lịch lái xe',rows.filter(e=>e.kind===4).length,'car','green'],['Nhân sự có lịch',new Set(rows.map(e=>e.employeeId)).size,'account-multiple-outline','amber']]);
        let html='<div class="op-month-grid">'+['Thứ Hai','Thứ Ba','Thứ Tư','Thứ Năm','Thứ Sáu','Thứ Bảy','Chủ Nhật'].map(d=>`<div class="op-weekday">${d}</div>`).join('');
        for(let day=new Date(bounds.start);day<=bounds.end;day.setDate(day.getDate()+1)) {
            const key=dateString(day),next=new Date(day);next.setDate(next.getDate()+1);
            const events=calendar.filter(e=>e.startAt<dateString(next)&&e.endAt>key+'T00:00:00');
            html+=`<div class="op-day ${day.getMonth()!==bounds.first.getMonth()?'is-outside':''} ${key===today?'is-today':''}"><span class="op-date">${day.getDate()}</span>`+
                events.slice(0,3).map(e=>`<button type="button" class="op-event kind-${e.kind}" data-event="${e.id}" title="${esc(e.employee+' · '+e.title)}"><span>${esc(e.allDay?'':e.startAt.slice(11,16)+' ')}${esc(e.employee)} · ${esc(e.title)}</span></button>`).join('')+
                (events.length>3?`<button type="button" class="op-more" data-day="${key}">+ ${events.length-3} lịch khác</button>`:'')+'</div>';
        }
        $id('opCalendarGrid').innerHTML=html+'</div>';
        $id('opAgenda').innerHTML=rows.length?rows.map(e=>`<article class="op-agenda-row"><div class="op-agenda-date">${date(e.startAt)}<br/><small>${e.allDay?'Cả ngày':e.startAt.slice(11,16)+' – '+e.endAt.slice(11,16)}</small></div><b class="op-dot ${['','op-trip','op-leave','op-passenger','op-drive','op-overtime'][e.kind]}" style="margin-top:5px"></b><div class="op-agenda-info"><strong>${esc(e.title)}</strong><p>${esc(e.employee)} · ${esc(eventKinds[e.kind])}${e.location?' · '+esc(e.location):''}</p></div>${sourceLink(e)}</article>`).join(''):'<div class="op-empty">Chưa có lịch trong tháng đã chọn.<br/>Các lịch được ghi nhận khi yêu cầu hoàn tất phê duyệt.</div>';
        $id('opCalendarGrid').hidden=agenda;$id('opAgenda').hidden=!agenda;
    }
    async function loadCalendar() {
        const epoch=++calendarEpoch,b=monthBounds(); const data=await api('calendar?'+params({from:dateString(b.start),to:dateString(b.end),employeeId:calendarEmployee?.option('value'),kind:$id('opCalendarKind').value}));
        if(epoch!==calendarEpoch)return;calendar=data.events;renderCalendar();if(data.truncated)notice('Lịch có hơn 5.000 mục. Hãy chọn nhân viên hoặc loại lịch để thu hẹp dữ liệu.');
    }
    function calendarDialog(events, heading) {
        openDialog(heading,'LỊCH ĐÃ ĐƯỢC PHÊ DUYỆT',events.map(e=>`<div class="op-timeline-row"><small>${esc(eventKinds[e.kind])} · ${esc(e.employee)}</small><strong>${esc(e.title)}</strong><p>${stamp(e.startAt)} → ${stamp(e.endAt)}${e.allDay?' (kết thúc không bao gồm ngày cuối hiển thị)':''}</p><p>${esc(e.location)}</p>${sourceLink(e)}</div>`).join(''),null);
    }
    function dispatchActions(row) {
        const buttons=[];
        if(row.canAssign && [0,1].includes(row.status))buttons.push({text:row.status===0?'Điều xe':'Đổi phân công',onClick:()=>task(()=>assignDialog(row))});
        if((row.canAssign||row.canDrive)&&row.status===1)buttons.push({text:'Xuất phát',onClick:()=>dispatchActionDialog(row,2)});
        if((row.canAssign||row.canDrive)&&row.status===2)buttons.push({text:'Hoàn thành',onClick:()=>dispatchActionDialog(row,3)});
        if(row.canAssign && [0,1].includes(row.status))buttons.push({text:'Hủy chuyến',onClick:()=>dispatchActionDialog(row,4)});
        buttons.push({text:'Lịch sử',onClick:()=>task(async()=>{const history=await api(`dispatches/${row.requestId}/history`);openDialog('Lịch sử điều phối',row.applicationNo,history.map(h=>`<div class="op-timeline-row"><small>${utcStamp(h.at)} · ${esc(h.actor)}</small><strong>${esc(statusNames[h.previousStatus])} → ${esc(statusNames[h.newStatus])}</strong><p>${esc(h.note)}</p></div>`).join(''),null);})});
        if(row.driverEmployeeId)buttons.push({text:'Lịch tài xế',onClick:()=>{calendarEmployee.option('value',row.driverEmployeeId);$id('opMonth').value=row.startAt.slice(0,7);activate('calendar');}});
        return buttons;
    }
    function renderDispatches() {
        kpis('opDispatchKpis',[['Chờ điều xe',dispatches.filter(r=>r.status===0).length,'clock-outline','amber'],['Đã phân công',dispatches.filter(r=>r.status===1).length,'car'],['Đang thực hiện',dispatches.filter(r=>r.status===2).length,'map-marker-path','purple'],['Hoàn thành',dispatches.filter(r=>r.status===3).length,'check-circle-outline','green']]);
        const options={dataSource:dispatches,keyExpr:'requestId',showBorders:false,columnAutoWidth:true,wordWrapEnabled:true,rowAlternationEnabled:true,paging:{pageSize:10},pager:{visible:true,showInfo:true,showPageSizeSelector:true,allowedPageSizes:[10,25,50]},noDataText:'Chưa có yêu cầu xe đã duyệt trong khoảng thời gian này.',columns:[
            {dataField:'applicationNo',caption:'Yêu cầu',cellTemplate:(c,o)=>{$('<div>').text(o.data.applicationNo).css({color:'#2479e8',fontWeight:600}).appendTo(c);$('<small>').text(o.data.title).appendTo(c);}},
            {caption:'Lịch trình',cellTemplate:(c,o)=>{$('<div>').text(stamp(o.data.startAt)+' → '+stamp(o.data.endAt)).appendTo(c);$('<small>').text(o.data.pickupLocation+' → '+o.data.destination).appendTo(c);}},
            {dataField:'passengerCount',caption:'Số khách',width:75},
            {dataField:'vehicle',caption:'Xe thực tế',calculateDisplayValue:r=>r.useGrab?'Đi Grab':(r.vehicle||'Chưa phân công')},
            {dataField:'driver',caption:'Tài xế',cellTemplate:(c,o)=>{$('<span>').text(o.data.useGrab?'Tài xế Grab':(o.data.driver||'Chưa phân công')).appendTo(c);}},
            {dataField:'status',caption:'Trạng thái',cellTemplate:(c,o)=>$('<span>').addClass('op-status status-'+o.data.status).text(statusNames[o.data.status]).appendTo(c)},
            {caption:'Thao tác',minWidth:160,cellTemplate:(c,o)=>dispatchActions(o.data).forEach(b=>$('<button type="button">').addClass('op-button').css({margin:'3px',padding:'5px 8px',minHeight:28,fontSize:11}).text(b.text).on('click',b.onClick).appendTo(c))}
        ]};
        const instance=DevExpress.ui.dxDataGrid.getInstance($id('opDispatchGrid'));if(instance)instance.option(options);else $('#opDispatchGrid').dxDataGrid(options);
        $id('opClearRequest').hidden=!sourceRequest;
    }
    async function loadDispatches() {
        const epoch=++dispatchEpoch;
        const data=await api('dispatches?'+params({from:$id('opDispatchFrom').value,to:$id('opDispatchTo').value,status:$id('opDispatchStatus').value,requestId:sourceRequest}));
        if(epoch!==dispatchEpoch)return;dispatches=data.items;renderDispatches();if(data.truncated)notice('Có hơn 2.000 chuyến. Hãy thu hẹp khoảng ngày để xem và xuất đủ dữ liệu.');
    }
    function openDialog(title,eyebrow,html,save,saveText='Lưu thay đổi') {
        closeDialog();$id('opDialogTitle').textContent=title;$id('opDialogEyebrow').textContent=eyebrow||'';$id('opDialogBody').innerHTML=html;
        $id('opDialogError').hidden=true;modalSave=save;$id('opDialogSave').hidden=!save;$id('opDialogSave').textContent=saveText;$id('opDialog').showModal();
    }
    function closeDialog() {
        if(modalBusy)return;availabilityEpoch++;while(dialogWidgets.length)dialogWidgets.pop().dispose();modalSave=null;if($id('opDialog').open)$id('opDialog').close();
    }
    function dispatchActionDialog(row,status) {
        openDialog(statusNames[status],row.applicationNo,`<div class="op-dialog-note">${esc(row.pickupLocation)} → ${esc(row.destination)}<br/>${stamp(row.startAt)} – ${stamp(row.endAt)}<br/>${esc(row.vehicle||'Chưa chọn xe')} · ${esc(row.driver||'Chưa có tài xế')}</div><div class="op-dialog-fields"><label class="op-full">${status===4?'Lý do hủy chuyến *':'Ghi chú thực hiện'}<textarea name="notes" maxlength="1000" ${status===4?'required':''}></textarea></label></div>`,async()=>{await api(`dispatches/${row.requestId}/status`,'POST',{revision:row.revision,status,notes:$id('opDialogForm').elements.notes.value});await loadDispatches();notice('Đã cập nhật trạng thái chuyến xe.');},'Xác nhận '+statusNames[status].toLocaleLowerCase('vi'));
    }
    async function assignDialog(row) {
        openDialog(row.status===0?'Phân công xe & tài xế':'Điều chỉnh phân công',row.applicationNo,`<div class="op-dialog-note"><strong>${esc(row.title)}</strong><br/>${esc(row.pickupLocation)} → ${esc(row.destination)} · ${row.passengerCount} hành khách<br/>Liên hệ: ${esc(row.contact||'—')} · ${esc(row.contactPhone||'—')}<br/>Lịch được phê duyệt: ${stamp(row.startAt)} → ${stamp(row.endAt)}</div><div class="op-dialog-fields"><label class="op-full" style="flex-direction:row;align-items:center;gap:10px"><input style="width:18px;height:18px;accent-color:#1768ef" type="checkbox" name="useGrab" id="opUseGrab" ${row.useGrab?'checked':''} /> Đi Grab (không phân công xe và tài xế của trường)</label><div class="op-form-stack"><label id="opAssignVehicleLabel" for="opAssignVehicle">Xe thực tế *</label><select id="opAssignVehicle"><option value="">Chọn xe...</option></select></div><div class="op-form-stack"><label id="opAssignDriverLabel" for="opAssignDriver">Tài xế *</label><select id="opAssignDriver"><option value="">Chọn tài xế...</option></select></div><label>Khởi hành thực tế dự kiến *<input type="datetime-local" name="startAt" required value="${esc(row.startAt.slice(0,16))}" /></label><label>Kết thúc thực tế dự kiến *<input type="datetime-local" name="endAt" required value="${esc(row.endAt.slice(0,16))}" /></label><label class="op-full">Ghi chú điều phối<textarea name="notes" maxlength="1000">${esc(row.notes)}</textarea></label></div><div id="opAssignAvailability" class="op-availability" role="status">Chọn tài xế và thời gian để xem lịch liên quan.</div>`,async()=>{
            const form=$id('opDialogForm'),vehicle=Number(vehicleSelect.value)||null,driver=driverSelect.value||null,useGrab=form.elements.useGrab.checked;if(!useGrab&&(!vehicle||!driver))throw new Error('Chọn xe và tài xế từ danh sách đang hoạt động.');
            await api(`dispatches/${row.requestId}`,'PUT',{revision:row.revision,useGrab,vehicleId:useGrab?0:vehicle,driverEmployeeId:useGrab?null:driver,startAt:form.elements.startAt.value,endAt:form.elements.endAt.value,notes:form.elements.notes.value});
            await loadDispatches();notice(useGrab?'Đã lưu phương án đi Grab.':'Đã lưu phân công và ghi lịch làm việc cho tài xế.');
        },'Lưu phân công');
        const vehicleSelect=$id('opAssignVehicle'), driverSelect=$id('opAssignDriver');
        lookups.vehicles.filter(v=>v.isActive&&v.siteId===row.siteId&&v.parentId===row.vehicleTypeId).forEach(v=>{
            vehicleSelect.add(new Option(`${v.code} · ${v.name} (${v.capacity??'?'} chỗ)`,v.id));
        });
        lookups.drivers.filter(d=>d.isActive&&d.siteId===row.siteId).forEach(d=>driverSelect.add(new Option(d.name,d.id)));
        vehicleSelect.value=String(row.vehicleId||row.suggestedVehicleId||'');
        driverSelect.value=String(row.driverEmployeeId||row.suggestedDriverId||'');
        let availabilityTimer;
        function scheduleAvailability(){clearTimeout(availabilityTimer);availabilityEpoch++;availabilityTimer=setTimeout(()=>{if(vehicleSelect.isConnected && $id('opDialog').open)availability();},180);}
        vehicleSelect.addEventListener('change',scheduleAvailability);
        driverSelect.addEventListener('change',scheduleAvailability);
        async function availability() {
            const epoch=++availabilityEpoch,driver=driverSelect.value,vehicle=vehicleSelect.value,form=$id('opDialogForm');
            if(form.elements.useGrab.checked){$id('opAssignAvailability').classList.remove('is-busy');$id('opAssignAvailability').textContent='Đi Grab: không cần chọn xe hoặc tài xế của trường.';return;}
            const start=form.elements.startAt.value,end=form.elements.endAt.value;if(!start||!end||start>=end)return;
            const target=$id('opAssignAvailability');if(!target)return;target.textContent='Đang kiểm tra lịch...';
            if(!driver || !vehicle){target.classList.remove('is-busy');target.textContent='Chọn xe và tài xế để kiểm tra lịch.';return;}
            try {
                const [events,jobs]=await Promise.all([driver?api('calendar?'+params({from:start.slice(0,10),to:end.slice(0,10),employeeId:driver})):Promise.resolve({events:[]}),vehicle?api('dispatches?'+params({from:start.slice(0,10),to:end.slice(0,10),vehicleId:vehicle})):Promise.resolve({items:[]})]);
                if(epoch!==availabilityEpoch||!$id('opDialog').open)return;
                const busy=events.events.filter(e=>e.requestId!==row.requestId&&[1,2,4].includes(e.kind)&&e.startAt<end&&e.endAt>start);
                const occupied=jobs.items.filter(j=>j.requestId!==row.requestId&&[1,2,3].includes(j.status)&&j.startAt<end&&j.endAt>start);
                target.classList.toggle('is-busy',busy.length+occupied.length>0);
                target.innerHTML=busy.length+occupied.length?`<strong>Phát hiện lịch trùng — cần chọn lại xe, tài xế hoặc thời gian.</strong>${busy.map(e=>`<div>${esc(eventKinds[e.kind])}: ${stamp(e.startAt)} → ${stamp(e.endAt)}</div>`).join('')}${occupied.map(j=>`<div>Xe đã có chuyến ${esc(j.applicationNo)}: ${stamp(j.startAt)} → ${stamp(j.endAt)}</div>`).join('')}`:(events.truncated||jobs.truncated?'Chưa thể kiểm tra hết lịch. Hệ thống sẽ kiểm tra toàn bộ khi lưu.':'Chưa thấy lịch trùng trong khoảng đã chọn. Hệ thống kiểm tra lại dữ liệu tại thời điểm lưu.');
            }catch(e){if(epoch===availabilityEpoch)target.textContent=e.message;}
        }
        function toggleGrab(){const checked=$id('opUseGrab').checked;vehicleSelect.disabled=checked;driverSelect.disabled=checked;scheduleAvailability();}
        $id('opUseGrab').addEventListener('change',toggleGrab);toggleGrab();
        $id('opDialogForm').elements.startAt.addEventListener('change',scheduleAvailability);$id('opDialogForm').elements.endAt.addEventListener('change',scheduleAvailability);
    }
    function renderBalances() {
        $id('opLeaveCards').innerHTML=balances.map((r,i)=>{const b=r.balance,unknown=b.unclassifiedRequests>0;return `<article class="op-leave-card"><header><h3>${esc(r.leaveType)}</h3><span class="op-status ${!b.policyConfigured?'status-0':b.deductsBalance?'status-1':'status-3'}">${!b.policyConfigured?'Chưa thiết lập':b.deductsBalance?'Trừ phép tồn':'Không trừ phép'}</span></header><div class="op-leave-numbers"><div><span>Cấp trong năm</span><strong>${b.configured?number(b.annualDays):'—'}</strong></div><div><span>Chuyển từ năm trước</span><strong>${b.configured?number(b.carryOverDays):'—'}</strong></div><div><span>Điều chỉnh</span><strong>${number(b.adjustments)}</strong></div><div><span>Đã dùng</span><strong>${number(b.usedDays)}</strong></div><div><span>Đang giữ chỗ</span><strong>${number(b.reservedDays)}</strong></div><div><span>Còn lại</span><strong>${number(b.remainingDays)}</strong></div></div><div class="op-available"><span>Khả dụng ${unknown?'(tạm tính)':''}</span><strong>${b.deductsBalance&&b.configured?number(b.availableDays)+' ngày':b.policyConfigured&&!b.deductsBalance?'Không áp dụng':'Chưa cấp sổ'}</strong></div>${unknown?`<p class="op-warning">${b.unclassifiedRequests} lượt ghi nhận chưa có chính sách. HR cần đối soát dữ liệu cũ trước khi dùng số dư này.</p>`:''}<footer><button type="button" class="op-button" data-leave-action="history" data-balance="${i}">Lịch sử</button>${lookups.hrWrite?`<button type="button" class="op-button" data-leave-action="grant" data-balance="${i}">${b.configured?'Cập nhật cấp phép':'Cấp sổ phép'}</button>${b.configured?`<button type="button" class="op-button" data-leave-action="adjust" data-balance="${i}">Điều chỉnh ±</button>`:''}${unknown?`<button type="button" class="op-button" data-leave-action="reconcile" data-balance="${i}">Đối soát phép cũ</button>`:''}`:''}</footer></article>`;}).join('')||'<div class="op-empty">Chưa có loại nghỉ phép trong danh mục.</div>';
    }
    async function loadBalances() {const epoch=++leaveEpoch,data=await api('leave/balances?'+params({employeeId:leaveEmployee.option('value'),year:$id('opLeaveYear').value}));if(epoch===leaveEpoch){balances=data;renderBalances();}}
    async function leaveAction(action,row) {
        const b=row.balance,context=`${row.leaveType} · Năm ${b.year}`,key={employeeId:b.employeeId,year:b.year,leaveTypeId:b.leaveTypeId};
        if(action==='history') {
            const data=await api('leave/history?'+params(key));
            openDialog('Lịch sử sổ phép',context,`<h3 style="font-size:14px">Yêu cầu nghỉ liên quan</h3><div class="op-timeline">${data.requests.map(r=>`<div class="op-timeline-row"><strong>${esc(r.applicationNo)} · ${number(r.days)} ngày</strong><p>${date(r.startAt)} – ${date(r.endAt)} · ${['Đang giữ chỗ','Đã ghi nhận sử dụng','Đã giải phóng'][r.status]}${!r.policyConfigured?' · Chưa có chính sách':r.deductsBalance?' · Trừ phép':' · Không trừ phép'}</p>${r.classifiedAt?`<p>Đối soát ${utcStamp(r.classifiedAt)} · ${esc(r.classifiedBy)}: ${number(r.previousDays)} → ${number(r.days)} ngày. ${esc(r.classificationNote)}</p>`:''}${sourceLink(r)}</div>`).join('')||'<p>Chưa có yêu cầu nghỉ.</p>'}</div><h3 style="font-size:14px;margin-top:23px">Cấp phép & điều chỉnh</h3><div class="op-timeline">${data.adjustments.map(a=>`<div class="op-timeline-row"><small>${utcStamp(a.at)} · ${esc(a.actor)}</small><strong>${a.days?number(a.days)+' ngày':'Cập nhật cấp phép'}</strong><p>${esc(a.reason)}</p></div>`).join('')||'<p>Chưa có giao dịch.</p>'}</div>`,null);return;
        }
        const adjustmentId=crypto.randomUUID();
        const body=action==='grant'?`<div class="op-dialog-fields"><label>Phép cấp trong năm (ngày)<input type="number" name="annual" min="0" max="366" step="0.5" value="${b.annualDays}" required /></label><label>Phép chuyển từ năm trước (ngày)<input type="number" name="carry" min="0" max="366" step="0.5" value="${b.carryOverDays}" required /></label><label class="op-full">Lý do / Căn cứ cấp phép *<textarea name="reason" maxlength="800" required></textarea></label></div>`:action==='adjust'?`<div class="op-dialog-note">Nhập số dương để cộng thêm, số âm để giảm phép. Thao tác được ghi nhật ký, không thay đổi các yêu cầu đã duyệt.</div><div class="op-dialog-fields"><label>Số ngày điều chỉnh *<input type="number" name="days" min="-732" max="732" step="0.5" required /></label><label class="op-full">Lý do điều chỉnh *<textarea name="reason" maxlength="800" required></textarea></label></div>`:`<div class="op-warning">Sẽ tính lại các lượt đã duyệt chưa được phân loại theo lịch làm việc và chính sách loại nghỉ hiện tại. Các lượt đã có chính sách được giữ nguyên.</div><div class="op-dialog-fields"><label class="op-full">Căn cứ đối soát *<textarea name="reason" maxlength="500" required></textarea></label></div>`;
        openDialog(action==='grant'?'Cấp phép theo năm':action==='adjust'?'Điều chỉnh phép':'Đối soát dữ liệu phép cũ',context,body,async()=>{
            const f=$id('opDialogForm');if(action==='grant')await api('leave/accounts','PUT',{...key,revision:b.revision,annualDays:Number(f.elements.annual.value),carryOverDays:Number(f.elements.carry.value),reason:f.elements.reason.value});
            else if(action==='adjust')await api('leave/adjustments','POST',{...key,id:adjustmentId,revision:b.revision,days:Number(f.elements.days.value),reason:f.elements.reason.value});
            else await api('leave/reconcile','POST',{...key,reason:f.elements.reason.value});
            await loadBalances();notice('Đã cập nhật sổ phép và lưu lịch sử thao tác.');
        });
    }
    async function refreshLookups() {lookups=await api('lookups');}
    function resourceLists() {
        const site=Number($id('opSite').value);
        $id('opDriverList').innerHTML=lookups.drivers.filter(d=>d.siteId===site).map(d=>`<div class="op-resource-row"><span>${esc(d.name)}<small>${d.isActive?'Đang nhận phân công':'Ngừng nhận phân công'}</small></span><button type="button" class="op-button" data-driver-toggle="${esc(d.id)}" data-active="${!d.isActive}">${d.isActive?'Tạm ngừng':'Kích hoạt'}</button></div>`).join('')||'<p>Chưa đăng ký tài xế.</p>';
        $id('opFleetList').innerHTML=lookups.vehicles.filter(v=>v.siteId===site).map(v=>`<div class="op-resource-row"><span>${esc(v.code)} · ${esc(v.name)}<small>${v.capacity??'?'} chỗ · ${v.isActive?'Đang hoạt động':'Ngừng sử dụng'}</small></span><button type="button" class="op-button" data-vehicle-toggle="${v.id}" data-active="${!v.isActive}">${v.isActive?'Tạm ngừng':'Kích hoạt'}</button></div>`).join('')||'<p>Chưa đăng ký xe cho cơ sở này.</p>';
        settingsWidgets.driver=select($id('opDriverPerson'),{dataSource:lookups.employees.filter(p=>p.siteId===site),valueExpr:'id',displayExpr:'name',value:null,inputAttr:{'aria-labelledby':'opDriverPersonLabel'}});
        settingsWidgets.vehicle=select($id('opFleetVehicle'),{dataSource:lookups.vehicleCatalog||[],valueExpr:'id',displayExpr:v=>v?v.code+' · '+v.name:'',value:null,inputAttr:{'aria-labelledby':'opFleetVehicleLabel'}});
    }
    async function loadSettings() {
        const epoch=++settingsEpoch,site=Number($id('opSite').value);if(!site)throw new Error('Chưa có cơ sở để thiết lập.');
        if(lookups.hrRead) {
            const data=await api('settings?'+params({siteId:site,year:$id('opSettingsYear').value}));if(epoch!==settingsEpoch)return;settings=data;
            $id('opWorkDays').innerHTML=['Chủ Nhật','Thứ Hai','Thứ Ba','Thứ Tư','Thứ Năm','Thứ Sáu','Thứ Bảy'].map((label,i)=>`<label><input type="checkbox" name="workDay" value="${1<<i}" ${settings.calendar&&(settings.calendar.workingDaysMask&(1<<i))?'checked':''} ${!lookups.hrWrite?'disabled':''} />${label}</label>`).join('');
            $id('opWorkPolicyForm').querySelector('button').disabled=!lookups.hrWrite;
            $id('opExceptionForm').querySelectorAll('input,select,button').forEach(x=>x.disabled=!lookups.hrWrite);
            $id('opExceptionList').innerHTML=data.exceptions.map(e=>`<div class="op-resource-row"><span>${date(e.date)} · ${esc(e.name)}<small>${e.isWorkingDay?'Ngày làm việc':'Ngày nghỉ'}</small></span>${lookups.hrWrite?`<button type="button" class="op-button" data-exception-delete="${e.date.slice(0,10)}">Xóa</button>`:''}</div>`).join('')||'<p>Chưa có ngoại lệ trong năm.</p>';
            $id('opLeaveRules').innerHTML=lookups.leaveTypes.map(t=>{const rule=data.leaveRules.find(r=>r.leaveTypeId===t.id);return `<div class="op-resource-row"><span>${esc(t.name)}<small>${rule?'Phiên bản '+rule.revision:'Chưa cấu hình — chưa kiểm soát hạn mức'}</small></span><select data-leave-rule="${t.id}" aria-label="Cách tính ${esc(t.name)}" ${!lookups.hrWrite||!t.isActive?'disabled':''}><option value="" ${!rule?'selected':''}>Chưa thiết lập</option><option value="true" ${rule?.deductsBalance?'selected':''}>Trừ phép tồn</option><option value="false" ${rule&&!rule.deductsBalance?'selected':''}>Không trừ phép tồn</option></select></div>`;}).join('');
        }
        if(lookups.canDispatch)resourceLists();
    }
    async function activate(next) {
        if(!lookups){showError('Ch?a t?i ???c d? li?u v?n h?nh. Vui l?ng t?i l?i trang.');return;}
        if(next==='settings'&&!lookups.hrRead&&!lookups.canDispatch)next='calendar';tab=next;showError('');
        document.querySelectorAll('#sidebar [data-operations-tab]').forEach(link=>{
            const active=link.dataset.operationsTab===tab;link.closest('.nav-item')?.classList.toggle('active',active);
            if(active)link.setAttribute('aria-current','page');else link.removeAttribute('aria-current');
        });
        document.querySelectorAll('[data-op-tab]').forEach(button=>{const active=button.dataset.opTab===tab;button.setAttribute('aria-selected',String(active));button.tabIndex=active?0:-1;});
        ['calendar','dispatch','leave','settings'].forEach(t=>$id('op'+t[0].toUpperCase()+t.slice(1)).hidden=t!==tab);
        $id('opExport').hidden=tab==='settings';
        await task(()=>tab==='calendar'?loadCalendar():tab==='dispatch'?loadDispatches():tab==='leave'?loadBalances():loadSettings());
    }
    function download(name,content,type) {const url=URL.createObjectURL(new Blob([content],{type}));const a=document.createElement('a');a.href=url;a.download=name;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),1000);}
    function exportCsv() {
        let rows;
        if(tab==='calendar')rows=[['Nhân viên','Loại lịch','Tiêu đề','Bắt đầu','Kết thúc (không bao gồm)','Địa điểm','Mã yêu cầu'],...calendar.map(e=>[e.employee,eventKinds[e.kind],e.title,e.startAt,e.endAt,e.location,e.applicationNo])];
        else if(tab==='dispatch')rows=[['Mã yêu cầu','Tiêu đề','Bắt đầu','Kết thúc','Trạng thái','Xe thực tế','Tài xế','Số khách','Điểm đón','Điểm đến'],...dispatches.map(d=>[d.applicationNo,d.title,d.startAt,d.endAt,statusNames[d.status],d.useGrab?'Đi Grab':d.vehicle,d.useGrab?'Tài xế Grab':d.driver,d.passengerCount,d.pickupLocation,d.destination])];
        else rows=[['Loại nghỉ','Năm','Đã cấp sổ','Cấp trong năm','Chuyển năm trước','Điều chỉnh','Đã dùng','Giữ chỗ','Còn lại','Khả dụng','Chưa phân loại'],...balances.map(r=>{const b=r.balance;return[r.leaveType,b.year,b.configured?'Có':'Chưa cấp',b.annualDays,b.carryOverDays,b.adjustments,b.usedDays,b.reservedDays,b.remainingDays,b.availableDays,b.unclassifiedRequests]})];
        const cell=v=>'"'+String(v??'').replace(/^[\s]*[=+@-]/,"'$&").replace(/"/g,'""')+'"';download(`van-hanh-${tab}-${today}.csv`,'\uFEFF'+rows.map(row=>row.map(cell).join(',')).join('\r\n'),'text/csv;charset=utf-8');
    }
    function exportIcs() {
        const escapeIcs=v=>String(v??'').replace(/\\/g,'\\\\').replace(/\r?\n/g,'\\n').replace(/;/g,'\\;').replace(/,/g,'\\,');
        const instant=v=>v.slice(0,19).replace(/[-:]/g,'');
        const lines=['BEGIN:VCALENDAR','VERSION:2.0','PRODID:-//NMV Approval//Operations Calendar//VI','CALSCALE:GREGORIAN','BEGIN:VTIMEZONE','TZID:Asia/Bangkok','BEGIN:STANDARD','DTSTART:19700101T000000','TZOFFSETFROM:+0700','TZOFFSETTO:+0700','END:STANDARD','END:VTIMEZONE'];
        calendar.forEach(e=>lines.push('BEGIN:VEVENT',`UID:operations-${e.id}@nmv-approval`,'DTSTAMP:'+new Date().toISOString().replace(/[-:]/g,'').slice(0,15)+'Z',e.allDay?'DTSTART;VALUE=DATE:'+e.startAt.slice(0,10).replace(/-/g,''):'DTSTART;TZID=Asia/Bangkok:'+instant(e.startAt),e.allDay?'DTEND;VALUE=DATE:'+e.endAt.slice(0,10).replace(/-/g,''):'DTEND;TZID=Asia/Bangkok:'+instant(e.endAt),'SUMMARY:'+escapeIcs(e.employee+' · '+e.title),'LOCATION:'+escapeIcs(e.location),'DESCRIPTION:'+escapeIcs(eventKinds[e.kind]+' · '+(e.applicationNo||'')),'END:VEVENT'));
        lines.push('END:VCALENDAR');
        const folded=lines.flatMap(line=>{let chunk='',length=0,result=[];for(const c of line){const bytes=new TextEncoder().encode(c).length;if(length+bytes>73){result.push(chunk);chunk=' ';length=1;}chunk+=c;length+=bytes;}result.push(chunk);return result;});
        download(`lich-lam-viec-${$id('opMonth').value}.ics`,folded.join('\r\n')+'\r\n','text/calendar;charset=utf-8');
    }
    $id('opDialogForm').addEventListener('submit',async e=>{
        e.preventDefault();if(modalBusy||!modalSave)return;if(!$id('opDialogForm').reportValidity())return;
        modalBusy=true;$id('opDialogError').hidden=true;['opDialogSave','opDialogCancel','opDialogClose'].forEach(id=>$id(id).disabled=true);
        try {await modalSave();modalBusy=false;closeDialog();}catch(error){$id('opDialogError').textContent=error.message;$id('opDialogError').hidden=false;}
        finally {modalBusy=false;['opDialogSave','opDialogCancel','opDialogClose'].forEach(id=>$id(id).disabled=false);}
    });
    $id('opDialogCancel').onclick=closeDialog;$id('opDialogClose').onclick=closeDialog;$id('opDialog').addEventListener('cancel',e=>{if(modalBusy)e.preventDefault();else{e.preventDefault();closeDialog();}});
    document.querySelectorAll('[data-op-tab]').forEach(button=>{
        button.onclick=()=>activate(button.dataset.opTab);
        button.addEventListener('keydown',e=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;e.preventDefault();const list=[...document.querySelectorAll('[data-op-tab]')].filter(b=>!b.hidden);let index=list.indexOf(button);index=e.key==='Home'?0:e.key==='End'?list.length-1:(index+(e.key==='ArrowLeft'?-1:1)+list.length)%list.length;list[index].focus();activate(list[index].dataset.opTab);});
    });
    function shiftMonth(delta){const [y,m]=$id('opMonth').value.split('-').map(Number),next=new Date(y,m-1+delta,1);$id('opMonth').value=dateString(next).slice(0,7);task(loadCalendar);}
    $id('opPrevMonth').onclick=()=>shiftMonth(-1);$id('opNextMonth').onclick=()=>shiftMonth(1);$id('opToday').onclick=()=>{$id('opMonth').value=today.slice(0,7);task(loadCalendar);};
    $id('opMonth').onchange=()=>task(loadCalendar);$id('opCalendarKind').onchange=()=>task(loadCalendar);
    $id('opMonthView').onclick=()=>{agenda=false;$id('opMonthView').classList.add('is-active');$id('opAgendaView').classList.remove('is-active');$id('opMonthView').setAttribute('aria-pressed','true');$id('opAgendaView').setAttribute('aria-pressed','false');renderCalendar();};
    $id('opAgendaView').onclick=()=>{agenda=true;$id('opMonthView').classList.remove('is-active');$id('opAgendaView').classList.add('is-active');$id('opMonthView').setAttribute('aria-pressed','false');$id('opAgendaView').setAttribute('aria-pressed','true');renderCalendar();};
    $id('opCalendarGrid').onclick=e=>{const event=e.target.closest('[data-event]'),day=e.target.closest('[data-day]');if(event)calendarDialog(calendar.filter(x=>x.id===Number(event.dataset.event)),'Chi tiết lịch làm việc');if(day){const next=new Date(day.dataset.day+'T00:00:00');next.setDate(next.getDate()+1);calendarDialog(calendar.filter(x=>x.startAt<dateString(next)&&x.endAt>day.dataset.day+'T00:00:00'),'Lịch ngày '+date(day.dataset.day));}};
    $id('opDispatchRefresh').onclick=()=>task(loadDispatches);$id('opDispatchStatus').onchange=()=>task(loadDispatches);$id('opClearRequest').onclick=()=>{sourceRequest=null;task(loadDispatches);};
    $id('opLeaveRefresh').onclick=()=>task(loadBalances);$id('opLeaveYear').onchange=()=>task(loadBalances);
    $id('opLeaveCards').onclick=e=>{const button=e.target.closest('[data-leave-action]');if(button)task(()=>leaveAction(button.dataset.leaveAction,balances[Number(button.dataset.balance)]));};
    $id('opSite').onchange=()=>task(loadSettings);$id('opSettingsYear').onchange=()=>task(loadSettings);$id('opSettingsRefresh').onclick=()=>task(loadSettings);
    $id('opWorkPolicyForm').onsubmit=e=>{e.preventDefault();task(async()=>{const mask=[...$id('opWorkDays').querySelectorAll('input:checked')].reduce((s,x)=>s+Number(x.value),0);if(!mask)throw new Error('Chọn ít nhất một ngày làm việc.');await api('settings/calendar','PUT',{siteId:Number($id('opSite').value),workingDaysMask:mask,revision:settings.calendar?.revision||0});await loadSettings();notice('Đã lưu lịch làm việc. Yêu cầu đã gửi giữ nguyên số ngày đã ghi nhận.');});};
    $id('opExceptionForm').onsubmit=e=>{e.preventDefault();task(async()=>{const f=e.target;await api('settings/exceptions','PUT',{siteId:Number($id('opSite').value),date:f.elements.date.value,name:f.elements.name.value,isWorkingDay:f.elements.isWorkingDay.value==='true'});await loadSettings();notice('Đã lưu ngoại lệ lịch làm việc.');});};
    $id('opExceptionList').onclick=e=>{const b=e.target.closest('[data-exception-delete]');if(b)openDialog('Xóa ngoại lệ lịch làm việc',date(b.dataset.exceptionDelete),'<p>Ngày này sẽ được tính theo lịch làm việc trong tuần. Các yêu cầu đã gửi giữ nguyên số ngày đã ghi nhận.</p>',async()=>{await api(`settings/exceptions/${$id('opSite').value}/${b.dataset.exceptionDelete}`,'DELETE');await loadSettings();});};
    $id('opLeaveRules').onchange=e=>{const control=e.target.closest('[data-leave-rule]');if(!control)return;if(!control.value){task(loadSettings);return;}task(async()=>{const id=Number(control.dataset.leaveRule),rule=settings.leaveRules.find(r=>r.leaveTypeId===id);await api('settings/leave-rules','PUT',{siteId:Number($id('opSite').value),leaveTypeId:id,deductsBalance:control.value==='true',revision:rule?.revision||0});await loadSettings();notice('Đã lưu cách tính loại nghỉ. Dữ liệu đã ghi nhận giữ nguyên chính sách cũ.');});};
    $id('opDriverForm').onsubmit=e=>{e.preventDefault();task(async()=>{const person=settingsWidgets.driver.option('value');if(!person)throw new Error('Chọn nhân viên để đăng ký tài xế.');await api('resources/drivers','PUT',{employeeId:person,isActive:true});await refreshLookups();resourceLists();notice('Đã đăng ký tài xế.');});};
    $id('opFleetForm').onsubmit=e=>{e.preventDefault();task(async()=>{const vehicle=settingsWidgets.vehicle.option('value');if(!vehicle)throw new Error('Chọn xe từ danh mục.');await api('resources/vehicles','PUT',{vehicleId:vehicle,siteId:Number($id('opSite').value),isActive:true});await refreshLookups();resourceLists();notice('Đã đăng ký xe cho cơ sở.');});};
    $id('opDriverList').onclick=e=>{const b=e.target.closest('[data-driver-toggle]');if(b)task(async()=>{await api('resources/drivers','PUT',{employeeId:b.dataset.driverToggle,isActive:b.dataset.active==='true'});await refreshLookups();resourceLists();});};
    $id('opFleetList').onclick=e=>{const b=e.target.closest('[data-vehicle-toggle]');if(b)task(async()=>{await api('resources/vehicles','PUT',{vehicleId:Number(b.dataset.vehicleToggle),siteId:Number($id('opSite').value),isActive:b.dataset.active==='true'});await refreshLookups();resourceLists();});};
    $id('opExport').onclick=exportCsv;$id('opExportIcs').onclick=exportIcs;
    $id('opSync').onclick=()=>openDialog('Đồng bộ yêu cầu đã duyệt','GHI NHẬN DỮ LIỆU TRƯỚC KHI TRIỂN KHAI',`<div class="op-dialog-note">Đồng bộ tối đa 200 yêu cầu mỗi lần. Công tác/làm thêm được ghi lịch, nghỉ phép được ghi sổ, đặt xe được đưa vào hàng chờ điều phối.<br/>Nên cấu hình lịch làm việc và loại nghỉ trước khi đồng bộ phép cũ. Thao tác không thay đổi luồng hoặc kết quả phê duyệt.</div>`,async()=>{const result=await api('sync','POST',{});notice(`Đã đồng bộ ${result.synced} yêu cầu. ${result.remaining?`Còn ${result.remaining} yêu cầu; bấm đồng bộ lần nữa để tiếp tục.`:'Không còn yêu cầu phù hợp chờ đồng bộ.'}`);await activate(tab);},'Đồng bộ dữ liệu');
    try {
        await refreshLookups();
        $id('opMonth').value=today.slice(0,7);$id('opLeaveYear').value=today.slice(0,4);$id('opSettingsYear').value=today.slice(0,4);$id('opDispatchFrom').value=today.slice(0,7)+'-01';$id('opDispatchTo').value=dateString(new Date(Number(today.slice(0,4)),Number(today.slice(5,7)),0));
        $id('opSync').hidden=!lookups.hrWrite&&!lookups.canDispatch;$id('opTabSettings').hidden=!lookups.hrRead&&!lookups.canDispatch;$id('opHRSettings').hidden=!lookups.hrRead;$id('opTransportSettings').hidden=!lookups.canDispatch;
        $id('opSite').innerHTML=lookups.sites.map(s=>`<option value="${s.id}">${esc(s.name)}</option>`).join('');if(lookups.siteId&&lookups.sites.some(s=>s.id===lookups.siteId))$id('opSite').value=lookups.siteId;
        calendarEmployee=select($id('opCalendarEmployee'),{dataSource:[{id:null,name:'Tất cả nhân viên được phép xem'},...lookups.employees.filter(e=>lookups.hrRead||e.id===lookups.currentEmployeeId||e.isDriver)],valueExpr:'id',displayExpr:'name',value:lookups.currentEmployeeId,inputAttr:{'aria-labelledby':'opCalendarEmployeeLabel'},onValueChanged:()=>{if(calendarEmployee)task(loadCalendar);}});
        leaveEmployee=select($id('opLeaveEmployee'),{dataSource:lookups.employees.filter(e=>lookups.hrRead||e.id===lookups.currentEmployeeId),valueExpr:'id',displayExpr:'name',value:lookups.currentEmployeeId,inputAttr:{'aria-labelledby':'opLeaveEmployeeLabel'},onValueChanged:()=>{if(leaveEmployee)task(loadBalances);}});
        const employee=query.get('employee');if(employee&&lookups.employees.some(e=>e.id===employee))calendarEmployee.option('value',employee);
        const month=query.get('month');if(/^\d{4}-\d{2}$/.test(month||''))$id('opMonth').value=month;
        const requested=query.get('tab');await activate(['calendar','dispatch','leave','settings'].includes(requested)?requested:'calendar');
    }catch(e){showError(e.message);}finally{app.inert=false;app.setAttribute('aria-busy','false');}
});
