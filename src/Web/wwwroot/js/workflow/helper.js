// Numeric RequestStatus enum values (must mirror C# enum)
const STATUS_ID = { ongoing: 1, completed: 2, returned: 3, rejected: 4 };
const STATUS_CANONICAL_ID = {
    ongoing: "On going",
    completed: "Complete",
    returned: "Returned",
    rejected: "Rejected",
};

// const STATUS_COLORS = {
//   complete: "#6BCB77",
//   ongoing: "#4DAEFF",
//   rejected: "#FF6B6B",
//   returned: "#eda908",
// };

const STATUS_COLORS = {
    complete: "#059669",
    ongoing: "linear-gradient(90deg,#2196f3,#42a5f5)",
    rejected: "#dc2626",
    returned: "#d97706",
};
const GRID_LOOKUP_CACHE = {};
const GRID_FILTER_FIELD_MAP = {
    section: "section",
    department: "departmentId",
    urgency: "urgencyLevel",
    status: "status",
};

function remoteLookupDataSource(url, key) {
    var cacheKey = url + "::raw";
    return new DevExpress.data.CustomStore({
        key: key || "id",
        loadMode: "raw",
        load: function() {
            if (!GRID_LOOKUP_CACHE[cacheKey]) {
                GRID_LOOKUP_CACHE[cacheKey] = fetch(url)
                    .then(function(response) {
                        if (!response.ok) throw new Error("Lookup fetch failed: " + response.status);
                        return response.json();
                    })
                    .then(function(payload) {
                        var rows = Array.isArray(payload)
                            ? payload
                            : Array.isArray(payload && payload.data)
                                ? payload.data
                                : [];
                        return rows.map(function(item) {
                            return Object.assign({}, item, {
                                id: item.id ?? item.Id,
                                name: item.name ?? item.Name ?? item.engName ?? item.EngName ?? "",
                            });
                        }).filter(function(item) {
                            return item.id !== null && item.id !== undefined && !!item.name;
                        });
                    })
                    .catch(function(error) {
                        delete GRID_LOOKUP_CACHE[cacheKey];
                        throw error;
                    });
            }
            return GRID_LOOKUP_CACHE[cacheKey];
        },
    });
}

function staticGridLookupDataSource(items, valueExpr, displayExpr) {
    return (items || []).map(function(item) {
        return { value: item[valueExpr || "id"], text: item[displayExpr || "name"] };
    });
}

function createNameLookupDataSource(url) {
    return new DevExpress.data.CustomStore({
        key: "value",
        loadMode: "raw",
        load: function() {
            if (!GRID_LOOKUP_CACHE[url]) {
                GRID_LOOKUP_CACHE[url] = fetch(url)
                    .then(function(response) {
                        if (!response.ok) throw new Error("Lookup fetch failed: " + response.status);
                        return response.json();
                    })
                    .then(function(rows) {
                        return (Array.isArray(rows) ? rows : [])
                            .map(function(item) {
                                var name = item.name ?? item.Name ?? item.engName ?? item.EngName ?? "";
                                return { value: name, text: name };
                            })
                            .filter(function(item) { return !!item.value; });
                    })
                    .catch(function() {
                        delete GRID_LOOKUP_CACHE[url];
                        return [];
                    });
            }
            return GRID_LOOKUP_CACHE[url];
        },
    });
}

function sectionGridLookupDataSource(url) {
    return createNameLookupDataSource(url || "/api/admin/sections/lookup");
}

function departmentGridLookupDataSource() {
    return createNameLookupDataSource("/api/admin/departments/lookup");
}

function loadLookupItems(url) {
    var store = remoteLookupDataSource(url);
    return store.load().then(function(rows) {
        return (rows || []).map(function(item) {
            return {
                id: item.id ?? item.Id,
                name: item.name ?? item.Name ?? item.engName ?? item.EngName ?? "",
            };
        }).filter(function(item) {
            return item.id !== null && item.id !== undefined && !!item.name;
        });
    });
}

