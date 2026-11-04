// ── Department Create / Edit popup ──
// Depends on: lookup-cache.js, user-picker.js
var DepartmentFormPopup = (function () {
    const _t = k => (window.dmI18n && window.dmI18n[k]) || k;
    var apiUrl = "/api/admin/departments";
    var popupInstance = null;
    var formInstance = null;
    var _mode = "create";
    var _editingItem = null;
    var _onSaved = null;

    var belongSitesStore = [];

    function ensureLookups() {
        return Promise.all([
            LookupCache.getBelongSites()
        ]).then(function (results) {
            belongSitesStore = results[0];
        });
    }

    function getFormInstance() { return formInstance; }

    function buildFormItems() {
        return [
            {
                itemType: "group",
                colCount: 2,
                items: [
                    {
                        dataField: "name",
                        label: { text: _t("lbl_name") },
                        editorOptions: { placeholder: _t("lbl_name") },
                        validationRules: [{ type: "required" }, { type: "stringLength", max: 500 }]
                    },
                    {
                        dataField: "shortName",
                        label: { text: _t("lbl_short_name") },
                        editorOptions: { placeholder: _t("lbl_short_name") },
                        validationRules: [{ type: "stringLength", max: 300 }]
                    }
                ]
            },
            {
                itemType: "group",
                colCount: 2,
                items: [
                    {
                        dataField: "belongSiteId",
                        label: { text: _t("lbl_site") },
                        editorType: "dxSelectBox",
                        editorOptions: {
                            items: belongSitesStore,
                            valueExpr: "id",
                            displayExpr: "name",
                            placeholder: _t("lbl_site"),
                            searchEnabled: true
                        },
                        validationRules: [{ type: "required" }]
                    }
                ]
            },
            {
                itemType: "group",
                colCount: 2,
                items: [
                    UserPicker.buildFormItem(getFormInstance, { dataField: "mainApprover", label: _t("lbl_main_approver"), validationRules: [{ type: "required" }] }),
                    UserPicker.buildFormItem(getFormInstance, { dataField: "backupApprover", label: _t("lbl_backup_approver") })
                ]
            },
            {
                dataField: "description",
                label: { text: _t("lbl_description") },
                editorType: "dxTextArea",
                editorOptions: { placeholder: _t("lbl_description"), height: 80 }
            }
        ];
    }

    function createPopup() {
        if (popupInstance) {
            popupInstance.dispose();
            $("#departmentFormPopup").remove();
        }

        $("body").append('<div id="departmentFormPopup"></div>');

        popupInstance = $("#departmentFormPopup").dxPopup({
            title: _mode === "create" ? _t("create_title") : _t("edit_title"),
            width: 720,
            height: "auto",
            maxHeight: "90vh",
            showCloseButton: true,
            visible: true,
            dragEnabled: true,
            contentTemplate: function (container) {
                var $content = $("<div>").addClass("p-3");
                var $form = $("<div>");
                $content.append($form);

                var formData = _mode === "edit" && _editingItem ? $.extend({}, _editingItem) : {};

                formInstance = $form.dxForm({
                    formData: formData,
                    labelLocation: "top",
                    items: buildFormItems()
                }).dxForm("instance");

                $content.append(
                    $("<div>").addClass("d-flex justify-content-end mt-3").append(
                        $("<div>").dxButton({
                            text: _t("btn_popup_cancel"),
                            stylingMode: "outlined",
                            onClick: function () { popupInstance.hide(); }
                        }).addClass("mr-2"),
                        $("<div>").dxButton({
                            text: _mode === "create" ? _t("btn_popup_create") : _t("btn_popup_save"),
                            type: "default",
                            icon: _mode === "create" ? "plus" : "save",
                            onClick: submitForm
                        })
                    )
                );

                container.append($content);
            },
            onHidden: function () {
                formInstance = null;
            }
        }).dxPopup("instance");
    }

    function submitForm() {
        var validation = formInstance.validate();
        if (!validation.isValid) return;

        var formData = formInstance.option("formData");
        var url, method;

        if (_mode === "create") {
            url = apiUrl;
            method = "POST";
        } else {
            url = apiUrl + "/" + _editingItem.id;
            method = "PUT";
        }

        var loadPanel = $("<div>").appendTo("body").dxLoadPanel({
            shadingColor: "rgba(0,0,0,0.4)",
            visible: true,
            showIndicator: true,
            showPane: true,
            message: _t("msg_saving")
        }).dxLoadPanel("instance");

        fetch(url, {
            method: method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                name: formData.name,
                shortName: formData.shortName || "",
                description: formData.description || "",
                belongSiteId: formData.belongSiteId,
                mainApprover: formData.mainApprover || "",
                backupApprover: formData.backupApprover || ""
            })
        })
        .then(function (response) {
            return response.json().then(function (data) {
                return { ok: response.ok, data: data };
            });
        })
        .then(function (res) {
            loadPanel.hide();
            if (!res.ok) {
                var msg = _t("msg_save_fail");
                if (res.data && res.data.errors) {
                    msg = Array.isArray(res.data.errors)
                        ? res.data.errors.join("\n")
                        : JSON.stringify(res.data.errors);
                } else if (res.data && res.data.message) {
                    msg = res.data.message;
                }
                DevExpress.ui.notify(msg, "error", 4000);
                return;
            }

            DevExpress.ui.notify(
                _mode === "create" ? _t("msg_created") : _t("msg_updated"),
                "success", 3000
            );
            popupInstance.hide();
            LookupCache.invalidate("departments");
            if (_onSaved) _onSaved();
        })
        .catch(function () {
            loadPanel.hide();
            DevExpress.ui.notify(_t("msg_error"), "error", 4000);
        });
    }

    return {
        showCreate: function (onSaved) {
            _mode = "create";
            _editingItem = null;
            _onSaved = onSaved;
            ensureLookups().then(function () { createPopup(); });
        },
        showEdit: function (rowData, onSaved) {
            _mode = "edit";
            _editingItem = rowData;
            _onSaved = onSaved;
            ensureLookups().then(function () { createPopup(); });
        }
    };
})();
