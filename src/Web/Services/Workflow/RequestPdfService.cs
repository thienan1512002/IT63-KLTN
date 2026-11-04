using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;

namespace Web.Services.Workflow;

/// <summary>Generates a printable snapshot from the same authorized detail model as the UI.</summary>
public sealed class RequestPdfService
{
    private static readonly object FontLock = new();
    private static bool fontsRegistered;
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private readonly string logoPath;

    public RequestPdfService(IWebHostEnvironment env)
    {
        logoPath = Path.Combine(env.WebRootPath, "images", "branding", "nam-viet-mark.png");
        lock (FontLock)
        {
            if (!fontsRegistered)
            {
                GlobalFontSettings.FontResolver = new UniversityFontResolver(Path.Combine(env.WebRootPath, "layout_assets", "fonts", "Roboto"));
                fontsRegistered = true;
            }
        }
    }

    public byte[] Generate(RequestDetailsDto item)
    {
        var definition = RequestFormCatalog.All.FirstOrDefault(x => x.Code == item.TemplateCode);
        var doc = new Document();
        doc.Info.Title = $"Yêu cầu {item.ApplicationNo}";
        var normal = doc.Styles["Normal"];
        normal.Font.Name = "UniversityRoboto";
        normal.Font.Size = 10;
        normal.ParagraphFormat.SpaceAfter = 4;
        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(3.4);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2);

        section.PageSetup.HeaderDistance = Unit.FromCentimeter(1.1);
        var header = section.Headers.Primary.AddTable();
        header.Borders.Bottom.Width = 0.7; header.Borders.Bottom.Color = Color.FromRgb(204, 215, 231);
        header.AddColumn(Unit.FromCentimeter(1.6)); header.AddColumn(Unit.FromCentimeter(8.6)); header.AddColumn(Unit.FromCentimeter(6.8));
        var headerRow = header.AddRow();
        var logo = headerRow.Cells[0].AddImage(logoPath); logo.LockAspectRatio = true; logo.Height = Unit.FromCentimeter(1.28);
        var organization = headerRow.Cells[1].AddParagraph("ĐẠI HỌC NAM VIỆT");
        organization.Format.Font.Bold = true; organization.Format.Font.Size = 11; organization.Format.Font.Color = Color.FromRgb(19, 43, 80);
        var system = headerRow.Cells[1].AddParagraph("HỆ THỐNG PHÊ DUYỆT NỘI BỘ"); system.Format.Font.Size = 8; system.Format.Font.Color = Colors.Gray;
        var reference = headerRow.Cells[2].AddParagraph("MÃ YÊU CẦU"); reference.Format.Alignment = ParagraphAlignment.Right; reference.Format.Font.Size = 8; reference.Format.Font.Color = Colors.Gray;
        var refCode = headerRow.Cells[2].AddParagraph(item.ApplicationNo ?? ""); refCode.Format.Alignment = ParagraphAlignment.Right; refCode.Format.Font.Bold = true; refCode.Format.Font.Color = Color.FromRgb(19, 43, 80);
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 8; footer.Format.Font.Color = Colors.Gray;
        footer.AddText("Bản trích xuất nội bộ  •  Trang "); footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();
        var title = section.AddParagraph(item.RequestType.ToUpper(Vietnamese));
        title.Format.Alignment = ParagraphAlignment.Center; title.Format.Font.Size = 17; title.Format.Font.Bold = true; title.Format.Font.Color = Color.FromRgb(19, 43, 80);
        title.Format.SpaceBefore = 7; title.Format.SpaceAfter = 5;
        var code = section.AddParagraph($"TRẠNG THÁI: {StatusName(item.Status).ToUpper(Vietnamese)}");
        code.Format.Alignment = ParagraphAlignment.Center; code.Format.Font.Size = 9; code.Format.Font.Color = Color.FromRgb(90, 110, 139); code.Format.SpaceAfter = 13;
        var subject = section.AddParagraph(item.Title ?? "");
        subject.Format.Alignment = ParagraphAlignment.Center; subject.Format.Font.Bold = true; subject.Format.Font.Size = 12; subject.Format.SpaceAfter = 14;

