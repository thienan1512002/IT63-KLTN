namespace Web.Common.UploadFileHelper
{
    public static class UploadAllowedExtensions
    {
        /// <summary>News thumbnails (matches client uploader).</summary>
        public static readonly IReadOnlyList<string> NewsThumbnailImage =
            new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

        /// <summary>Portal notification / news attachments (aligned with <c>PortalNotiFileKind</c> mapping).</summary>
        public static readonly IReadOnlyList<string> PortalNotiAttachment =
            new[]
            {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg",
            ".mp4", ".webm", ".ogg", ".mov", ".m4v", ".avi", ".mkv",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip", ".rar", ".7z",
            };

        /// <summary>User avatar uploads.</summary>
        public static readonly IReadOnlyList<string> UserAvatarImage =
            new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

        /// <summary>Portal category PDF documents.</summary>
        public static readonly IReadOnlyList<string> PortalCategoryPdf =
            new[] { ".pdf" };

    }
}
