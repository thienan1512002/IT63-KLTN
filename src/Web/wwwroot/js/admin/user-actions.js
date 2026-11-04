// ── User Actions: Reset Password, Lock Account ──
var UserActions = (function () {
    const _t = k => (window.umI18n && window.umI18n[k]) || k;
    var apiUrl = "/api/admin/users";

    function showResetPassword(userName, onDone) {
        var confirmResult = DevExpress.ui.dialog.confirm(
            _t("uma_reset_confirm") + " <b>" + $("<span>").text(userName).html() + "</b>?",
            _t("uma_reset_title")
        );

        confirmResult.done(function (confirmed) {
            if (!confirmed) return;

            fetch(apiUrl + "/" + encodeURIComponent(userName) + "/reset-password", {
                method: "POST",
                headers: { "Content-Type": "application/json" }
            })
            .then(function (response) {
                return response.json().then(function (d) { return { ok: response.ok, data: d }; });
            })
            .then(function (res) {
                if (!res.ok) {
                    var msg = (res.data && res.data.errors)
                        ? res.data.errors.join("\n")
                        : (res.data && res.data.message) || _t("uma_reset_failed");
                    DevExpress.ui.notify(msg, "error", 4000);
                    return;
                }
                showNewPasswordPopup(userName, res.data.newPassword);
                if (onDone) onDone();
            })
            .catch(function () {
                DevExpress.ui.notify(_t("uma_error"), "error", 4000);
            });
        });
    }

    function showNewPasswordPopup(userName, newPassword) {
        var popup = $("<div>").appendTo("body").dxPopup({
            title: _t("uma_new_pwd_title") + " \u2014 " + userName,
            width: 450,
            height: "auto",
            showCloseButton: true,
            visible: true,
            dragEnabled: true,
            contentTemplate: function (container) {
                var $content = $("<div>").addClass("p-3");

                $content.append(
                    $("<div>").addClass("alert alert-warning mb-3").html(
                        "<i class='fa fa-exclamation-triangle mr-1'></i> " +
                        _t("uma_new_pwd_warning")
                    )
                );

                var $inputGroup = $("<div>").addClass("input-group");
                var $input = $("<input>").attr({
                    type: "text",
                    readonly: true
                }).addClass("form-control font-weight-bold").val(newPassword);

                var $copyBtn = $("<div>");
                $copyBtn.dxButton({
                    icon: "copy",
                    text: _t("uma_btn_copy"),
                    type: "default",
                    onClick: function () {
                        navigator.clipboard.writeText(newPassword).then(function () {
                            DevExpress.ui.notify(_t("uma_copied"), "success", 2000);
                        });
                    }
                });

                $inputGroup.append($input);
                $content.append($inputGroup);
                $content.append($("<div>").addClass("mt-3 d-flex justify-content-end").append($copyBtn));

                container.append($content);
            }
        });
    }

    function toggleAccountStatus(userName, isActive, onDone) {
        var actionText = isActive ? "activate" : "deactivate";
        var title = isActive ? _t("uma_unlock_title") : _t("uma_lock_title");
        var confirmMsg = isActive 
            ? _t("uma_unlock_confirm") + " <b>" + $("<span>").text(userName).html() + "</b>?"
            : _t("uma_lock_confirm") + " <b>" + $("<span>").text(userName).html() + "</b>?";

        var result = DevExpress.ui.dialog.confirm(confirmMsg, title);

        result.done(function (confirmed) {
            if (!confirmed) return;

            fetch(apiUrl + "/" + encodeURIComponent(userName), {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ isActive: isActive })
            })
            .then(function (response) {
                return response.json().then(function (d) { return { ok: response.ok, data: d }; });
            })
            .then(function (res) {
                if (!res.ok) {
                    var msg = (res.data && res.data.message) ? res.data.message : _t("uma_op_failed");
                    DevExpress.ui.notify(msg, "error", 4000);
                    return;
                }
                DevExpress.ui.notify(res.data.message || _t("uma_status_changed"), "success", 3000);
                if (onDone) onDone();
            })
            .catch(function () {
                DevExpress.ui.notify(_t("uma_error"), "error", 4000);
            });
        });
    }

    function lockAccount(userName, onDone) {
        toggleAccountStatus(userName, false, onDone);
    }

    function unlockAccount(userName, onDone) {
        toggleAccountStatus(userName, true, onDone);
    }

    return {
        showResetPassword: showResetPassword,
        lockAccount: lockAccount,
        unlockAccount: unlockAccount
    };
})();
