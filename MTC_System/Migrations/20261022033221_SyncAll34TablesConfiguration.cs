using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MTC_System.Migrations
{
    /// <inheritdoc />
    public partial class SyncAll34TablesConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdditionalDevices",
                table: "UpgradePackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AdditionalStorageGB",
                table: "UpgradePackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "UpgradePackages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UpgradePackages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                table: "UpgradePackages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "UpgradePackages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UpgradeName",
                table: "UpgradePackages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllowedMediaTypes",
                table: "TemplateZones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "HeightPercent",
                table: "TemplateZones",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "TemplateZones",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "WidthPercent",
                table: "TemplateZones",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "X",
                table: "TemplateZones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Y",
                table: "TemplateZones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZIndex",
                table: "TemplateZones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ZoneName",
                table: "TemplateZones",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Templates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "Templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LayoutConfigJson",
                table: "Templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateName",
                table: "Templates",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "Templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "SystemNotifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityId",
                table: "SystemNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSent",
                table: "SystemNotifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotificationType",
                table: "SystemNotifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SystemNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAt",
                table: "SystemNotifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentAt",
                table: "SystemNotifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetOrganizationIds",
                table: "SystemNotifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                table: "SystemNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "SystemNotifications",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "NotificationId",
                table: "SystemNotificationReads",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadAt",
                table: "SystemNotificationReads",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SystemNotificationReads",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "ActualPrice",
                table: "Subscriptions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CustomDurationMonths",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomMaxDevices",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomPackageName",
                table: "Subscriptions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomPrice",
                table: "Subscriptions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomStorageGB",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Subscriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Subscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Đang hoạt động");

            migrationBuilder.AddColumn<Guid>(
                name: "FromFacilityId",
                table: "SharedMedia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MediaId",
                table: "SharedMedia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SharedMedia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RespondedAt",
                table: "SharedMedia",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RespondedBy",
                table: "SharedMedia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SharedAt",
                table: "SharedMedia",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SharedMedia",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chờ tiếp nhận");

            migrationBuilder.AddColumn<Guid>(
                name: "ToFacilityId",
                table: "SharedMedia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Playlists",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Playlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlayMode",
                table: "Playlists",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Theo thứ tự");

            migrationBuilder.AddColumn<string>(
                name: "PlaylistName",
                table: "Playlists",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalDurationSeconds",
                table: "Playlists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "PlaylistItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "PlaylistItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MediaId",
                table: "PlaylistItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlaylistId",
                table: "PlaylistItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Packages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMonths",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxDevices",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxStorageGB",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PackageCode",
                table: "Packages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PackageName",
                table: "Packages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMonth",
                table: "Packages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "MediaCategories",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MediaCategories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MediaCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "MediaCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chờ duyệt");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Media",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChecksumMd5",
                table: "Media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChecksumSha256",
                table: "Media",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Media",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Media",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Media",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "Media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FrequencyHz",
                table: "Media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSharedFromParent",
                table: "Media",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MediaName",
                table: "Media",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Media",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedBy",
                table: "Media",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Media",
                type: "character varying(510)",
                maxLength: 510,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeInBytes",
                table: "Media",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Media",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountDue",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceCode",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chờ thanh toán");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Đăng ký mới");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActivatedAt",
                table: "EmergencyBroadcasts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "ActivatedBy",
                table: "EmergencyBroadcasts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastId",
                table: "EmergencyBroadcasts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "BroadcastScope",
                table: "EmergencyBroadcasts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Tất cả thiết bị");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "EmergencyBroadcasts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityId",
                table: "EmergencyBroadcasts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EmergencyBroadcasts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Đang phát");

            migrationBuilder.AddColumn<string>(
                name: "TargetIdsJson",
                table: "EmergencyBroadcasts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "DeviceVendors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "DeviceVendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "DeviceVendors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "DeviceVendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "DeviceVendors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "DeviceVendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "DeviceVendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationOtp",
                table: "Devices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationQrToken",
                table: "Devices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActivationTokenExpiry",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ActiveStatus",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConnectionStatus",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Mất kết nối");

            migrationBuilder.AddColumn<string>(
                name: "DeviceCode",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceGroupId",
                table: "Devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "Devices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstallationAddress",
                table: "Devices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActivated",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOnlineAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastScreenshotAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Devices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Devices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "OsType",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Android");

            migrationBuilder.AddColumn<string>(
                name: "PlayStatus",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Dừng phát bản tin");

            migrationBuilder.AddColumn<long>(
                name: "ScreenshotSizeInBytes",
                table: "Devices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotUrl",
                table: "Devices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimPhoneNumber",
                table: "Devices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimSerialNumber",
                table: "Devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Volume",
                table: "Devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastId",
                table: "DevicePlayHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastScheduleId",
                table: "DevicePlayHistories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "DevicePlayHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "DevicePlayHistories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DevicePlayHistories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayResult",
                table: "DevicePlayHistories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Hoàn thành");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "DevicePlayHistories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DeviceGroups",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "DeviceGroups",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DeviceGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DeviceErrorCodes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "DeviceErrorCodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorName",
                table: "DeviceErrorCodes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "DeviceErrorCodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Trung bình");

            migrationBuilder.AddColumn<string>(
                name: "ActionDetail",
                table: "DeviceActivityLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "DeviceActivityLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "DeviceActivityLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ErrorCodeRef",
                table: "DeviceActivityLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "DeviceActivityLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "DeviceActivityLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "DeviceActivityLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Thành công");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "DeviceActivityLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastScheduleId",
                table: "BroadcastTimeSlots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "BroadcastTimeSlots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "BroadcastTimeSlots",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "BroadcastTimeSlots",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastId",
                table: "BroadcastSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                table: "BroadcastSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "BroadcastSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RepeatDays",
                table: "BroadcastSchedules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepeatType",
                table: "BroadcastSchedules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Không lặp");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                table: "BroadcastSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "Broadcasts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "Broadcasts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BroadcastCode",
                table: "Broadcasts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BroadcastName",
                table: "Broadcasts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BroadcastType",
                table: "Broadcasts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Thường");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Broadcasts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Broadcasts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MediaId",
                table: "Broadcasts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlaylistId",
                table: "Broadcasts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Broadcasts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Broadcasts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Broadcasts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chờ duyệt");

            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastScheduleId",
                table: "BroadcastDevices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "BroadcastDevices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DeviceStatus",
                table: "BroadcastDevices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chờ phát");

            migrationBuilder.AddColumn<bool>(
                name: "IsPreempted",
                table: "BroadcastDevices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PreemptedByBroadcastId",
                table: "BroadcastDevices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValueJson",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValueJson",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeInBytes",
                table: "AuditLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "DeviceErrorCodes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "ErrorCode", "ErrorName", "IsDeleted", "Severity", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("eeeeeeee-0001-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4741), new TimeSpan(0, 0, 0, 0, 0)), null, "Thiết bị không thể ping đến máy chủ trung tâm trong 3 phút", "MTT-001", "Mất kết nối mạng Internet", false, "Nghiêm trọng", null, null },
                    { new Guid("eeeeeeee-0002-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4745), new TimeSpan(0, 0, 0, 0, 0)), null, "Nguồn cấp điện không ổn định hoặc sụt áp", "MTT-002", "Mất nguồn điện / Pin yếu", false, "Nghiêm trọng", null, null },
                    { new Guid("eeeeeeee-0003-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4749), new TimeSpan(0, 0, 0, 0, 0)), null, "Không nhận diện được card âm thanh hoặc tín hiệu DAC méo", "MTT-003", "Lỗi phần cứng âm thanh / Loa rè", false, "Trung bình", null, null },
                    { new Guid("eeeeeeee-0004-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4752), new TimeSpan(0, 0, 0, 0, 0)), null, "Màn hình LED/LCD không phản hồi hiển thị", "MTT-004", "Màn hình đen / Không nhận tín hiệu HDMI", false, "Nghiêm trọng", null, null },
                    { new Guid("eeeeeeee-0005-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4754), new TimeSpan(0, 0, 0, 0, 0)), null, "Tệp tin tải về bị hỏng hoặc mã MD5 không khớp", "MTT-005", "Lỗi tải tệp Media / Sai Checksum", false, "Trung bình", null, null },
                    { new Guid("eeeeeeee-0006-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4756), new TimeSpan(0, 0, 0, 0, 0)), null, "Dung lượng đĩa cục bộ không đủ để lưu cache bản tin", "MTT-006", "Bộ nhớ trong đầy (>95%)", false, "Trung bình", null, null },
                    { new Guid("eeeeeeee-0007-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4759), new TimeSpan(0, 0, 0, 0, 0)), null, "Cảm biến nhiệt báo vượt ngưỡng an toàn (>75 độ C)", "MTT-007", "Nhiệt độ CPU / Thiết bị quá cao", false, "Thấp", null, null },
                    { new Guid("eeeeeeee-0008-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 819, DateTimeKind.Unspecified).AddTicks(4761), new TimeSpan(0, 0, 0, 0, 0)), null, "JWT Token của thiết bị không hợp lệ hoặc đã bị thu hồi", "MTT-008", "Lỗi xác thực Token / Phiên làm việc hết hạn", false, "Trung bình", null, null }
                });

            migrationBuilder.InsertData(
                table: "DeviceVendors",
                columns: new[] { "Id", "Address", "ContactEmail", "ContactPhone", "CreatedAt", "CreatedBy", "IsDefault", "IsDeleted", "OrganizationId", "UpdatedAt", "UpdatedBy", "VendorName", "Website" },
                values: new object[] { new Guid("dddddddd-0001-0000-0000-000000000000"), "86/33 Âu Cơ, Phường 9, Quận Tân Bình, TP. Hồ Chí Minh", "support@altamedia.vn", "02822432432", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 821, DateTimeKind.Unspecified).AddTicks(8488), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, null, null, null, "Alta Media", "https://altamedia.vn" });

            migrationBuilder.InsertData(
                table: "MediaCategories",
                columns: new[] { "Id", "CategoryName", "CreatedAt", "CreatedBy", "Description", "IsDefault", "IsDeleted", "OrganizationId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("cccccccc-0001-0000-0000-000000000000"), "Chính trị - Xã hội", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(853), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0002-0000-0000-000000000000"), "Kinh tế", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(856), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0003-0000-0000-000000000000"), "Văn hóa - Nghệ thuật", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(858), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0004-0000-0000-000000000000"), "Giáo dục", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(861), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0005-0000-0000-000000000000"), "Y tế - Sức khỏe", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(863), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0006-0000-0000-000000000000"), "Thể thao", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(904), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0007-0000-0000-000000000000"), "An ninh - Quốc phòng", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(907), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0008-0000-0000-000000000000"), "Khoa học - Công nghệ", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(913), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0009-0000-0000-000000000000"), "Môi trường", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(915), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0010-0000-0000-000000000000"), "Quảng cáo", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(917), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null },
                    { new Guid("cccccccc-0011-0000-0000-000000000000"), "Khác", new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 824, DateTimeKind.Unspecified).AddTicks(919), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, false, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "Packages",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "DurationMonths", "IsActive", "IsCustom", "IsDeleted", "MaxDevices", "MaxStorageGB", "PackageCode", "PackageName", "PricePerMonth", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("ffffffff-0001-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 825, DateTimeKind.Unspecified).AddTicks(3121), new TimeSpan(0, 0, 0, 0, 0)), null, "Dành cho cơ sở quy mô nhỏ, tối đa 5 màn hình/loa, dung lượng 50GB", 1, true, false, false, 5, 50, "PKG-BASIC", "Gói Cơ Bản (Khởi nghiệp)", 500000m, null, null },
                    { new Guid("ffffffff-0002-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 825, DateTimeKind.Unspecified).AddTicks(3125), new TimeSpan(0, 0, 0, 0, 0)), null, "Dành cho cơ sở quy mô vừa, tối đa 20 màn hình/loa, dung lượng 200GB", 6, true, false, false, 20, 200, "PKG-STANDARD", "Gói Tiêu Chuẩn (Doanh nghiệp)", 1500000m, null, null },
                    { new Guid("ffffffff-0003-0000-0000-000000000000"), new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 825, DateTimeKind.Unspecified).AddTicks(3129), new TimeSpan(0, 0, 0, 0, 0)), null, "Dành cho chuỗi cơ sở lớn, tối đa 100 màn hình/loa, dung lượng 1000GB (1TB)", 12, true, false, false, 100, 1000, "PKG-ENTERPRISE", "Gói Cao Cấp (Tập đoàn)", 5000000m, null, null }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 828, DateTimeKind.Unspecified).AddTicks(3686), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 828, DateTimeKind.Unspecified).AddTicks(3690), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 828, DateTimeKind.Unspecified).AddTicks(3693), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 32, 18, 828, DateTimeKind.Unspecified).AddTicks(3696), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_UpgradePackages_PackageId",
                table: "UpgradePackages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateZones_TemplateId",
                table: "TemplateZones",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_FacilityId",
                table: "Templates",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_OrganizationId",
                table: "Templates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_FacilityId",
                table: "SystemNotifications",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_OrganizationId",
                table: "SystemNotifications",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_TargetUserId",
                table: "SystemNotifications",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotificationReads_NotificationId_UserId",
                table: "SystemNotificationReads",
                columns: new[] { "NotificationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotificationReads_UserId",
                table: "SystemNotificationReads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PackageId",
                table: "Subscriptions",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMedia_FromFacilityId_ToFacilityId_MediaId",
                table: "SharedMedia",
                columns: new[] { "FromFacilityId", "ToFacilityId", "MediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedMedia_MediaId",
                table: "SharedMedia",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMedia_OrganizationId",
                table: "SharedMedia",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMedia_ToFacilityId",
                table: "SharedMedia",
                column: "ToFacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_FacilityId",
                table: "Playlists",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_OrganizationId",
                table: "Playlists",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_MediaId",
                table: "PlaylistItems",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId_MediaId",
                table: "PlaylistItems",
                columns: new[] { "PlaylistId", "MediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageCode",
                table: "Packages",
                column: "PackageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaCategories_OrganizationId_CategoryName",
                table: "MediaCategories",
                columns: new[] { "OrganizationId", "CategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_CategoryId",
                table: "Media",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_FacilityId",
                table: "Media",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_OrganizationId",
                table: "Media",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceCode",
                table: "Invoices",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrganizationId",
                table: "Invoices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SubscriptionId",
                table: "Invoices",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyBroadcasts_BroadcastId",
                table: "EmergencyBroadcasts",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyBroadcasts_FacilityId",
                table: "EmergencyBroadcasts",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceVendors_OrganizationId_VendorName",
                table: "DeviceVendors",
                columns: new[] { "OrganizationId", "VendorName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceCode",
                table: "Devices",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceGroupId",
                table: "Devices",
                column: "DeviceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_FacilityId",
                table: "Devices",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OrganizationId",
                table: "Devices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePlayHistories_BroadcastId",
                table: "DevicePlayHistories",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePlayHistories_DeviceId",
                table: "DevicePlayHistories",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceGroups_FacilityId",
                table: "DeviceGroups",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceGroups_OrganizationId",
                table: "DeviceGroups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceErrorCodes_ErrorCode",
                table: "DeviceErrorCodes",
                column: "ErrorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActivityLogs_DeviceId",
                table: "DeviceActivityLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceActivityLogs_UserId",
                table: "DeviceActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastTimeSlots_BroadcastScheduleId",
                table: "BroadcastTimeSlots",
                column: "BroadcastScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastSchedules_BroadcastId",
                table: "BroadcastSchedules",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasts_BroadcastCode",
                table: "Broadcasts",
                column: "BroadcastCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasts_FacilityId",
                table: "Broadcasts",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasts_MediaId",
                table: "Broadcasts",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasts_OrganizationId",
                table: "Broadcasts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Broadcasts_PlaylistId",
                table: "Broadcasts",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastDevices_BroadcastScheduleId_DeviceId",
                table: "BroadcastDevices",
                columns: new[] { "BroadcastScheduleId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastDevices_DeviceId",
                table: "BroadcastDevices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_FacilityId",
                table: "AuditLogs",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId",
                table: "AuditLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Facilities_FacilityId",
                table: "AuditLogs",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Organizations_OrganizationId",
                table: "AuditLogs",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BroadcastDevices_BroadcastSchedules_BroadcastScheduleId",
                table: "BroadcastDevices",
                column: "BroadcastScheduleId",
                principalTable: "BroadcastSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BroadcastDevices_Devices_DeviceId",
                table: "BroadcastDevices",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Broadcasts_Facilities_FacilityId",
                table: "Broadcasts",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Broadcasts_Media_MediaId",
                table: "Broadcasts",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Broadcasts_Organizations_OrganizationId",
                table: "Broadcasts",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Broadcasts_Playlists_PlaylistId",
                table: "Broadcasts",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BroadcastSchedules_Broadcasts_BroadcastId",
                table: "BroadcastSchedules",
                column: "BroadcastId",
                principalTable: "Broadcasts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BroadcastTimeSlots_BroadcastSchedules_BroadcastScheduleId",
                table: "BroadcastTimeSlots",
                column: "BroadcastScheduleId",
                principalTable: "BroadcastSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceActivityLogs_Devices_DeviceId",
                table: "DeviceActivityLogs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceActivityLogs_Users_UserId",
                table: "DeviceActivityLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceGroups_Facilities_FacilityId",
                table: "DeviceGroups",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceGroups_Organizations_OrganizationId",
                table: "DeviceGroups",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DevicePlayHistories_Broadcasts_BroadcastId",
                table: "DevicePlayHistories",
                column: "BroadcastId",
                principalTable: "Broadcasts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DevicePlayHistories_Devices_DeviceId",
                table: "DevicePlayHistories",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_DeviceGroups_DeviceGroupId",
                table: "Devices",
                column: "DeviceGroupId",
                principalTable: "DeviceGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Facilities_FacilityId",
                table: "Devices",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Organizations_OrganizationId",
                table: "Devices",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceVendors_Organizations_OrganizationId",
                table: "DeviceVendors",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyBroadcasts_Broadcasts_BroadcastId",
                table: "EmergencyBroadcasts",
                column: "BroadcastId",
                principalTable: "Broadcasts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyBroadcasts_Facilities_FacilityId",
                table: "EmergencyBroadcasts",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Organizations_OrganizationId",
                table: "Invoices",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Subscriptions_SubscriptionId",
                table: "Invoices",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Facilities_FacilityId",
                table: "Media",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_MediaCategories_CategoryId",
                table: "Media",
                column: "CategoryId",
                principalTable: "MediaCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Organizations_OrganizationId",
                table: "Media",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistItems_Media_MediaId",
                table: "PlaylistItems",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistItems_Playlists_PlaylistId",
                table: "PlaylistItems",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_Facilities_FacilityId",
                table: "Playlists",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_Organizations_OrganizationId",
                table: "Playlists",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedMedia_Facilities_FromFacilityId",
                table: "SharedMedia",
                column: "FromFacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedMedia_Facilities_ToFacilityId",
                table: "SharedMedia",
                column: "ToFacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedMedia_Media_MediaId",
                table: "SharedMedia",
                column: "MediaId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SharedMedia_Organizations_OrganizationId",
                table: "SharedMedia",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Organizations_OrganizationId",
                table: "Subscriptions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Packages_PackageId",
                table: "Subscriptions",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemNotificationReads_SystemNotifications_NotificationId",
                table: "SystemNotificationReads",
                column: "NotificationId",
                principalTable: "SystemNotifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemNotificationReads_Users_UserId",
                table: "SystemNotificationReads",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemNotifications_Facilities_FacilityId",
                table: "SystemNotifications",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemNotifications_Organizations_OrganizationId",
                table: "SystemNotifications",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemNotifications_Users_TargetUserId",
                table: "SystemNotifications",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Facilities_FacilityId",
                table: "Templates",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateZones_Templates_TemplateId",
                table: "TemplateZones",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UpgradePackages_Packages_PackageId",
                table: "UpgradePackages",
                column: "PackageId",
                principalTable: "Packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Facilities_FacilityId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Organizations_OrganizationId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_BroadcastDevices_BroadcastSchedules_BroadcastScheduleId",
                table: "BroadcastDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_BroadcastDevices_Devices_DeviceId",
                table: "BroadcastDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_Broadcasts_Facilities_FacilityId",
                table: "Broadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_Broadcasts_Media_MediaId",
                table: "Broadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_Broadcasts_Organizations_OrganizationId",
                table: "Broadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_Broadcasts_Playlists_PlaylistId",
                table: "Broadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_BroadcastSchedules_Broadcasts_BroadcastId",
                table: "BroadcastSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_BroadcastTimeSlots_BroadcastSchedules_BroadcastScheduleId",
                table: "BroadcastTimeSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceActivityLogs_Devices_DeviceId",
                table: "DeviceActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceActivityLogs_Users_UserId",
                table: "DeviceActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceGroups_Facilities_FacilityId",
                table: "DeviceGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceGroups_Organizations_OrganizationId",
                table: "DeviceGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_DevicePlayHistories_Broadcasts_BroadcastId",
                table: "DevicePlayHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_DevicePlayHistories_Devices_DeviceId",
                table: "DevicePlayHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_DeviceGroups_DeviceGroupId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Facilities_FacilityId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Organizations_OrganizationId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceVendors_Organizations_OrganizationId",
                table: "DeviceVendors");

            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyBroadcasts_Broadcasts_BroadcastId",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyBroadcasts_Facilities_FacilityId",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Organizations_OrganizationId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Subscriptions_SubscriptionId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Facilities_FacilityId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_MediaCategories_CategoryId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Organizations_OrganizationId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistItems_Media_MediaId",
                table: "PlaylistItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistItems_Playlists_PlaylistId",
                table: "PlaylistItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_Facilities_FacilityId",
                table: "Playlists");

            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_Organizations_OrganizationId",
                table: "Playlists");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedMedia_Facilities_FromFacilityId",
                table: "SharedMedia");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedMedia_Facilities_ToFacilityId",
                table: "SharedMedia");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedMedia_Media_MediaId",
                table: "SharedMedia");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedMedia_Organizations_OrganizationId",
                table: "SharedMedia");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Organizations_OrganizationId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Packages_PackageId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemNotificationReads_SystemNotifications_NotificationId",
                table: "SystemNotificationReads");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemNotificationReads_Users_UserId",
                table: "SystemNotificationReads");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemNotifications_Facilities_FacilityId",
                table: "SystemNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemNotifications_Organizations_OrganizationId",
                table: "SystemNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemNotifications_Users_TargetUserId",
                table: "SystemNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Facilities_FacilityId",
                table: "Templates");

            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Organizations_OrganizationId",
                table: "Templates");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateZones_Templates_TemplateId",
                table: "TemplateZones");

            migrationBuilder.DropForeignKey(
                name: "FK_UpgradePackages_Packages_PackageId",
                table: "UpgradePackages");

            migrationBuilder.DropIndex(
                name: "IX_UpgradePackages_PackageId",
                table: "UpgradePackages");

            migrationBuilder.DropIndex(
                name: "IX_TemplateZones_TemplateId",
                table: "TemplateZones");

            migrationBuilder.DropIndex(
                name: "IX_Templates_FacilityId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_OrganizationId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotifications_FacilityId",
                table: "SystemNotifications");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotifications_OrganizationId",
                table: "SystemNotifications");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotifications_TargetUserId",
                table: "SystemNotifications");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotificationReads_NotificationId_UserId",
                table: "SystemNotificationReads");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotificationReads_UserId",
                table: "SystemNotificationReads");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OrganizationId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PackageId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_SharedMedia_FromFacilityId_ToFacilityId_MediaId",
                table: "SharedMedia");

            migrationBuilder.DropIndex(
                name: "IX_SharedMedia_MediaId",
                table: "SharedMedia");

            migrationBuilder.DropIndex(
                name: "IX_SharedMedia_OrganizationId",
                table: "SharedMedia");

            migrationBuilder.DropIndex(
                name: "IX_SharedMedia_ToFacilityId",
                table: "SharedMedia");

            migrationBuilder.DropIndex(
                name: "IX_Playlists_FacilityId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_Playlists_OrganizationId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistItems_MediaId",
                table: "PlaylistItems");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistItems_PlaylistId_MediaId",
                table: "PlaylistItems");

            migrationBuilder.DropIndex(
                name: "IX_Packages_PackageCode",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_MediaCategories_OrganizationId_CategoryName",
                table: "MediaCategories");

            migrationBuilder.DropIndex(
                name: "IX_Media_CategoryId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_FacilityId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_OrganizationId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceCode",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OrganizationId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SubscriptionId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyBroadcasts_BroadcastId",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyBroadcasts_FacilityId",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropIndex(
                name: "IX_DeviceVendors_OrganizationId_VendorName",
                table: "DeviceVendors");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceCode",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceGroupId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_FacilityId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_OrganizationId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_DevicePlayHistories_BroadcastId",
                table: "DevicePlayHistories");

            migrationBuilder.DropIndex(
                name: "IX_DevicePlayHistories_DeviceId",
                table: "DevicePlayHistories");

            migrationBuilder.DropIndex(
                name: "IX_DeviceGroups_FacilityId",
                table: "DeviceGroups");

            migrationBuilder.DropIndex(
                name: "IX_DeviceGroups_OrganizationId",
                table: "DeviceGroups");

            migrationBuilder.DropIndex(
                name: "IX_DeviceErrorCodes_ErrorCode",
                table: "DeviceErrorCodes");

            migrationBuilder.DropIndex(
                name: "IX_DeviceActivityLogs_DeviceId",
                table: "DeviceActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_DeviceActivityLogs_UserId",
                table: "DeviceActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_BroadcastTimeSlots_BroadcastScheduleId",
                table: "BroadcastTimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_BroadcastSchedules_BroadcastId",
                table: "BroadcastSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Broadcasts_BroadcastCode",
                table: "Broadcasts");

            migrationBuilder.DropIndex(
                name: "IX_Broadcasts_FacilityId",
                table: "Broadcasts");

            migrationBuilder.DropIndex(
                name: "IX_Broadcasts_MediaId",
                table: "Broadcasts");

            migrationBuilder.DropIndex(
                name: "IX_Broadcasts_OrganizationId",
                table: "Broadcasts");

            migrationBuilder.DropIndex(
                name: "IX_Broadcasts_PlaylistId",
                table: "Broadcasts");

            migrationBuilder.DropIndex(
                name: "IX_BroadcastDevices_BroadcastScheduleId_DeviceId",
                table: "BroadcastDevices");

            migrationBuilder.DropIndex(
                name: "IX_BroadcastDevices_DeviceId",
                table: "BroadcastDevices");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_FacilityId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OrganizationId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs");

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0001-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0002-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0005-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0006-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0007-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceErrorCodes",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0008-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "DeviceVendors",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0001-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0001-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0002-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0003-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0004-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0005-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0006-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0007-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0008-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0009-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0010-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "MediaCategories",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-0011-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Packages",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0001-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Packages",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0002-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "Packages",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0003-0000-0000-000000000000"));

            migrationBuilder.DropColumn(
                name: "AdditionalDevices",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "AdditionalStorageGB",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "UpgradeName",
                table: "UpgradePackages");

            migrationBuilder.DropColumn(
                name: "AllowedMediaTypes",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "HeightPercent",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "WidthPercent",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "X",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "ZIndex",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "ZoneName",
                table: "TemplateZones");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "LayoutConfigJson",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "TemplateName",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "IsSent",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "NotificationType",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "TargetOrganizationIds",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "SystemNotifications");

            migrationBuilder.DropColumn(
                name: "NotificationId",
                table: "SystemNotificationReads");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "SystemNotificationReads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SystemNotificationReads");

            migrationBuilder.DropColumn(
                name: "ActualPrice",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CustomDurationMonths",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CustomMaxDevices",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CustomPackageName",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CustomPrice",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CustomStorageGB",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "FromFacilityId",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "MediaId",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "RespondedBy",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "SharedAt",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "ToFacilityId",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "PlayMode",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "PlaylistName",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "TotalDurationSeconds",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "MediaId",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "DurationMonths",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "MaxDevices",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "MaxStorageGB",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PackageCode",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PackageName",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PricePerMonth",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "MediaCategories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MediaCategories");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MediaCategories");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "MediaCategories");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ChecksumMd5",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ChecksumSha256",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FrequencyHz",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "IsSharedFromParent",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "MediaName",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "SizeInBytes",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AmountDue",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "ActivatedBy",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "BroadcastId",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "BroadcastScope",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "TargetIdsJson",
                table: "EmergencyBroadcasts");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "ActivationOtp",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ActivationQrToken",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiry",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ActiveStatus",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ConnectionStatus",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DeviceCode",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DeviceGroupId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "InstallationAddress",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsActivated",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastOnlineAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastScreenshotAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "OsType",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PlayStatus",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ScreenshotSizeInBytes",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ScreenshotUrl",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SimPhoneNumber",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SimSerialNumber",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "BroadcastId",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "BroadcastScheduleId",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "PlayResult",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "DevicePlayHistories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "DeviceGroups");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "DeviceGroups");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DeviceGroups");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "DeviceErrorCodes");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "DeviceErrorCodes");

            migrationBuilder.DropColumn(
                name: "ErrorName",
                table: "DeviceErrorCodes");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "DeviceErrorCodes");

            migrationBuilder.DropColumn(
                name: "ActionDetail",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "ErrorCodeRef",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DeviceActivityLogs");

            migrationBuilder.DropColumn(
                name: "BroadcastScheduleId",
                table: "BroadcastTimeSlots");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "BroadcastTimeSlots");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "BroadcastTimeSlots");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "BroadcastTimeSlots");

            migrationBuilder.DropColumn(
                name: "BroadcastId",
                table: "BroadcastSchedules");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "BroadcastSchedules");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "BroadcastSchedules");

            migrationBuilder.DropColumn(
                name: "RepeatDays",
                table: "BroadcastSchedules");

            migrationBuilder.DropColumn(
                name: "RepeatType",
                table: "BroadcastSchedules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "BroadcastSchedules");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "BroadcastCode",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "BroadcastName",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "BroadcastType",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "MediaId",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Broadcasts");

            migrationBuilder.DropColumn(
                name: "BroadcastScheduleId",
                table: "BroadcastDevices");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "BroadcastDevices");

            migrationBuilder.DropColumn(
                name: "DeviceStatus",
                table: "BroadcastDevices");

            migrationBuilder.DropColumn(
                name: "IsPreempted",
                table: "BroadcastDevices");

            migrationBuilder.DropColumn(
                name: "PreemptedByBroadcastId",
                table: "BroadcastDevices");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValueJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OldValueJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SizeInBytes",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AuditLogs");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9374), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9378), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9381), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9383), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
