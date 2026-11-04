namespace Web.Common
{
    public record FileServerConfig
    {
        public string Path { set; get; }
        public string AddinsPath { get; set; }
        /// <summary>
        /// Kích thước tối đa (bytes). 0 = không giới hạn trong upload service.
        /// </summary>
        public long MaxUploadBytes { get; set; }
    }
}
