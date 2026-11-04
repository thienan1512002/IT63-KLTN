/**
 * approver-setting.js
 * CRUD page for FormApproverSetting.
 *
 * Features:
 *   - Single dxDataGrid (local data, no server-side paging)
 *   - Add / Edit via dxPopup form
 *   - UserNames field: multi-select active-user picker grid
 */

import { useFetch } from "../../common/fetch-helper.js";

const API = "/api/workflow/approver-settings";
const USERS_API = "/api/admin/users/lookup";

const fetch_ = useFetch();

// ── i18n helper ───────────────────────────────────────────────────────────────
const i18n = window.approverI18n ?? {};
function t(key, fallback) { return i18n[key] || fallback || key; }

// ── State ─────────────────────────────────────────────────────────────────────
let _records = []; // local data for the main grid
let _users = []; // cached user list for the picker
let _editing = null; // { id, title, displayTitle, formCode, userNames } | null (create)
let _pickerSelected = []; // currently selected users in picker

let _mainGrid = null;
let _formPopup = null;
let _pickerPopup = null;
let _pickerGrid = null;

// ── Helpers ───────────────────────────────────────────────────────────────────
function notify(msg, type = "success") {
  DevExpress.ui.notify(msg, type, 3000);
}

function swalConfirm(text, onConfirm) {
  DevExpress.ui.dialog.confirm(text, t("confirm", "Confirm")).done(function (result) {
    if (result) onConfirm();
  });
}

// Pre-load user list (cached)
let _usersPromise = null;
function loadUsers() {
  if (!_usersPromise) {
    _usersPromise = fetch_.get(USERS_API).then((response) => response.data || response);
  }
  return _usersPromise;
}

// ── Main grid ─────────────────────────────────────────────────────────────────
async function initGrid() {
  _records = await fetch_.get(API);

  _mainGrid = $("#approverSettingGrid")
    .dxDataGrid({
      dataSource: _records,
      keyExpr: "id",
      showBorders: true,
      columnAutoWidth: true,
      allowColumnResizing: true,
      rowAlternationEnabled: true,
      paging: { enabled: false },
      scrolling: { mode: "standard" },
      searchPanel: { visible: true, width: 240, placeholder: t("searchPlaceholder", "Search…") },
      toolbar: {
        items: [
          {
            location: "before",
            widget: "dxButton",
            options: {
              icon: "plus",
              text: t("addNew", "Add New"),
              type: "default",
              onClick: () => openPopup(null),
            },
          },
          "searchPanel",
        ],
      },
      columns: [
        {
          caption: t("colNo", "No."),
          width: 55,
          alignment: "center",
          allowFiltering: false,
          allowSorting: false,
          cellTemplate(container, options) {
            $(container).text(options.rowIndex + 1);
          },
        },
        { dataField: "formCode", caption: t("colFormCode", "Form Code"), width: 140 },
        { dataField: "title", caption: t("colTitle", "Title") },
        { dataField: "displayTitle", caption: t("colDisplayTitle", "Display Title") },
        {
          dataField: "userNames",
          caption: t("colApprovers", "Approvers"),
          cellTemplate(container, options) {
            const names = (options.value || "")
              .split(",")
              .map((s) => s.trim())
              .filter(Boolean);
            const $wrap = $(
              '<div style="display:flex;flex-wrap:wrap;gap:4px;padding:2px 0;">',
            );
            if (names.length === 0) {
              $wrap.append($('<span class="text-muted fst-italic">—</span>'));
            } else {
              names.forEach((n) => {
                $wrap.append(
                  $(
                    `<span class="badge badge-light border" style="font-size:0.8rem;">${$("<span>").text(n).html()}</span>`,
                  ),
                );
              });
            }
            $(container).append($wrap);
          },
        },
        {
          caption: t("colActions", "Actions"),
          width: 130,
          alignment: "center",
          allowFiltering: false,
          allowSorting: false,
          cellTemplate(container, options) {
            const $edit = $("<div>").dxButton({
              icon: "edit",
              hint: t("popupEdit", "Edit"),
              stylingMode: "text",
              type: "default",
              onClick: () => openPopup(options.data),
            });
            const $del = $("<div style='margin-left:4px;'>").dxButton({
              icon: "trash",
              hint: t("deleted", "Delete"),
              stylingMode: "text",
              type: "danger",
              onClick: () => confirmDelete(options.data),
            });
            $(container)
              .css({
                // display: "flex",
                justifyContent: "center",
                alignItems: "center",
              })
              .append($edit)
              .append($del);
          },
        },
      ],
    })
    .dxDataGrid("instance");
}

