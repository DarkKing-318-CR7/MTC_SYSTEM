using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDev4DeviceSrs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VendorCode",
                table: "DeviceVendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Devices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceVendorId",
                table: "Devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DeviceVendors",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-0001-0000-0000-000000000000"),
                column: "VendorCode",
                value: "AltaMedia");

            migrationBuilder.Sql("""
                UPDATE "DeviceVendors"
                SET "VendorCode" = 'Vendor-' || replace("Id"::text, '-', '')
                WHERE "VendorCode" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "VendorCode",
                table: "DeviceVendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.Sql("""
                UPDATE "Devices" AS d
                SET "DeviceVendorId" = v."Id",
                    "VendorName" = v."VendorName"
                FROM "DeviceVendors" AS v
                WHERE d."DeviceVendorId" IS NULL
                  AND v."IsDeleted" = FALSE
                  AND (
                    (d."VendorName" IS NOT NULL AND lower(d."VendorName") = lower(v."VendorName"))
                    OR (d."VendorName" IS NULL AND v."Id" = 'dddddddd-0001-0000-0000-000000000000'::uuid)
                  )
                  AND (v."OrganizationId" IS NULL OR v."OrganizationId" = d."OrganizationId");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceVendors_VendorCode",
                table: "DeviceVendors",
                column: "VendorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceVendorId",
                table: "Devices",
                column: "DeviceVendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_DeviceVendors_DeviceVendorId",
                table: "Devices",
                column: "DeviceVendorId",
                principalTable: "DeviceVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_DeviceVendors_DeviceVendorId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_DeviceVendors_VendorCode",
                table: "DeviceVendors");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceVendorId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "VendorCode",
                table: "DeviceVendors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DeviceVendorId",
                table: "Devices");
        }
    }
}
