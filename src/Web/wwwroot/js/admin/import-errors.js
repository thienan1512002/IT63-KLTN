var ImportErrors = (function () {
    function _t(key, fallback) {
        var dictionaries = [window.dmI18n, window.smI18n, window.umI18n];
        for (var i = 0; i < dictionaries.length; i += 1) {
            if (dictionaries[i] && dictionaries[i][key]) {
                return dictionaries[i][key];
            }
        }
        return fallback || key;
    }

    function show(result, summaryMsg) {
        var errorData = [];
        result.errors.forEach(function (err) {
            err.messages.forEach(function (m) {
                errorData.push({
                    row: err.row,
                    issuer: err.issuer,
                    action: err.action,
                    message: m
                });
            });
        });

        $("#importErrorsPopup").dxPopup({
            title: _t("import_result_title", "Import Result"),
            width: 700,
            height: "auto",
            showCloseButton: true,
            visible: true,
            contentTemplate: function (container) {
                $("<p>").addClass("mb-2 font-weight-bold").text(summaryMsg).appendTo(container);
                $("<div>").appendTo(container).dxDataGrid({
                    dataSource: errorData,
                    showBorders: false,
                    columns: [
                        { dataField: "row", caption: _t("import_error_row", "Row"), width: 60 },
                        { dataField: "issuer", caption: _t("import_error_issuer", "Issuer"), width: 120 },
                        { dataField: "action", caption: _t("col_action", "Action"), width: 80 },
                        { dataField: "message", caption: _t("import_error_message", "Error") }
                    ],
                    paging: { pageSize: 10 }
                });
            }
        });
    }

    return { show: show };
})();
