// ── Section Create / Edit popup ──
// Depends on: lookup-cache.js, user-picker.js
var SectionFormPopup = (function () {
  const _t = function (key) {
    return (window.smI18n && window.smI18n[key]) || key;
  };
  var apiUrl = "/api/admin/sections";
  var popupInstance = null;
  var formInstance = null;
  var _mode = "create";
  var _editingItem = null;
  var _onSaved = null;

  var belongSitesStore = [];
  var departmentsStore = [];

  function ensureLookups() {
    return Promise.all([
      LookupCache.getBelongSites(),
      LookupCache.getDepartments(),
    ]).then(function (results) {
      belongSitesStore = results[0];
      departmentsStore = results[1];
    });
  }

  function getFilteredDepartments(belongSiteId) {
    if (!belongSiteId) return departmentsStore;
    return departmentsStore.filter(function (d) {
      return d.belongSiteId === belongSiteId;
    });
  }

  function getFormInstance() {
    return formInstance;
  }

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
            validationRules: [
              { type: "required" },
              { type: "stringLength", max: 500 },
            ],
          },
          {
            dataField: "engName",
            label: { text: _t("lbl_eng_name") },
            editorOptions: { placeholder: _t("lbl_eng_name") },
            validationRules: [
              { type: "required" },
              { type: "stringLength", max: 500 },
            ],
          },
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "shortName",
            label: { text: _t("lbl_short_name") },
            editorOptions: {
              placeholder: _t("lbl_short_name"),
              //   readOnly: _mode === "create",
            },
            validationRules: [{ type: "stringLength", max: 200 }],
          },
        ],
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
              searchEnabled: true,
              onValueChanged: function (e) {
                if (!formInstance) return;
                var deptEditor = formInstance.getEditor("departmentId");
                if (deptEditor) {
                  deptEditor.option("items", getFilteredDepartments(e.value));
                  deptEditor.option("value", null);
                }
              },
            },
            validationRules: [{ type: "required" }],
          },
          {
            dataField: "departmentId",
            label: { text: _t("lbl_dept") },
            editorType: "dxSelectBox",
            editorOptions: {
              items: departmentsStore,
              valueExpr: "id",
              displayExpr: "name",
              placeholder: _t("lbl_dept"),
              searchEnabled: true,
            },
            validationRules: [{ type: "required" }],
          },
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          UserPicker.buildFormItem(getFormInstance, {
            dataField: "mainApprover",
            label: _t("lbl_main_approver"),
            validationRules: [{ type: "required" }],
          }),
          UserPicker.buildFormItem(getFormInstance, {
            dataField: "backupApprover",
            label: _t("lbl_backup_approver"),
          }),
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          UserPicker.buildFormItem(getFormInstance, {
            dataField: "pic",
            label: "PIC",
            multiple: true,
          }),
            UserPicker.buildFormItem(getFormInstance, {
              dataField: "deputyApprovers",
              label: "Deputy Approvers",
              multiple: true,
            }),
        ],
      },
      {
        dataField: "note",
        label: { text: _t("lbl_note") },
        editorType: "dxTextArea",
        editorOptions: {
          placeholder: _t("lbl_note"),
          height: 80,
        },
      },
    ];
  }

  function createPopup() {
    if (popupInstance) {
      popupInstance.dispose();
      $("#sectionFormPopup").remove();
    }

    $("body").append('<div id="sectionFormPopup"></div>');

    popupInstance = $("#sectionFormPopup")
      .dxPopup({
        title: _mode === "create" ? _t("create_title") : _t("edit_title"),
        width: "90vw",
        maxWidth: 1000,
        height: "auto",
        maxHeight: "90vh",
        showCloseButton: true,
        visible: true,
        dragEnabled: true,
        contentTemplate: function (container) {
          var $content = $("<div>").addClass("p-3");
          var $form = $("<div>");
          $content.append($form);

          var formData =
            _mode === "edit" && _editingItem ? $.extend({}, _editingItem) : {};

          formInstance = $form
            .dxForm({
              formData: formData,
              labelLocation: "top",
              items: buildFormItems(),
            })
            .dxForm("instance");

          // Set filtered department dropdown when editing
          if (_mode === "edit" && _editingItem) {
            setTimeout(function () {
              var deptEditor = formInstance.getEditor("departmentId");
              if (deptEditor && _editingItem.belongSiteId) {
                deptEditor.option(
                  "items",
                  getFilteredDepartments(_editingItem.belongSiteId),
                );
              }
            }, 100);
          }

          $content.append(
            $("<div>")
              .addClass("d-flex justify-content-end mt-3")
              .append(
                $("<div>")
                  .dxButton({
                    text: _t("btn_popup_cancel"),
                    stylingMode: "outlined",
                    onClick: function () {
                      popupInstance.hide();
                    },
                  })
                  .addClass("mr-2"),
                $("<div>").dxButton({
                  text: _mode === "create" ? _t("btn_popup_create") : _t("btn_popup_save"),
                  type: "default",
                  icon: _mode === "create" ? "plus" : "save",
                  onClick: submitForm,
                }),
              ),
          );

          container.append($content);
        },
        onHidden: function () {
          formInstance = null;
        },
      })
      .dxPopup("instance");
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

    var loadPanel = $("<div>")
      .appendTo("body")
      .dxLoadPanel({
        shadingColor: "rgba(0,0,0,0.4)",
        visible: true,
        showIndicator: true,
        showPane: true,
        message: _t("msg_saving"),
      })
      .dxLoadPanel("instance");

    fetch(url, {
      method: method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: formData.name,
        engName: formData.engName || "",
        note: formData.note || "",
        shortName: formData.shortName || "",
        departmentId: formData.departmentId,
        belongSiteId: formData.belongSiteId,
        mainApprover: formData.mainApprover || "",
        backupApprover: formData.backupApprover || "",
        pic: formData.pic || "",
        deputyApprovers: formData.deputyApprovers || "",
      }),
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
          "success",
          3000,
        );
        popupInstance.hide();
        LookupCache.invalidate("sections");
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
      ensureLookups().then(function () {
        createPopup();
      });
    },
    showEdit: function (rowData, onSaved) {
      _mode = "edit";
      _editingItem = rowData;
      _onSaved = onSaved;
      ensureLookups().then(function () {
        createPopup();
      });
    },
  };
})();
