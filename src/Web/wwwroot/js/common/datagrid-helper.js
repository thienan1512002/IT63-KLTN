export function createBasicGridConfig(overrides = {}) {
    return {
        showBorders: true,
        columnAutoWidth: false,
        wordWrapEnabled: true,
        showColumnLines: true,
        allowColumnReordering: false,
        allowColumnResizing: true,
        columnResizingMode: "widget", //nextColumn, widget
        repaintChangesOnly: false,
        showRowLines: true,
        hoverStateEnabled: false,
        height: "auto",
        width: "100%",
        //noDataText: "No data",
        scrolling: {
            useNative: false,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never" | "onHover"
        },
        filterRow: { visible: true },

        paging: {
            pageSize: 20,
        },
        pager: {
            visible: true,
            showInfo: true,
            //infoText: "Total: {2} | Page {0}/{1}",
            showNavigationButtons: true,
            allowedPageSizes: [10, 20, 50],
            showPageSizeSelector: true,
        },
        ...overrides,
    };
};

export function createVirtualGridConfig(overrides = {}) {
    return {
        ...createBasicGridConfig(),

        paging: {
            enabled: false,
        },

        pager: {
            visible: false,
        },
        scrolling: {
            mode: "virtual",   // hoặc "infinite"
            rowRenderingMode: "virtual", // tối ưu render
            preloadEnabled: true,
            useNative: false,
            scrollByContent: true,
            scrollByThumb: true,
            showScrollbar: "onHover", // or "onScroll" | "always" | "never" | "onHover"
        },

        height: 600, // bắt buộc có height fixed để virtual scroll hoạt động tốt

        ...overrides,
    };
}

export function renderRowNumberCell(container, opt) {
    const grid = opt.component;
    const pageIndex = grid.pageIndex();
    const pageSize = grid.pageSize();
    const rowIndex = opt.rowIndex ?? 0;

    const stt = pageIndex * pageSize + rowIndex + 1;
    container.text(stt);
};