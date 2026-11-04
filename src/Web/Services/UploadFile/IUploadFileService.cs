namespace Web.Services.UploadFile
{
    public sealed record UploadFileResult(
        string OriginalFileName,
        string StoredFileName,
        string RelativeFolder,
        string RelativePath,
        string PhysicalPath,
        long Size
    );

    public interface IUploadFileService
    {
        Task<UploadFileResult> UploadFileAsync(
            IFormFile file,
            string relativeFolder,
            IReadOnlyCollection<string> allowedExtensions = null,
            CancellationToken ct = default);

        /// <summary>
        /// Xóa file theo thư mục con + tên file đã lưu (giống tham số upload).
        /// </summary>
        /// <returns>true nếu file tồn tại và đã xóa; false nếu không có file.</returns>
        Task<bool> DeleteFileAsync(string relativeFolder, string storedFileName, CancellationToken ct = default);

        /// <summary>
        /// Xóa file theo đường dẫn tương đối dưới root (ví dụ giá trị <see cref="UploadFileResult.RelativePath"/>).
        /// </summary>
        /// <returns>true nếu file tồn tại và đã xóa; false nếu không có file.</returns>
        Task<bool> DeleteFileByRelativePathAsync(string relativePath, CancellationToken ct = default);

        Task<IReadOnlyList<string>> ListRelativeFilesAsync(string relativeFolder, CancellationToken ct = default);

        /// <summary>
        /// Xóa thư mục tương đối dưới file-server root (kèm nội dung còn lại).
        /// </summary>
        Task<bool> DeleteFolderAsync(string relativeFolder, CancellationToken ct = default);

        Task<string> MoveByRelativePathAsync(string relativePath, string destRelativeFolder, CancellationToken ct = default);

        string GetPhysicalPath(string relativePath);

        Task OverwriteByRelativePathAsync(string relativePath, byte[] content, CancellationToken ct = default);
    }
}
