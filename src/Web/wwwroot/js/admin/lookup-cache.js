// ── Shared lookup cache for admin dropdowns ──
// Caches belongSites, departments, sections to avoid redundant API calls.
var LookupCache = (function () {
    var _cache = {};

    function fetchAndCache(key, url) {
        if (_cache[key]) {
            return $.Deferred().resolve(_cache[key]).promise();
        }
        return fetch(url)
            .then(function (r) {
                if (!r.ok) throw new Error("Failed to load " + key);
                return r.json();
            })
            .then(function (data) {
                _cache[key] = data;
                return data;
            });
    }

    return {
        getBelongSites: function () {
            return fetchAndCache("belongSites", "/api/admin/belong-sites/lookup");
        },
        getDepartments: function () {
            return fetchAndCache("departments", "/api/admin/departments/lookup");
        },
        getSections: function () {
            return fetchAndCache("sections", "/api/admin/sections/lookup");
        },
        invalidate: function (key) {
            if (key) {
                delete _cache[key];
            } else {
                _cache = {};
            }
        }
    };
})();
