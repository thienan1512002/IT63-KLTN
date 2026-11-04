/** Normalize extensions: always leading dot, lowercase (e.g. ".pdf"). */
function normalizeAllowedExtensions(list) {
  if (!Array.isArray(list) || list.length === 0) return null;
  return list.map((ext) => {
    const s = String(ext).trim().toLowerCase();
    if (!s) return "";
    return s.startsWith(".") ? s : `.${s}`;
  }).filter(Boolean);
}

function getClientMaxUploadBytes() {
  const v = globalThis.UPLOAD_MAX_BYTES;
  if (typeof v === "number" && Number.isFinite(v) && v > 0) {
    return v;
  }
  // Fallback (kept for safety if constants.js is not loaded)
  return 30 * 1024 * 1024;
}

function formatBytesAsMb(bytes) {
  const mb = bytes / (1024 * 1024);
  if (!Number.isFinite(mb) || mb <= 0) return `${bytes} bytes`;
  const rounded = Math.ceil(mb);
  return `${rounded} MB`;
}

function uploadText(key, fallback, replacements = {}) {
  const value = window.i18n?.upload?.[key] || fallback;
  return Object.entries(replacements).reduce(
    (text, [name, replacement]) => text.split(`{${name}}`).join(replacement),
    value,
  );
}

/** Build the input `accept` attribute from the extension list. */
function buildAcceptAttribute(extensionsNormalized) {
  if (!extensionsNormalized?.length) return "";
  return extensionsNormalized.join(",");
}

function isFileExtensionAllowed(file, extensionsNormalized) {
  if (!extensionsNormalized?.length) return true;
  if (!file?.name) return false;
  const lower = file.name.toLowerCase();
  return extensionsNormalized.some((ext) => lower.endsWith(ext));
}

function getDefaultHintText(extensionsNormalized, maxFiles) {
  const isSingle = typeof maxFiles === "number" && maxFiles === 1;
  if (isSingle) {
    return "Click, paste or drag & drop a file here";
  }
  if (!extensionsNormalized?.length) {
    return "Click, paste or drag & drop files here!";
  }
  const shown = extensionsNormalized.join(", ");
  return `Click, paste or drag & drop files here!`;
}

/** Static icons under wwwroot/images/uploader-img (upload area uses CSS). */
const UPLOADER_IMG = "/images/uploader-img";

function staticUploaderPreview(fileName) {
  return {
    kind: "static",
    src: `${UPLOADER_IMG}/${fileName}`,
    backgroundSize: "inherit",
  };
}

/**
 * Extension → static icon filename in uploader-img.
 * Image previews use blobs first; these apply when blob preview is not used.
 */
const PREVIEW_EXT_TO_ICON = {
  pdf: "pdf.png",
  doc: "word.png",
  docx: "word.png",
  dot: "word.png",
  dotx: "word.png",
  rtf: "word.png",
  odt: "word.png",
  xls: "excel.png",
  xlsx: "excel.png",
  xlsm: "excel.png",
  xlsb: "excel.png",
  csv: "excel.png",
  ods: "excel.png",
  ppt: "ppt.png",
  pptx: "ppt.png",
  pps: "ppt.png",
  ppsx: "ppt.png",
  pot: "ppt.png",
  potx: "ppt.png",
  txt: "txt.png",
  log: "txt.png",
  md: "txt.png",
  json: "txt.png",
  xml: "txt.png",
  yml: "txt.png",
  yaml: "txt.png",
  ini: "txt.png",
  cfg: "txt.png",
  conf: "txt.png",
  js: "txt.png",
  mjs: "txt.png",
  cjs: "txt.png",
  ts: "txt.png",
  tsx: "txt.png",
  jsx: "txt.png",
  html: "txt.png",
  htm: "txt.png",
  css: "txt.png",
  scss: "txt.png",
  cs: "txt.png",
  py: "txt.png",
  java: "txt.png",
  /** HEIC/HEIF: poor blob preview in img → use icon */
  heic: "jpeg.png",
  heif: "jpeg.png",
  /** TIFF: use PNG icon (blob preview is limited) */
  tif: "png.png",
  tiff: "png.png",
};

