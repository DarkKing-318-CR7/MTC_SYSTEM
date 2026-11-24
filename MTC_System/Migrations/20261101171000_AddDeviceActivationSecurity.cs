using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceActivationSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Loại bỏ dữ liệu QR/OTP rõ của phiên bản cũ. Mã mới chỉ được lưu dưới dạng HMAC.
            migrationBuilder.DropColumn(name: "ActivationOtp", table: "Devices");
            migrationBuilder.DropColumn(name: "ActivationQrToken", table: "Devices");

            migrationBuilder.RenameColumn(
                name: "ActivationTokenExpiry",
                table: "Devices",
                newName: "ActivationCodeExpiresAt");

            migrationBuilder.AddColumn<DateTimeOffset>(name: "ActivatedAt", table: "Devices", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<int>(name: "ActivationFailedAttempts", table: "Devices", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ActivationLockedUntil", table: "Devices", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ActivationOtpHash", table: "Devices", type: "character varying(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ActivationQrTokenHash", table: "Devices", type: "character varying(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "DeviceRefreshTokenExpiresAt", table: "Devices", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "DeviceRefreshTokenHash", table: "Devices", type: "character varying(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<int>(name: "DeviceTokenVersion", table: "Devices", type: "integer", nullable: false, defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceRefreshTokenHash",
                table: "Devices",
                column: "DeviceRefreshTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Devices_DeviceRefreshTokenHash", table: "Devices");
            migrationBuilder.DropColumn(name: "ActivatedAt", table: "Devices");
            migrationBuilder.DropColumn(name: "ActivationFailedAttempts", table: "Devices");
            migrationBuilder.DropColumn(name: "ActivationLockedUntil", table: "Devices");
            migrationBuilder.DropColumn(name: "ActivationOtpHash", table: "Devices");
            migrationBuilder.DropColumn(name: "ActivationQrTokenHash", table: "Devices");
            migrationBuilder.DropColumn(name: "DeviceRefreshTokenExpiresAt", table: "Devices");
            migrationBuilder.DropColumn(name: "DeviceRefreshTokenHash", table: "Devices");
            migrationBuilder.DropColumn(name: "DeviceTokenVersion", table: "Devices");

            migrationBuilder.RenameColumn(
                name: "ActivationCodeExpiresAt",
                table: "Devices",
                newName: "ActivationTokenExpiry");

            migrationBuilder.AddColumn<string>(name: "ActivationOtp", table: "Devices", type: "character varying(10)", maxLength: 10, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ActivationQrToken", table: "Devices", type: "character varying(500)", maxLength: 500, nullable: true);
        }
    }
}
