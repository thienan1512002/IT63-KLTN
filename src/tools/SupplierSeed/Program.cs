using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;

var suppliers = new[] {
    ("NV-SUP-IT", "Công ty Thiết bị Công nghệ Học đường (giả lập)"),
    ("NV-SUP-LAB", "Công ty Thiết bị Phòng thí nghiệm Nam Việt (giả lập)"),
    ("NV-SUP-BOOK", "Công ty Sách và Học liệu Tri thức (giả lập)"),
    ("NV-SUP-OFFICE", "Công ty Văn phòng phẩm Đại học (giả lập)"),
    ("NV-SUP-FURNITURE", "Công ty Nội thất Giáo dục An Phú (giả lập)"),
    ("NV-SUP-PRINT", "Công ty In ấn và Dịch vụ Học đường (giả lập)")
};
if (!args.Contains("--apply")) { Console.WriteLine($"Dự kiến thêm tối đa {suppliers.Length} nhà cung cấp giả lập. Dùng --apply để thực hiện."); return; }
var root=Directory.GetCurrentDirectory();
var config=new ConfigurationBuilder().SetBasePath(Path.Combine(root,"Web")).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true).AddEnvironmentVariables().Build();
await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
var added=0;
foreach(var (code,name) in suppliers) {
    if(await db.RequestMasterItems.AnyAsync(x=>x.Category=="Partner" && x.Code==code)) continue;
    db.RequestMasterItems.Add(new RequestMasterItem { Category="Partner",Code=code,Name=name,IsActive=true }); added++;
}
await db.SaveChangesAsync(); await tx.CommitAsync();
Console.WriteLine($"Đã thêm {added} nhà cung cấp; {suppliers.Length-added} mã đã có được giữ nguyên.");