function sectionFilterLookupDataSource(url) {
    return remoteLookupDataSource(url || "/api/admin/sections/lookup");
}

function departmentFilterLookupDataSource(url) {
    return remoteLookupDataSource(url || "/api/admin/departments/lookup");
}


function urgencyDisplay(value, labels) {
    var text = labels || {};
    return value ? (text.urgent || "Urgent") : (text.normal || "Normal");
}

function urgencyGridLookupDataSource(labels) {
    return [
        { value: true, text: urgencyDisplay(true, labels) },
        { value: false, text: urgencyDisplay(false, labels) },
    ];
}

function urgencyBadge(value, labels) {
    var css = value
        ? "badge bg-danger px-2 py-1 text-white"
        : "badge bg-secondary-subtle text-secondary px-2 py-1";
    return '<span class="' + css + '">' + urgencyDisplay(!!value, labels) + '</span>';
}

function gridFilterDataField(event) {
    if (!event || !event.fullName) return null;
    var match = /^columns\[(\d+)\]\.(filterValue|filterValues|selectedFilterOperation)$/.exec(event.fullName);
    return match ? event.component.columnOption(Number(match[1]), "dataField") : null;
}

function gridColumnDataField(filterField) {
    return GRID_FILTER_FIELD_MAP[filterField] || null;
}

function gridGlobalFilterField(dataField) {
    var entry = Object.keys(GRID_FILTER_FIELD_MAP).find(function(key) {
        return GRID_FILTER_FIELD_MAP[key] === dataField;
    });
    return entry || null;
}

function getHr05RequestTypeNames(labels) {
    var text = labels || {};
    return [
        text.hr05TypeShortBreak || 'Short break',
        text.hr05TypeLongBreak || 'Long break',
        text.hr05TypeGatePass || 'Gate pass',
    ];
}

function _tx(key, fallback) {
    return (window.i18n && window.i18n[key]) || fallback;
}

// Normalise: accept numeric ID (1–4) or legacy string key
function normalizeStatusKey(status, labels) {
    if (typeof status === "number") {
        var found = Object.keys(STATUS_ID).find(function(k) { return STATUS_ID[k] === status; });
        return found !== undefined ? found : null;
    }
    if (typeof status !== "string") return null;
    var value = status.trim().toLowerCase();
    if (/^\d+$/.test(value)) return normalizeStatusKey(Number(value), labels);

    var translated = labels || {};
    var translatedKeys = {
        completed: translated.completed || translated.complete,
        ongoing: translated.ongoing,
        returned: translated.returned,
        rejected: translated.rejected,
    };
    var translatedMatch = Object.keys(translatedKeys).find(function (key) {
        return String(translatedKeys[key] || "").trim().toLowerCase() === value;
    });
    if (translatedMatch) return translatedMatch;

    return ({
        complete: "completed",
        completed: "completed",
        ongoing: "ongoing",
        "on going": "ongoing",
        returned: "returned",
        rejected: "rejected",
    })[value] || value;
}

function statusEnumValue(status, labels) {
    var key = normalizeStatusKey(status, labels);
    return key ? (STATUS_ID[key] ?? null) : null;
}

function statusCanonicalId(status, labels) {
    var key = normalizeStatusKey(status, labels);
    return key ? (STATUS_CANONICAL_ID[key] || null) : null;
}

function statusDisplay(status, labels) {
    var key = normalizeStatusKey(status, labels);
    var text = labels || {};
    return ({
        completed: text.completed || text.complete,
        ongoing: text.ongoing,
        returned: text.returned,
        rejected: text.rejected,
    })[key] || status || "";
}

function statusDataSource(labels, includeAll, valueMode) {
    var text = labels || {};
    var items = ["completed", "ongoing", "rejected", "returned"].map(function (key) {
        var canonicalId = STATUS_CANONICAL_ID[key];
        var value = valueMode === "enum"
            ? STATUS_ID[key]
            : valueMode === "translated"
                ? statusDisplay(canonicalId, text)
                : canonicalId;
        return { id: value, name: statusDisplay(canonicalId, text) };
    });
    if (includeAll) items.unshift({ id: null, name: text.all || "All" });
    return items;
}

