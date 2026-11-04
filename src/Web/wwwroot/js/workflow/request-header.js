/**
 * request-header.js — DevExtreme widget initializer for the Request Header partials.
 *
 * Works with both partials by reading values from data attributes:
 *   _RequestHeaderPartial.cshtml      → UrgencyLevel is editable (data-editable="true")
 *   _RequestHeaderDetailPartial.cshtml → all fields readonly  (data-editable="false")
 *
 * Auto-initializes on DOM ready.
 * Also exposes window.getHeaderFormValue() to retrieve the editable UrgencyLevel value.
 */

$(function() {
    if (!document.getElementById("header-urgency-level")) return;
    _initHeaderWidgets();
});

function _initHeaderWidgets() {
    const urgencyEl = document.getElementById("header-urgency-level");
    const urgencyEditable = urgencyEl && urgencyEl.dataset.editable === "true";

    // ── Readonly text fields ────────────────────────────────────────────────────
    const textFields = [{
            id: "header-request-name",
        },
        { id: "header-submit-date" },
        { id: "header-description-no" },
        { id: "header-faxcode" },
        { id: "header-fullname" },
        { id: "header-section" },
        { id: "header-department" },
        { id: "header-email" },
        { id: "header-phone" },
    ];

    textFields.forEach(({ id, inputAttr }) => {
        const el = document.getElementById(id);
        if (!el) return;
        const opts = {
            value: el.dataset.value || "",
            readOnly: true,
            stylingMode: "filled",
        };
        if (inputAttr) opts.inputAttr = inputAttr;
        $(el).dxTextBox(opts);
    });

    // ── UrgencyLevel SelectBox ──────────────────────────────────────────────────
    if (!urgencyEl) return;

    const normalLabel = urgencyEl.dataset.labelNormal || "Normal";
    const urgentLabel = urgencyEl.dataset.labelUrgent || "Urgent";
    const urgencyValue = urgencyEl.dataset.value === "true";

    $(urgencyEl).dxSelectBox({
        items: [
            { value: false, text: normalLabel },
            { value: true, text: urgentLabel },
        ],
        value: urgencyValue,
        valueExpr: "value",
        displayExpr: "text",
        readOnly: !urgencyEditable,
        stylingMode: "filled",
    });
}

/**
 * Returns the current header form values (only the editable field).
 * Call this from the page submit handler to include UrgencyLevel in the DTO.
 * @returns {{ urgencyLevel: boolean }}
 */
function getHeaderFormValue() {
    const inst = $("#header-urgency-level").dxSelectBox("instance");
    return { urgencyLevel: inst ? inst.option("value") : false };
}

// Expose globally so page JS files can call without an import
window.getHeaderFormValue = getHeaderFormValue;