// ── Delete ────────────────────────────────────────────────────────────────────
function confirmDelete(row) {
  const label = row.title || row.formCode;
  const msg = t("deleteConfirm", 'Delete approver setting "{0}"?').replace("{0}", label);
  swalConfirm(msg, async () => {
      try {
        await fetch_.del(`${API}/${row.id}`);
        notify(t("deleted", "Deleted successfully."));
        await refreshGrid();
      } catch (e) {
        notify(e.message || t("deleteFailed", "Delete failed."), "error");
      }
    },
  );
}

async function refreshGrid() {
  _records = await fetch_.get(API);
  _mainGrid.option("dataSource", _records);
  _mainGrid.refresh();
}

// ── Popup form ────────────────────────────────────────────────────────────────
function openPopup(row) { 
  _editing = row
    ? { ...row }
    : { id: null, title: "", displayTitle: "", formCode: "", userNames: "" };

  if (!_formPopup) {
    _formPopup = $("#approverSettingPopup")
      .dxPopup({
        width: 560,
        height: "auto",
        maxHeight: "90vh",
        showTitle: true,
        dragEnabled: true,
        hideOnOutsideClick: false,
        contentTemplate: buildFormContent,
        toolbarItems: [
          {
            toolbar: "bottom",
            location: "after",
            widget: "dxButton",
            options: {
              text: t("save", "Save"),
              type: "default",
              icon: "save",
              onClick: onSave,
            },
          },
          {
            toolbar: "bottom",
            location: "after",
            widget: "dxButton",
            options: {
              text: t("cancel", "Cancel"),
              stylingMode: "outlined",
              onClick: () =>
                {

                   _formPopup.hide()
                }
            },
          },
        ],
      })
      .dxPopup("instance");
  }

  const isCreate = !_editing.id;
  _formPopup.option(
    "title",
    isCreate ? t("popupAdd", "Add Approver Setting") : t("popupEdit", "Edit Approver Setting"),
  );

  // Re-render form content each time
  _formPopup.option("contentTemplate", null); // reset to force re-render
  _formPopup.option("contentTemplate", buildFormContent); 

  _formPopup.show();
}

function buildFormContent(contentEl) {
  const $c = $(contentEl).empty().css({ padding: "16px", overflowY: "auto" });

  // Form Code
  $c.append(
    fieldRow(
      t("colFormCode", "Form Code"),
      $("<div id='fld-formCode'>").dxTextBox({
        value: _editing.formCode,
        stylingMode: "outlined",
        onValueChanged: (e) => {
          _editing.formCode = e.value;
        },
      }),
    ),
  );

  // Title
  $c.append(
    fieldRow(
      t("colTitle", "Title"),
      $("<div id='fld-title'>").dxTextBox({
        value: _editing.title,
        stylingMode: "outlined",
        onValueChanged: (e) => {
          _editing.title = e.value;
        },
      }),
    ),
  );

  // Display Title
  $c.append(
    fieldRow(
      t("colDisplayTitle", "Display Title"),
      $("<div id='fld-displayTitle'>").dxTextBox({
        value: _editing.displayTitle,
        stylingMode: "outlined",
        onValueChanged: (e) => {
          _editing.displayTitle = e.value;
        },
      }),
    ),
  );

  // UserNames (multi-user picker)
  const $userRow = $('<div class="mb-3">');
  $userRow.append(
    $(`<label class="form-label fw-semibold">${t("fieldUsers", "Approver Users")}</label>`),
  );
  const $inputGroup = $('<div class="input-group">');

  const $userBox = $('<div style="flex:1;">').dxTextBox({
    value: _editing.userNames,
    stylingMode: "outlined",
    readOnly: true,
    placeholder: t("placeholderUsers", "Select users…"),
    elementAttr: { id: "fld-userNames" },
  });

  const $browseBtn = $('<div style="margin-left:4px;">').dxButton({
    text: t("btnBrowse", "Browse"),
    icon: "group",
    stylingMode: "outlined",
    onClick: () => openUserPicker(),
  });

  $inputGroup.append($userBox).append($browseBtn);
  $userRow.append($inputGroup);
  $c.append($userRow);
}

function fieldRow(label, $widget) {
  const $row = $('<div class="mb-3">');
  $row.append($(`<label class="form-label fw-semibold">${label}</label>`));
  $row.append($widget);
  return $row;
}

