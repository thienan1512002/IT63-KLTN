using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Admin;
using Web.Data.Entities.Identity;
using Web.Data.Entities.Workflow.Operations;

namespace Web.Data.Seeding;

public record UniversityUnit(string Code, string Name, string FirstSection, string SecondSection, string FirstEnglish, string SecondEnglish, string StaffPosition);
public record UniversityPerson(int Number, string FullName, string Position, string UnitCode, int SectionNumber, string[] Roles, bool IsDriver)
{
    public string Id => $"c0000001-0000-4000-8000-{Number:D12}";
    public string UserName => $"uni{Number:D3}";
    public string EmployeeCode => $"UNI{Number:D4}";
    public string Email => $"{UserName}@namviet.example";
}
public record UniversitySeedResult(int SiteId, int AddedSites, int AddedDepartments, int AddedSections, int AddedUsers, int ExistingUsers, int AddedDrivers);

/// <summary>Explicit, additive demo-data import. Never called automatically on application startup.</summary>
public class UniversityDemoSeeder(ApplicationDbContext db, IPasswordHasher<AppUser> hasher)
{
    public const string SiteCode = "UNI-NV";
    public const string SiteName = "Trường Đại học Nam Việt (giả lập)";
    public const string DefaultPassword = "Daihoc@2026!";
    public const int UserCount = 200;
    private const string SeedActor = "university-demo-seed-v1";

    public static readonly UniversityUnit[] Units = [
        new("BGH", "Ban Giám hiệu", "Văn phòng Hiệu trưởng", "Tổ Thư ký Ban Giám hiệu", "Rector's Office", "University Executive Secretariat", "Chuyên viên văn phòng"),
        new("TCNS", "Phòng Tổ chức - Nhân sự", "Bộ phận Tổ chức - Tuyển dụng", "Bộ phận Chế độ - Chính sách", "Organization and Recruitment", "Compensation and Personnel Policy", "Chuyên viên nhân sự"),
        new("DTDH", "Phòng Đào tạo Đại học", "Bộ phận Kế hoạch - Chương trình", "Bộ phận Học vụ - Văn bằng", "Academic Planning and Curriculum", "Academic Records and Degrees", "Chuyên viên đào tạo"),
        new("SDH", "Phòng Đào tạo Sau đại học", "Bộ phận Đào tạo Thạc sĩ", "Bộ phận Đào tạo Tiến sĩ", "Master's Programs", "Doctoral Programs", "Chuyên viên sau đại học"),
        new("CTSV", "Phòng Công tác Sinh viên", "Bộ phận Công tác Sinh viên", "Bộ phận Hỗ trợ - Việc làm", "Student Affairs", "Student Support and Careers", "Chuyên viên công tác sinh viên"),
        new("KHTC", "Phòng Kế hoạch - Tài chính", "Bộ phận Kế hoạch - Ngân sách", "Bộ phận Kế toán - Thanh toán", "Planning and Budget", "Accounting and Payments", "Chuyên viên tài chính - kế toán"),
        new("HCQT", "Phòng Hành chính - Quản trị", "Bộ phận Hành chính - Cơ sở vật chất", "Đội xe - Hậu cần", "Administration and Facilities", "Transport and Logistics", "Chuyên viên hành chính"),
        new("KTDBCL", "Phòng Khảo thí và Bảo đảm Chất lượng", "Bộ phận Khảo thí", "Bộ phận Bảo đảm Chất lượng", "Examinations", "Quality Assurance", "Chuyên viên khảo thí - chất lượng"),
        new("KHCNHTQT", "Phòng Khoa học Công nghệ và Hợp tác Quốc tế", "Bộ phận Quản lý Khoa học", "Bộ phận Hợp tác Quốc tế", "Research Management", "International Cooperation", "Chuyên viên khoa học - hợp tác quốc tế"),
        new("TTPC", "Phòng Thanh tra - Pháp chế", "Bộ phận Thanh tra Nội bộ", "Bộ phận Pháp chế", "Internal Inspection", "Legal Affairs", "Chuyên viên thanh tra - pháp chế"),
        new("KCNTT", "Khoa Công nghệ Thông tin", "Bộ môn Công nghệ Phần mềm", "Bộ môn Hệ thống Thông tin", "Software Engineering", "Information Systems", "Giảng viên công nghệ thông tin"),
        new("KKTCN", "Khoa Kỹ thuật - Công nghệ", "Bộ môn Điện - Điện tử", "Bộ môn Cơ khí - Tự động hóa", "Electrical and Electronics Engineering", "Mechanical Engineering and Automation", "Giảng viên kỹ thuật"),
        new("KKTQTKD", "Khoa Kinh tế - Quản trị Kinh doanh", "Bộ môn Kinh tế", "Bộ môn Quản trị Kinh doanh", "Economics", "Business Administration", "Giảng viên kinh tế - quản trị"),
        new("KKTTT", "Khoa Kế toán - Tài chính", "Bộ môn Kế toán - Kiểm toán", "Bộ môn Tài chính - Ngân hàng", "Accounting and Auditing", "Finance and Banking", "Giảng viên kế toán - tài chính"),
        new("KNN", "Khoa Ngoại ngữ", "Bộ môn Tiếng Anh", "Bộ môn Ngôn ngữ và Văn hóa", "English", "Languages and Culture", "Giảng viên ngoại ngữ"),
        new("KLUAT", "Khoa Luật", "Bộ môn Luật Kinh tế", "Bộ môn Luật Dân sự - Hành chính", "Business Law", "Civil and Administrative Law", "Giảng viên luật"),
        new("KKHCB", "Khoa Khoa học Cơ bản", "Bộ môn Toán - Tin học", "Bộ môn Khoa học Tự nhiên", "Mathematics and Computing", "Natural Sciences", "Giảng viên khoa học cơ bản"),
        new("TTVHL", "Trung tâm Thư viện - Học liệu", "Bộ phận Nghiệp vụ Thư viện", "Bộ phận Học liệu số", "Library Services", "Digital Learning Resources", "Chuyên viên thư viện - học liệu"),
        new("TTCNTT", "Trung tâm Công nghệ Thông tin", "Bộ phận Hạ tầng - Mạng", "Bộ phận Phần mềm - Dữ liệu", "Infrastructure and Networks", "Applications and Data", "Chuyên viên công nghệ thông tin"),
        new("TTTSTT", "Trung tâm Tuyển sinh - Truyền thông", "Bộ phận Tuyển sinh", "Bộ phận Truyền thông - Quan hệ Công chúng", "Admissions", "Communications and Public Relations", "Chuyên viên tuyển sinh - truyền thông")
    ];

