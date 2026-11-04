export const DATE_FORMAT = "dd/MM/yyyy";
export const TIME_FORMAT = "HH:mm";
export const DATETIME_FORMAT = "dd/MM/yyyy HH:mm";

// Client-side upload warning limit (bytes).
// Note: server still enforces its own limit via appsettings/IIS/Nginx.
export const UPLOAD_MAX_BYTES = 200 * 1024 * 1024;

// Expose for non-module scripts (e.g. upload-file-module.js).
globalThis.UPLOAD_MAX_BYTES = UPLOAD_MAX_BYTES;
globalThis.buildPortalFileOpenUrl = buildPortalFileOpenUrl;


export function buildPortalFileOpenUrl(relativePath) {
    const normalized = (relativePath ?? "").replace(/\\/g, "/").replace(/^\/+/, "");
    const baseUrl = `/Files/${encodeURI(normalized)}`;
    const v = formatDate(new Date(), "yyyyMMddHHmm");
    const hashIdx = baseUrl.indexOf("#");
    const urlNoHash = hashIdx >= 0 ? baseUrl.slice(0, hashIdx) : baseUrl;
    const hash = hashIdx >= 0 ? baseUrl.slice(hashIdx) : "";
    const sep = urlNoHash.includes("?") ? "&" : "?";
    return `${urlNoHash}${sep}v=${encodeURIComponent(v)}${hash}`;
}



/**
 * Format Date theo pattern .NET style
 */
export function formatDate(input, format = DATE_FORMAT) {
    const d = toDate(input);
    if (!d) return "";

    const map = {
        dd: pad(d.getDate()),
        MM: pad(d.getMonth() + 1),
        yyyy: d.getFullYear(),
        HH: pad(d.getHours()),
        mm: pad(d.getMinutes()),
    };

    return format.replace(/dd|MM|yyyy|HH|mm/g, (token) => map[token]);
}


/**
 * Parse input → Date
 */
function toDate(input) {
    if (!input) return null;

    // Nếu đã là Date
    if (input instanceof Date) {
        return isNaN(input.getTime()) ? null : input;
    }

    // Nếu là number (timestamp)
    if (typeof input === "number") {
        const d = new Date(input);
        return isNaN(d.getTime()) ? null : d;
    }

    // Nếu là string
    if (typeof input === "string") {
        // Ưu tiên ISO
        let d = new Date(input);
        if (!isNaN(d.getTime())) return d;

        // Parse format dd-MM-yyyy hoặc dd-MM-yyyy HH:mm
        const match = input.match(
            /^(\d{2})-(\d{2})-(\d{4})(?:\s+(\d{2}):(\d{2}))?$/
        );

        if (match) {
            const [, dd, MM, yyyy, HH = "00", mm = "00"] = match;
            d = new Date(
                Number(yyyy),
                Number(MM) - 1,
                Number(dd),
                Number(HH),
                Number(mm)
            );
            return isNaN(d.getTime()) ? null : d;
        }
    }

    return null;
}

/**
 * Pad số (01, 02...)
 */
function pad(n) {
    return n.toString().padStart(2, "0");
}

export function showSwalLoading(title = "Loading...") {
    Swal.fire({
        title: title,
        allowOutsideClick: false,
        allowEscapeKey: false,
        didOpen: () => {
            Swal.showLoading();
        },
    });
}