
function formatDateTimeForServer(date) {
  if (!date) return "";

  if (typeof date === "string") {
    date = new Date(date);
  }

  const yyyy = date.getFullYear();
  const mm = (date.getMonth() + 1).toString().padStart(2, "0");
  const dd = date.getDate().toString().padStart(2, "0");
  const hh = date.getHours().toString().padStart(2, "0");
  const mi = date.getMinutes().toString().padStart(2, "0");
  const se = date.getSeconds().toString().padStart(2, "0");
  return `${yyyy}-${mm}-${dd}T${hh}:${mi}:${se}`;
}
// ── User Create / Edit popup ──
// Depends on: lookup-cache.js
var UserFormPopup = (function () {
  const _t = k => (window.umI18n && window.umI18n[k]) || k;
  var apiUrl = "/api/admin/users";
  var popupInstance = null;
  var formInstance = null;
  var avatarUploaderInstance = null;
  var _mode = "create"; // "create" | "edit"
  var _editingUser = null;
  var _onSaved = null;
  var _avatarFile = null;

  // Lookup data stores for dxSelectBox
  var belongSitesStore = [];
  var departmentsStore = [];
  var sectionsStore = [];

  function ensureLookups() {
    return Promise.all([
      LookupCache.getBelongSites(),
      LookupCache.getDepartments(),
      LookupCache.getSections(),
    ]).then(function (results) {
      belongSitesStore = results[0];
      departmentsStore = results[1];
      sectionsStore = results[2];
    });
  }

  function getFilteredDepartments(belongSiteId) {
    if (!belongSiteId) return departmentsStore;
    return departmentsStore.filter(function (d) {
      return d.belongSiteId === belongSiteId;
    });
  }

  function getFilteredSections(departmentId) {
    if (!departmentId) return sectionsStore;
    return sectionsStore.filter(function (s) {
      return s.departmentId === departmentId;
    });
  }

  function buildFormItems() {
    var items = [
      {
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "userName",
            label: { text: _t("umf_lbl_faxcode") },
            editorOptions: {
              readOnly: _mode === "edit",
              placeholder: _t("umf_lbl_faxcode"),
            },
            validationRules: [{ type: "required" }],
          },
          {
            dataField: "empId",
            label: { text: _t("umf_lbl_empid") },
            editorOptions: { placeholder: _t("umf_lbl_empid") },
            validationRules: [{ type: "required" }],
          },
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "fullName",
            label: { text: _t("umf_lbl_fullname") },
            editorOptions: { placeholder: _t("umf_lbl_fullname") },
            validationRules: [{ type: "required" }],
          },
          {
            dataField: "email",
            label: { text: _t("umf_lbl_email") },
            editorType: "dxTextBox",
            editorOptions: {
              mode: "email",
              placeholder: _t("umf_lbl_email"),
            },
            validationRules: [{ type: "email" }],
          },
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "phoneNumber",
            label: { text: _t("umf_lbl_phone") },
            editorType: "dxTextBox",
            editorOptions: { placeholder: _t("umf_lbl_phone") },
          },
          {
            dataField: "position",
            label: { text: _t("umf_lbl_position") },
            editorType: "dxTextBox",
            editorOptions: { placeholder: _t("umf_lbl_position") },
          },
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "dateOfBirth",
            label: { text: _t("umf_lbl_dob") },
            editorType: "dxDateBox",
            editorOptions: {
              displayFormat: "yyyy/MM/dd",
              type: "date",
              placeholder: _t("umf_lbl_dob"),
            },
          },
          {
            dataField: "belongSiteId",
            label: { text: _t("umf_lbl_site") },
            editorType: "dxSelectBox",
            editorOptions: {
              items: belongSitesStore,
              valueExpr: "id",
              displayExpr: "name",
              placeholder: _t("umf_lbl_site"),
              searchEnabled: true,
              onValueChanged: function (e) {
                if (!formInstance) return;
                var deptEditor = formInstance.getEditor("departmentId");
                var secEditor = formInstance.getEditor("sectionId");
                if (deptEditor) {
                  deptEditor.option("items", getFilteredDepartments(e.value));
                  deptEditor.option("value", null);
                }
                if (secEditor) {
                  secEditor.option("items", []);
                  secEditor.option("value", null);
                }
              },
            },
            validationRules: [{ type: "required" }],
          },
        ],
      },
      {
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "departmentId",
            label: { text: _t("umf_lbl_dept") },
            editorType: "dxSelectBox",
            editorOptions: {
              items: departmentsStore,
              valueExpr: "id",
              displayExpr: "name",
              placeholder: _t("umf_lbl_dept"),
              searchEnabled: true,
              onValueChanged: function (e) {
                if (!formInstance) return;
                var secEditor = formInstance.getEditor("sectionId");
                if (secEditor) {
                  secEditor.option("items", getFilteredSections(e.value));
                  secEditor.option("value", null);
                }
              },
            },
            validationRules: [{ type: "required" }],
          },
          {
            dataField: "sectionId",
            label: { text: _t("umf_lbl_section") },
            editorType: "dxSelectBox",
            editorOptions: {
              items: sectionsStore,
              valueExpr: "id",
              displayExpr: "name",
              placeholder: _t("umf_lbl_section"),
              searchEnabled: true,
            },
            validationRules: [{ type: "required" }],
          },
        ],
      },
    ];

    // Password fields only for create mode
    if (_mode === "create") {
      items.push({
        itemType: "group",
        colCount: 2,
        items: [
          {
            dataField: "password",
            label: { text: _t("umf_lbl_password") },
            editorOptions: { mode: "password", placeholder: _t("umf_lbl_password") },
            validationRules: [
              { type: "required" },
              {
                type: "pattern",
                pattern:
                  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{7,}$/,
                message: _t("umf_pwd_rules"),
              },
            ],
          },
          {
            dataField: "confirmPassword",
            label: { text: _t("umf_lbl_confirm_pwd") },
            editorOptions: {
              mode: "password",
              placeholder: _t("umf_lbl_confirm_pwd"),
            },
            validationRules: [
              { type: "required" },
              {
                type: "compare",
                comparisonTarget: function () {
                  return formInstance.option("formData").password;
                },
                message: _t("umf_pwd_mismatch"),
              },
            ],
          },
        ],
      });
    }

    // Avatar upload (custom drop zone 128x128)
    items.push({
      itemType: "group",
      colCount: 1,
      items: [
        {
          dataField: "avatar",
          label: { text: _t("umf_lbl_avatar") },
          template: function (data, element) {
            var $wrapper = $("<div>");

            // Drop zone area – fixed 128×128
            var $dropzone = $("<div>").addClass("avatar-dropzone").css({
              width: "128px",
              height: "128px",
              border: "2px dashed #ccc",
              borderRadius: "4px",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              cursor: "pointer",
              overflow: "hidden",
              position: "relative",
              backgroundSize: "cover",
              backgroundPosition: "center",
            });

            var placeholderHtml =
              '<div style="text-align:center;color:#999">' +
              '<i class="dx-icon-image" style="font-size:32px"></i><br>' +
              '<span style="font-size:11px">' + _t("umf_lbl_avatar") + '</span></div>';

            if (_mode === "edit" && _editingUser && _editingUser.avatar) {
              $dropzone.css(
                "backgroundImage",
                "url(" + _editingUser.avatar + ")",
              );
            } else {
              $dropzone.html(placeholderHtml);
            }

            $wrapper.append($dropzone);

            // Hidden file uploader bound to drop zone
            var $uploader = $("<div>");
            $wrapper.append($uploader);
            element.append($wrapper);

            avatarUploaderInstance = $uploader
              .dxFileUploader({
                dialogTrigger: $dropzone,
                dropZone: $dropzone,
                visible: false,
                accept: "image/*",
                multiple: false,
                uploadMode: "useForm",
                onValueChanged: function (e) {
                  var files = e.value;
                  _avatarFile = files && files.length > 0 ? files[0] : null;
                  $dropzone.empty().css("backgroundImage", "");
                  if (_avatarFile) {
                    var reader = new FileReader();
                    reader.onload = function (ev) {
                      $dropzone.css(
                        "backgroundImage",
                        "url(" + ev.target.result + ")",
                      );
                    };
                    reader.readAsDataURL(_avatarFile);
                  } else {
                    $dropzone.html(placeholderHtml);
                  }
                },
              })
              .dxFileUploader("instance");
          },
        },
      ],
    });

    return items;
  }

  function createPopup() {
    if (popupInstance) {
      popupInstance.dispose();
      $("#userFormPopup").remove();
    }

    $("body").append('<div id="userFormPopup"></div>');

    popupInstance = $("#userFormPopup")
      .dxPopup({
        title: _mode === "create" ? _t("umf_create_title") : _t("umf_edit_title"),
        width: "90vw",
        height: "auto",
        maxHeight: "90vh",
        maxWidth: 900,
        showCloseButton: true,
        visible: true,
        dragEnabled: true,
        contentTemplate: function (container) {
          var $content = $("<div>").addClass("p-3");

          var $form = $("<div>");
          $content.append($form);

          var formData =
            _mode === "edit" && _editingUser ? $.extend({}, _editingUser) : {};

          formInstance = $form
            .dxForm({
              formData: formData,
              labelLocation: "top",
              items: buildFormItems(),
            })
            .dxForm("instance");

          // Set filtered dropdowns when editing
          if (_mode === "edit" && _editingUser) {
            setTimeout(function () {
              var deptEditor = formInstance.getEditor("departmentId");
              if (deptEditor && _editingUser.belongSiteId) {
                deptEditor.option(
                  "items",
                  getFilteredDepartments(_editingUser.belongSiteId),
                );
              }
              var secEditor = formInstance.getEditor("sectionId");
              if (secEditor && _editingUser.departmentId) {
                secEditor.option(
                  "items",
                  getFilteredSections(_editingUser.departmentId),
                );
              }
            }, 100);
          }

          // Buttons
          $content.append(
            $("<div>")
              .addClass("d-flex justify-content-end mt-3")
              .append(
                $("<div>")
                  .dxButton({
                    text: _t("umf_btn_cancel"),
                    stylingMode: "outlined",
                    onClick: function () {
                      popupInstance.hide();
                    },
                  })
                  .addClass("mr-2"),
                $("<div>").dxButton({
                  text: _mode === "create" ? _t("umf_btn_create") : _t("umf_btn_save"),
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
          avatarUploaderInstance = null;
          _avatarFile = null;
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
      url = apiUrl + "/" + _editingUser.userName;
      method = "PUT";
    }

    var loadPanel = $("<div>")
      .appendTo("body")
      .dxLoadPanel({
        shadingColor: "rgba(0,0,0,0.4)",
        visible: true,
        showIndicator: true,
        showPane: true,
        message: _t("umf_msg_saving"),
      })
      .dxLoadPanel("instance");

    // Build FormData payload (supports avatar file upload)
    var fd = new FormData();
    fd.append("userName", formData.userName || "");
    fd.append("fullName", formData.fullName || "");
    if (formData.email) fd.append("email", formData.email);
    if (formData.phoneNumber) fd.append("phoneNumber", formData.phoneNumber);
    fd.append("empId", formData.empId || "");
    if (formData.position) fd.append("position", formData.position);
    if (formData.dateOfBirth)
      fd.append(
        "dateOfBirth",
        formatDateTimeForServer(new Date(formData.dateOfBirth)),
      );
    fd.append("belongSiteId", formData.belongSiteId);
    fd.append("sectionId", formData.sectionId);

    if (_mode === "create") {
      fd.append("password", formData.password || "");
    }

    if (_avatarFile) {
      fd.append("avatar", _avatarFile);
    }

    fetch(url, {
      method: method,
      body: fd,
    })
      .then(function (response) {
        return response.json().then(function (data) {
          return { ok: response.ok, data: data };
        });
      })
      .then(function (res) {
        loadPanel.hide();
        if (!res.ok) {
          var msg = _t("umf_msg_save_fail");
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
          _mode === "create"
            ? _t("umf_msg_created")
            : _t("umf_msg_updated"),
          "success",
          3000,
        );
        popupInstance.hide();
        if (_onSaved) _onSaved();
      })
      .catch(function () {
        loadPanel.hide();
        DevExpress.ui.notify(_t("uma_error"), "error", 4000);
      });
  }

  return {
    showCreate: function (onSaved) {
      _mode = "create";
      _editingUser = null;
      _onSaved = onSaved;
      ensureLookups().then(function () {
        createPopup();
      });
    },
    showEdit: function (userData, onSaved) {
      _mode = "edit";
      _editingUser = userData;
      _onSaved = onSaved;
      ensureLookups().then(function () {
        createPopup();
      });
    },
  };
})();
