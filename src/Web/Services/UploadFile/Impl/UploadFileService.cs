using Microsoft.Extensions.Options;
using Web.Common;
using Web.Common.UploadFileHelper;

namespace Web.Services.UploadFile.Impl
{
    public class UploadFileService : IUploadFileService
    {
        private readonly FileServerConfig _fileServerConfig;
        private readonly ILogger<UploadFileService> _logger;

        public UploadFileService(IOptions<FileServerConfig> fileServerConfig, ILogger<UploadFileService> logger)
        {
            _fileServerConfig = fileServerConfig.Value;
            _logger = logger;
        }


        public async Task<UploadFileResult> UploadFileAsync(
            IFormFile file,
            string relativeFolder,
            IReadOnlyCollection<string> allowedExtensions = null,
            CancellationToken ct = default)
        {
            try
            {
                ValidateInput(file, relativeFolder, allowedExtensions);
                ValidateStorageRoot();

                var safeRelativeFolder = NormalizeRelativeFolder(relativeFolder);
                var targetFolder = Path.Combine(_fileServerConfig.Path, safeRelativeFolder);
                Directory.CreateDirectory(targetFolder);

                var originalFileName = Path.GetFileName(file.FileName);
                var safeFileName = EnsureCorrectFilename(originalFileName);
                var storedFileName = EnsureUniqueFileName(targetFolder, safeFileName);
                var fullPath = Path.Combine(targetFolder, storedFileName);

                await using (var fileStream = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await file.CopyToAsync(fileStream, ct);
                }

                var relativePath = string.IsNullOrWhiteSpace(safeRelativeFolder)
                    ? storedFileName
                    : Path.Combine(safeRelativeFolder, storedFileName);

                _logger.LogInformation(
                    "Uploaded file {OriginalFileName} as {StoredFileName} to {TargetFolder} ({Size} bytes)",
                    originalFileName,
                    storedFileName,
                    targetFolder,
                    file.Length);

                return new UploadFileResult(
                    originalFileName,
                    storedFileName,
                    safeRelativeFolder,
                    relativePath.Replace("\\", "/"),
                    fullPath,
                    file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to {RelativeFolder}", file?.FileName, relativeFolder);
                throw;
            }
        }

        public Task<bool> DeleteFileAsync(string relativeFolder, string storedFileName, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ValidateStorageRoot();

            if (storedFileName is null)
                throw new ArgumentNullException(nameof(storedFileName));

            var safeName = Path.GetFileName(storedFileName);
            if (string.IsNullOrWhiteSpace(safeName))
                throw new ArgumentException("Invalid file name.", nameof(storedFileName));

            if (!string.Equals(safeName, storedFileName, StringComparison.Ordinal)
                && (storedFileName.Contains('/') || storedFileName.Contains('\\')))
            {
                throw new ArgumentException("storedFileName must be a file name only (no path).", nameof(storedFileName));
            }

            var safeFolder = NormalizeRelativeFolder(relativeFolder ?? string.Empty);
            var fullPath = Path.Combine(_fileServerConfig.Path, safeFolder, safeName);
            EnsurePhysicalPathUnderFileServerRoot(fullPath);

            return Task.FromResult(TryDeleteFileAt(fullPath));
        }

        public Task<bool> DeleteFileByRelativePathAsync(string relativePath, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ValidateStorageRoot();

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path is required.", nameof(relativePath));

            var normalized = NormalizeRelativeFolder(relativePath);
            var fullPath = Path.Combine(_fileServerConfig.Path, normalized);
            EnsurePhysicalPathUnderFileServerRoot(fullPath);

            return Task.FromResult(TryDeleteFileAt(fullPath));
        }

        public Task<IReadOnlyList<string>> ListRelativeFilesAsync(string relativeFolder, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ValidateStorageRoot();

            var safeFolder = NormalizeRelativeFolder(relativeFolder ?? string.Empty);
            var folderFull = Path.Combine(_fileServerConfig.Path, safeFolder);
            EnsurePhysicalPathUnderFileServerRoot(folderFull);

            if (!Directory.Exists(folderFull))
                return Task.FromResult<IReadOnlyList<string>>([]);

            var files = Directory.GetFiles(folderFull)
                .Select(full => Path.Combine(safeFolder, Path.GetFileName(full)).Replace("\\", "/"))
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(files);
        }

        public Task<bool> DeleteFolderAsync(string relativeFolder, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ValidateStorageRoot();

            if (string.IsNullOrWhiteSpace(relativeFolder))
                throw new ArgumentException("Relative folder is required.", nameof(relativeFolder));

            var safeFolder = NormalizeRelativeFolder(relativeFolder);
            if (string.IsNullOrWhiteSpace(safeFolder))
                throw new ArgumentException("Relative folder is required.", nameof(relativeFolder));

            var folderFull = Path.Combine(_fileServerConfig.Path, safeFolder);
            EnsurePhysicalPathUnderFileServerRoot(folderFull);

            var rootFull = Path.GetFullPath(
                _fileServerConfig.Path!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var candidateFull = Path.GetFullPath(folderFull);
            if (string.Equals(rootFull, candidateFull, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Cannot delete the file server root.");

            if (!Directory.Exists(folderFull))
            {
                _logger.LogDebug("Delete folder skipped; directory not found at {Path}", folderFull);
                return Task.FromResult(false);
            }

            Directory.Delete(folderFull, recursive: true);
            _logger.LogInformation("Deleted folder at {Path}", safeFolder.Replace("\\", "/"));
            return Task.FromResult(true);
        }

        public Task<string> MoveByRelativePathAsync(string relativePath, string destRelativeFolder, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ValidateStorageRoot();

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path is required.", nameof(relativePath));

            var sourceRelative = NormalizeRelativeFolder(relativePath);
            var sourceFull = Path.Combine(_fileServerConfig.Path, sourceRelative);
            EnsurePhysicalPathUnderFileServerRoot(sourceFull);

            if (!File.Exists(sourceFull))
                throw new FileNotFoundException("Source file was not found.", sourceFull);

            var destFolder = NormalizeRelativeFolder(destRelativeFolder ?? string.Empty);
            var destFolderFull = Path.Combine(_fileServerConfig.Path, destFolder);
            EnsurePhysicalPathUnderFileServerRoot(destFolderFull);
            Directory.CreateDirectory(destFolderFull);

            var storedName = EnsureUniqueFileName(destFolderFull, Path.GetFileName(sourceFull));
            var destFull = Path.Combine(destFolderFull, storedName);
            EnsurePhysicalPathUnderFileServerRoot(destFull);
            File.Move(sourceFull, destFull);

            var destRelative = string.IsNullOrWhiteSpace(destFolder)
                ? storedName
                : Path.Combine(destFolder, storedName);

            _logger.LogInformation("Moved file {Source} to {Dest}", sourceRelative, destRelative);
            return Task.FromResult(destRelative.Replace("\\", "/"));
        }

        public string GetPhysicalPath(string relativePath)
        {
            ValidateStorageRoot();

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path is required.", nameof(relativePath));

            var normalized = NormalizeRelativeFolder(relativePath);
            var fullPath = Path.Combine(_fileServerConfig.Path, normalized);
            EnsurePhysicalPathUnderFileServerRoot(fullPath);
            return fullPath;
        }

        public async Task OverwriteByRelativePathAsync(string relativePath, byte[] content, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            ct.ThrowIfCancellationRequested();

            var fullPath = GetPhysicalPath(relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File was not found.", fullPath);

            await File.WriteAllBytesAsync(fullPath, content, ct);
        }

        private bool TryDeleteFileAt(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                _logger.LogDebug("Delete skipped; file not found at {Path}", fullPath);
                return false;
            }

            File.Delete(fullPath);
            _logger.LogInformation("Deleted file at {Path}", fullPath);
            return true;
        }

        private void EnsurePhysicalPathUnderFileServerRoot(string candidateFullPath)
        {
            ValidateStorageRoot();

            var rootFull = Path.GetFullPath(
                _fileServerConfig.Path!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var candidateFull = Path.GetFullPath(candidateFullPath);
            var rel = Path.GetRelativePath(rootFull, candidateFull);

            if (rel.Equals("..", StringComparison.Ordinal)
                || rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Resolved path is outside the file server root.");
            }
        }

        public string EnsureCorrectFilename(string filename)
        {
            var fileNameOnly = Path.GetFileName(filename ?? string.Empty);
            if (string.IsNullOrWhiteSpace(fileNameOnly))
            {
                fileNameOnly = "file";
            }

            // Replace invalid filename characters to avoid OS-level errors.
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileNameOnly = fileNameOnly.Replace(c, '_');
            }

            return fileNameOnly;
        }

        private void ValidateInput(IFormFile file, string relativeFolder, IReadOnlyCollection<string> allowedExtensions)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (file.Length <= 0)
            {
                throw new ArgumentException("File content is empty.", nameof(file));
            }

            ArgumentNullException.ThrowIfNull(relativeFolder);
            
            var originalFileName = Path.GetFileName(file.FileName);
            UploadFileValidationHelper.ValidateExtension(originalFileName, allowedExtensions);
            UploadFileValidationHelper.ValidateMaxSize(file, _fileServerConfig.MaxUploadBytes);
        }

        private void ValidateStorageRoot()
        {
            if (string.IsNullOrWhiteSpace(_fileServerConfig.Path))
            {
                throw new InvalidOperationException("FileServerConfig.Path is not configured.");
            }
        }

        private static string NormalizeRelativeFolder(string relativeFolder)
        {
            var normalized = (relativeFolder ?? string.Empty).Trim();
            normalized = normalized.Replace('/', Path.DirectorySeparatorChar)
                                   .Replace('\\', Path.DirectorySeparatorChar)
                                   .Trim(Path.DirectorySeparatorChar);

            // Block traversal and rooted inputs.
            if (normalized.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(normalized))
            {
                throw new ArgumentException("Invalid relative folder.", nameof(relativeFolder));
            }

            return normalized;
        }

        private static string EnsureUniqueFileName(string folderPath, string baseFileName)
        {
            var candidate = baseFileName;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
            var extension = Path.GetExtension(baseFileName);
            var index = 1;

            while (File.Exists(Path.Combine(folderPath, candidate)))
            {
                candidate = $"{nameWithoutExt}_{index}{extension}";
                index++;
            }

            return candidate;
        }
    }
}