        void Heading(string text)
        {
            var p = section.AddParagraph(text); p.Format.Font.Bold = true; p.Format.Font.Size = 12;
            p.Format.Font.Color = Color.FromRgb(25, 66, 125); p.Format.SpaceBefore = 14; p.Format.SpaceAfter = 7; p.Format.KeepWithNext = true;
            p.Format.Borders.Bottom.Width = 0.5; p.Format.Borders.Bottom.Color = Color.FromRgb(205, 219, 238);
        }
        void Field(string label, string value)
        {
            value = string.IsNullOrWhiteSpace(value) ? "—" : value;
            if (value.Length > 220 || value.Contains('\n') || label.Length > 48)
            {
                var p = section.AddParagraph(); p.AddFormattedText(label + ": ", TextFormat.Bold); p.AddText(value);
                p.Format.SpaceBefore = 4; p.Format.SpaceAfter = 6; return;
            }
            var table = section.AddTable(); table.Borders.Width = 0.35; table.Borders.Color = Color.FromRgb(222, 231, 242);
            table.AddColumn(Unit.FromCentimeter(5.2)); table.AddColumn(Unit.FromCentimeter(11.8));
            table.LeftPadding = 6; table.RightPadding = 6;
            var row = table.AddRow(); row.TopPadding = 4; row.BottomPadding = 4;
            row.Cells[0].Shading.Color = Color.FromRgb(246, 249, 253);
            var key = row.Cells[0].AddParagraph(label); key.Format.Font.Color = Color.FromRgb(75, 97, 130); key.Format.Font.Size = 9;
            row.Cells[1].AddParagraph(value);
        }
        static string Date(DateTime? value) => value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "—";
        Heading("Thông tin chung");
        Field("Người đề nghị", item.CreatedBy); Field("Đơn vị", item.Department);
        Field("Mức độ ưu tiên", item.Priority switch { RequestPriority.Low => "Thấp", RequestPriority.High => "Cao", RequestPriority.Urgent => "Khẩn cấp", _ => "Bình thường" });
        Field("Ngày tạo", Date(item.CreatedAt)); Field("Ngày gửi", Date(item.SubmittedAt));
        if (item.RequestedDate.HasValue && definition?.HasPeriod != true) Field(definition?.DateLabel ?? "Ngày yêu cầu", item.RequestedDate.Value.ToString("dd/MM/yyyy"));
        if (!string.IsNullOrWhiteSpace(item.CurrentApprover)) Field("Người đang xử lý", item.CurrentApprover);

