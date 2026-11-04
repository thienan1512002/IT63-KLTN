namespace Web.Services.Workflow;

public record RequestFormField(string Name, string Label, string Input = "text", string Lookup = null, bool Required = true, string Section = "Thông tin nghiệp vụ");
public record RequestFormDefinition(string Code, string Name, string Icon, string ContentLabel, string DateLabel, bool HasPeriod, string LineCategory, string LineLabel, bool PricedLines, bool RequireLines, bool HasParticipants, RequestFormField[] Fields);

public static class RequestFormCatalog
{
    public static readonly RequestFormDefinition[] All = [
        new("LEAVE", "Đơn xin nghỉ phép", "calendar-clock", "Nội dung bàn giao công việc", null, true, null, null, false, false, false, [
            new("EmployeeId", "Nhân viên nghỉ phép", "select", "employees"), new("LeaveTypeId", "Loại nghỉ phép", "select", "LeaveType"),
            new("StartAt", "Ngày bắt đầu nghỉ", "date", Section:"Thời gian nghỉ"), new("EndAt", "Ngày kết thúc nghỉ", "date", Section:"Thời gian nghỉ"),
            new("StartSession", "Buổi bắt đầu", "select", "sessions", Section:"Thời gian nghỉ"), new("EndSession", "Buổi kết thúc", "select", "sessions", Section:"Thời gian nghỉ"),
            new("ReplacementEmployeeId", "Người nhận bàn giao", "select", "employees", false, "Bàn giao công việc")]),
        new("PURCHASE", "Đề nghị mua hàng", "cart-outline", "Mục đích sử dụng / Yêu cầu kỹ thuật", "Ngày cần hàng", false, "Product", "Danh sách hàng hóa cần mua", true, true, false, [
            new("WorkSiteId", "Cơ sở nhận hàng", "select", "sites"), new("TargetDepartmentId", "Đơn vị sử dụng", "select", "departments"), new("PartnerId", "Nhà cung cấp đề xuất", "select", "Partner", false),
            new("Location", "Địa điểm giao hàng"), new("CurrencyId", "Loại tiền", "select", "Currency", Section:"Dự toán mua hàng"),
            new("ExchangeRate", "Tỷ giá quy đổi sang VND", "number", Section:"Dự toán mua hàng")]),
        new("PAYMENT", "Đề nghị thanh toán", "credit-card", "Nội dung thanh toán", "Hạn thanh toán", false, "ExpenseCategory", "Các khoản thanh toán", true, true, false, [
            new("PartnerId", "Đối tác / Đơn vị thụ hưởng", "select", "Partner"), new("PaymentMethodId", "Hình thức thanh toán", "select", "PaymentMethod"),
            new("Payee", "Tên người / Đơn vị nhận tiền"), new("BankAccount", "Số tài khoản", Required:false), new("BankId", "Ngân hàng", "select", "Bank", false),
            new("InvoiceNumber", "Số hóa đơn / Chứng từ", Required:false, Section:"Chứng từ thanh toán"), new("InvoiceDate", "Ngày hóa đơn", "date", Required:false, Section:"Chứng từ thanh toán"),
            new("CurrencyId", "Loại tiền", "select", "Currency", Section:"Chi phí thanh toán"), new("ExchangeRate", "Tỷ giá quy đổi sang VND", "number", Section:"Chi phí thanh toán")]),
        new("EQUIPMENT", "Yêu cầu cấp thiết bị", "monitor", "Mục đích sử dụng / Yêu cầu cấu hình", "Ngày cần cấp thiết bị", false, "Equipment", "Thiết bị đề nghị cấp", false, true, false, [
            new("EmployeeId", "Nhân viên nhận thiết bị", "select", "employees"), new("WorkSiteId", "Cơ sở sử dụng", "select", "sites"), new("TargetDepartmentId", "Đơn vị sử dụng", "select", "departments"), new("Location", "Vị trí sử dụng thiết bị")]),
        new("OVERTIME", "Yêu cầu làm thêm giờ", "clock-outline", "Nội dung công việc làm thêm", null, true, null, null, false, false, true, [
            new("WorkSiteId", "Cơ sở làm thêm", "select", "sites"), new("TargetDepartmentId", "Đơn vị làm thêm", "select", "departments"), new("OvertimeTypeId", "Loại làm thêm giờ", "select", "OvertimeType"),
            new("StartAt", "Bắt đầu làm thêm", "datetime-local", Section:"Thời gian làm thêm"), new("EndAt", "Kết thúc làm thêm", "datetime-local", Section:"Thời gian làm thêm"), new("Location", "Địa điểm làm thêm")]),
        new("INTERNAL", "Đề xuất nội bộ", "lightbulb-outline", "Nội dung đề xuất / Phương án thực hiện", "Ngày dự kiến thực hiện", false, null, null, false, false, false, [
            new("ProposalCategoryId", "Nhóm đề xuất", "select", "ProposalCategory"), new("WorkSiteId", "Cơ sở thực hiện", "select", "sites"), new("TargetDepartmentId", "Đơn vị thực hiện", "select", "departments"),
            new("ExpectedBenefit", "Lợi ích / Kết quả kỳ vọng", "textarea", Section:"Hiệu quả đề xuất")]),
        new("BUSINESS_TRIP", "Đăng ký công tác", "airplane", "Nội dung công việc / Lịch trình công tác", null, true, "ExpenseCategory", "Chi phí công tác dự kiến", true, false, true, [
            new("EmployeeId", "Người đi công tác chính", "select", "employees"), new("ReplacementEmployeeId", "Người thay thế", "select", "employees", false),
            new("PurposeId", "Mục đích công tác", "select", "TripPurpose"),
            new("StartAt", "Ngày bắt đầu", "date", Section:"Thời gian công tác"), new("EndAt", "Ngày kết thúc", "date", Section:"Thời gian công tác"),
            new("CountryId", "Quốc gia", "select", "Country", Section:"Địa điểm & Đối tác"), new("CityId", "Tỉnh / Thành phố", "select", "City", Section:"Địa điểm & Đối tác"),
            new("Location", "Địa chỉ / Địa điểm cụ thể", Required:false, Section:"Địa điểm & Đối tác"), new("PartnerId", "Đối tác / Công ty", "select", "Partner", false, "Địa điểm & Đối tác"),
            new("CurrencyId", "Loại tiền dự toán", "select", "Currency", Section:"Dự toán chi phí"), new("ExchangeRate", "Tỷ giá quy đổi sang VND", "number", Section:"Dự toán chi phí")]),
        new("VEHICLE", "Đăng ký xe nhà trường", "car", "Mục đích / Nội dung chuyến đi", null, true, null, null, false, false, true, [
            new("EmployeeId", "Người liên hệ chuyến đi", "select", "employees"), new("ContactPhone", "Số điện thoại liên hệ"), new("WorkSiteId", "Cơ sở xuất phát", "select", "sites", false),
            new("StartAt", "Thời gian khởi hành", "datetime-local", Section:"Lịch trình xe"), new("EndAt", "Thời gian về dự kiến", "datetime-local", Section:"Lịch trình xe"),
            new("PickupLocation", "Điểm đón", Section:"Lịch trình xe"), new("Destination", "Điểm đến", Section:"Lịch trình xe"),
            new("PassengerCount", "Số hành khách", "number", Section:"Thông tin xe"), new("VehicleTypeId", "Loại xe", "select", "VehicleType", Section:"Thông tin xe"),
            new("VehicleId", "Xe đề xuất (nếu có)", "select", "Vehicle", false, "Thông tin xe"), new("DriverEmployeeId", "Tài xế đề xuất (nếu có)", "select", "drivers", false, "Thông tin xe")])
    ];
    public static RequestFormDefinition Get(string code) => All.FirstOrDefault(x => x.Code == code) ?? throw new InvalidOperationException("Mẫu biểu của loại yêu cầu chưa được cấu hình.");
    public static readonly IReadOnlyDictionary<string,string> MasterCategories = new Dictionary<string,string> {
        ["LeaveType"]="Loại nghỉ phép", ["TripPurpose"]="Mục đích công tác", ["Country"]="Quốc gia", ["City"]="Tỉnh / Thành phố", ["Bank"]="Ngân hàng",
        ["Partner"]="Đối tác / Nhà cung cấp", ["Currency"]="Loại tiền", ["ExpenseCategory"]="Khoản mục chi phí", ["Product"]="Hàng hóa", ["Equipment"]="Thiết bị", ["OvertimeType"]="Loại làm thêm giờ", ["PaymentMethod"]="Hình thức thanh toán", ["ProposalCategory"]="Nhóm đề xuất", ["VehicleType"]="Loại xe", ["Vehicle"]="Xe nhà trường"
    };
}