function resolvePreview(fileData) {
  const name = (fileData.name || "").toLowerCase();
  const type = (fileData.type || "").toLowerCase();

  /** Direct blob image preview (except heic/heif/tiff mapped to icons above). */
  const extMatch = /\.([a-z0-9]+)$/i.exec(fileData.name || "");
  const ext = extMatch ? extMatch[1].toLowerCase() : "";

  if (ext === "heic" || ext === "heif") {
    return staticUploaderPreview("jpeg.png");
  }
  if (ext === "tif" || ext === "tiff") {
    return staticUploaderPreview("png.png");
  }

  if (type.includes("image") || /\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(name)) {
    if (!fileData._previewBlobUrl) {
      fileData._previewBlobUrl = URL.createObjectURL(fileData.data);
    }
    return {
      kind: "image",
      src: fileData._previewBlobUrl,
      backgroundSize: "cover",
    };
  }

  if (
    type.includes("sheet") ||
    type.includes("csv") ||
    type.includes("excel") ||
    /\.(xlsx?|xlsm|xlsb|csv|ods)$/i.test(name)
  ) {
    return staticUploaderPreview("excel.png");
  }
  if (type.includes("pdf") || name.endsWith(".pdf")) {
    return staticUploaderPreview("pdf.png");
  }
  if (
    type.includes("wordprocessingml") ||
    type.includes("msword") ||
    /\.(docx?|dotx?|odt|rtf)$/i.test(name)
  ) {
    return staticUploaderPreview("word.png");
  }
  if (
    type.includes("presentation") ||
    type.includes("powerpoint") ||
    /\.(pptx?|ppsx?|potx?)$/i.test(name)
  ) {
    return staticUploaderPreview("ppt.png");
  }
  if (type.startsWith("text/") || type.includes("json") || type.includes("xml")) {
    return staticUploaderPreview("txt.png");
  }
  if (type.startsWith("video/")) {
    return staticUploaderPreview("img.png");
  }
  if (type.startsWith("audio/")) {
    return staticUploaderPreview("img.png");
  }

  if (ext && Object.prototype.hasOwnProperty.call(PREVIEW_EXT_TO_ICON, ext)) {
    return staticUploaderPreview(PREVIEW_EXT_TO_ICON[ext]);
  }

  return staticUploaderPreview("txt.png");
}

export class FileUploader {
  constructor(containerId, options = {}) {
    this.container = document.getElementById(containerId);
    this.options = Object.assign(
      {
        onFilesChange: () => {},
        maxFiles: null,
        /** e.g. [".pdf"] or ["pdf","xlsx"] — null/undefined/[] = no extension filter */
        allowedExtensions: null,
        /** Override input accept (if unset, derived from allowedExtensions) */
        accept: null,
        /** Override hint text on the upload area */
        hintText: null,
      },
      options,
    );

    this._allowedExtensionsNormalized = normalizeAllowedExtensions(this.options.allowedExtensions);
    this._acceptAttr =
      typeof this.options.accept === "string" && this.options.accept.trim().length > 0
        ? this.options.accept.trim()
        : buildAcceptAttribute(this._allowedExtensionsNormalized);

    this.files = [];
    /** Remote image preview from URL (not in `files`; e.g. existing server thumbnail). */
    this._remotePreviewBlock = null;
    this.container.classList.add("file-uploader-container");
    this.createElements();
    this.addEventListeners();
  }

  /** Remove remote preview only (does not change `this.files`). */
  clearRemoteImagePreview() {
    if (this._remotePreviewBlock?.parentNode) {
      this._remotePreviewBlock.remove();
    }
    this._remotePreviewBlock = null;
  }

