using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MTC_System.Data;

#nullable disable

namespace MTC_System.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute(typeof(MyDbContext))]
[Migration("20261106000000_Dev5PackageChangeRules")]
public partial class Dev5PackageChangeRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var initial = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        migrationBuilder.AddColumn<bool>("IsSuspended", "Packages", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>("RegistrationStartsAt", "Packages", type: "timestamp with time zone", nullable: false, defaultValue: initial);
        migrationBuilder.AddColumn<DateTimeOffset>("RegistrationEndsAt", "Packages", type: "timestamp with time zone", nullable: false, defaultValue: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(7)));
        migrationBuilder.AddColumn<bool>("AutoRenew", "Subscriptions", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<int>("DurationMonths", "UpgradePackages", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<DateTimeOffset>("SalesStartsAt", "UpgradePackages", type: "timestamp with time zone", nullable: false, defaultValue: initial);
        migrationBuilder.AddColumn<DateTimeOffset>("SalesEndsAt", "UpgradePackages", type: "timestamp with time zone", nullable: false, defaultValue: DateTimeOffset.MaxValue);
        migrationBuilder.AddColumn<string>("ImageUrl", "UpgradePackages", type: "character varying(1000)", maxLength: 1000, nullable: true);

        migrationBuilder.CreateTable(
            name: "SubscriptionChanges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                NewPackageId = table.Column<Guid>(type: "uuid", nullable: true),
                CustomPackageName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                CustomStorageGB = table.Column<int>(type: "integer", nullable: true),
                CustomMaxDevices = table.Column<int>(type: "integer", nullable: true),
                CustomDurationMonths = table.Column<int>(type: "integer", nullable: true),
                CustomPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                AmountDue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                BonusDays = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubscriptionChanges", x => x.Id);
                table.ForeignKey("FK_SubscriptionChanges_Organizations_OrganizationId", x => x.OrganizationId, "Organizations", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SubscriptionChanges_Packages_NewPackageId", x => x.NewPackageId, "Packages", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SubscriptionChanges_Subscriptions_SubscriptionId", x => x.SubscriptionId, "Subscriptions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UpgradePurchases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UpgradePackageId = table.Column<Guid>(type: "uuid", nullable: false),
                SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UpgradePurchases", x => x.Id);
                table.ForeignKey("FK_UpgradePurchases_Organizations_OrganizationId", x => x.OrganizationId, "Organizations", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_UpgradePurchases_Subscriptions_SubscriptionId", x => x.SubscriptionId, "Subscriptions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_UpgradePurchases_UpgradePackages_UpgradePackageId", x => x.UpgradePackageId, "UpgradePackages", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Packages_DurationMonths_RegistrationStartsAt_RegistrationEndsAt", "Packages", new[] { "DurationMonths", "RegistrationStartsAt", "RegistrationEndsAt" });
        migrationBuilder.CreateIndex("IX_Packages_MaxStorageGB", "Packages", "MaxStorageGB", unique: true, filter: "\"IsCustom\" = false AND \"IsDeleted\" = false");
        migrationBuilder.CreateIndex("IX_SubscriptionChanges_OrganizationId", "SubscriptionChanges", "OrganizationId");
        migrationBuilder.CreateIndex("IX_SubscriptionChanges_NewPackageId", "SubscriptionChanges", "NewPackageId");
        migrationBuilder.CreateIndex("IX_SubscriptionChanges_SubscriptionId", "SubscriptionChanges", "SubscriptionId", unique: true, filter: "\"Status\" = 'Đã đặt' AND \"IsDeleted\" = false");
        migrationBuilder.CreateIndex("IX_UpgradePackages_IsActive_SalesStartsAt_SalesEndsAt", "UpgradePackages", new[] { "IsActive", "SalesStartsAt", "SalesEndsAt" });
        migrationBuilder.CreateIndex("IX_UpgradePurchases_OrganizationId", "UpgradePurchases", "OrganizationId");
        migrationBuilder.CreateIndex("IX_UpgradePurchases_SubscriptionId", "UpgradePurchases", "SubscriptionId");
        migrationBuilder.CreateIndex("IX_UpgradePurchases_UpgradePackageId_OrganizationId", "UpgradePurchases", new[] { "UpgradePackageId", "OrganizationId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SubscriptionChanges");
        migrationBuilder.DropTable("UpgradePurchases");
        migrationBuilder.DropIndex("IX_Packages_DurationMonths_RegistrationStartsAt_RegistrationEndsAt", "Packages");
        migrationBuilder.DropIndex("IX_Packages_MaxStorageGB", "Packages");
        migrationBuilder.DropIndex("IX_UpgradePackages_IsActive_SalesStartsAt_SalesEndsAt", "UpgradePackages");
        migrationBuilder.DropColumn("IsSuspended", "Packages");
        migrationBuilder.DropColumn("RegistrationStartsAt", "Packages");
        migrationBuilder.DropColumn("RegistrationEndsAt", "Packages");
        migrationBuilder.DropColumn("AutoRenew", "Subscriptions");
        migrationBuilder.DropColumn("DurationMonths", "UpgradePackages");
        migrationBuilder.DropColumn("SalesStartsAt", "UpgradePackages");
        migrationBuilder.DropColumn("SalesEndsAt", "UpgradePackages");
        migrationBuilder.DropColumn("ImageUrl", "UpgradePackages");
    }
}
