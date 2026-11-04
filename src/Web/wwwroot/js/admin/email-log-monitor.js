/**
 * email-log-monitor.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Real-time Email Log Monitor for the WorkerMonitor/Index view.
 * Uses DevExtreme dxDataGrid with a CustomStore (server-side paging/filtering)
 * backed by /api/admin/email-log.
 * Polls for new data every AUTO_REFRESH_INTERVAL_MS when auto-refresh is on.
 */

const i18n = window.elmI18n ?? {};
function t(key, fallback) { return i18n[key] || fallback || key; }

const API_BASE = "/api/admin/email-log";
const AUTO_REFRESH_INTERVAL_MS = 30_000; // 30 s default

const STATUS_SENT = "Sent";
const STATUS_FAILED = "Failed";
const STATUS_PENDING = "Pending";

let _grid = null;
let _autoTimer = null;
let _autoOn = true; // auto-refresh starts enabled

// ── Bootstrap ────────────────────────────────────────────────────────────────
document.addEventListener("DOMContentLoaded", () => {
  _initGrid();
  _initRefreshBtn();
  //_initAutoRefreshToggle();
  //_startAutoRefresh();
});

// ── DataGrid ─────────────────────────────────────────────────────────────────
function _initGrid() {
  const store = new DevExpress.data.CustomStore({
    key: "id",
    load(loadOptions) {
      const params = new URLSearchParams();
      const keys = [
        "skip",
        "take",
        "requireTotalCount",
        "sort",
        "filter",
        "searchExpr",
        "searchOperation",
        "searchValue",
        "group",
        "groupSummary",
        "totalSummary",
        "select",
      ];
      for (const k of keys) {
        if (loadOptions[k] !== undefined && loadOptions[k] !== null) {
          params.set(
            k,
            typeof loadOptions[k] === "object"
              ? JSON.stringify(loadOptions[k])
              : loadOptions[k],
          );
        }
      }
      return fetch(`${API_BASE}?${params}`)
        .then((r) => {
          if (!r.ok) throw new Error(`HTTP ${r.status}`);
          return r.json();
        })
        .then((data) => {
          _updateTimestamp();
          //_updateBadgesFromGrid();
          return data;
        });
    },
  });

  _grid = $("#elm-grid")
    .dxDataGrid({
      dataSource: { store, paginate: true },
      remoteOperations: { paging: true, filtering: true, sorting: true },
      paging: { pageSize: 20 },
      pager: {
        showPageSizeSelector: true,
        allowedPageSizes: [20, 50, 100],
        showInfo: true,
      },
      filterRow: { visible: true },
      headerFilter: { visible: false },
      sorting: { mode: "multiple" },
      scrolling: { mode: "standard" },
      showBorders: true,
      //searchPanel: { visible: true, placeholder: "Search…" },
      repaintChangesOnly: true,
      wordWrapEnabled: true,
      columnAutoWidth: true,
      hoverStateEnabled: true,
      onRowPrepared(e) {
        if (e.rowType !== "data") return;
        const s = e.data.status;
        if (s === STATUS_FAILED) {
          // e.rowElement.style.background = "#fff0f0";
        }
        if (s === STATUS_PENDING) {
          // e.rowElement.style.background = "#fffbe6";
        }
        if (s === STATUS_SENT) {
          //e.rowElement.style.background = "#f0fff4";
        }
      },
      masterDetail: {
        enabled: true,
        template(container, info) {
          _renderAttemptDetail(container, info.data.attempts);
        },
      },
      columns: [
        {
          dataField: "id",
          caption: "ID",
          width: 80,
          alignment: "right",
          allowFiltering: false,
        },
        {
          dataField: "status",
          caption: t("col_status", "Status"),
          width: 100,
          alignment: "center",
          cellTemplate(container, { value }) {
            const cls =
              value === STATUS_SENT
                ? "badge badge-success"
                : value === STATUS_FAILED
                  ? "badge badge-danger"
                  : "badge badge-warning";
            $(`<span class="${cls}">${value ?? "–"}</span>`).appendTo(
              container,
            );
          },
          lookup: {
            dataSource: [STATUS_SENT, STATUS_FAILED, STATUS_PENDING].map(
              (s) => ({ id: s, name: s }),
            ),
            displayExpr: "name",
            valueExpr: "id",
          },
        },
        {
          dataField: "subject",
          caption: t("col_subject", "Subject"),
          minWidth: 200,
        },
        {
          dataField: "toAddress",
          caption: t("col_to", "To"),
          width: 200,
        },
        {
          dataField: "ccAddresses",
          caption: "CC",
          width: 160,
          visible: false,
        },
        {
          dataField: "notificationType",
          caption: t("col_type", "Type"),
          width: 120,
        },
        {
          dataField: "workflowAction",
          caption: t("col_action", "Action"),
          width: 120,
        },
        {
          dataField: "attemptNumber",
          caption: t("col_attempts", "Attempts"),
          width: 85,
          alignment: "center",
          allowFiltering: false,
        },
        {
          dataField: "createdAt",
          caption: t("col_created", "Created"),
          dataType: "datetime",
          format: "yyyy/MM/dd HH:mm:ss",
          width: 160,
          allowFiltering: false,
          sortOrder: "desc",
          sortIndex: 0,
        },
        {
          dataField: "sentAt",
          caption: t("col_sent_at", "Sent At"),
          dataType: "datetime",
          format: "yyyy/MM/dd HH:mm:ss",
          width: 160,
          allowFiltering: false,
        },
        {
          dataField: "retryAt",
          caption: t("col_retry_at", "Retry At"),
          dataType: "datetime",
          format: "yyyy/MM/dd HH:mm:ss",
          width: 160,
          allowFiltering: false,
          visible: false,
        },
        {
          dataField: "lastError",
          caption: t("col_last_error", "Last Error"),
          minWidth: 240,
          allowFiltering: false,
          cellTemplate(container, { value }) {
            if (!value) return;
            $(
              `<span class="text-danger small" title="${_esc(value)}">${_esc(_truncate(value, 120))}</span>`,
            ).appendTo(container);
          },
        },
      ],
    })
    .dxDataGrid("instance");
}