  /**
   * Show an image preview from a URL (e.g. current thumbnail). Not added to `files`.
   * Cleared automatically when the user picks a new file (maxFiles === 1).
   * @param {string} imageUrl
   * @param {string} [caption] — Non-empty string shows one line under the image; omit for image only.
   */
  setRemoteImagePreview(imageUrl, caption) {
    if (!imageUrl) return;
    this.clearRemoteImagePreview();

    const captionText = typeof caption === "string" && caption.trim() ? caption.trim() : "";

    const previewBlock = document.createElement("div");
    previewBlock.className = "file-preview-wrapper";

    const control = document.createElement("div");
    control.className = "icon-wrapper preview-control";
    control.innerHTML = `<i style="font-size: large;" class="icon dx-icon-close"></i>`;
    control.addEventListener("click", () => {
      this.clearRemoteImagePreview();
      this.options.onFilesChange(this.files);
    });

    /** Same visual model as `appendFile`: background on .img-wrapper, cover + bottom file-desc. */
    const escapedForCssUrl = String(imageUrl).replace(/\\/g, "/").replace(/"/g, "%22");
    const imgWrapper = document.createElement("div");
    imgWrapper.className =
      "w-100 img-wrapper preview d-flex align-items-center justify-content-center";
    imgWrapper.style.backgroundImage = `url("${escapedForCssUrl}")`;
    imgWrapper.style.backgroundSize = "cover";
    imgWrapper.style.backgroundRepeat = "no-repeat";
    imgWrapper.style.backgroundPosition = "center";

    imgWrapper.addEventListener("click", () => {
      window.open(imageUrl, "_blank", "noopener");
    });

    const probe = new Image();
    probe.onload = () => {};
    probe.onerror = () => {
      imgWrapper.style.backgroundImage = `url('${UPLOADER_IMG}/img.png')`;
      imgWrapper.style.backgroundSize = "contain";
      imgWrapper.style.backgroundRepeat = "no-repeat";
      imgWrapper.style.backgroundPosition = "center";
    };
    probe.src = imageUrl;

    const desc = document.createElement("div");
    desc.className = "file-desc d-flex flex-column";
    const nameSpan = document.createElement("span");
    nameSpan.className = "file-name";
    nameSpan.textContent = captionText;
    const metaSpan = document.createElement("span");
    metaSpan.textContent = "";
    desc.appendChild(nameSpan);
    desc.appendChild(metaSpan);

    previewBlock.appendChild(control);
    previewBlock.appendChild(imgWrapper);
    previewBlock.appendChild(desc);

    this.container.appendChild(previewBlock);
    this._remotePreviewBlock = previewBlock;
    this.options.onFilesChange(this.files);
  }

  /** @param {File} file @param {string[]} errorMessages @returns {boolean} */
  _validateRawFile(file, errorMessages) {
    if (!file || file.size === 0) {
      errorMessages.push(
        uploadText("emptyFile", 'File "{fileName}" is empty (0 bytes).', {
          fileName: file?.name || uploadText("unnamedFile", "unnamed"),
        }),
      );
      return false;
    }
    const maxBytes = getClientMaxUploadBytes();
    if (file.size > maxBytes) {
      errorMessages.push(
        uploadText("fileTooLarge", 'File "{fileName}" exceeds the {maxSize} limit.', {
          fileName: file.name,
          maxSize: formatBytesAsMb(maxBytes),
        }),
      );
      return false;
    }
    if (!isFileExtensionAllowed(file, this._allowedExtensionsNormalized)) {
      const allowed = this._allowedExtensionsNormalized?.join(", ") ?? "";
      errorMessages.push(
        allowed
          // ? `File type not allowed. Only: ${allowed}. File "${file.name}" is invalid.`
          ? uploadText("unsupportedType", "This file type is not supported.")
          : uploadText("invalidFile", 'File "{fileName}" is not valid.', {
              fileName: file.name,
            }),
      );
      return false;
    }
    return true;
  }

  /** @param {File} file */
  _buildFileDataFromFile(file) {
    return {
      name: file.name,
      type: file.type,
      time: new Date(file.lastModified).toLocaleString(),
      data: file,
    };
  }

  /** maxFiles === 1: replace with new file (last valid item in the list wins). */
  _replaceSingleSlotFromFileList(fileList, errorMessages) {
    const list = Array.from(fileList ?? []);
    if (list.length === 0) return;
    let chosen = null;
    for (let i = list.length - 1; i >= 0; i--) {
      if (this._validateRawFile(list[i], errorMessages)) {
        chosen = list[i];
        break;
      }
    }
    if (!chosen) return;
    this.clearRemoteImagePreview();
    while (this.files.length > 0) {
      this.removeFile(0);
    }
    const fileData = this._buildFileDataFromFile(chosen);
    this.files.push(fileData);
    this.appendFile(fileData, 0);
  }

  onSelectFile(newFiles) {
    const errorMessages = [];
    const maxFiles = this.options.maxFiles;

    if (typeof maxFiles === "number" && maxFiles === 1 && newFiles && newFiles.length > 0) {
      this._replaceSingleSlotFromFileList(newFiles, errorMessages);
      if (errorMessages.length > 0) {
        Swal.fire({
          icon: "warning",
          title: uploadText("invalidFilesTitle", "Some files are invalid"),
          html: errorMessages.map((m) => `<div>${m}</div>`).join(""),
        });
      }
      this.fileInput.value = null;
      this.options.onFilesChange(this.files);
      return;
    }

    Array.from(newFiles).forEach((file) => {
      if (typeof maxFiles === "number" && this.files.length >= maxFiles) {
        errorMessages.push(
          uploadText("maxFilesExceeded", "You can only select up to {maxFiles} file(s).", {
            maxFiles,
          }),
        );
        return;
      }

      if (!this._validateRawFile(file, errorMessages)) {
        return;
      }

      const fileData = this._buildFileDataFromFile(file);
      this.files.push(fileData);
      this.appendFile(fileData, this.files.length - 1);
    });

    if (errorMessages.length > 0) {
      Swal.fire({
        icon: "warning",
        title: uploadText("invalidFilesTitle", "Some files are invalid"),
        html: errorMessages.map((m) => `<div>${m}</div>`).join(""),
      });
    }

    this.fileInput.value = null;
    this.options.onFilesChange(this.files);
  }

  openSelectFile() {
    this.fileInput.click();
  }

  onOpenFile(fileData) {
    const file = fileData.data;
    const fileType = file.type;
    const url = URL.createObjectURL(file);

    if (fileType.includes("image") || fileType.includes("pdf")) {
      window.open(url, "_blank");
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      return;
    }

    const a = document.createElement("a");
    a.href = url;
    a.download = fileData.name;
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }

  onPasted(event) {
    event.preventDefault();
    const items = (event.clipboardData || window.clipboardData).items;
    const errorMessages = [];
    const maxFiles = this.options.maxFiles;

    const pastedFiles = [];
    for (const item of items) {
      if (item.kind === "file") {
        pastedFiles.push(item.getAsFile());
      }
    }

    if (typeof maxFiles === "number" && maxFiles === 1 && pastedFiles.length > 0) {
      let chosen = null;
      for (let i = pastedFiles.length - 1; i >= 0; i--) {
        const file = pastedFiles[i];
        if (this._validateRawFile(file, errorMessages)) {
          chosen = file;
          break;
        }
      }
      if (chosen) {
        this.clearRemoteImagePreview();
        const uniqueFileName = this.generateUniqueFileName(chosen.name);
        const updateFile = new File([chosen], uniqueFileName, {
          type: chosen.type,
        });
        while (this.files.length > 0) {
          this.removeFile(0);
        }
        const fileData = {
          name: uniqueFileName,
          type: updateFile.type,
          time: new Date(updateFile.lastModified).toLocaleString(),
          data: updateFile,
        };
        this.files.push(fileData);
        this.appendFile(fileData, 0);
      }
      if (errorMessages.length > 0) {
        Swal.fire({
          icon: "warning",
          title: uploadText(
            "invalidClipboardFilesTitle",
            "Some clipboard files are invalid",
          ),
          html: errorMessages.map((m) => `<div>${m}</div>`).join(""),
        });
      }
      this.options.onFilesChange(this.files);
      return;
    }

    for (const item of items) {
      if (typeof maxFiles === "number" && this.files.length >= maxFiles) {
        errorMessages.push(
          uploadText("maxFilesExceeded", "You can only select up to {maxFiles} file(s).", {
            maxFiles,
          }),
        );
        continue;
      }

      if (item.kind === "file") {
        const file = item.getAsFile();
        if (!this._validateRawFile(file, errorMessages)) {
          continue;
        }

        const uniqueFileName = this.generateUniqueFileName(file.name);
        const updateFile = new File([file], uniqueFileName, {
          type: file.type,
        });

        const fileData = {
          name: uniqueFileName,
          type: updateFile.type,
          time: new Date(updateFile.lastModified).toLocaleString(),
          data: updateFile,
        };

        this.files.push(fileData);
        this.appendFile(fileData, this.files.length - 1);
      }
    }
    if (errorMessages.length > 0) {
      Swal.fire({
        icon: "warning",
        title: uploadText(
          "invalidClipboardFilesTitle",
          "Some clipboard files are invalid",
        ),
        html: errorMessages.map((m) => `<div>${m}</div>`).join(""),
      });
    }
    this.options.onFilesChange(this.files);
  }

  generateUniqueFileName(baseName) {
    const timestamp = Date.now();
    const uniqueFileName = `${timestamp}_${baseName}`;
    return uniqueFileName;
  }

  createElements() {
    const hint =
      typeof this.options.hintText === "string" && this.options.hintText.length > 0
        ? this.options.hintText
        : getDefaultHintText(this._allowedExtensionsNormalized, this.options.maxFiles);

    this.uploadArea = document.createElement("div");
    this.uploadArea.innerHTML = `<div onclick="this.focus();" class="file-preview-wrapper file-upload-select" id="pasting-wrapper" tabindex="0">                               
                                  <div class="w-100 d-flex img-wrapper flex-column align-items-center justify-content-center">
                                      <div class="upload-background preview"></div>                                    
                                  </div>
                                   <div class="file-desc d-flex flex-column" style="background-color:unset">
                                      <div class="text-secondary text-center">${hint}</div>
                                  </div>
                              </div>`;
    this.uploadArea = this.uploadArea.firstChild;

    this.fileInput = document.createElement("input");
    this.fileInput.type = "file";
    this.fileInput.multiple = this.options.maxFiles === 1 ? false : true;
    this.fileInput.style.display = "none";
    if (this._acceptAttr) {
      this.fileInput.accept = this._acceptAttr;
    }

    this.container.appendChild(this.uploadArea);
    this.container.appendChild(this.fileInput);
  }

  addEventListeners() {
    this.uploadArea.addEventListener("click", () => this.fileInput.click());

    this.fileInput.addEventListener("change", (e) => this.onSelectFile(e.target.files));

    this.uploadArea.addEventListener("dragover", (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.uploadArea.classList.add("drag-over");
    });

    this.uploadArea.addEventListener("dragenter", (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.uploadArea.classList.add("drag-over");
    });

    this.uploadArea.addEventListener("dragleave", (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.uploadArea.classList.remove("drag-over");
    });

    this.uploadArea.addEventListener("drop", (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.uploadArea.classList.remove("drag-over");

      const files = e.dataTransfer.files;
      if (files && files.length > 0) {
        this.handleFiles(files);
      }
    });

    this.container.addEventListener("paste", (e) => {
      this.onPasted(e);
    });
  }

  renderFilePreviews() {
    this.clearPreviews();
    this.files.forEach((file, index) => {
      this.appendFile(file, index);
    });
  }

  appendFile(data, index) {
    const preview = resolvePreview(data);
    const backgroundSize = preview.backgroundSize;
    const src = preview.src;

    const previewBlock = document.createElement("div");
    previewBlock.className = "file-preview-wrapper";
    previewBlock.innerHTML = `<div class="icon-wrapper preview-control">
                                          <i style="font-size: large;" class="icon dx-icon-close"></i>
                                      </div>
                                      <div
                                          class="w-100 img-wrapper preview d-flex align-items-center justify-content-center" style="
                                          background-image: url('${src}');
                                          background-size: ${backgroundSize};
                                          background-repeat: no-repeat;
                                          background-position: center;"
                                      >                                   
                                      </div>
  
                                      <div class="file-desc d-flex flex-column">
                                          <span class="file-name">${data.name}</span>
                                          <span>${data.time}</span>
                                      </div>`;
    previewBlock.querySelector(".preview-control").addEventListener("click", () => {
      this.removeFile(index);
    });

    previewBlock.querySelector(".img-wrapper").addEventListener("click", () => {
      this.onOpenFile(data);
    });
    this.container.appendChild(previewBlock);
  }

  removeFile(index) {
    const removed = this.files[index];
    if (removed?._previewBlobUrl) {
      URL.revokeObjectURL(removed._previewBlobUrl);
      removed._previewBlobUrl = null;
    }
    this.files.splice(index, 1);
    this.renderFilePreviews();
  }

  clearPreviews() {
    this.clearRemoteImagePreview();
    for (const f of this.files) {
      if (f?._previewBlobUrl) {
        URL.revokeObjectURL(f._previewBlobUrl);
        f._previewBlobUrl = null;
      }
    }
    this.container.innerHTML = "";
    this.container.appendChild(this.fileInput);
    this.container.appendChild(this.uploadArea);
    this.options.onFilesChange(this.files);
  }

  handleFiles(fileList) {
    if (!fileList || fileList.length === 0) return;
    this.onSelectFile(fileList);
  }
}