// ── User picker popup (multi-select) ─────────────────────────────────────────
async function openUserPicker() {
  // Load users if not yet cached
  if (_users.length === 0) {
    try {
      _users = await loadUsers();
    } catch (e) {
      notify(t("loadUsersFailed", "Failed to load user list."), "error");
      return;
    }
  }

  // Pre-select users that are already in _editing.userNames
  const currentNames = (_editing.userNames || "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);

  _pickerSelected = currentNames;

  if (!_pickerPopup) {
    _pickerPopup = $("#userPickerPopup")
      .dxPopup({
        title: t("pickerTitle", "Select Approver Users"),
        width: "min(820px, 96vw)",
        height: 520,
        showTitle: true,
        dragEnabled: true,
        hideOnOutsideClick: false,
        contentTemplate: buildPickerContent,
        toolbarItems: [
          {
            toolbar: "bottom",
            location: "after",
            widget: "dxButton",
            options: {
              text: t("confirm", "Confirm"),
              type: "default",
              icon: "check",
              onClick: onPickerConfirm,
            },
          },
          {
            toolbar: "bottom",
            location: "after",
            widget: "dxButton",
            options: {
              text: t("cancel", "Cancel"),
              stylingMode: "outlined",
              onClick: () => {                
                // Reset selection to current editing value on cancel
                _pickerSelected = currentNames;
                if (_pickerGrid) _pickerGrid.selectRows(_pickerSelected, false);
                _pickerPopup.hide();
              }
            },
          },
        ],
      })
      .dxPopup("instance");
  } else {
    // Re-render grid on each open so pre-selection is applied   
    _pickerPopup.option("contentTemplate", null); // reset to force re-render
    _pickerPopup.option("contentTemplate", buildPickerContent);
  }

  _pickerPopup.show();
}

function buildPickerContent(contentEl) {
  const $c = $(contentEl).empty().css({
    padding: "8px",
    height: "100%",
    display: "flex",
    flexDirection: "column",
  });

  const $grid = $("<div>")
    .css({ flex: "1", minHeight: 0 })
    .dxDataGrid({
      dataSource: _users,
      keyExpr: "userName",
      showBorders: true,
      height: "100%",
      columnAutoWidth: false,
      allowColumnResizing: true,
      selection: { mode: "multiple", showCheckBoxesMode: "always" },
      searchPanel: { visible: true, width: 240, placeholder: t("searchPlaceholder", "Search…") },
      paging: { enabled: false },
      scrolling: { mode: "virtual", useNative: true },
      selectedRowKeys: _pickerSelected,
      onSelectionChanged(e) {
        _pickerSelected = e.component.getSelectedRowKeys();
      },
      columns: [
        {
          caption: t("colNo", "No."),
          width: 50,
          alignment: "center",
          allowFiltering: false,
          allowSorting: false,
          cellTemplate(container, options) {
            $(container).text(options.rowIndex + 1);
          },
        },
        { dataField: "userName", caption: t("pickerUsername", "Username"), width: 140 },
        { dataField: "fullName", caption: t("pickerFullname", "Full Name"), minWidth: 140 },
        { dataField: "sectionName", caption: t("pickerSection", "Section"), minWidth: 120 },
        { dataField: "position", caption: t("pickerPosition", "Position"), minWidth: 100 },
        { dataField: "email", caption: t("pickerEmail", "Email"), minWidth: 160 },
      ],
    });

  _pickerGrid = $grid.dxDataGrid("instance");
  $c.append($grid);
}

function onPickerConfirm() {
  const joined = _pickerSelected.join(",");
  _editing.userNames = joined;

  // Update the read-only text box in the form
  const inst = $("#fld-userNames").dxTextBox("instance");
  if (inst) inst.option("value", joined);

  _pickerPopup.hide();
}

// ── Save ──────────────────────────────────────────────────────────────────────
async function onSave() {
  const dto = {
    title: _editing.title,
    displayTitle: _editing.displayTitle,
    formCode: _editing.formCode,
    userNames: _editing.userNames,
  };

  try {
    // validate required fields
    if (!dto.formCode || !dto.formCode.trim()) {
      notify(t("formcodeRequired", "Form Code is required."), "error");
      return;
    }

    if (!dto.title || !dto.title.trim()) {
      notify(t("titleRequired", "Title is required."), "error");
      return;
    }

    if (!dto.userNames || !dto.userNames.trim()) {
      notify(t("approverRequired", "At least one approver user must be selected."), "error");
      return;
    }

    if (!dto.displayTitle || !dto.displayTitle.trim()) {
      notify(t("displayTitleRequired", "Display Title is required."), "error");
      return;
    }

    if (_editing.id) {
      await fetch_.put(`${API}/${_editing.id}`, dto);
      notify(t("updated", "Updated successfully."));
    } else {
      await fetch_.post(API, dto);
      notify(t("created", "Created successfully."));
    }
    _formPopup.hide();
    await refreshGrid();
  } catch (e) {
    notify(e.message || t("saveFailed", "Save failed."), "error");
  }
}

// ── Bootstrap ─────────────────────────────────────────────────────────────────
$(function () {
  initGrid().catch((e) => {
    notify(e.message || t("loadFailed", "Failed to load data."), "error");
  });
});
