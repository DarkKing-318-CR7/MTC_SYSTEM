-- ==============================================================================
-- MTC SYSTEM - COMPLETE SEED DATA SCRIPT FOR SYSTEM-WIDE API TESTING
-- Bộ dữ liệu mẫu đầy đủ để kiểm thử toàn diện toàn bộ API hệ thống MTC Media
-- ==============================================================================

-- 1. BẢNG ORGANIZATIONS (TỔ CHỨC CẤP CAO NHẤT)
INSERT INTO "Organizations" ("Id", "OrganizationCode", "OrganizationName", "TaxCode", "Address", "Status", "EffectiveDate", "CreatedAt", "IsDeleted")
VALUES 
('00000000-0001-0000-0000-000000000001', 'ORG-01', 'Tổng Công ty Phát thanh Truyền hình MTC', '0312345678', 'Số 1 Lê Duẩn, Bến Nghé, Quận 1, TP.HCM', 'Còn hạn', NOW(), NOW(), false),
('00000000-0002-0000-0000-000000000001', 'ORG-02', 'Công ty Truyền thông và Quảng cáo Sài Gòn', '0398765432', 'Số 2 Hải Triều, Bến Nghé, Quận 1, TP.HCM', 'Còn hạn', NOW(), NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 2. BẢNG FACILITIES (CƠ SỞ CHA - CƠ SỞ CON)
-- Cơ sở Cha: Đài Truyền thanh Quận 1 (ParentId = NULL)
-- Cơ sở Con 1: Trạm Đa Kao (ParentId = Cơ sở Cha)
-- Cơ sở Con 2: Trạm Tân Định (ParentId = Cơ sở Cha)
INSERT INTO "Facilities" ("Id", "FacilityCode", "FacilityName", "OrganizationId", "ParentId", "CreatedAt", "IsDeleted")
VALUES 
('11111111-0001-0000-0000-000000000001', 'FAC-PARENT', 'Đài Truyền thanh Quận 1 (Cơ sở Cha)', '00000000-0001-0000-0000-000000000001', NULL, NOW(), false),
('11111111-0002-0000-0000-000000000001', 'FAC-CHILD-1', 'Trạm Truyền thanh Phường Đa Kao (Cơ sở Con 1)', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', NOW(), false),
('11111111-0003-0000-0000-000000000001', 'FAC-CHILD-2', 'Trạm Truyền thanh Phường Tân Định (Cơ sở Con 2)', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 3. BẢNG USERS (TÀI KHOẢN NGƯỜI DÙNG - MẬT KHẨU MẶC ĐỊNH: Admin@123456)
-- Hash BCrypt chuẩn của 'Admin@123456': $2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
INSERT INTO "Users" ("Id", "Username", "PasswordHash", "FullName", "Email", "PhoneNumber", "IsActive", "OrganizationId", "FacilityId", "TwoFactorEnabled", "CreatedAt", "IsDeleted")
VALUES 
-- 3.1. Quản trị viên cấp cao nhất (System Admin): không gắn Org/Facility
('11111111-1111-1111-1111-111111111111', 'admin', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Quản trị viên Hệ thống MTC', 'admin@mtc.vn', '0901234567', true, NULL, NULL, false, NOW(), false),
-- 3.2. Quản lý cấp Tổ chức (Org Admin): Gắn Org, Facility = null
('11111111-2222-1111-1111-111111111111', 'org_admin', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Quản lý Tổ chức ORG-01', 'org_admin@mtc.vn', '0902345678', true, '00000000-0001-0000-0000-000000000001', NULL, false, NOW(), false),
-- 3.3. Quản lý Cơ sở Cha (Parent Admin): Gắn Org và Facility Cha
('22222222-2222-2222-2222-222222222222', 'parent_admin', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Nguyễn Văn Cha (Quản lý Quận 1)', 'parent@mtc.vn', '0903456789', true, '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', false, NOW(), false),
-- 3.4. Nhân viên Cơ sở Con 1 (Child User): Gắn Org và Facility Con 1 (Đa Kao)
('33333333-3333-3333-3333-333333333333', 'child_user', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Trần Thị Con (Nhân viên Đa Kao)', 'child1@mtc.vn', '0904567890', true, '00000000-0001-0000-0000-000000000001', '11111111-0002-0000-0000-000000000001', false, NOW(), false),
-- 3.5. Nhân viên Cơ sở Con 2 (Tân Định)
('33333333-4444-3333-3333-333333333333', 'child_user2', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Lê Văn Tân (Nhân viên Tân Định)', 'child2@mtc.vn', '0905678901', true, '00000000-0001-0000-0000-000000000001', '11111111-0003-0000-0000-000000000001', false, NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 4. BẢNG USER ROLES (GÁN VAI TRÒ)
-- Role Administrator: aaaaaaaa-0001-0000-0000-000000000000
-- Role Creator: aaaaaaaa-0002-0000-0000-000000000000
INSERT INTO "UserRoles" ("Id", "UserId", "RoleId", "CreatedAt", "IsDeleted")
VALUES 
('a1111111-0001-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'aaaaaaaa-0001-0000-0000-000000000000', NOW(), false),
('a1111111-0002-0000-0000-000000000001', '11111111-2222-1111-1111-111111111111', 'aaaaaaaa-0001-0000-0000-000000000000', NOW(), false),
('a1111111-0003-0000-0000-000000000001', '22222222-2222-2222-2222-222222222222', 'aaaaaaaa-0002-0000-0000-000000000000', NOW(), false),
('a1111111-0004-0000-0000-000000000001', '33333333-3333-3333-3333-333333333333', 'aaaaaaaa-0002-0000-0000-000000000000', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 5. BẢNG SYSTEM SETTINGS (THIẾT LẬP HỆ THỐNG CẤP TỔ CHỨC)
INSERT INTO "SystemSettings" ("Id", "OrganizationId", "SettingKey", "SettingValue", "Description", "CreatedAt", "IsDeleted")
VALUES 
('c0000001-0001-0000-0000-000000000001', '00000000-0001-0000-0000-000000000001', 'BROADCAST_MODE', 'Phát theo cơ sở', 'Chế độ tạo bản tin: Phát theo cơ sở hoặc Phát theo thiết bị', NOW(), false),
('c0000001-0002-0000-0000-000000000001', '00000000-0001-0000-0000-000000000001', 'TIMEZONE', 'SE Asia Standard Time', 'Múi giờ chuẩn của tổ chức (UTC+07:00)', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 6. BẢNG DEVICE VENDORS & DEVICE GROUPS (NHÀ SẢN XUẤT & NHÓM THIẾT BỊ)
INSERT INTO "DeviceVendors" ("Id", "VendorCode", "VendorName", "ContactEmail", "ContactPhone", "CreatedAt", "IsDeleted")
VALUES 
('44444444-0001-0000-0000-000000000001', 'VND-ALTA', 'Tập đoàn Công nghệ Alta Media', 'support@altamedia.vn', '19001234', NOW(), false),
('44444444-0002-0000-0000-000000000001', 'VND-BOSCH', 'Tập đoàn Bosch Security Systems', 'info@bosch.vn', '19005678', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "DeviceGroups" ("Id", "GroupCode", "GroupName", "Description", "OrganizationId", "FacilityId", "CreatedAt", "IsDeleted")
VALUES 
('55555555-0001-0000-0000-000000000001', 'GRP-DAKAO', 'Cụm loa phát thanh Phường Đa Kao', 'Nhóm thiết bị loa IP tại Phường Đa Kao', '00000000-0001-0000-0000-000000000001', '11111111-0002-0000-0000-000000000001', NOW(), false),
('55555555-0002-0000-0000-000000000001', 'GRP-TANDINH', 'Cụm loa phát thanh Phường Tân Định', 'Nhóm thiết bị loa IP tại Phường Tân Định', '00000000-0001-0000-0000-000000000001', '11111111-0003-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 7. BẢNG DEVICES (THIẾT BỊ LOA IP / MÀN HÌNH LED)
INSERT INTO "Devices" ("Id", "DeviceCode", "DeviceName", "Description", "SerialNumber", "IpAddress", "OsType", "ConnectionStatus", "PlayStatus", "Volume", "Brightness", "OrganizationId", "FacilityId", "DeviceGroupId", "DeviceVendorId", "CreatedAt", "IsDeleted")
VALUES 
('d1111111-1111-1111-1111-111111111111', 'DEV-DAKAO-01', 'Loa IP Đa Kao Số 1 (Ngã tư Đinh Tiên Hoàng)', 'Loa IP công suất 50W', 'SN-DK-001', '192.168.1.101', 'Android', 'Đang hoạt động', 'Dừng phát bản tin', 75, 80, '00000000-0001-0000-0000-000000000001', '11111111-0002-0000-0000-000000000001', '55555555-0001-0000-0000-000000000001', '44444444-0001-0000-0000-000000000001', NOW(), false),
('d1111111-2222-2222-2222-222222222222', 'DEV-DAKAO-02', 'Loa IP Đa Kao Số 2 (Chợ Đa Kao)', 'Loa IP công suất 50W', 'SN-DK-002', '192.168.1.102', 'Android', 'Đang hoạt động', 'Dừng phát bản tin', 70, 80, '00000000-0001-0000-0000-000000000001', '11111111-0002-0000-0000-000000000001', '55555555-0001-0000-0000-000000000001', '44444444-0001-0000-0000-000000000001', NOW(), false),
('d1111111-3333-3333-3333-333333333333', 'DEV-TANDINH-01', 'Loa IP Tân Định Số 1 (Nhà thờ Tân Định)', 'Loa IP công suất 50W', 'SN-TD-001', '192.168.2.101', 'Android', 'Đang hoạt động', 'Dừng phát bản tin', 60, 80, '00000000-0001-0000-0000-000000000001', '11111111-0003-0000-0000-000000000001', '55555555-0002-0000-0000-000000000001', '44444444-0001-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 8. BẢNG MEDIA CATEGORIES & MEDIA
INSERT INTO "MediaCategories" ("Id", "CategoryCode", "CategoryName", "Description", "OrganizationId", "FacilityId", "CreatedAt", "IsDeleted")
VALUES 
('66666666-0001-0000-0000-000000000001', 'CAT-NEWS', 'Tin tức & Thời sự địa phương', 'Danh mục tin tức thời sự hằng ngày', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', NOW(), false),
('66666666-0002-0000-0000-000000000001', 'CAT-HEALTH', 'Tuyên truyền Sức khỏe & Y tế', 'Thông tin tuyên truyền phòng chống dịch bệnh', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Media" ("Id", "MediaName", "Description", "MediaType", "FileUrl", "StoragePath", "DurationSeconds", "FileSizeBytes", "Status", "OrganizationId", "FacilityId", "CategoryId", "CreatedAt", "IsDeleted")
VALUES 
('77777777-0001-0000-0000-000000000001', 'Bản tin Sáng 22-11.mp3', 'File âm thanh phát thanh sáng', 'Audio', '/storage/audio/tin_sang_2211.mp3', 'audio/tin_sang_2211.mp3', 600, 10485760, 'Đã duyệt', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', '66666666-0001-0000-0000-000000000001', NOW(), false),
('77777777-0002-0000-0000-000000000001', 'Tuyên truyền Phòng Cháy Chữa Cháy.mp3', 'Thông điệp PCCC mùa hanh khô', 'Audio', '/storage/audio/pccc.mp3', 'audio/pccc.mp3', 300, 5242880, 'Đã duyệt', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', '66666666-0002-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 9. BẢNG PLAYLISTS & PLAYLIST ITEMS
INSERT INTO "Playlists" ("Id", "PlaylistName", "Description", "TotalDurationSeconds", "Status", "OrganizationId", "FacilityId", "CreatedAt", "IsDeleted")
VALUES 
('88888888-0001-0000-0000-000000000001', 'Playlist Phát thanh Sáng 30 Phút', 'Danh sách phát 2 bài thời lượng cố định 900 giây', 900, 'Hoạt động', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "PlaylistItems" ("Id", "PlaylistId", "MediaId", "ItemOrder", "DurationSeconds", "CreatedAt", "IsDeleted")
VALUES 
('88888888-0002-0000-0000-000000000001', '88888888-0001-0000-0000-000000000001', '77777777-0001-0000-0000-000000000001', 1, 600, NOW(), false),
('88888888-0003-0000-0000-000000000001', '88888888-0001-0000-0000-000000000001', '77777777-0002-0000-0000-000000000001', 2, 300, NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 10. BẢNG SUBSCRIPTIONS (THUÊ BAO GÓI CƯỚC TỔ CHỨC)
-- Gói chuẩn: PKG-STANDARD (ffffffff-0002-0000-0000-000000000000)
INSERT INTO "Subscriptions" ("Id", "SubscriptionCode", "OrganizationId", "PackageId", "Status", "StartDate", "EndDate", "CurrentStorageUsageGB", "CurrentActiveDevices", "CreatedAt", "IsDeleted")
VALUES 
('e0000001-0001-0000-0000-000000000001', 'SUB-2026-ORG01', '00000000-0001-0000-0000-000000000001', 'ffffffff-0002-0000-0000-000000000000', 'Đang hoạt động', NOW(), NOW() + INTERVAL '365 days', 15.5, 3, NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- 11. BẢNG BROADCASTS (BẢN TIN PHÁT THANH MẪU)
-- 11.1. Bản tin thường của Cơ sở Cha
INSERT INTO "Broadcasts" ("Id", "BroadcastCode", "BroadcastName", "Description", "BroadcastType", "Priority", "Status", "ContentType", "MediaId", "OrganizationId", "FacilityId", "CreatedAt", "IsDeleted")
VALUES 
('b1111111-0001-0000-0000-000000000001', 'BC-PARENT-01', 'Bản tin Tuyên truyền PCCC của Quận 1', 'Bản tin phát thanh do cơ sở cha tạo', 'Thường', 2, 'Chờ duyệt', 'Media', '77777777-0002-0000-0000-000000000001', '00000000-0001-0000-0000-000000000001', '11111111-0001-0000-0000-000000000001', NOW(), false),
-- 11.2. Bản tin của Cơ sở Con (Đa Kao)
('b1111111-0002-0000-0000-000000000001', 'BC-CHILD-01', 'Bản tin Khuyến học Phường Đa Kao', 'Bản tin do cơ sở con Đa Kao tạo', 'Thường', 2, 'Chờ duyệt', 'Media', '77777777-0001-0000-0000-000000000001', '00000000-0001-0000-0000-000000000001', '11111111-0002-0000-0000-000000000001', NOW(), false)
ON CONFLICT ("Id") DO NOTHING;
