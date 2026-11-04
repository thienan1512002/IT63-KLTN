# iWorkspace — Hệ thống phê duyệt yêu cầu

Ứng dụng ASP.NET Core quản lý yêu cầu, quy trình phê duyệt, báo cáo và vận hành sau phê duyệt. Giải pháp gồm Web, Worker và Worker.Shared.

## Thiết lập cục bộ

1. Cài .NET SDK 10 và SQL Server hoặc SQL Server LocalDB.
2. Sao chép `src/Web/appsettings.example.json` thành `src/Web/appsettings.json` và `src/Worker/appsettings.example.json` thành `src/Worker/appsettings.json`. Chỉnh chuỗi kết nối phù hợp.
3. Cấu hình SMTP cho Worker bằng biến môi trường hoặc user secrets. Không đưa thông tin xác thực vào Git.
4. Khôi phục và build bằng `dotnet restore IT63.slnx` và `dotnet build IT63.slnx`. Chạy Web và Worker theo cấu hình môi trường của bạn.

Các tệp cấu hình cục bộ, dữ liệu tải lên và kết quả build được loại khỏi Git.
