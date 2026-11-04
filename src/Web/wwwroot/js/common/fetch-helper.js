const fetchWithTimeout = async (url, options = {}, timeout = 10000) => {
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeout);
  options.signal = controller.signal;

  try {
    const response = await fetch(url, options);
    clearTimeout(id);
    return response;
  } catch (error) {
    clearTimeout(id);
    if (error.name === "AbortError") {
      throw new Error("Hết thời gian chờ phản hồi từ máy chủ");
    }
    throw error;
  }
};

export const useFetch = () => {
  const defaultTimeout = 500000;
  const handleResponse = async (response) => {
    if (!response.ok) {
      let message = `HTTP error! status: ${response.status}`;

      try {
        const error = await response.json();
        message = error.message || message;
      } catch {
        // fallback nếu không phải JSON
        const text = await response.text();
        if (text) message = text;
      }

      throw new Error(message);
    }

    return response.json();
  };

  const get = async (url, timeout = defaultTimeout) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "GET",
        headers: { Accept: "application/json" },
      },
      timeout,
    );
    return handleResponse(response);
  };

  const post = async (url, data = {}, timeout = defaultTimeout) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify(data),
      },
      timeout,
    );
    return handleResponse(response);
  };

  const put = async (url, data = {}, timeout = defaultTimeout) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify(data),
      },
      timeout,
    );
    return handleResponse(response);
  };

  const del = async (url, timeout = defaultTimeout) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "DELETE",
        headers: { Accept: "application/json" },
      },
      timeout,
    );
    return handleResponse(response);
  };

  const postForm = async (url, formData, timeout = defaultTimeout) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "POST",
        body: formData, // Không set Content-Type, browser sẽ tự đặt boundary đúng
        credentials: "same-origin",
      },
      timeout,
    );
    return handleResponse(response);
  };

  const putForm = async (url, formData, timeout = defaultTimeout) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "PUT",
        body: formData,
        credentials: "same-origin",
      },
      timeout,
    );
    return handleResponse(response);
  };

  const getBlob = async (url, timeout = defaultTimeout, header = {}) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "GET",
        headers: {
          ...header,
          Accept: "*/*",
        },
      },
      timeout,
    );
    if (!response.ok) {
      const error = await response.json();
      throw new Error(
        error.message || `HTTP error! status: ${response.status}`,
      );
    }
    return response;
  };

  const handleDownload = async (response, defaultFileName) => {
    const disposition = response.headers.get("Content-Disposition");
    let fileName = defaultFileName;

    let match = disposition?.match(/filename\*=UTF-8''(.+?)(?:;|$)/);
    if (match?.[1]) fileName = decodeURIComponent(match[1]);
    else {
      match = disposition?.match(/filename="?([^"]+)"?/);
      if (match?.[1]) fileName = match[1];
    }

    const blob = await response.blob();
    const urlFile = URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = urlFile;
    a.download = fileName;
    a.click();
    a.remove();
    URL.revokeObjectURL(urlFile);
  };

  const download = async (
    url,
    defaultFileName = "download.file",
    timeout = defaultTimeout,
  ) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "GET",
        headers: {
          Accept: "*/*",
        },
      },
      timeout,
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(
        error.message || `HTTP error! status: ${response.status}`,
      );
    }
    await handleDownload(response, defaultFileName);
  };

  const downloadPost = async (
    url,
    body,
    defaultFileName = "download.file",
    timeout = defaultTimeout,
  ) => {
    const response = await fetchWithTimeout(
      url,
      {
        method: "POST",
        headers: {
          Accept: "*/*",
          "Content-Type": "application/json",
        },
        body: JSON.stringify(body || {}),
      },
      timeout,
    );

    // lỗi trả về JSON
    if (!response.ok) {
      let errMsg = `HTTP error! status: ${response.status}`;
      try {
        const error = await response.json();
        errMsg = error.message || errMsg;
      } catch {
        /* ignore */
      }
      throw new Error(errMsg);
    }

    // lấy filename
    await handleDownload(response, defaultFileName);
  };

  return {
    get,
    post,
    put,
    del,
    postForm,
    putForm,
    getBlob,
    download,
    downloadPost,
  };
};
