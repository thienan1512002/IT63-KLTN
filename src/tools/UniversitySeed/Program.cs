using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Data.Seeding;

Console.OutputEncoding = Encoding.UTF8;
if (args.Contains("--help"))
{
    Console.WriteLine("dotnet run --project tools/UniversitySeed -- --apply [--source-root <src>] [--export <csv path>]");
    Console.WriteLine("Without --apply, prints the dataset plan only. Uses Web/appsettings.json + appsettings.Development.json + environment variables.");
    Console.WriteLine("Optional password override: UNIVERSITY_SEED_PASSWORD. Existing records and passwords are never overwritten.");
    return;
}
string Option(string name) { var i = Array.IndexOf(args, name); if (i < 0) return null; if (i + 1 >= args.Length || args[i + 1].StartsWith("--")) throw new ArgumentException("Missing value for " + name); return args[i + 1]; }
try
{
    var root = Path.GetFullPath(Option("--source-root") ?? Directory.GetCurrentDirectory());
    if (!File.Exists(Path.Combine(root, "Web", "appsettings.json"))) throw new InvalidOperationException("Run from the src directory or specify --source-root.");
    var export = Path.GetFullPath(Option("--export") ?? Path.Combine(root, "seed-data", "university-users.csv"));
    Console.WriteLine($"Dataset: {UniversityDemoSeeder.SiteName}; {UniversityDemoSeeder.Units.Length} units; 40 sections; {UniversityDemoSeeder.UserCount} staff accounts (uni001–uni200).");
    if (!args.Contains("--apply")) { Console.WriteLine("Preview only. Add --apply to seed the configured development database."); return; }
    var config = new ConfigurationBuilder().SetBasePath(Path.Combine(root, "Web")).AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", optional: true).AddEnvironmentVariables().Build();
    var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options;
    await using var db = new ApplicationDbContext(options);
    if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("The database has pending migrations. Apply them before running this seed command.");
    var seeder = new UniversityDemoSeeder(db, new PasswordHasher<AppUser>());
    var result = await seeder.SeedAsync(Environment.GetEnvironmentVariable("UNIVERSITY_SEED_PASSWORD") ?? UniversityDemoSeeder.DefaultPassword,
        (count, total) => Console.WriteLine($"Prepared {count}/{total} new accounts..."));
    var userIds = UniversityDemoSeeder.People().Select(x => x.Id).ToArray();
    var rows = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.Id)).OrderBy(x => x.UserName)
        .Select(x => new { x.Id, x.UserName, x.EmpId, x.FullName, x.Email, x.Position, Site = x.BelongSite.Name,
            Department = x.Section.Department.Name, Section = x.Section.Name }).ToListAsync();
    var roles = await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id where userIds.Contains(ur.UserId)
        select new { ur.UserId, role.Name }).ToListAsync();
    string Cell(string value) {
        value ??= string.Empty;
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    var csv = new List<string> { string.Join(",", new[] { "Tài khoản", "Mã cán bộ", "Họ tên", "Email", "Chức vụ", "Cơ sở", "Phòng/Khoa/Trung tâm", "Bộ phận/Bộ môn", "Quyền" }.Select(Cell)) };
    csv.AddRange(rows.Select(x => string.Join(",", new[] { x.UserName, x.EmpId, x.FullName, x.Email, x.Position, x.Site, x.Department, x.Section,
        string.Join("; ", roles.Where(r => r.UserId == x.Id).Select(r => r.Name).OrderBy(r => r)) }.Select(Cell))));
    Directory.CreateDirectory(Path.GetDirectoryName(export));
    await File.WriteAllLinesAsync(export, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    Console.WriteLine($"Committed: +{result.AddedSites} site, +{result.AddedDepartments} units, +{result.AddedSections} sections, +{result.AddedUsers} users, +{result.AddedDrivers} drivers; {result.ExistingUsers} existing seed accounts preserved.");
    Console.WriteLine($"University site ID: {result.SiteId}. Exported {rows.Count} staff records: {export}");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex is Microsoft.Data.SqlClient.SqlException ? "SQL Server seed failed. Check database availability, permissions and connection configuration." : ex.Message);
    Environment.ExitCode = 1;
}
