/**
 * employee-picker.js — Shared reusable employee picker for DataGrid columns.
 *
 * USAGE IN A DATAGRID COLUMN (row / batch / cell mode):
 * ------------------------------------------------------
 *   import { loadEmployees, createEmpPickerColumn } from "../../common/employee-picker.js";
 *
 *   const employees = await loadEmployees();   // cached — one HTTP call ever
 *
 *   columns: [
 *     createEmpPickerColumn(employees, {
 *       dataField: "empId",
 *       caption:   "Mã NV",
 *       width:     120,
 *       // fillFields: { <gridField>: <userProperty> }
 *       // => auto-sets sibling cells after selection (batch-mode safe)
 *       fillFields: {
 *         fullName:       "fullName",
 *         departmentName: "departmentName",
 *       },
 *     }),
 *     { dataField: "fullName",       caption: "Họ và tên", allowEditing: false },
 *     { dataField: "departmentName", caption: "Bộ phận",   allowEditing: false },
 *   ]
 *
 * API (GET /api/lookup/employees):
 *   [{ empId, userName, fullName, departmentName, sectionName, position, email, availableLeaveDays }]
 *
 * i18n: include /Views/Workflow/_EmployeePickerLocalization.cshtml on the page.
 *       Keys are exposed as window.i18n.employeeGrid (colEmpId, colFullName, ...).
 */

import { useFetch } from "./fetch-helper.js";

function employeeGridI18n() {
    return (typeof window !== "undefined" && window.i18n && window.i18n.employeeGrid) || {};
}

function t(key, fallback) {
    const labels = employeeGridI18n();
    return labels[key] || fallback;
}

// ── Cache ─────────────────────────────────────────────────────────────────────
let _promise = null;
const _cache = new Map();

function buildEmployeeLookupQuery(filters = {}) {
    const params = new URLSearchParams();
    if (filters.departmentId != null) params.set("departmentId", filters.departmentId);
    if (filters.sectionId != null) params.set("sectionId", filters.sectionId);
    const qs = params.toString();
    return qs ? `/api/lookup/employees?${qs}` : "/api/lookup/employees";
}


/**
 * Fetch (or return cached) the active employee list.
 * Safe to call multiple times — only one HTTP request is ever made.
 * @returns {Promise<Array>}
 */
export function loadEmployees(filters = {}) {
    const key = JSON.stringify(filters);
    if (!_cache.has(key)) {
        _cache.set(key, useFetch().get(buildEmployeeLookupQuery(filters)));
    }
    return _cache.get(key);
}

function buildEmpPickerPopupColumns(gridLabels, includeAvailableLeaveDays) {
    var columns = [
        {
            caption: gridLabels.colNo || "#",
            width: 50,
            alignment: "center",
            allowSorting: false,
            cellTemplate: function(c, o) { $(c).text(o.row.dataIndex + 1); },
        },
        {
            dataField: "userName",
            caption: gridLabels.colUserName || t("colUserName", "FaxCode"),
            width: 80,
        },
        {
            dataField: "empId",
            caption: gridLabels.colEmpId || t("colEmpId", "Mã NV"),
            width: 100,
        },
        {
            dataField: "fullName",
            caption: gridLabels.colFullName || t("colFullName", "Họ và tên"),
            minWidth: 150,
        },
        {
            dataField: "departmentName",
            caption: gridLabels.colDepartment || t("colDepartment", "Phòng-Ban"),
            minWidth: 120,
        },
        {
            dataField: "sectionName",
            caption: gridLabels.colSection || t("colSection", "Tên phòng"),
            minWidth: 110,
        },
        {
            dataField: "position",
            caption: gridLabels.colPosition || t("colPosition", "Chức vụ"),
            minWidth: 100,
        },
    ];

    if (includeAvailableLeaveDays) {
        columns.push({
            dataField: "availableLeaveDays",
            caption: gridLabels.colAvailableLeaveDays || t("colAvailableLeaveDays", "Phép tồn"),
            width: 90,
            alignment: "right",
            cellTemplate: function(container, info) {
                var val = info.value;
                $(container).text(val == null || val === "" ? "\u2014" : Number(val).toFixed(1));
            },
        });
    }

    return columns;
}

