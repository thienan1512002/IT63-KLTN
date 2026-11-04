$(function () {
  const _t = function (key) {
    return (window.smI18n && window.smI18n[key]) || key;
  };
  var apiUrl = "/api/admin/sections";
  const allowManage =
    typeof window !== "undefined" && window.allowManage === true;
  var lastLoadOptions = {};

  // ── CustomStore with server-side filtering ──
  var sectionStore = new DevExpress.data.CustomStore({
    key: "id",
    load: function (loadOptions) {
      lastLoadOptions = AdminGridExport.captureLoadOptions(loadOptions);
      var params = new URLSearchParams();
      [
        "skip",
        "take",
        "requireTotalCount",
        "sort",
        "filter",
        "totalSummary",
      ].forEach(function (key) {
        if (
          key in loadOptions &&
          loadOptions[key] !== undefined &&
          loadOptions[key] !== null
        ) {
          params.set(key, JSON.stringify(loadOptions[key]));
        }
      });

      return fetch(apiUrl + "?" + params.toString()).then(function (response) {
        if (!response.ok) throw new Error("Load failed");
        return response.json();
      });
    },
  });

  function refreshGrid() {
    $("#gridContainer").dxDataGrid("instance").refresh();
  }

  // ── DataGrid ──
  $("#gridContainer").dxDataGrid({
    width: "100%",
    columnAutoWidth: true,
    dataSource: { store: sectionStore },
    remoteOperations: { filtering: true, sorting: true, paging: true },
    scrolling: {
      useNative: false,
      scrollByContent: true,
      scrollByThumb: true,
      showScrollbar: "onHover", // or "onScroll" | "always" | "never" | "onHover"
    },
    columns: [
      {
        caption: _t("col_no"),
        width: 55,
        alignment: "center",
        allowFiltering: false,
        allowSorting: false,
        cellTemplate: function (container, options) {
          var pageIndex = options.component.pageIndex();
          var pageSize = options.component.pageSize();
          $(container).text(pageIndex * pageSize + options.rowIndex + 1);
        },
      },
      {
        dataField: "name",
        caption: _t("col_name"),
        sortIndex: 0,
        sortOrder: "asc",
      },
      { dataField: "shortName", caption: _t("col_short_name"), width: 120 },
      { dataField: "belongSiteName", caption: _t("col_site"), width: 100 },
      { dataField: "departmentName", caption: _t("col_dept") },
      { dataField: "mainApprover", caption: _t("col_main_approver"), width: 140 },
      { dataField: "backupApprover", caption: _t("col_backup_approver"), width: 140 },
      { dataField: "pic", caption: _t("col_section_pic"), width: 180 },
      {
        dataField: "deputyApprovers",
        caption: _t("col_section_deputy"),
        width: 130,
        visible: true,
      },
      {
        caption: _t("col_action"),
        width: 160,
        alignment: "center",
        visible: allowManage,
        cellTemplate: function (container, options) {
          var $btn = $("<div>").appendTo(container);
          var rowData = options.data;
          var items = [
            { id: "edit", text: _t("action_edit"), icon: "edit" },
            { id: "copy", text: _t("action_copy"), icon: "copy" },
          ];

          $btn.dxDropDownButton({
            text: _t("col_action"),
            displayExpr: "text",
            keyExpr: "id",
            icon: "optionsgear",
            stylingMode: "text",
            width: "100%",
            dropDownOptions: { width: "auto", minWidth: 250 },
            items: items,
            onItemClick: function (e) {
              switch (e.itemData.id) {
                case "edit":
                  SectionFormPopup.showEdit(rowData, refreshGrid);
                  break;
                case "copy":
                  navigator.clipboard
                    .writeText(rowData.name)
                    .then(function () {
                      DevExpress.ui.notify(_t("copied"), "success", 2000);
                    })
                    .catch(function () {
                      DevExpress.ui.notify(_t("copy_failed"), "error", 2000);
                    });
                  break;
              }
            },
          });
        },
      },
      {
        dataField: "isActive",
        caption: _t("col_status"),
        dataType: "boolean",
        trueText: _t("status_active"),
        falseText: _t("status_inactive"),
        width: 80,
        visible: false,
      },
      {
        dataField: "createdAt",
        caption: _t("col_created"),
        dataType: "date",
        format: "yyyy/MM/dd",
        width: 110,
        visible: false,
      },
    ],
    showBorders: true,
    filterRow: { visible: true },
    headerFilter: { visible: false },
    paging: { pageSize: 15 },
    pager: {
      showPageSizeSelector: true,
      allowedPageSizes: [10, 15, 20, 50, 100],
      showInfo: true,
    },
    columnAutoWidth: true,
    allowColumnResizing: true,
    columnResizingMode: "widget",
    wordWrapEnabled: true,
    showRowLines: true,
    rowAlternationEnabled: true,
    onToolbarPreparing: function (e) {
      e.toolbarOptions.items.unshift(
        {
          location: "before",
          widget: "dxButton",
          options: {
            icon: "plus",
            visible: allowManage,
            text: _t("btn_create"),
            stylingMode: "contained",
            type: "default",
            onClick: function () {
              SectionFormPopup.showCreate(refreshGrid);
            },
          },
        },
        {
          location: "before",
          widget: "dxButton",
          options: {
            icon: "upload",
            visible: allowManage,
            stylingMode: "contained",
            text: _t("btn_import"),
            onClick: function () {
              importPopup.show();
            },
          },
        },
        {
          location: "before",
          widget: "dxButton",
          options: {
            icon: "exportxlsx",
            text: _t("btn_export_excel"),
            stylingMode: "contained",
            onClick: function () {
              AdminGridExport.exportExcel(apiUrl, lastLoadOptions, {
                exporting: _t("msg_exporting"),
                failed: _t("msg_export_fail"),
                fileName: "sections.xlsx",
              });
            },
          },
        },
      );
    },
  });

  // ── Import popup ──
  var $fileInput = null;
  var importPopup = $("#importPopup")
    .dxPopup({
      title: _t("import_title"),
      width: 480,
      height: "auto",
      showCloseButton: true,
      visible: false,
      contentTemplate: function (container) {
        var $content = $("<div>").addClass("p-3");

        $content.append(
          $("<div>")
            .addClass("mb-3")
            .append(
              $("<label>")
                .addClass("form-label font-weight-bold")
                .text(_t("import_select_file")),
              ($fileInput = $("<div>").dxFileUploader({
                selectButtonText: _t("import_select_file"),
                labelText: "",
                accept: ".xlsx,.xls",
                multiple: false,
                uploadMode: "useForm",
              })),
            ),
        );

        $content.append(
          $("<div>")
            .addClass("d-flex justify-content-end mt-3")
            .append(
              $("<div>")
                .dxButton({
                  text: _t("btn_cancel"),
                  stylingMode: "outlined",
                  onClick: function () {
                    importPopup.hide();
                  },
                })
                .addClass("mr-2"),
              $("<div>").dxButton({
                text: _t("btn_import_do"),
                type: "default",
                icon: "upload",
                onClick: doImport,
              }),
            ),
        );

        container.append($content);
      },
    })
    .dxPopup("instance");

  function doImport() {
    var fileUploader = $fileInput.dxFileUploader("instance");
    var files = fileUploader.option("value");

    if (!files || files.length === 0) {
      DevExpress.ui.notify(_t("msg_no_file"), "warning", 3000);
      return;
    }

    var formData = new FormData();
    formData.append("file", files[0]);

    var loadPanel = $("<div>")
      .appendTo("body")
      .dxLoadPanel({
        shadingColor: "rgba(0,0,0,0.4)",
        visible: true,
        showIndicator: true,
        showPane: true,
        message: _t("msg_importing"),
      })
      .dxLoadPanel("instance");

    fetch(apiUrl + "/import", {
      method: "POST",
      body: formData,
    })
      .then(function (response) {
        return response.json().then(function (data) {
          return { ok: response.ok, data: data };
        });
      })
      .then(function (res) {
        loadPanel.hide();
        if (!res.ok) {
          var errorMsg =
            res.data && res.data.message ? res.data.message : _t("msg_import_fail");
          DevExpress.ui.notify(errorMsg, "error", 4000);
          return;
        }

        var result = res.data;
        importPopup.hide();
        fileUploader.reset();

        var msg =
          `${_t("msg_import_completed")} ` +
          `${_t("msg_total")} ${result.totalRows} -` +
          `${_t("msg_succeeded")} ${result.succeeded} -` +
          `${_t("msg_failed_count")} ${result.failed}`;

        if (result.hasErrors && result.errors && result.errors.length > 0) {
          ImportErrors.show(result, msg);
        } else {
          DevExpress.ui.notify(msg, "success", 4000);
        }

        LookupCache.invalidate("sections");
        refreshGrid();
      })
      .catch(function () {
        loadPanel.hide();
        DevExpress.ui.notify(_t("msg_import_fail"), "error", 4000);
      });
  }
});