        if (item.BusinessFields.Count > 0 || item.ParticipantNames.Count > 0)
        {
            Heading("Thông tin nghiệp vụ");
            foreach (var field in item.BusinessFields) Field(field.Label, field.Value);
            if (item.ParticipantNames.Count > 0) Field("Nhân viên tham gia", string.Join(", ", item.ParticipantNames));
        }
        Heading("Nội dung yêu cầu");
        Field(definition?.ContentLabel ?? "Nội dung", item.Content);
        if (!string.IsNullOrWhiteSpace(item.Reason)) Field("Lý do", item.Reason);
        if (item.BusinessItems.Count > 0)
        {
            Heading(definition?.LineLabel ?? "Chi tiết");
            for (var i = 0; i < item.BusinessItems.Count; i++)
            {
                var line = item.BusinessItems[i];
                var label = section.AddParagraph($"{i + 1}. {line.Item}"); label.Format.Font.Bold = true; label.Format.KeepWithNext = true;
                if (!string.IsNullOrWhiteSpace(line.Description)) Field("Quy cách / Diễn giải", line.Description);
                Field("Số lượng", line.Quantity.ToString("N2", Vietnamese) + (string.IsNullOrWhiteSpace(line.Unit) ? "" : " " + line.Unit));
                if (definition?.PricedLines == true) { Field("Đơn giá", line.UnitPrice.ToString("N2", Vietnamese)); Field("Thành tiền", line.LineTotal.ToString("N2", Vietnamese)); }
                if (!string.IsNullOrWhiteSpace(line.Note)) Field("Ghi chú", line.Note);
            }
        }
        if (item.BusinessData?.OriginalAmount != null) Field("Tổng tiền nguyên tệ", item.BusinessData.OriginalAmount.Value.ToString("N2", Vietnamese));
        if (item.Amount.HasValue) Field("Tổng tiền (VND)", item.Amount.Value.ToString("N2", Vietnamese));
        Heading("Luồng phê duyệt");
        if (item.Steps.Count == 0) section.AddParagraph("Chưa có tầng phê duyệt được gán.");
        foreach (var step in item.Steps.OrderBy(x => x.StepOrder))
            Field($"Tầng {step.StepOrder} · {step.StepName}", $"{step.ApproverName} — {StepStatus(step.Status)}");
        Heading("Lịch sử xử lý");
        if (item.Histories.Count == 0) section.AddParagraph("Chưa có lịch sử xử lý.");
        foreach (var h in item.Histories.OrderBy(x => x.ActionAt).ThenBy(x => x.Id))
        {
            var p = section.AddParagraph($"{Date(h.ActionAt)}  ·  {h.ActionBy}  ·  {ActionName(h.Action)}");
            p.Format.Font.Bold = true; p.Format.SpaceBefore = 5;
            if (h.StepOrder.HasValue) Field("Tầng phê duyệt", $"Tầng {h.StepOrder}" + (string.IsNullOrWhiteSpace(h.StepName) ? "" : $" · {h.StepName}"));
            Field("Trạng thái", StatusName(h.NewStatus));
            if (!string.IsNullOrWhiteSpace(h.Comment)) Field("Ý kiến", h.Comment);
        }
        Heading("Tệp đính kèm");
        if (item.Attachments.Count == 0) section.AddParagraph("Không có tệp đính kèm.");
        else { foreach (var attachment in item.Attachments) section.AddParagraph(attachment.FileName); section.AddParagraph("Danh sách tên tệp; nội dung tệp không được gộp vào bản PDF này."); }
        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var output = new MemoryStream(); renderer.PdfDocument.Save(output, false); return output.ToArray();
    }

    private static string StatusName(RequestStatus status) => status switch {
        RequestStatus.Ongoing => "Đang xử lý", RequestStatus.Completed => "Đã hoàn thành", RequestStatus.Returned => "Yêu cầu bổ sung",
        RequestStatus.Rejected => "Đã từ chối", RequestStatus.Draft => "Bản nháp", RequestStatus.Cancelled => "Đã hủy", _ => "Chưa xác định" };
    private static string StepStatus(ApprovalStepStatus status) => status switch {
        ApprovalStepStatus.Pending => "Đang chờ phê duyệt", ApprovalStepStatus.Approved => "Đã phê duyệt",
        ApprovalStepStatus.Returned => "Yêu cầu bổ sung", ApprovalStepStatus.Rejected => "Đã từ chối",
        ApprovalStepStatus.Cancelled => "Đã hủy", _ => "Chưa xử lý" };
    private static string ActionName(ApprovalAction action) => action switch {
        ApprovalAction.Created => "Tạo yêu cầu", ApprovalAction.Submitted => "Gửi phê duyệt", ApprovalAction.Approved => "Phê duyệt",
        ApprovalAction.Returned => "Trả lại / Yêu cầu bổ sung", ApprovalAction.Rejected => "Từ chối",
        ApprovalAction.Resubmitted => "Gửi lại yêu cầu", ApprovalAction.Cancelled => "Hủy yêu cầu", ApprovalAction.Updated => "Cập nhật yêu cầu", _ => action.ToString() };
    private sealed class UniversityFontResolver(string directory) : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "university-bold" : "university-regular", false, isItalic);
        public byte[] GetFont(string faceName) => File.ReadAllBytes(Path.Combine(directory, faceName == "university-bold" ? "Roboto-Bold.ttf" : "Roboto-Regular.ttf"));
    }
}
