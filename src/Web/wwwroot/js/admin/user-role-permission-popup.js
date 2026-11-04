// ── Phân quyền user: dxPopup + dxDataGrid (nhóm) + dxSwitch (Super / nhóm / từng quyền) ──
// API: PUT /api/admin/users/{userName}/roles { roleIds: [] }
var UserRolePermissionPopup = (function () {
    const _t = function (key) {
        return (window.umI18n && window.umI18n[key]) || key;
    };
    var apiUrl = "/api/admin/users";
    var popupInstance = null;
    var _gridInstance = null;
    var _superSwitchInstance = null;
    var _onSaved = null;
    var _rowData = null;
    var _payload = null;
    var _selectedIds = null;
    var _allRoleIds = [];
    var _superUserRoleId = null;
    var _flatRowsForUi = [];
    var EXCLUDED_ROLE_NAMES = [];
    var _selectionLock = false;
    /** @type {function[]} */
    var _disposeDx = [];

    function disposePopup() {
        _superSwitchInstance = null;
        _gridInstance = null;
        disposeDxWidgets();
        if (popupInstance) {
            popupInstance.dispose();
            popupInstance = null;
        }
        $("#userRolePermissionPopup").remove();
    }

    function disposeDxWidgets() {
        _disposeDx.forEach(function (fn) {
            try {
                fn();
            } catch (ignore) {}
        });
        _disposeDx = [];
    }

    function registerDispose(fn) {
        _disposeDx.push(fn);
    }

    function rebuildFromPayload() {
        _allRoleIds = [];
        var groups = _payload.groups || _payload.Groups || [];
        groups.forEach(function (g) {
            (g.roles || g.Roles || []).forEach(function (r) {
                var id = r.id || r.Id;
                if (id) _allRoleIds.push(id);
            });
        });
    }

    function flattenRoles() {
        var rows = [];
        var groups = _payload.groups || _payload.Groups || [];
        groups.forEach(function (g) {
            var cat = g.categoryName || g.CategoryName || "";
            var ord = g.categoryOrder !== undefined && g.categoryOrder !== null ? g.categoryOrder : g.CategoryOrder;
            if (ord === undefined || ord === null) ord = 0;
            (g.roles || g.Roles || []).forEach(function (r) {
                var id = r.id || r.Id;
                if (!id) return;
                rows.push({
                    id: id,
                    categoryName: cat,
                    categoryOrder: ord,
                    displayName: r.displayName || r.DisplayName || r.name || r.Name || "",
                    name: r.name || r.Name || "",
                });
            });
        });
        rows.sort(function (a, b) {
            if (a.categoryOrder !== b.categoryOrder) return a.categoryOrder - b.categoryOrder;
            if (a.categoryName !== b.categoryName) return String(a.categoryName).localeCompare(String(b.categoryName));
            return String(a.displayName || "").localeCompare(String(b.displayName || ""));
        });
        return rows;
    }

    function buildFlatRowsForUi() {
        return flattenRoles().filter(function (r) {
            if (_superUserRoleId && r.id === _superUserRoleId) return false;
            if (EXCLUDED_ROLE_NAMES.indexOf(r.name) >= 0) return false;
            return true;
        });
    }

    function maybeStripSuperIfPartial() {
        if (!_superUserRoleId) return;
        if (!_selectedIds.has(_superUserRoleId)) return;
        var granular = _allRoleIds.filter(function (id) {
            return id !== _superUserRoleId;
        });
        var allGranularOn = granular.every(function (id) {
            return _selectedIds.has(id);
        });
        if (!allGranularOn) {
            _selectedIds.delete(_superUserRoleId);
        }
    }

    function syncSuperSwitch() {
        if (_superSwitchInstance && _superUserRoleId) {
            _selectionLock = true;
            _superSwitchInstance.option("value", _selectedIds.has(_superUserRoleId));
            _selectionLock = false;
        }
    }

    function refreshGridAfterSelectionChange() {
        if (!_gridInstance) return;
        _selectionLock = true;
        _gridInstance.refresh();
        _selectionLock = false;
        syncSuperSwitch();
    }

    function renderContent($container) {
        $container.empty();
        disposeDxWidgets();
        _superSwitchInstance = null;
        _gridInstance = null;

        _flatRowsForUi = buildFlatRowsForUi();
        var uname = _payload.userName || _payload.UserName || "";

        var $shell = $("<div>").addClass("user-role-popup-shell");

        var $meta = $("<div>").addClass("user-role-popup-meta small text-muted mb-2").append(
            $("<span>").addClass("font-weight-bold text-body").text(_t("urp_user_account") + ": "),
            $("<span>").addClass("font-weight-bold text-primary").text(uname)
        );

        var $tool = $("<div>").addClass(
            "user-role-popup-toolbar d-flex flex-wrap align-items-center justify-content-between gap-2 mb-2"
        );

        var $superWrap = $("<div>").addClass("d-flex align-items-center user-role-super-wrap");
        var $superHost = $("<div>");

        var $searchHost = $("<div>").addClass("user-role-popup-search flex-grow-1").css({ minWidth: "200px", maxWidth: "360px" });

        var $gridHost = $("<div>").attr("id", "userRoleRolesGrid").addClass("user-role-roles-grid");

        $gridHost.dxDataGrid({
            dataSource: _flatRowsForUi,
            keyExpr: "id",
            showColumnHeaders: true,
            showBorders: true,
            showRowLines: true,
            rowAlternationEnabled: false,
            columnAutoWidth: true,
            wordWrapEnabled: true,
            height: "calc(85vh - 220px)",
            minHeight: 280,
            scrolling: { mode: "standard", useNative: true },
            paging: { enabled: false },
            searchPanel: { visible: false },
            filterRow: { visible: false },
            grouping: {
                autoExpandAll: true,
                allowCollapsing: true,
            },
            groupPanel: { visible: false },
            sorting: { mode: "single" },
            selection: { mode: "none" },
            columns: [
                {
                    dataField: "categoryName",
                    caption: _t("urp_group"),
                    groupIndex: 0,
                    sortOrder: "asc",
                    groupCellTemplate: function (container, options) {
                        container.empty().addClass("user-role-group-cell");
                        var category = options.value;
                        var rolesInGroup = _flatRowsForUi.filter(function (x) {
                            return x.categoryName === category;
                        });
                        var idsInGroup = rolesInGroup.map(function (r) {
                            return r.id;
                        });
                        var allOn =
                            idsInGroup.length > 0 &&
                            idsInGroup.every(function (id) {
                                return _selectedIds.has(id);
                            });

                        var $row = $("<div>").addClass("d-flex align-items-center flex-wrap w-100 justify-content-between gap-2");
                        var $left = $("<div>").addClass("d-flex align-items-center flex-wrap gap-2 flex-grow-1");
                        $left.append(
                            $("<span>").addClass("text-primary font-weight-bold user-role-group-title mr-3").text(category || "")
                        );
                        var $swHost = $("<div>").addClass("user-role-group-switch flex-shrink-0");
                        $left.append($swHost);
                        $swHost.dxSwitch({
                            value: allOn,
                            hint: _t("urp_group_toggle_hint"),
                            onValueChanged: function (e) {
                                if (_selectionLock) return;
                                if (e.value) {
                                    idsInGroup.forEach(function (id) {
                                        _selectedIds.add(id);
                                    });
                                } else {
                                    idsInGroup.forEach(function (id) {
                                        _selectedIds.delete(id);
                                    });
                                }
                                maybeStripSuperIfPartial();
                                refreshGridAfterSelectionChange();
                            },
                        });
                        $row.append($left);
                        container.append($row);
                    },
                },
                {
                    dataField: "displayName",
                    caption: _t("urp_permission"),
                    minWidth: 200,
                    cssClass: "user-role-col-permission",
                    cellTemplate: function (container, options) {
                        container.empty();
                        if (!options.data) return;
                        $("<span>")
                            .addClass("user-role-permission-label")
                            .text(options.data.displayName || "")
                            .appendTo(container);
                    },
                },
                {
                    caption: "",
                    width: 72,
                    alignment: "center",
                    allowSorting: false,
                    cssClass: "user-role-col-switch",
                    cellTemplate: function (container, options) {
                        container.empty();
                        if (!options.data) return;
                        var rid = options.data.id;
                        var $h = $("<div>").addClass("user-role-row-switch");
                        $(container).append($h);
                        $h.dxSwitch({
                            value: _selectedIds.has(rid),
                            onValueChanged: function (e) {
                                if (_selectionLock) return;
                                if (e.value) _selectedIds.add(rid);
                                else _selectedIds.delete(rid);
                                maybeStripSuperIfPartial();
                                refreshGridAfterSelectionChange();
                            },
                        });
                    },
                },
            ],
        });

        _gridInstance = $gridHost.dxDataGrid("instance");
        registerDispose(function () {
            try {
                $gridHost.dxDataGrid("dispose");
            } catch (ignore) {}
        });

        $searchHost.dxTextBox({
            mode: "search",
            placeholder: _t("common_search_placeholder"),
            showClearButton: true,
            stylingMode: "outlined",
            width: "100%",
            //valueChangeEvent: "input",
            onValueChanged: function (e) {
                if (_gridInstance) {
                    _gridInstance.searchByText(e.value || "");
                }
            },
        });
        registerDispose(function () {
            try {
                $searchHost.dxTextBox("dispose");
            } catch (ignore) {}
        });

        $superWrap.append($superHost);
        $superHost.dxSwitch({
            value: _superUserRoleId ? _selectedIds.has(_superUserRoleId) : false,
            hint: _t("urp_super_user_hint"),
            onValueChanged: function (e) {
                if (_selectionLock) return;
                if (!_superUserRoleId) return;
                if (e.value) {
                    _allRoleIds.forEach(function (id) {
                        _selectedIds.add(id);
                    });
                } else {
                    _selectedIds.delete(_superUserRoleId);
                }
                refreshGridAfterSelectionChange();
            },
        });
        _superSwitchInstance = $superHost.dxSwitch("instance");
        registerDispose(function () {
            try {
                $superHost.dxSwitch("dispose");
            } catch (ignore) {}
        });
        $superWrap.append(
            $("<span>").addClass("text-danger font-weight-bold user-role-super-text ml-2").text(_t("urp_super_user"))
        );

        $tool.append($superWrap, $searchHost);

        $shell.append($meta, $tool, $gridHost);
        $container.append($shell);
    }

    function showPopup() {
        var fullName =
            _payload.fullName ||
            _payload.FullName ||
            (_rowData && (_rowData.fullName || _rowData.FullName)) ||
            "";
        var uname = _payload.userName || _payload.UserName || (_rowData && (_rowData.userName || _rowData.UserName)) || "";
        var title = _t("action_edit_perm") + " - " + uname + " - " + fullName;

        disposePopup();
        $("body").append('<div id="userRolePermissionPopup"></div>');

        var $root = $("#userRolePermissionPopup");
        popupInstance = $root.dxPopup({
            title: title,
            width: Math.min(720, Math.floor(window.innerWidth * 0.92)),
            height: "auto",
            maxHeight: "90vh",
            visible: true,
            hideOnOutsideClick: false,
            showCloseButton: true,
            dragEnabled: true,
            showTitle: true,
            shading: true,
            wrapperAttr: { class: "user-role-permission-dx-popup" },
            toolbarItems: [
                {
                    widget: "dxButton",
                    toolbar: "bottom",
                    location: "after",
                    options: {
                        text: _t("common_cancel"),
                        stylingMode: "contained",
                        type: "info", 
                        onClick: function () {
                            if (popupInstance) popupInstance.hide();
                        },
                    },
                },
                {
                    widget: "dxButton",
                    toolbar: "bottom",
                    location: "after",
                    options: {
                        text: _t("common_save"),
                        type: "success",
                        stylingMode: "contained",
                        onClick: function () {
                            save();
                        },
                    },
                },
            ],
            onHidden: function () {
                disposePopup();
            },
            contentTemplate: function (container) {
                var $rootBody = $("<div>").addClass("p-3 user-role-popup-body");
                renderContent($rootBody);
                $(container).append($rootBody);
            },
        }).dxPopup("instance");
    }

    function save() {
        if (!_rowData || !_payload) return;

        var userName = _payload.userName || _payload.UserName;
        var loadPanel = $("<div>")
            .appendTo("body")
            .dxLoadPanel({
                shadingColor: "rgba(0,0,0,0.35)",
                visible: true,
                showIndicator: true,
                message: _t("umf_msg_saving"),
            })
            .dxLoadPanel("instance");

        fetch(apiUrl + "/" + encodeURIComponent(userName) + "/roles", {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ roleIds: Array.from(_selectedIds) }),
        })
            .then(function (res) {
                return res.json().then(function (data) {
                    return { ok: res.ok, data: data };
                });
            })
            .then(function (res) {
                loadPanel.hide();
                if (!res.ok) {
                    var msg =
                        (res.data && (res.data.message || (res.data.errors && res.data.errors[0]))) ||
                        _t("urp_msg_save_failed");
                    DevExpress.ui.notify(msg, "error", 4000);
                    return;
                }
                DevExpress.ui.notify(res.data.message || _t("urp_msg_saved"), "success", 2500);
                popupInstance.hide();
                if (typeof _onSaved === "function") {
                    _onSaved();
                }
            })
            .catch(function () {
                loadPanel.hide();
                DevExpress.ui.notify(_t("ra_network_error"), "error", 3000);
            });
    }

    function show(rowData, onSaved) {
        _rowData = rowData;
        _onSaved = onSaved;

        if (!rowData || !rowData.userName) {
            DevExpress.ui.notify(_t("urp_msg_missing_user"), "warning", 2500);
            return;
        }

        var loadPanel = $("<div>")
            .appendTo("body")
            .dxLoadPanel({
                shadingColor: "rgba(0,0,0,0.35)",
                visible: true,
                showIndicator: true,
                message: _t("workflow_swal_loading"),
            })
            .dxLoadPanel("instance");

        fetch(apiUrl + "/" + encodeURIComponent(rowData.userName) + "/roles")
            .then(function (res) {
                if (!res.ok) {
                    throw new Error("load failed");
                }
                return res.json();
            })
            .then(function (data) {
                loadPanel.hide();
                _payload = data;
                _superUserRoleId = data.superUserRoleId || data.SuperUserRoleId || null;
                _selectedIds = new Set();
                var groups = data.groups || data.Groups || [];
                groups.forEach(function (g) {
                    var roles = g.roles || g.Roles || [];
                    roles.forEach(function (r) {
                        var id = r.id || r.Id;
                        var assigned = r.assigned === true || r.Assigned === true;
                        if (assigned && id) {
                            _selectedIds.add(id);
                        }
                    });
                });
                rebuildFromPayload();
                showPopup();
            })
            .catch(function () {
                loadPanel.hide();
                DevExpress.ui.notify(_t("urp_msg_load_failed"), "error", 3000);
            });
    }

    function renderUsersRolesPopup(userData, onSaved) {
        show(userData, onSaved);
    }

    return {
        show: show,
        renderUsersRolesPopup: renderUsersRolesPopup,
        setExcludedRoleNames: function (names) {
            EXCLUDED_ROLE_NAMES = Array.isArray(names) ? names.slice() : [];
        },
    };
})();
