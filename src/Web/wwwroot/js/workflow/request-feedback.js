(function () {
    'use strict';
    // SweetAlert2 is served locally by the shared layout.
    const dialog = Swal.mixin({
        customClass: { popup: 'wf-feedback-popup', confirmButton: 'wf-feedback-confirm', cancelButton: 'wf-feedback-cancel' },
        confirmButtonColor: '#0964ed', cancelButtonColor: '#edf2f8',
        confirmButtonText: 'Đã hiểu', cancelButtonText: 'Quay lại',
        reverseButtons: true, heightAuto: false
    });
    const actions = {
        approve: {
            title: 'Phê duyệt yêu cầu?', button: 'Xác nhận phê duyệt', icon: 'question', color: '#079751',
            description: 'Ý kiến phê duyệt của bạn sẽ được ghi vào lịch sử xử lý.',
            success: 'Phê duyệt thành công', message: 'Đã ghi nhận phê duyệt của bạn và cập nhật luồng xử lý.'
        },
        reject: {
            title: 'Từ chối yêu cầu?', button: 'Xác nhận từ chối', icon: 'warning', color: '#dc3545',
            description: 'Yêu cầu sẽ kết thúc và người đề nghị sẽ nhận được lý do từ chối.',
            success: 'Đã từ chối yêu cầu', message: 'Đã lưu lý do từ chối và cập nhật trạng thái yêu cầu.'
        },
        return: {
            title: 'Trả lại để bổ sung?', button: 'Trả lại yêu cầu', icon: 'warning', color: '#b77908',
            description: 'Người đề nghị sẽ nhận được ý kiến để chỉnh sửa và gửi lại yêu cầu.',
            success: 'Đã trả lại yêu cầu', message: 'Đã lưu ý kiến yêu cầu bổ sung vào lịch sử xử lý.'
        },
        cancel: {
            title: 'Hủy yêu cầu?', button: 'Xác nhận hủy', icon: 'warning', color: '#dc3545',
            description: 'Yêu cầu sẽ ngừng xử lý và được chuyển sang trạng thái đã hủy.',
            success: 'Đã hủy yêu cầu', message: 'Yêu cầu đã được hủy thành công.'
        },
        submit: {
            title: 'Gửi yêu cầu phê duyệt?', button: 'Gửi phê duyệt', icon: 'question', color: '#0964ed',
            description: 'Yêu cầu sẽ được chuyển đến người phê duyệt theo luồng đã gán.',
            success: 'Gửi yêu cầu thành công', message: 'Yêu cầu đã được gửi đến người phê duyệt.'
        }
    };
    window.requestFeedback = {
        async confirm(action, requestTitle, comment) {
            const config = actions[action];
            if (!config) throw new Error('Thao tác không được hỗ trợ.');
            const result = await dialog.fire({
                titleText: config.title, icon: config.icon,
                text: [requestTitle ? '“' + requestTitle + '”' : '', config.description, comment ? 'Ý kiến của bạn:\n' + comment : ''].filter(Boolean).join('\n\n'),
                showCancelButton: true, focusCancel: action === 'reject' || action === 'cancel',
                confirmButtonText: config.button, confirmButtonColor: config.color
            });
            return result.isConfirmed === true;
        },
        loading(title = 'Đang xử lý yêu cầu…') {
            void dialog.fire({
                titleText: title, text: 'Vui lòng chờ trong giây lát.',
                showConfirmButton: false, allowOutsideClick: false, allowEscapeKey: false,
                didOpen: popup => { if (Swal.getPopup() === popup) Swal.showLoading(); }
            });
        },
        success(title, message, button = 'Đã hiểu') {
            return dialog.fire({ titleText: title, text: message, icon: 'success', confirmButtonText: button, allowOutsideClick: false });
        },
        actionSuccess(action) {
            const config = actions[action];
            return this.success(config.success, config.message);
        },
        error(message) {
            return dialog.fire({ titleText: 'Chưa thể hoàn tất thao tác', text: message, icon: 'error', confirmButtonText: 'Quay lại kiểm tra' });
        }
    };
})();
