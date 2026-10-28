using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MTC_System.Data;

#nullable disable

namespace MTC_System.Migrations
{
    [DbContext(typeof(MyDbContext))]
    [Migration("20261023090000_AddMediaUploadSessions")]
    public partial class AddMediaUploadSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaUploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UploadIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpectedFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpectedFileSize = table.Column<long>(type: "bigint", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    ReceivedChunksBitmap = table.Column<byte[]>(type: "bytea", nullable: false),
                    TempFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChecksumMd5 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChecksumSha256 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MediaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaUploadSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaUploadSessions_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaUploadSessions_MediaId",
                table: "MediaUploadSessions",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaUploadSessions_UploadIdentifier",
                table: "MediaUploadSessions",
                column: "UploadIdentifier",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MediaUploadSessions");
        }
    }
}