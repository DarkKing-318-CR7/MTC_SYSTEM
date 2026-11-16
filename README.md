# 📡 MTC System - Backend (Media & Telecommunication Centralized System)
> **Hệ thống Quản lý, Giám sát và Điều hành Tập trung Mạng lưới Màn hình / Cụm Loa Phát thanh Thông minh theo Mô hình Cây Cơ sở Đa cấp & Quản lý Thuê bao Gói cước SaaS.**
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-336791.svg)](https://www.postgresql.org/)
[![SignalR](https://img.shields.io/badge/SignalR-Realtime-green.svg)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-HLS%20Streaming-orange.svg)](https://ffmpeg.org/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)]()
---
## 📖 1. Giới thiệu Tổng quan (Overview)
**MTC System** là giải pháp nền tảng Backend SaaS phục vụ chuyển đổi số trong lĩnh vực truyền thanh cơ sở và bảng tin quảng cáo/thông tin đô thị. Hệ thống cho phép:
* **Quản trị đa khách hàng (Multi-tenancy)**: Phân quyền quản trị theo cây phân cấp địa bàn chặt chẽ: `Tổ chức` $\rightarrow$ `Cơ sở cha (Tỉnh/Quận/Huyện)` $\rightarrow$ `Cơ sở con (Phường/Xã)`.
* **Quản lý thiết bị & Giám sát Realtime**: Kết nối thiết bị (Màn hình LED/LCD, Cụm loa thông minh) qua WebSocket/SignalR, kích hoạt bảo mật qua mã OTP/QR một lần, điều khiển âm lượng và gửi lệnh reboot từ xa.
* **Kho Media dung lượng lớn & HLS Streaming**: Tải lên từng phần (Chunked Upload) file lên đến 1GB, kiểm định nhị phân chống mã độc (Deep Magic-bytes validation), tự động chuyển đổi sang chuẩn phát trực tuyến **HLS (HTTP Live Streaming)** thông qua FFmpeg.
* **Lập lịch & Thuật toán chống trùng lịch phát sóng**: Quản lý bản tin thường và bản tin khẩn cấp ưu tiên ghi đè, kiểm tra xung đột thời gian phát sóng trên thiết bị và tự động cập nhật trạng thái theo chu kỳ 1 phút.
* **Thương mại hóa Gói cước (Billing & Subscription Engine)**: Quản lý hợp đồng gói cước (dung lượng GB lưu trữ, số thiết bị), thuật toán tính toán tiền bù trừ tài chính (Proration Engine) khi khách hàng Nâng cấp / Hạ cấp gói cước.
---
## 🛠️ 2. Ngăn xếp Công nghệ (Tech Stack)
* **Ngôn ngữ & Nền tảng**: C# 12 / .NET 8 Web API
* **Cơ sở dữ liệu**: PostgreSQL với Entity Framework Core 8 (Code-First Migrations)
* **Thời gian thực (Realtime)**: ASP.NET Core SignalR (`DeviceHub`)
* **Xử lý Đa phương tiện**: FFmpeg & FFprobe (Transcoding H.264 / AAC, HLS slicing 6s, `.m3u8` playlist)
* **Bảo mật & Phân quyền**:
  * Xác thực JWT Bearer Token (AccessToken 15 phút, RefreshToken xoay vòng 7 ngày)
  * Xác thực 2 bước (2FA TOTP Google Authenticator & 10 mã dự phòng khẩn cấp)
  * Mã hóa mật khẩu: BCrypt
  * Hỗ trợ chứng chỉ bảo mật SSL/TLS tự cấp (`.pfx`)
  * Kiểm soát phân vùng dữ liệu tổ chức (Tenant Scoped Queries / Organization Isolation)
* **Tiện ích gửi mail & tài liệu**: MailKit (SMTP) & Swagger OpenAPI (Swashbuckle)
---
## 🏛️ 3. Cấu trúc Dự án (Project Structure)
```text
MTC_System/
├── Helpers/                            # Thư viện tiện ích dùng chung
│   └── SendMail.cs                     # Xử lý gửi email mẫu (EML-01..08) qua MailKit
├── MTC_System/                         # Dự án API chính
│   ├── Common/                         # BaseEntity, UserScopeContext, ApiResponse envelope
│   ├── Controllers/                    # 21 Controller xử lý API RESTful
│   ├── Data/                           # MyDbContext, Configurations Fluent API (34 bảng)
│   ├── DTOs/                           # Data Transfer Objects theo từng module
│   ├── Hubs/                           # SignalR DeviceHub (kết nối thiết bị)
│   ├── Migrations/                     # Lịch sử EF Core Migrations
│   ├── Models/                         # 34 Entities nghiệp vụ
│   ├── Realtime/                       # Background Services (Broadcast, Device Heartbeat)
│   ├── Services/                       # Business Logic Layer & Interfaces
│   │   ├── Implements/                 # MediaService, HlsProcessingService, ChunkedUpload...
│   │   └── Implementations/            # DeviceService, UserService, SubscriptionService...
│   ├── Properties/                     # launchSettings.json
│   ├── Program.cs                      # Khởi tạo DI, Middleware, Kestrel, Seed Data
│   └── appsettings.json                # Cấu hình Database, JWT, FFmpeg, Storage Root
├── FRONTEND_INTEGRATION_GUIDE.md       # Hướng dẫn chi tiết cho đội ngũ Frontend
├── MTC_API_Collection.postman_collection.json # Bộ Postman Test API toàn diện
