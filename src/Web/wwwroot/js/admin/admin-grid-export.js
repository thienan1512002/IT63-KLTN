(function (window, $) {
  function captureLoadOptions(loadOptions) {
    var payload = {};
    [
      "skip",
      "take",
      "requireTotalCount",
      "sort",
      "filter",
      "totalSummary",
      "group",
      "searchExpr",
      "searchOperation",
      "searchValue",
    ].forEach(function (key) {
      if (
        key in loadOptions &&
        loadOptions[key] !== undefined &&
        loadOptions[key] !== null
      ) {
        payload[key] = JSON.parse(JSON.stringify(loadOptions[key]));
      }
    });
    return payload;
  }

  function getFileName(response, fallback) {
    var disposition = response.headers.get("content-disposition") || "";
    var utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match) {
      return decodeURIComponent(utf8Match[1].replace(/"/g, ""));
    }

    var match = disposition.match(/filename="?([^"]+)"?/i);
    return match ? match[1] : fallback;
  }

  function downloadBlob(blob, fileName) {
    var url = window.URL.createObjectURL(blob);
    var link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  function exportExcel(apiUrl, loadOptions, messages) {
    var loadPanel = $("<div>")
      .appendTo("body")
      .dxLoadPanel({
        shadingColor: "rgba(0,0,0,0.4)",
        visible: true,
        showIndicator: true,
        showPane: true,
        message: messages.exporting,
      })
      .dxLoadPanel("instance");

    return fetch(apiUrl + "/export", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(loadOptions || {}),
    })
      .then(function (response) {
        if (!response.ok) {
          throw new Error(messages.failed);
        }

        var fileName = getFileName(response, messages.fileName);
        return response.blob().then(function (blob) {
          downloadBlob(blob, fileName);
        });
      })
      .catch(function () {
        DevExpress.ui.notify(messages.failed, "error", 4000);
      })
      .finally(function () {
        loadPanel.hide();
      });
  }

  window.AdminGridExport = {
    captureLoadOptions: captureLoadOptions,
    exportExcel: exportExcel,
  };
})(window, jQuery);
