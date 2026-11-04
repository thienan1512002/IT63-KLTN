/**
 * Khởi tạo DevExtreme HtmlEditor (widget rich text chính thức; lõi dùng Quill — cần dx-quill.min.js + dx.all.js).
 * Nếu chỉ cần văn bản thuần, dùng dxTextArea thay cho helper này (không cần Quill).
 * Yêu cầu: layout đã load dx-quill.min.js + dx.all.js
 *
 * @param {string|JQuery} containerSelector — selector hoặc jQuery của phần tử container
 * @param {object} [options]
 * @param {number} [options.height=220]
 * @param {string} [options.value=""]
 * @param {boolean} [options.toolbar=true] — false để tắt toolbar
 * @param {object} [options.dxOptions] — merge trực tiếp vào dxHtmlEditor (ghi đè toolbar/height/value nếu cần)
 * @returns {{ getValue: () => string, setValue: (v: string) => void, getInstance: () => object, dispose: () => void }}
 */
export function createHtmlEditor(containerSelector, options = {}) {
  const $el =
    containerSelector && containerSelector.jquery ? containerSelector : $(containerSelector);
  if (!$el.length) {
    throw new Error("HtmlEditor: container not found.");
  }

  const height = options.height ?? 220;
  const value = options.value ?? "";

  const defaultToolbar = {
    multiline: true,
    items: [
      "undo",
      "redo",
      "separator",
      {
        name: "size",
        acceptedValues: ["8pt", "10pt", "12pt", "14pt", "18pt", "24pt", "36pt"],
      },
      {
        name: "font",
        acceptedValues: [
          "Arial",
          "Courier New",
          "Georgia",
          "Impact",
          "Tahoma",
          "Times New Roman",
          "Verdana",
        ],
      },
      "separator",
      "bold",
      "italic",
      "strike",
      "underline",
      "separator",
      "alignLeft",
      "alignCenter",
      "alignRight",
      "alignJustify",
      "separator",
      "orderedList",
      "bulletList",
      "separator",
      //"color",
      //"background",
      //"separator",
      //"link",
      //"image",
      //"separator",
      //"clear",
      //"codeBlock",
      //"blockquote",
    ],
  };

  const base = {
    height,
    value,
  };
  if (options.toolbar !== false) {
    base.toolbar = options.toolbar && typeof options.toolbar === "object" ? options.toolbar : defaultToolbar;
  }

  const merged = { ...base, ...(options.dxOptions || {}) };

  $el.dxHtmlEditor(merged);
  const instance = $el.dxHtmlEditor("instance");

  return {
    getValue: () => instance.option("value") ?? "",
    setValue: (v) => instance.option("value", v ?? ""),
    getInstance: () => instance,
    dispose: () => {
      $el.dxHtmlEditor("dispose");
    },
  };
}
