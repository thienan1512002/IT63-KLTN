(() => {
    const token = document.querySelector('meta[name="csrf-token"]')?.content;
    const safe = /^(GET|HEAD|OPTIONS|TRACE)$/i;
    const originalFetch = window.fetch.bind(window);
    window.fetch = (input, init = {}) => {
        const url = new URL(typeof input === 'string' ? input : input.url, location.href);
        const method = init.method || input.method || 'GET';
        if (url.origin === location.origin && !safe.test(method)) {
            const headers = new Headers(init.headers || input.headers);
            headers.set('RequestVerificationToken', token);
            init = { ...init, headers };
        }
        return originalFetch(input, init);
    };
    $.ajaxPrefilter((options, original, xhr) => {
        if (!options.crossDomain && !safe.test(options.type || 'GET'))
            xhr.setRequestHeader('RequestVerificationToken', token);
    });
})();