// ── Attempt detail (master-detail template) ───────────────────────────────────
function _renderAttemptDetail(container, attempts) {
  const $wrap = $('<div class="p-2"></div>').appendTo(container);
  const logs = [...attempts];

  if (!logs.length) {
    $(`<p class="small text-muted">${t("no_attempts", "No attempt logs recorded.")}</p>`).appendTo(
      $wrap,
    );
    return;
  }
  const $table = $(`
            <table class="table table-sm table-bordered small mb-0">
                <thead class="thead-light">
                <tr>
                    <th>#</th>
                    <th>${t("detail_result", "Result")}</th>
                    <th>${t("detail_time", "Time")}</th>
                    <th>${t("detail_error", "Error")}</th>
                </tr>
                </thead>
                <tbody></tbody>
            </table>
        `).appendTo($wrap);

  const $tbody = $table.find("tbody");
  logs.forEach((log, i) => {
    const ok = log.isSuccess;
    const cls = ok ? "text-success" : "text-danger";
    $tbody.append(`
                    <tr>
                      <td>${i + 1}</td>
                      <td class="${cls} font-weight-bold">${ok ? t("detail_success", "Success") : t("detail_failed", "Failed")}</td>
                      <td>${_fmtDate(log.createdAt)}</td>
                      <td class="text-danger">${_esc(log.errorMessage || "")}</td>
                    </tr>
                `);
  });
}

// ── Toolbar buttons ───────────────────────────────────────────────────────────
function _initRefreshBtn() {
  $("#elm-refresh-btn").dxButton({
    icon: "refresh",
    text: t("refresh", "Refresh"),
    type: "success",
    stylingMode: "outlined",
    onClick() {
      _reload();
    },
  });
}

function _initAutoRefreshToggle() {
  $("#elm-auto-refresh-toggle").dxSwitch({
    value: _autoOn,
    switchedOnText: "Auto",
    switchedOffText: "Auto",
    width: 76,
    onValueChanged({ value }) {
      _autoOn = value;
      if (value) {
        _startAutoRefresh();
      } else {
        _stopAutoRefresh();
      }
    },
  });
}

// ── Auto-refresh ──────────────────────────────────────────────────────────────
function _startAutoRefresh() {
  _stopAutoRefresh();
  _autoTimer = setInterval(_reload, AUTO_REFRESH_INTERVAL_MS);
}

function _stopAutoRefresh() {
  if (_autoTimer !== null) {
    clearInterval(_autoTimer);
    _autoTimer = null;
  }
}

function _reload() {
  if (_grid) _grid.getDataSource().reload();
}

// ── Summary badges ────────────────────────────────────────────────────────────
function _updateBadgesFromGrid() {
  // Request lightweight counts in a separate fire-and-forget fetch
  const params = new URLSearchParams({
    requireTotalCount: "true",
    take: "0",
  });
  const counts = { total: "–", sent: "–", pending: "–", failed: "–" };

  const fetchCount = (extraFilter) => {
    const p = new URLSearchParams(params);
    if (extraFilter) p.set("filter", JSON.stringify(extraFilter));
    return fetch(`${API_BASE}?${p}`)
      .then((r) => r.json())
      .then((d) => d.totalCount ?? 0);
  };

  Promise.all([
    fetchCount(null),
    fetchCount(["status", "=", STATUS_SENT]),
    fetchCount(["status", "=", STATUS_PENDING]),
    fetchCount(["status", "=", STATUS_FAILED]),
  ])
    .then(([total, sent, pending, failed]) => {
  $("#elm-badge-total").text(`${t("badge_total", "Total")}: ${total}`);
      $("#elm-badge-sent").text(`${t("badge_sent", "Sent")}: ${sent}`);
      $("#elm-badge-pending").text(`${t("badge_pending", "Pending")}: ${pending}`);
      $("#elm-badge-failed").text(`${t("badge_failed", "Failed")}: ${failed}`);
    })
    .catch(() => {
      /* ignore badge errors */
    });
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function _updateTimestamp() {
  const now = new Date();
  const pad = (n) => String(n).padStart(2, "0");
  const ts =
    `${now.getFullYear()}/${pad(now.getMonth() + 1)}/${pad(now.getDate())} ` +
    `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;
  $("#elm-last-refresh").text(`${t("last_refresh", "Last refresh:")} ${ts}`);
}

function _fmtDate(iso) {
  if (!iso) return "–";
  const d = new Date(iso);
  const pad = (n) => String(n).padStart(2, "0");
  return (
    `${d.getFullYear()}/${pad(d.getMonth() + 1)}/${pad(d.getDate())} ` +
    `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
  );
}

function _truncate(str, max) {
  if (!str) return "";
  return str.length <= max ? str : str.slice(0, max) + "…";
}

function _esc(str) {
  if (!str) return "";
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}
