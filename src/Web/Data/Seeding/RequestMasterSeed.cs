using Microsoft.EntityFrameworkCore;
using Web.Data.Entities.Workflow;
namespace Web.Data.Seeding;
public static class RequestMasterSeed
{
    public static void Apply(ModelBuilder builder)
    {
        var items = new List<RequestMasterItem>();
        void Add(int id, string category, string code, string name, int? parent = null, string unit = null) => items.Add(new() { Id=id, Category=category, Code=code, Name=name, ParentId=parent, Unit=unit, IsActive=true });
        Add(1,"LeaveType","ANNUAL","Nghỉ phép năm"); Add(2,"LeaveType","UNPAID","Nghỉ không hưởng lương"); Add(3,"LeaveType","SICK","Nghỉ ốm"); Add(4,"LeaveType","PERSONAL","Nghỉ việc riêng");
        Add(10,"TripPurpose","CUSTOMER","Gặp gỡ khách hàng / Đối tác"); Add(11,"TripPurpose","CONFERENCE","Tham gia hội thảo / Triển lãm"); Add(12,"TripPurpose","TRAINING","Đào tạo / Tập huấn"); Add(13,"TripPurpose","SITE_VISIT","Khảo sát / Làm việc tại cơ sở");
        Add(20,"Country","VN","Việt Nam"); Add(21,"Country","JP","Nhật Bản"); Add(22,"Country","SG","Singapore");
        Add(30,"City","VN-HN","Hà Nội",20); Add(31,"City","VN-HCM","Thành phố Hồ Chí Minh",20); Add(32,"City","VN-DN","Đà Nẵng",20); Add(33,"City","JP-TOKYO","Tokyo",21); Add(34,"City","JP-OSAKA","Osaka",21); Add(35,"City","SG-SINGAPORE","Singapore",22);
        Add(40,"Currency","VND","Việt Nam đồng (VND)"); Add(41,"Currency","JPY","Yên Nhật (JPY)"); Add(42,"Currency","USD","Đô la Mỹ (USD)"); Add(43,"Currency","SGD","Đô la Singapore (SGD)");
        Add(50,"ExpenseCategory","AIRFARE","Vé máy bay",unit:"vé"); Add(51,"ExpenseCategory","HOTEL","Lưu trú / Khách sạn",unit:"đêm"); Add(52,"ExpenseCategory","MEALS","Ăn uống / Công tác phí",unit:"ngày"); Add(53,"ExpenseCategory","TRANSPORT","Di chuyển",unit:"lượt"); Add(54,"ExpenseCategory","SERVICE","Dịch vụ",unit:"lần"); Add(55,"ExpenseCategory","OTHER","Chi phí khác",unit:"khoản");
        Add(60,"Product","LAPTOP","Máy tính xách tay",unit:"chiếc"); Add(61,"Product","STATIONERY","Văn phòng phẩm",unit:"bộ");
        Add(70,"Equipment","LAPTOP","Máy tính xách tay",unit:"chiếc"); Add(71,"Equipment","MONITOR","Màn hình",unit:"chiếc"); Add(72,"Equipment","KEYBOARD","Bàn phím",unit:"chiếc"); Add(73,"Equipment","MOUSE","Chuột máy tính",unit:"chiếc");
        Add(80,"OvertimeType","WEEKDAY","Làm thêm ngày thường"); Add(81,"OvertimeType","WEEKEND","Làm thêm ngày nghỉ"); Add(82,"OvertimeType","HOLIDAY","Làm thêm ngày lễ");
        Add(90,"PaymentMethod","TRANSFER","Chuyển khoản"); Add(91,"PaymentMethod","CASH","Tiền mặt");
        Add(100,"ProposalCategory","PROCESS","Cải tiến quy trình"); Add(101,"ProposalCategory","WORKPLACE","Cải thiện môi trường làm việc"); Add(102,"ProposalCategory","TRAINING","Đào tạo nhân sự"); Add(103,"ProposalCategory","OTHER","Đề xuất khác");
        items.AddRange(new[] { new RequestMasterItem { Id=110,Category="VehicleType",Code="4_SEATS",Name="Xe 4 chỗ",Capacity=3 }, new RequestMasterItem { Id=111,Category="VehicleType",Code="7_SEATS",Name="Xe 7 chỗ",Capacity=6 }, new RequestMasterItem { Id=112,Category="VehicleType",Code="16_SEATS",Name="Xe 16 chỗ",Capacity=15 } });
        builder.Entity<RequestMasterItem>().HasData(items);
    }
}