function statusGridLookupDataSource(labels, valueMode) {
    return statusDataSource(labels, false, valueMode).map(function (item) {
        return { value: item.id, text: item.name };
    });
}

function clearGridColumnFilter(grid, dataField) {
    if (!grid) return;

    grid.columnOption(dataField, "filterValue", null);
    grid.columnOption(dataField, "filterValues", null);
    grid.columnOption(dataField, "selectedFilterOperation", null);
}

function clearGridFilters(grid) {
    if (!grid) return;
    grid.clearFilter();
}

function statusBadge(status, labels) {
    const key = normalizeStatusKey(status, labels);
    switch (key) {
        case "ongoing":
            return `<span class="badge" style="background: ${STATUS_COLORS.ongoing}; color: white; padding: 0.25rem 0.5rem; width: 100%;">${labels ? statusDisplay(status, labels) : _tx("statusOngoing", "On-Going")}</span>`;
        case "completed":
            return `<span class="badge" style="background: ${STATUS_COLORS.complete}; color: white; padding: 0.25rem 0.5rem; width: 100%;">${labels ? statusDisplay(status, labels) : _tx("statusCompleted", "Completed")}</span>`;
        case "rejected":
            return `<span class="badge" style="background: ${STATUS_COLORS.rejected}; color: white; padding: 0.25rem 0.5rem; width: 100%;">${labels ? statusDisplay(status, labels) : _tx("statusRejected", "Rejected")}</span>`;
        case "returned":
            return `<span class="badge" style="background: ${STATUS_COLORS.returned}; color: white; padding: 0.25rem 0.5rem; width: 100%;">${labels ? statusDisplay(status, labels) : _tx("statusReturned", "Returned")}</span>`;
        default:
            return `<span class="badge bg-secondary text-white px-2 py-1 w-100">${status ?? "—"}</span>`;
    }
}

function progressBadge(process, totalStep, status) {
    var percent = totalStep > 0 ? Math.round((process / totalStep) * 100) : 0;
    const statusKey = normalizeStatusKey(status);
    var barColor = statusKey === 'completed' ? STATUS_COLORS.complete :
        statusKey === 'rejected' ? STATUS_COLORS.rejected :
        statusKey === 'returned' ? STATUS_COLORS.returned :
        STATUS_COLORS.ongoing;

    var fillStyle = `width:${percent}%;height:100%;border-radius:20px;background:${barColor};transition:width 0.3s ease;`;
    
    const text = $('<div>')
        .addClass('wfr-process-text text-black')
        .text(`${process}/${totalStep}`);
    const fillArea = $('<div>').attr('style', fillStyle);
    const bg = $('<div>').addClass('wfr-process-bar')
        .append(fillArea);
        
    const container =  $('<div>').addClass('wfr-process-cell')
    .append(text)
    .append(bg);

    return container
        
}

window.helper = {
    statusIds: STATUS_ID,
    normalizeStatusKey,
    statusEnumValue,
    statusCanonicalId,
    statusDisplay,
    statusDataSource,
    statusGridLookupDataSource,
    clearGridColumnFilter,
    clearGridFilters,
    getHr05RequestTypeNames,
    remoteLookupDataSource,
    loadLookupItems,
    staticGridLookupDataSource,
    sectionGridLookupDataSource,
    departmentGridLookupDataSource,
    sectionFilterLookupDataSource,
    departmentFilterLookupDataSource,
    urgencyDisplay,
    urgencyGridLookupDataSource,
    urgencyBadge,
    gridFilterDataField,
    gridColumnDataField,
    gridGlobalFilterField,
    statusBadge,
    progressBadge,
    statusColors: STATUS_COLORS,
};
