// ── Shared User Picker: dxDropDownBox + dxDataGrid for selecting a user ──
// Usage: UserPicker.buildFormItem(formInstanceGetter, dataField, label)
var UserPicker = (function () {
  function _t(key, fallback) {
    var dictionaries = [window.dmI18n, window.smI18n, window.umI18n, window.usI18n];
    for (var i = 0; i < dictionaries.length; i += 1) {
      if (dictionaries[i] && dictionaries[i][key]) {
        return dictionaries[i][key];
      }
    }
    return fallback || key;
  }

  var userLookupUrl = "/api/admin/users/lookup";
  var cachedUsers = null;
  var usersRequest = null;

  function extractUsers(result) {
    var items = result && result.data ? result.data : result;
    return Array.isArray(items) ? items : [];
  }

  function loadUsers() {
    if (cachedUsers) {
      return Promise.resolve(cachedUsers);
    }

    if (usersRequest) {
      return usersRequest;
    }

    usersRequest = fetch(userLookupUrl)
      .then(function (r) {
        if (!r.ok) throw new Error("Load failed");
        return r.json();
      })
      .then(function (result) {
        cachedUsers = extractUsers(result);
        return cachedUsers;
      })
      .catch(function (error) {
        usersRequest = null;
        throw error;
      });

    return usersRequest;
  }

  function bindUsersToGrid(grid, selectedKeys, setSelectionSyncState) {
    return loadUsers()
      .then(function (users) {
        setSelectionSyncState(true);
        grid.option("dataSource", users);

        return Promise.resolve(grid.clearSelection())
          .then(function () {
            if (selectedKeys.length > 0) {
              return grid.selectRows(selectedKeys, false);
            }
          })
          .then(function () {
            setSelectionSyncState(false);
          });
      })
      .catch(function (error) {
        setSelectionSyncState(false);
        throw error;
      });
  }

  function syncGridSelection(grid, value, multiple, setSelectionSyncState) {
    if (!grid) {
      return Promise.resolve();
    }

    return Promise.resolve(
      bindUsersToGrid(
        grid,
        normalizeSelectedKeys(value, multiple),
        setSelectionSyncState,
      ),
    );
  }

  function normalizeSelectedKeys(value, multiple) {
    if (!multiple) {
      return value ? [value] : [];
    }

    if (!value) {
      return [];
    }

    return value
      .split(",")
      .map(function (item) {
        return item.trim();
      })
      .filter(function (item) {
        return item.length > 0;
      });
  }

  function serializeSelectedKeys(selectedKeys, multiple) {
    if (!multiple) {
      return selectedKeys.length > 0 ? selectedKeys[0] : null;
    }

    return selectedKeys.length > 0 ? selectedKeys.join(", ") : null;
  }

  /**
   * Builds a dxForm item with a dxDropDownBox containing a user DataGrid.
   * @param {Function} getFormInstance - function returning the current dxForm instance
   * @param {string} dataField - the form data field name (e.g. "mainApprover")
   * @param {string} label - display label
   */
  function buildFormItem(getFormInstance, options) {
    var dataField = options.dataField;
    var label = options.label;
    var validationRules = options.validationRules;
    var multiple = options.multiple === true;
    return {
      dataField: dataField,
      label: { text: label },
      validationRules: validationRules,
      template: function (data, itemElement) {
        var currentValue = data.component.option("formData")[dataField] || null;
        var selectedKeys = normalizeSelectedKeys(currentValue, multiple);
        var isSyncingSelection = false;
        var gridInstance = null;
        var $dropDown = $("<div>").dxDropDownBox({
          value: multiple
            ? serializeSelectedKeys(selectedKeys, true)
            : currentValue,
          acceptCustomValue: true,
          placeholder: label,
          showClearButton: true,
          deferRendering: false,
          openOnFieldClick: true,
          dropDownOptions: { width: 720, minWidth: 500 },
          onOpened: function (e) {
            syncGridSelection(
              gridInstance,
              e.component.option("value"),
              multiple,
              function (value) {
                isSyncingSelection = value;
              },
            );
          },
          contentTemplate: function (e) {
            var $grid = $("<div>").dxDataGrid({
              dataSource: cachedUsers || [],
              keyExpr: "userName",
              remoteOperations: false,
              columns: [
                { dataField: "userName", caption: _t("picker_user_name", _t("col_username", "User Name")), width: 120 },
                { dataField: "fullName", caption: _t("picker_full_name", _t("col_fullname", "Full Name")) },
                { dataField: "sectionName", caption: _t("picker_section", "Section"), width: 120 },
                {
                  dataField: "departmentName",
                  caption: _t("picker_department", "Department"),
                  width: 120,
                },
                { dataField: "belongSiteName", caption: _t("picker_site", "Site"), width: 90 },
                { dataField: "position", caption: _t("picker_position", "Position"), width: 100 },
              ],
              selection: { mode: multiple ? "multiple" : "single" },
              selectedRowKeys: selectedKeys,
              searchPanel: { visible: true, placeholder: _t("common_search_placeholder", "Search...") },
              filterRow: { visible: false },
              paging: { pageSize: 10 },
              height: 340,
              showBorders: true,
              scrolling: { mode: "virtual" },
              onSelectionChanged: function (args) {
                if (isSyncingSelection) {
                  return;
                }

                var value = serializeSelectedKeys(
                  args.selectedRowKeys,
                  multiple,
                );
                e.component.option("value", value);
                var fi = getFormInstance();
                if (fi) {
                  var formData = fi.option("formData");
                  formData[dataField] = value;
                }

                if (!multiple) e.component.close();
              },
            });

            gridInstance = $grid.dxDataGrid("instance");
            bindUsersToGrid(gridInstance, selectedKeys, function (value) {
              isSyncingSelection = value;
            });
            return $grid;
          },
          onValueChanged: function (e) {
            var fi = getFormInstance();
            if (fi) {
              var formData = fi.option("formData");
              formData[dataField] = e.value || null;
            }

            syncGridSelection(
              gridInstance,
              e.value,
              multiple,
              function (value) {
                isSyncingSelection = value;
              },
            );
          },
        });

        itemElement.append($dropDown);
      },
    };
  }

  return {
    buildFormItem: buildFormItem,
  };
})();
