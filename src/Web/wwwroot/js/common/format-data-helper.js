export const cleanFilter = (obj) => Object.fromEntries(
    Object.entries(obj).filter(([_, value]) => value !== null)
);

export function formatDateTimeForServer(date) {
    if (!date) return "";

    if (typeof date === "string") {
        date = new Date(date);
    }

    const yyyy = date.getFullYear();
    const mm = (date.getMonth() + 1).toString().padStart(2, "0");
    const dd = date.getDate().toString().padStart(2, "0");
    const hh = date.getHours().toString().padStart(2, "0");
    const mi = date.getMinutes().toString().padStart(2, "0");
    const se = date.getSeconds().toString().padStart(2, "0");
    return `${yyyy}-${mm}-${dd}T${hh}:${mi}:${se}`;
}