    public static IReadOnlyList<UniversityPerson> People()
    {
        string[] surnames = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng"];
        string[] names = ["Minh Anh", "Quốc Bảo", "Ngọc Châu", "Tiến Dũng", "Thùy Dương", "Minh Đức", "Thu Hà", "Trung Hiếu", "Khánh Hòa", "Mạnh Hùng", "Thanh Hương", "Bảo Khánh", "Phương Linh", "Hoàng Long", "Nhật Minh", "Hồng Ngọc", "Hữu Phúc", "Quỳnh Trang", "Anh Tuấn", "Hải Yến"];
        var people = new List<UniversityPerson>();
        for (var i = 0; i < UserCount; i++)
        {
            var unit = Units[i / 10]; var slot = i % 10; var head = unit.Code.StartsWith('K') && unit.Name.StartsWith("Khoa ") ? "Trưởng khoa" : unit.Name.StartsWith("Trung tâm ") ? "Giám đốc trung tâm" : "Trưởng phòng";
            var deputyTitle = head.StartsWith("Trưởng ") ? "Phó trưởng" + head[6..] : "Phó giám đốc trung tâm";
            var position = slot == 0 ? head : slot == 1 ? deputyTitle : slot == 5 ? (unit.Name.StartsWith("Khoa ") ? "Trưởng bộ môn" : "Trưởng bộ phận") : slot == 6 ? (unit.Name.StartsWith("Khoa ") ? "Phó trưởng bộ môn" : "Phó trưởng bộ phận") : unit.StaffPosition;
            if (unit.Code == "BGH") position = slot == 0 ? "Hiệu trưởng" : slot is 1 or 2 ? "Phó hiệu trưởng" : slot == 5 ? "Tổ trưởng Thư ký" : "Chuyên viên văn phòng Ban Giám hiệu";
            var driver = unit.Code == "HCQT" && slot >= 6;
            if (unit.Code == "HCQT" && slot == 5) position = "Đội trưởng Đội xe - Hậu cần";
            if (driver) position = slot == 6 ? "Tổ phó Đội xe - Tài xế" : "Tài xế";
            var roles = new List<string> { RoleConstants.Employee };
            if (slot is 0 or 1 or 5 or 6 || unit.Code == "BGH" && slot == 2) roles.Add(RoleConstants.Approver);
            if (unit.Code == "TCNS" && slot is 0 or 1) roles.AddRange([RoleConstants.HRPersonnelManage, RoleConstants.HRLeaveManage]);
            if (unit.Code == "TCNS" && slot == 2) roles.AddRange([RoleConstants.HRPersonnelView, RoleConstants.HRLeaveView]);
            if (unit.Code == "HCQT" && slot is 0 or 1 or 5) roles.Add(RoleConstants.TransportDispatch);
            people.Add(new(i + 1, surnames[i / names.Length] + " " + names[i % names.Length], position, unit.Code, slot < 5 ? 1 : 2, roles.ToArray(), driver));
        }
        return people;
    }

