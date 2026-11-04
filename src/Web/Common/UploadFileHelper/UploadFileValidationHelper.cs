namespace Web.Common.UploadFileHelper
{
    public static class UploadFileValidationHelper
    {
        /// <summary>
        /// Throws <see cref="ArgumentException"/> if file exceeds <paramref name="maxUploadBytes"/>.
        /// When <paramref name="maxUploadBytes"/> is 0 or negative, size check is skipped.
        /// </summary>
        public static void ValidateMaxSize(IFormFile file, long maxUploadBytes)
        {
            if (maxUploadBytes <= 0)
            {
                return;
            }

            if (file.Length > maxUploadBytes)
            {
                throw new ArgumentException(
                    $"File size ({file.Length} bytes) exceeds the maximum allowed size ({maxUploadBytes} bytes).",
                    nameof(file));
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if extension is not in the allow-list.
        /// When <paramref name="allowedExtensions"/> is null or empty, no extension check is applied.
        /// Extensions may be passed with or without a leading dot (e.g. ".pdf" or "pdf").
        /// </summary>
        public static void ValidateExtension(string fileName, IReadOnlyCollection<string> allowedExtensions)
        {
            if (allowedExtensions is null || allowedExtensions.Count == 0)
            {
                return;
            }

            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            if (string.IsNullOrEmpty(ext))
            {
                throw new ArgumentException("File must have an extension.", nameof(fileName));
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in allowedExtensions)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var s = raw.Trim().ToLowerInvariant();
                if (!s.StartsWith('.'))
                {
                    s = "." + s;
                }

                allowed.Add(s);
            }

            if (allowed.Count == 0)
            {
                return;
            }

            if (!allowed.Contains(ext))
            {
                throw new ArgumentException(
                    $"File type '{ext}' is not allowed. Allowed: {string.Join(", ", allowed.OrderBy(x => x))}.",
                    nameof(fileName));
            }
        }
    }

}
