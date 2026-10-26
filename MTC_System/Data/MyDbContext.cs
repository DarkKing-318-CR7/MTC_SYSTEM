using Microsoft.EntityFrameworkCore;
using MTC_System.Models;

namespace MTC_System.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        // ============================================================
        // NHÓM 1 - FacilityScopedEntity (Gắn chặt với 1 Cơ sở & 1 Tổ chức)
        // Bắt buộc có OrganizationId & FacilityId non-null
        // ============================================================

        // [Dev 2]
        public DbSet<Media> Media { get; set; } = null!;
        public DbSet<Template> Templates { get; set; } = null!;
        public DbSet<Playlist> Playlists { get; set; } = null!;

        // [Dev 3]
        public DbSet<Broadcast> Broadcasts { get; set; } = null!;

        // [Dev 4]
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<DeviceGroup> DeviceGroups { get; set; } = null!;

        // ============================================================
        // NHÓM 2 - OrganizationScopedEntity (Phạm vi cấp Tổ chức)
        // Bắt buộc có OrganizationId non-null, không có FacilityId lặp lại
        // ============================================================

        // [Dev 1]
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;

        // [Dev 5]
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;

        // ============================================================
        // NHÓM 3 - BaseEntity (Dùng chung System/End-User hoặc Bảng con chi tiết)
        // ============================================================

        // [Dev 1]
        public DbSet<Organization> Organizations { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // [Dev 2]
        public DbSet<SharedMedia> SharedMedia { get; set; } = null!;
        public DbSet<TemplateZone> TemplateZones { get; set; } = null!;
        public DbSet<PlaylistItem> PlaylistItems { get; set; } = null!;
        public DbSet<MediaCategory> MediaCategories { get; set; } = null!;

        // [Dev 3]
        public DbSet<BroadcastSchedule> BroadcastSchedules { get; set; } = null!;
        public DbSet<BroadcastTimeSlot> BroadcastTimeSlots { get; set; } = null!;
        public DbSet<BroadcastDevice> BroadcastDevices { get; set; } = null!;
        public DbSet<EmergencyBroadcast> EmergencyBroadcasts { get; set; } = null!;

        // [Dev 4]
        public DbSet<DeviceVendor> DeviceVendors { get; set; } = null!;
        public DbSet<DeviceErrorCode> DeviceErrorCodes { get; set; } = null!;
        public DbSet<DeviceActivityLog> DeviceActivityLogs { get; set; } = null!;
        public DbSet<DevicePlayHistory> DevicePlayHistories { get; set; } = null!;

        // [Dev 5]
        public DbSet<Package> Packages { get; set; } = null!;
        public DbSet<UpgradePackage> UpgradePackages { get; set; } = null!;
        public DbSet<SystemNotification> SystemNotifications { get; set; } = null!;
        public DbSet<SystemNotificationRead> SystemNotificationReads { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public DbSet<MediaUploadSession> MediaUploadSessions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DÒNG THẦN THÁNH: Tự động quét và nạp file Configuration riêng của từng Dev
            // Mỗi Dev tạo file riêng trong Data/Configurations/ mà KHÔNG cần đụng vào file này!
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);
        }
    }
}