    public async Task<UniversitySeedResult> SeedAsync(string password, Action<int, int> progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10 || !password.Any(char.IsLower) || !password.Any(char.IsUpper) || !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
            throw new ArgumentException("Mật khẩu seed cần ít nhất 10 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.");
        var plan = People(); var addedSite = 0; var addedDepartments = 0; var addedSections = 0; var addedUsers = 0; var addedDrivers = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var neededRoles = plan.SelectMany(p => p.Roles).Distinct().ToArray();
        var roleMap = await db.Roles.AsNoTracking().Where(x => neededRoles.Contains(x.Name)).ToDictionaryAsync(x => x.Name, x => x.Id, ct);
        if (plan.SelectMany(x => x.Roles).Except(roleMap.Keys).Any()) throw new InvalidOperationException("Thiếu các role cần thiết. Hãy cập nhật đầy đủ migrations trước khi seed.");
        var site = await db.BelongSites.SingleOrDefaultAsync(x => x.Code == SiteCode, ct);
        if (site != null && site.Name != SiteName && !await db.Departments.AnyAsync(x => x.SiteId == site.Id && x.CreatedBy == SeedActor, ct))
            throw new InvalidOperationException("Mã cơ sở UNI-NV đang thuộc dữ liệu khác. Không nhập dữ liệu đại học vào cơ sở này.");
        if (site == null)
        {
            if (await db.BelongSites.AnyAsync(x => x.Name == SiteName, ct)) throw new InvalidOperationException("Tên cơ sở đại học đã được dùng bởi dữ liệu khác. Không ghi đè dữ liệu hiện có.");
            site = new BelongSite { Code = SiteCode, Name = SiteName, Description = "Dữ liệu giả lập trường đại học: 20 phòng/khoa/trung tâm, 40 bộ phận/bộ môn, 200 cán bộ và giảng viên.", IsActive = true, CreatedAt = DateTime.UtcNow };
            db.BelongSites.Add(site); await db.SaveChangesAsync(ct); addedSite++;
        }
        var departments = await db.Departments.Where(x => x.SiteId == site.Id).ToListAsync(ct);
        var sections = await db.Sections.Where(x => x.SiteId == site.Id).ToListAsync(ct);
        var sectionMap = new Dictionary<(string, int), Section>();
        for (var index = 0; index < Units.Length; index++)
        {
            var unit = Units[index]; var main = plan[index * 10]; var backup = plan[index * 10 + 1];
            var matches = departments.Where(x => x.ShortName == unit.Code).ToList();
            if (matches.Count > 1) throw new InvalidOperationException($"Mã đơn vị {unit.Code} bị trùng. Không thể seed an toàn.");
            var department = matches.SingleOrDefault();
            if (department == null)
            {
                if (departments.Any(x => x.Name == unit.Name)) throw new InvalidOperationException($"Đơn vị {unit.Name} đã tồn tại với mã khác.");
                department = new Department { Name = unit.Name, ShortName = unit.Code, Description = "Đơn vị thuộc " + SiteName,
                    SiteId = site.Id, MainApprover = main.UserName, BackupApprover = backup.UserName, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = SeedActor };
                db.Departments.Add(department); await db.SaveChangesAsync(ct); departments.Add(department); addedDepartments++;
            }
            else if (department.CreatedBy != SeedActor) throw new InvalidOperationException($"Mã đơn vị {unit.Code} thuộc dữ liệu khác. Không ghi đè.");
            for (var part = 1; part <= 2; part++)
            {
                var code = unit.Code + "-" + part; var manager = plan[index * 10 + (part == 1 ? 0 : 5)]; var deputy = plan[index * 10 + (part == 1 ? 1 : 6)];
                var name = part == 1 ? unit.FirstSection : unit.SecondSection;
                var existing = sections.Where(x => x.DepartmentId == department.Id && x.ShortName == code).ToList();
                if (existing.Count > 1) throw new InvalidOperationException($"Mã bộ phận {code} bị trùng.");
                var section = existing.SingleOrDefault();
                if (section == null)
                {
                    if (sections.Any(x => x.DepartmentId == department.Id && x.Name == name)) throw new InvalidOperationException($"Bộ phận {name} đã tồn tại với mã khác.");
                    section = new Section { Name = name, EngName = part == 1 ? unit.FirstEnglish : unit.SecondEnglish, ShortName = code,
                        Note = "Bộ phận/bộ môn thuộc " + unit.Name, DepartmentId = department.Id, SiteId = site.Id,
                        MainApprover = manager.UserName, BackupApprover = deputy.UserName, PIC = manager.UserName, DeputyApprovers = deputy.UserName,
                        IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = SeedActor };
                    db.Sections.Add(section); sections.Add(section); addedSections++;
                }
                else if (section.CreatedBy != SeedActor) throw new InvalidOperationException($"Mã bộ phận {code} thuộc dữ liệu khác. Không ghi đè.");
                sectionMap[(unit.Code, part)] = section;
            }
        }
        await db.SaveChangesAsync(ct);
        var ids = plan.Select(x => x.Id).ToArray(); var names = plan.Select(x => x.UserName.ToUpperInvariant()).ToArray(); var codes = plan.Select(x => x.EmployeeCode).ToArray(); var emails = plan.Select(x => x.Email.ToUpperInvariant()).ToArray();
        var existingUsers = await db.Users.Where(x => ids.Contains(x.Id) || names.Contains(x.NormalizedUserName) || codes.Contains(x.EmpId) || emails.Contains(x.NormalizedEmail)).ToListAsync(ct);
        // Validate every collision before creating any users; failures roll back new organizational data too.
        foreach (var person in plan)
        {
            var matches = existingUsers.Where(x => x.Id == person.Id || string.Equals(x.UserName, person.UserName, StringComparison.OrdinalIgnoreCase) || string.Equals(x.EmpId, person.EmployeeCode, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Email, person.Email, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1 || matches.Count == 1 && (matches[0].Id != person.Id || !string.Equals(matches[0].UserName, person.UserName, StringComparison.OrdinalIgnoreCase) || matches[0].EmpId != person.EmployeeCode))
                throw new InvalidOperationException($"Tài khoản hoặc mã nhân viên {person.UserName}/{person.EmployeeCode} trùng dữ liệu khác. Toàn bộ lần seed được hủy.");
        }
        foreach (var person in plan)
        {
            if (existingUsers.Any(x => x.Id == person.Id)) continue; // Preserve edits, credentials, claims and manually assigned roles.
            var user = new AppUser { Id = person.Id, UserName = person.UserName, NormalizedUserName = person.UserName.ToUpperInvariant(),
                Email = person.Email, NormalizedEmail = person.Email.ToUpperInvariant(), EmailConfirmed = true,
                FullName = person.FullName, EmpId = person.EmployeeCode, Position = person.Position, Avatar = string.Empty,
                SiteId = site.Id, SectionId = sectionMap[(person.UnitCode, person.SectionNumber)].Id,
                DateOfBirth = new DateTime(1974 + person.Number % 25, 1 + person.Number % 12, 1 + person.Number % 28),
                HireDate = new DateTime(2010 + person.Number % 15, 1 + person.Number % 12, 1), CreatedAt = DateTime.UtcNow,
                IsActive = true, IsOnline = false, LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            foreach (var role in person.Roles) db.UserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = roleMap[role] });
            db.UserClaims.Add(new IdentityUserClaim<string> { UserId = user.Id, ClaimType = "IsFirstTimeLogin", ClaimValue = "true" });
            if (person.IsDriver) { db.DriverProfiles.Add(new DriverProfile { EmployeeId = user.Id, IsActive = true }); addedDrivers++; }
            addedUsers++; if (addedUsers % 25 == 0) progress?.Invoke(addedUsers, UserCount - existingUsers.Count);
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(site.Id, addedSite, addedDepartments, addedSections, addedUsers, UserCount - addedUsers, addedDrivers);
    }
}
