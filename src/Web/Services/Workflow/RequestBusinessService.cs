using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Data.Seeding;
using Web.Models.Dtos.Workflow;

namespace Web.Services.Workflow;

public class RequestBusinessService(ApplicationDbContext db)
{
    private static readonly PropertyInfo[] ValueProperties = typeof(RequestBusinessValues).GetProperties();
    public static RequestBusinessDataDto ToInput(RequestBusinessData entity)
    {
        if (entity == null) return null;
        var dto = new RequestBusinessDataDto();
        foreach (var property in ValueProperties) property.SetValue(dto, property.GetValue(entity));
        dto.ParticipantIds = entity.Participants.Select(x => x.EmployeeId).ToList();
        dto.Items = entity.Items.OrderBy(x => x.LineOrder).Select(x => new RequestLineInputDto { CatalogItemId=x.CatalogItemId, Description=x.Description, Unit=x.Unit, Quantity=x.Quantity, UnitPrice=x.UnitPrice, Note=x.Note }).ToList();
        return dto;
    }

    public async Task SaveAsync(ApprovalRequest request, RequestType type, RequestBusinessDataDto input, bool submit, CancellationToken ct)
    {
        var definition = RequestFormCatalog.Get(type.TemplateCode);
        input ??= new();
        var clean = new RequestBusinessDataDto();
        foreach (var field in definition.Fields)
        {
            var property = ValueProperties.Single(x => x.Name == field.Name);
            var value = property.GetValue(input);
            if (value is string text) value = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            property.SetValue(clean, value);
            if (submit && field.Required && (value == null || value is string s && string.IsNullOrWhiteSpace(s)))
                throw new InvalidOperationException($"Vui lòng nhập {field.Label.ToLowerInvariant()}.");
        }
        if (definition.LineCategory != null) clean.Items = input.Items ?? [];
        if (clean.Items.Count > 100) throw new InvalidOperationException("Mỗi yêu cầu được có tối đa 100 dòng chi tiết.");
        if (definition.HasParticipants)
        {
            clean.ParticipantIds = (input.ParticipantIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (definition.Code != "VEHICLE" && !string.IsNullOrWhiteSpace(clean.EmployeeId) && !clean.ParticipantIds.Contains(clean.EmployeeId)) clean.ParticipantIds.Add(clean.EmployeeId);
            if (clean.ParticipantIds.Count > 100) throw new InvalidOperationException("Mỗi yêu cầu được có tối đa 100 nhân viên tham gia.");
            if (submit && clean.ParticipantIds.Count == 0) throw new InvalidOperationException("Vui lòng chọn ít nhất một nhân viên tham gia.");
        }
        var employeeIds = new[] { clean.EmployeeId, clean.ReplacementEmployeeId, clean.DriverEmployeeId }.Concat(clean.ParticipantIds).Where(x => x != null).Distinct().ToList();
        var activeEmployees = await db.AppUsers.AsNoTracking().Where(x => employeeIds.Contains(x.Id) && request.SiteId.HasValue && x.SiteId == request.SiteId && x.Id != IdentityModelSeed.AdminUserId && x.IsActive && (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow)).Select(x => x.Id).ToListAsync(ct);
        if (employeeIds.Except(activeEmployees).Any()) throw new InvalidOperationException("Nhân viên được chọn không tồn tại hoặc đã bị khóa.");
        if (clean.DriverEmployeeId != null && !await db.DriverProfiles.AnyAsync(x => x.EmployeeId == clean.DriverEmployeeId && x.IsActive, ct))
            throw new InvalidOperationException("Tài xế chưa được đăng ký vào danh sách điều phối đang hoạt động.");
        if (clean.EmployeeId != null && clean.EmployeeId == clean.ReplacementEmployeeId) throw new InvalidOperationException("Người thay thế phải khác nhân viên đề nghị.");
        if (clean.WorkSiteId.HasValue && !await db.BelongSites.AnyAsync(x => x.Id == clean.WorkSiteId && x.Id == request.SiteId && x.IsActive, ct)) throw new InvalidOperationException("Cơ sở không tồn tại, đã ngừng sử dụng hoặc ngoài phạm vi tài khoản.");
        if (clean.TargetDepartmentId.HasValue)
        {
            var department = await db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clean.TargetDepartmentId && x.IsActive, ct)
                ?? throw new InvalidOperationException("Đơn vị không tồn tại hoặc đã ngừng sử dụng.");
            if (department.SiteId != request.SiteId) throw new InvalidOperationException("Đơn vị nằm ngoài phạm vi cơ sở của tài khoản.");
            if (clean.WorkSiteId.HasValue && department.SiteId != clean.WorkSiteId) throw new InvalidOperationException("Đơn vị phải thuộc cơ sở đã chọn.");
            clean.WorkSiteId ??= department.SiteId;
        }

        var masterFields = definition.Fields.Where(x => x.Lookup != null && RequestFormCatalog.MasterCategories.ContainsKey(x.Lookup)).ToList();
        var masterIds = masterFields.Select(x => (int?)ValueProperties.Single(p => p.Name == x.Name).GetValue(clean)).Where(x => x.HasValue).Select(x => x.Value).Concat(clean.Items.Where(x => x.CatalogItemId.HasValue).Select(x => x.CatalogItemId.Value)).Distinct().ToList();
        var masters = await db.RequestMasterItems.AsNoTracking().Where(x => masterIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        foreach (var field in masterFields)
        {
            var id = (int?)ValueProperties.Single(x => x.Name == field.Name).GetValue(clean);
            if (id.HasValue && (!masters.TryGetValue(id.Value, out var master) || master.Category != field.Lookup)) throw new InvalidOperationException($"{field.Label} không hợp lệ hoặc đã ngừng sử dụng.");
        }
        if (clean.CityId.HasValue && (!clean.CountryId.HasValue || masters[clean.CityId.Value].ParentId != clean.CountryId)) throw new InvalidOperationException("Thành phố phải thuộc quốc gia đã chọn.");
        if (clean.VehicleId.HasValue && (!clean.VehicleTypeId.HasValue || masters[clean.VehicleId.Value].ParentId != clean.VehicleTypeId)) throw new InvalidOperationException("Xe phải thuộc loại xe đã chọn.");
        if (definition.HasPeriod && clean.StartAt.HasValue && clean.EndAt.HasValue)
        {
            if (clean.EndAt < clean.StartAt) throw new InvalidOperationException("Thời điểm kết thúc phải sau thời điểm bắt đầu.");
            if (definition.Code is "OVERTIME" or "VEHICLE")
            {
                clean.Duration = Math.Round((decimal)(clean.EndAt.Value - clean.StartAt.Value).TotalHours, 2);
                if (clean.Duration <= 0 || clean.Duration > 8760) throw new InvalidOperationException("Khoảng thời gian phải lớn hơn 0 và không quá một năm.");
            }
            else
            {
                clean.StartAt = clean.StartAt.Value.Date; clean.EndAt = clean.EndAt.Value.Date;
                clean.Duration = (clean.EndAt.Value - clean.StartAt.Value).Days + 1;
                if (definition.Code == "LEAVE")
                {
                    if (!submit) { clean.StartSession ??= "AM"; clean.EndSession ??= "PM"; }
                    if (clean.StartSession is not ("AM" or "PM") || clean.EndSession is not ("AM" or "PM")) throw new InvalidOperationException("Vui lòng chọn buổi bắt đầu và kết thúc nghỉ.");
                    if (clean.StartSession == "PM") clean.Duration -= .5m;
                    if (clean.EndSession == "AM") clean.Duration -= .5m;
                    if (clean.Duration <= 0) throw new InvalidOperationException("Khoảng nghỉ không hợp lệ.");
                }
                if (clean.Duration > 366) throw new InvalidOperationException("Khoảng thời gian không được quá 366 ngày.");
            }
            request.RequestedDate = clean.StartAt.Value.Date;
        }
        if (clean.PassengerCount.HasValue && (clean.PassengerCount <= 0 || clean.PassengerCount > 100)) throw new InvalidOperationException("Số hành khách phải từ 1 đến 100.");
        if (definition.Code == "VEHICLE" && submit && clean.PassengerCount.HasValue && clean.PassengerCount != clean.ParticipantIds.Count)
            throw new InvalidOperationException($"Số hành khách ({clean.PassengerCount}) phải bằng số nhân viên tham gia đã chọn ({clean.ParticipantIds.Count}).");
        if (clean.PassengerCount.HasValue && clean.VehicleTypeId.HasValue)
        {
            var capacity = clean.VehicleId.HasValue ? masters[clean.VehicleId.Value].Capacity : masters[clean.VehicleTypeId.Value].Capacity;
            if (capacity.HasValue && clean.PassengerCount > capacity) throw new InvalidOperationException("Số hành khách vượt quá sức chứa đã cấu hình của xe.");
        }
        if (submit && clean.VehicleId.HasValue && await db.RequestBusinessData.AnyAsync(x => x.RequestId != request.Id && x.VehicleId == clean.VehicleId && x.StartAt < clean.EndAt && x.EndAt > clean.StartAt &&
            (x.Request.Status == RequestStatus.Ongoing || x.Request.Status == RequestStatus.Completed && !db.VehicleDispatches.Any(d => d.RequestId == x.RequestId)), ct))
            throw new InvalidOperationException("Xe đã có lịch trong khoảng thời gian này. Vui lòng chọn xe hoặc thời gian khác.");
        if (clean.BankId.HasValue) clean.BankName = masters[clean.BankId.Value].Name;
        if (submit && clean.PaymentMethodId.HasValue && masters[clean.PaymentMethodId.Value].Code == "TRANSFER" && (string.IsNullOrWhiteSpace(clean.BankAccount) || !clean.BankId.HasValue)) throw new InvalidOperationException("Thanh toán chuyển khoản cần số tài khoản và ngân hàng.");
        if (submit && definition.RequireLines && clean.Items.Count == 0) throw new InvalidOperationException("Vui lòng nhập ít nhất một dòng chi tiết.");
        var lines = new List<RequestLineItem>();
        foreach (var line in clean.Items)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(line, new ValidationContext(line), results, true)) throw new InvalidOperationException(string.Join(" ", results.Select(x => x.ErrorMessage)));
            if (line.Quantity != decimal.Round(line.Quantity, 2) || line.UnitPrice != decimal.Round(line.UnitPrice, 2)) throw new InvalidOperationException("Số lượng và đơn giá chỉ được có tối đa 2 chữ số thập phân.");
            if (!line.CatalogItemId.HasValue || !masters.TryGetValue(line.CatalogItemId.Value, out var catalog) || catalog.Category != definition.LineCategory) throw new InvalidOperationException("Vui lòng chọn hàng hóa / thiết bị / khoản mục từ danh mục đang sử dụng.");
            if (definition.Code == "EQUIPMENT" && line.Quantity != decimal.Truncate(line.Quantity)) throw new InvalidOperationException("Số lượng thiết bị phải là số nguyên.");
            var total = definition.PricedLines ? Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero) : 0;
            if (total > 9999999999999999.99m) throw new InvalidOperationException("Thành tiền vượt quá giới hạn lưu trữ.");
            lines.Add(new() { LineOrder=lines.Count+1, CatalogItemId=catalog.Id, Description=line.Description?.Trim(), Unit=catalog.Unit ?? line.Unit?.Trim(), Quantity=line.Quantity, UnitPrice=definition.PricedLines ? line.UnitPrice : 0, LineTotal=total, Note=line.Note?.Trim() });
        }
        if (definition.PricedLines)
        {
            if (clean.CurrencyId.HasValue && masters[clean.CurrencyId.Value].Code == "VND") clean.ExchangeRate = 1;
            if (clean.ExchangeRate.HasValue && (clean.ExchangeRate <= 0 || clean.ExchangeRate > 1000000000)) throw new InvalidOperationException("Tỷ giá phải lớn hơn 0 và không quá 1 tỷ.");
            if (clean.ExchangeRate.HasValue && clean.ExchangeRate != decimal.Round(clean.ExchangeRate.Value, 6)) throw new InvalidOperationException("Tỷ giá chỉ được có tối đa 6 chữ số thập phân.");
            clean.OriginalAmount = lines.Sum(x => x.LineTotal);
            if (submit && definition.RequireLines && clean.OriginalAmount <= 0) throw new InvalidOperationException("Tổng chi phí phải lớn hơn 0 khi gửi yêu cầu.");
            if (clean.OriginalAmount > 9999999999999999.99m) throw new InvalidOperationException("Tổng chi phí vượt quá giới hạn lưu trữ.");
            request.Amount = clean.ExchangeRate.HasValue ? Math.Round(clean.OriginalAmount.Value * clean.ExchangeRate.Value, 2, MidpointRounding.AwayFromZero) : null;
            if (request.Amount > 9999999999999999.99m) throw new InvalidOperationException("Tổng chi phí quy đổi vượt quá giới hạn lưu trữ.");
        }
        else if (definition.Code != "INTERNAL") request.Amount = null;
        request.BusinessData ??= new RequestBusinessData();
        var entity = request.BusinessData;
        foreach (var property in ValueProperties) property.SetValue(entity, property.GetValue(clean));
        entity.TemplateCode = definition.Code;
        // Reuse existing line keys to avoid unique-index conflicts during reordering or editing.
        var existing = entity.Items.OrderBy(x => x.LineOrder).ToList();
        for (var i=0; i<lines.Count; i++)
        {
            if (i >= existing.Count) { entity.Items.Add(lines[i]); continue; }
            var target = existing[i]; var source=lines[i];
            target.CatalogItemId=source.CatalogItemId; target.Description=source.Description; target.Unit=source.Unit; target.Quantity=source.Quantity; target.UnitPrice=source.UnitPrice; target.LineTotal=source.LineTotal; target.Note=source.Note;
        }
        foreach (var extra in existing.Skip(lines.Count)) entity.Items.Remove(extra);
        foreach (var old in entity.Participants.Where(x => !clean.ParticipantIds.Contains(x.EmployeeId)).ToList()) entity.Participants.Remove(old);
        foreach (var id in clean.ParticipantIds.Where(id => !entity.Participants.Any(x => x.EmployeeId==id))) entity.Participants.Add(new() {EmployeeId=id});
    }

    public async Task PopulateDetailsAsync(RequestDetailsDto dto, RequestBusinessData entity, CancellationToken ct)
    {
        if (entity == null) return;
        dto.TemplateCode = entity.TemplateCode;
        dto.BusinessData = ToInput(entity);
        var definition = RequestFormCatalog.Get(entity.TemplateCode);
        var masterIds = definition.Fields.Where(x => x.Lookup != null && RequestFormCatalog.MasterCategories.ContainsKey(x.Lookup)).Select(x => (int?)ValueProperties.Single(p => p.Name==x.Name).GetValue(entity)).Where(x => x.HasValue).Select(x => x.Value).Concat(entity.Items.Where(x => x.CatalogItemId.HasValue).Select(x => x.CatalogItemId.Value)).Distinct().ToList();
        var masters = await db.RequestMasterItems.AsNoTracking().Where(x => masterIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id,ct);
        var employeeIds = new[]{entity.EmployeeId,entity.ReplacementEmployeeId,entity.DriverEmployeeId}.Concat(entity.Participants.Select(x=>x.EmployeeId)).Where(x=>x!=null).Distinct().ToList();
        var employees = await db.AppUsers.AsNoTracking().Where(x=>employeeIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.FullName,ct);
        var department = entity.TargetDepartmentId.HasValue ? await db.Departments.Where(x=>x.Id==entity.TargetDepartmentId).Select(x=>x.Name).SingleOrDefaultAsync(ct) : null;
        var workSite = entity.WorkSiteId.HasValue ? await db.BelongSites.Where(x=>x.Id==entity.WorkSiteId).Select(x=>x.Name).SingleOrDefaultAsync(ct) : null;
        foreach (var field in definition.Fields)
        {
            var value=ValueProperties.Single(x=>x.Name==field.Name).GetValue(entity);
            string display = value switch { null=>"—", DateTime date=>date.ToString(field.Input=="datetime-local" ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy"), decimal number=>number.ToString(field.Name=="ExchangeRate" ? "N6" : "N2",CultureInfo.GetCultureInfo("vi-VN")), _=>value.ToString() };
            if (value != null && field.Lookup is "employees" or "drivers") display=employees.GetValueOrDefault((string)value,"—");
            else if(value != null && field.Lookup=="departments") display=department ?? "—";
            else if(value != null && field.Lookup=="sites") display=workSite ?? "—";
            else if(value != null && field.Lookup=="sessions") display=(string)value=="AM" ? "Buổi sáng" : "Buổi chiều";
            else if(value != null && field.Lookup != null && masters.TryGetValue(Convert.ToInt32(value),out var master)) display=master.Name;
            dto.BusinessFields.Add(new(field.Label,display));
        }
        if(entity.Duration.HasValue) dto.BusinessFields.Add(new(definition.Code is "OVERTIME" or "VEHICLE" ? "Thời lượng (giờ)" : "Số ngày (theo lịch)",entity.Duration.Value.ToString("N2")));
        dto.ParticipantNames=entity.Participants.Select(x=>employees.GetValueOrDefault(x.EmployeeId,x.EmployeeId)).ToList();
        dto.BusinessItems=entity.Items.OrderBy(x=>x.LineOrder).Select(x=>new RequestLineDisplayDto(x.CatalogItemId.HasValue && masters.TryGetValue(x.CatalogItemId.Value,out var master) ? master.Name : "—",x.Description,x.Unit,x.Quantity,x.UnitPrice,x.LineTotal,x.Note)).ToList();
    }
}