function createEmpPickerColumnInternal(employees, opts, includeAvailableLeaveDays) {
    opts = opts || {};
    var dataField = opts.dataField || "empId";
    var caption = opts.caption || t("colEmpId", "Mã NV");
    var width = opts.width || 120;
    var fillFields = opts.fillFields || {};
    var gridLabels = employeeGridI18n();
    var popupColumns = buildEmpPickerPopupColumns(gridLabels, includeAvailableLeaveDays);
    var dropDownWidth = includeAvailableLeaveDays ? "min(920px, 95vw)" : "min(800px, 95vw)";

    return {
        dataField: dataField,
        caption: caption,
        width: width,
        editCellTemplate: function(cellElement, cellInfo) {
            var _displayVal = cellInfo.value || "";
            var _innerTextBox = null;
            var _dropInst = null;

            var $host = $("<div>").appendTo(cellElement);

            $host.dxDropDownBox({
                value: cellInfo.value || null,
                stylingMode: "filled",
                placeholder: "\u2014",
                showClearButton: false,

                fieldTemplate: function(val, container) {
                    $("<div>").appendTo(container).dxTextBox({
                        value: _displayVal,
                        readOnly: true,
                        stylingMode: "filled",
                        onInitialized: function(e) { _innerTextBox = e.component; },
                    });
                },

                dropDownOptions: {
                    width: dropDownWidth,
                    height: 460,
                    title: caption,
                    showTitle: true,
                },

                contentTemplate: function(e) {
                    _dropInst = e.component;

                    var $wrap = $("<div>");

                    $("<div>").appendTo($wrap).dxDataGrid({
                        dataSource: employees,
                        keyExpr: "empId",
                        showBorders: true,
                        height: 380,
                        columnAutoWidth: false,
                        allowColumnResizing: true,
                        selection: { mode: "single" },
                        searchPanel: {
                            visible: true,
                            width: 220,
                            placeholder: t("searchPlaceholder", "Tìm..."),
                        },
                        scrolling: {
                            // mode: "virtual", 
                            preloadEnabled: true,
                            useNative: false, // là dùng scroll của browser, false là dùng scroll của dxDataGrid
                            scrollByContent: true,
                            scrollByThumb: true,
                            showScrollbar: "onHover", },
                        paging: { enabled: false },
                        columns: popupColumns,
                        onRowClick: function(ev) {
                            var user = ev.data;
                            if (!user) return;

                            _displayVal = user.empId || "";
                            if (_innerTextBox) { _innerTextBox.option("value", _displayVal); }

                            cellInfo.setValue(user.empId);

                            var keys = Object.keys(fillFields);
                            for (var i = 0; i < keys.length; i++) {
                                var gridField = keys[i];
                                var userProp = fillFields[gridField];
                                cellInfo.component.cellValue(
                                    cellInfo.rowIndex,
                                    gridField,
                                    user[userProp] ?? ""
                                );
                            }

                            _dropInst.close();
                        },
                    });

                    return $wrap;
                },
            });
        },
    };
}

/**
 * Build a dxDataGrid column definition that renders a custom employee picker
 * (dxDropDownBox + inner dxDataGrid popup) for the given dataField.
 *
 * @param {Array}  employees
 * @param {object} opts
 * @param {string}  opts.dataField          - grid field bound to empId
 * @param {string}  opts.caption            - column header
 * @param {number}  [opts.width=120]
 * @param {object}  [opts.fillFields={}]    - { gridField: userProp } auto-filled on select
 * @returns {object} dxDataGrid column config
 */
export function createEmpPickerColumn(employees, opts) {
    return createEmpPickerColumnInternal(employees, opts, false);
}
