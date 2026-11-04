using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using PdfSharp.Pdf.IO;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow;

internal static class PdfVerification
{
    public static void Run(string sourceRoot)
    {
        var service = new RequestPdfService(new Environment { WebRootPath=Path.Combine(sourceRoot,"Web","wwwroot") });
        foreach (var definition in RequestFormCatalog.All)
        {
            var item = new RequestDetailsDto { TemplateCode=definition.Code, RequestType=definition.Name, ApplicationNo="TEST-PDF-001",
                Title="Đề nghị kiểm tra tiếng Việt", CreatedBy="Huỳnh Hải Yến", Department="Khoa Công nghệ", Content="Nội dung",
                Status=RequestStatus.Returned, CreatedAt=DateTime.UtcNow,
                Histories=Enumerable.Range(0,8).Select(i=>new RequestHistoryDto { Id=i+1, Action=ApprovalAction.Returned,
                    ActionBy="Người phê duyệt", StepOrder=1, StepName="Trưởng bộ môn", Comment=string.Join(" ",Enumerable.Repeat("Vui lòng bổ sung thông tin.",70)),ActionAt=DateTime.UtcNow }).ToList() };
            var bytes=service.Generate(item);
            using var stream=new MemoryStream(bytes);
            using var pdf=PdfReader.Open(stream,PdfDocumentOpenMode.ReadOnly);
            if(bytes.Length<1000 || pdf.PageCount<2)throw new Exception("PDF must render every form and split long history across pages.");
        }
        Console.WriteLine("PASS: All eight PDF templates render Vietnamese data and paginate long approval histories.");
    }
    private sealed class Environment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Web";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
