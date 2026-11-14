using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaHlsProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HlsFailureCode",
                table: "Media",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HlsOutputSizeBytes",
                table: "Media",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HlsProcessingStartedAt",
                table: "Media",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HlsRelativePath",
                table: "Media",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HlsStatus",
                table: "Media",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.CreateIndex(
                name: "IX_Media_HlsStatus_HlsProcessingStartedAt",
                table: "Media",
                columns: new[] { "HlsStatus", "HlsProcessingStartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_HlsStatus_HlsProcessingStartedAt",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "HlsFailureCode",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "HlsOutputSizeBytes",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "HlsProcessingStartedAt",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "HlsRelativePath",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "HlsStatus",
                table: "Media");
        }
    }
}
