/** 401 từ /api/ → về Login (Swal do LoginSessionNotice). */
(function () {
    function onLogin() {
        return window.location.pathname.indexOf("/Account/Login") === 0;
    }
    function apiUrl(input) {
        var u = typeof input === "string" ? input : (input && input.url) ? input.url : "";
        return u.indexOf("/api/") !== -1;
    }
    function go() {
        if (!onLogin()) window.location.href = "/Account/Login";
    }
    if (window.fetch) {
        var f = window.fetch;
        window.fetch = function (input, init) {
            return f.apply(this, arguments).then(function (r) {
                if (!onLogin() && r.status === 401 && apiUrl(input)) go();
                return r;
            });
        };
    }
    if (window.jQuery) {
        $(document).ajaxComplete(function (_, xhr, s) {
            if (!onLogin() && xhr.status === 401 && (s.url || "").indexOf("/api/") !== -1) go();
        });
    }
})();
