using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Web.Data.DbContexts;
using Web.Data.Seeding;
using Web.Services.Workflow.Operations;

Console.OutputEncoding = Encoding.UTF8;
if (args.Contains("--help"))
{
    Console.WriteLine("dotnet run --project tools/LeaveSeed -- --year 2026 [--apply] [--source-root <src>]");
    Console.WriteLine("Adds 12 annual-leave days for active users without a ledger. Existing grants, policies, usage and adjustments are preserved.");
    return;
}
string Option(string name) {
    var i = Array.IndexOf(args, name); if (i < 0) return null;
    if (i + 1 >= args.Length || args[i + 1].StartsWith("--")) throw new ArgumentException("Missing value for " + name);
    return args[i + 1];
}
try
{
    var year = int.Parse(Option("--year") ?? DateTime.UtcNow.AddHours(7).Year.ToString());
    if (year is < 2000 or > 2200) throw new ArgumentException("Year must be between 2000 and 2200.");
    var root = Path.GetFullPath(Option("--source-root") ?? Directory.GetCurrentDirectory());
    if (!File.Exists(Path.Combine(root, "Web/appsettings.json"))) throw new InvalidOperationException("Run from src or specify --source-root.");
    var config = new ConfigurationBuilder().SetBasePath(Path.Combine(root, "Web")).AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", optional: true).AddEnvironmentVariables().Build();
    await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
    if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply pending database migrations before seeding leave.");
    var users = await db.Users.CountAsync(x => x.IsActive);
    Console.WriteLine($"Year {year}: {users} active users; 12 days of annual leave each. Missing calendars default to Monday–Friday.");
    if (!args.Contains("--apply")) { Console.WriteLine("Preview only. Add --apply to create missing ledgers and policies."); return; }
    var result = await new AnnualLeaveDemoSeeder(db).SeedAsync(year);
    Console.WriteLine($"Committed: +{result.AddedAccounts} ledgers; {result.ExistingAccounts} existing ledgers preserved; +{result.AddedLeavePolicies} annual-leave policies; +{result.AddedCalendars} work calendars.");
    Console.WriteLine($"Users without a site: {result.UsersWithoutSite}. Existing non-deducting site policies preserved: {result.PreservedNonDeductingSites}.");
    var sampleId = await db.Users.Where(x => x.IsActive && x.UserName == "uni002").Select(x => x.Id).FirstOrDefaultAsync();
    sampleId ??= await db.Users.Where(x => x.IsActive).Select(x => x.Id).FirstOrDefaultAsync();
    if (sampleId != null) {
        var balance = await new LeaveAccountingService(db).BalanceAsync(sampleId, year, result.LeaveTypeId, null, default);
        Console.WriteLine($"Sample ledger via balance service: annual={balance.AnnualDays}; used={balance.UsedDays}; reserved={balance.ReservedDays}; available={balance.AvailableDays}; policy={balance.PolicyConfigured}; deducts={balance.DeductsBalance}.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex is Microsoft.Data.SqlClient.SqlException ? "Leave seed failed. Check database availability, permissions and configuration." : ex.Message);
    Environment.ExitCode = 1;
}
