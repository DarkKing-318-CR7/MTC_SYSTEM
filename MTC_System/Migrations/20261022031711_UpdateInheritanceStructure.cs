using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInheritanceStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_SettingKey",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SharedMedia");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "Facilities");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "FacilityId",
                table: "Users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "BackupCodesCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BackupCodesJson",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActivityAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecretKey",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "SystemSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Subscriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "OrganizationId" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9374), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0002-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "OrganizationId" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9378), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0003-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "OrganizationId" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9381), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0004-0000-0000-000000000000"),
                columns: new[] { "CreatedAt", "OrganizationId" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 10, 22, 3, 17, 8, 712, DateTimeKind.Unspecified).AddTicks(9383), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_OrganizationId_SettingKey",
                table: "SystemSettings",
                columns: new[] { "OrganizationId", "SettingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_OrganizationId",
                table: "Roles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_OrganizationId",
                table: "Facilities",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_Organizations_OrganizationId",
                table: "Facilities",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Organizations_OrganizationId",
                table: "Roles",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemSettings_Organizations_OrganizationId",
                table: "SystemSettings",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_Organizations_OrganizationId",
                table: "Facilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Organizations_OrganizationId",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemSettings_Organizations_OrganizationId",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_OrganizationId_SettingKey",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_Roles_OrganizationId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_OrganizationId",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "BackupCodesCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BackupCodesJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecretKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Invoices");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FacilityId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityId",
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

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityId",
                table: "Facilities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 21, 12, 27, 31, 310, DateTimeKind.Unspecified).AddTicks(9830), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 21, 12, 27, 31, 310, DateTimeKind.Unspecified).AddTicks(9835), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 21, 12, 27, 31, 310, DateTimeKind.Unspecified).AddTicks(9838), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 10, 21, 12, 27, 31, 310, DateTimeKind.Unspecified).AddTicks(9841), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_SettingKey",
                table: "SystemSettings",
                column: "SettingKey",
                unique: true);
        }
    }
}
