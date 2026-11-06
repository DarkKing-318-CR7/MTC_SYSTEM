using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations
{
    public partial class Issue2MediaSourceAndCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_MediaCategories_OrganizationId_CategoryName", table: "MediaCategories");

            migrationBuilder.AlterColumn<string>(name: "Url", table: "Media", type: "character varying(2048)", maxLength: 2048, nullable: true,
                oldClrType: typeof(string), oldType: "character varying(1000)", oldMaxLength: 1000, oldNullable: true);

            migrationBuilder.RenameColumn(name: "FrequencyHz", table: "Media", newName: "LegacyFrequencyHz");
            migrationBuilder.AddColumn<decimal>(name: "FrequencyHz", table: "Media", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SourceType", table: "Media", type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "File");
            migrationBuilder.AddColumn<string>(name: "SourceUrl", table: "Media", type: "character varying(2048)", maxLength: 2048, nullable: true);

            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0001-0000-0000-000000000000"), column: "CategoryName", value: "Thông tin và Tin tức");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0002-0000-0000-000000000000"), column: "CategoryName", value: "Quảng cáo và Tiếp thị");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0003-0000-0000-000000000000"), column: "CategoryName", value: "Giáo dục");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0004-0000-0000-000000000000"), column: "CategoryName", value: "Giải trí");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0005-0000-0000-000000000000"), column: "CategoryName", value: "Sự kiện và Hội nghị");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0006-0000-0000-000000000000"), column: "CategoryName", value: "Nghệ thuật và Văn hóa");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0007-0000-0000-000000000000"), column: "CategoryName", value: "Du lịch");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0008-0000-0000-000000000000"), column: "CategoryName", value: "Sự kiện và Khuyến mãi");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0009-0000-0000-000000000000"), column: "CategoryName", value: "Giao thông và Đô thị");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0010-0000-0000-000000000000"), column: "CategoryName", value: "Y tế");

            migrationBuilder.CreateIndex(name: "IX_MediaCategories_CategoryName", table: "MediaCategories", column: "CategoryName", unique: true, filter: "\"OrganizationId\" IS NULL");
            migrationBuilder.CreateIndex(name: "IX_MediaCategories_OrganizationId_CategoryName", table: "MediaCategories", columns: new[] { "OrganizationId", "CategoryName" }, unique: true, filter: "\"OrganizationId\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_MediaCategories_CategoryName", table: "MediaCategories");
            migrationBuilder.DropIndex(name: "IX_MediaCategories_OrganizationId_CategoryName", table: "MediaCategories");

            migrationBuilder.DropColumn(name: "SourceType", table: "Media");
            migrationBuilder.DropColumn(name: "SourceUrl", table: "Media");
            migrationBuilder.DropColumn(name: "FrequencyHz", table: "Media");
            migrationBuilder.RenameColumn(name: "LegacyFrequencyHz", table: "Media", newName: "FrequencyHz");

            migrationBuilder.AlterColumn<string>(name: "Url", table: "Media", type: "character varying(1000)", maxLength: 1000, nullable: true,
                oldClrType: typeof(string), oldType: "character varying(2048)", oldMaxLength: 2048, oldNullable: true);

            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0001-0000-0000-000000000000"), column: "CategoryName", value: "Chính trị - Xã hội");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0002-0000-0000-000000000000"), column: "CategoryName", value: "Kinh tế");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0003-0000-0000-000000000000"), column: "CategoryName", value: "Văn hóa - Nghệ thuật");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0004-0000-0000-000000000000"), column: "CategoryName", value: "Giáo dục");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0005-0000-0000-000000000000"), column: "CategoryName", value: "Y tế - Sức khỏe");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0006-0000-0000-000000000000"), column: "CategoryName", value: "Thể thao");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0007-0000-0000-000000000000"), column: "CategoryName", value: "An ninh - Quốc phòng");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0008-0000-0000-000000000000"), column: "CategoryName", value: "Khoa học - Công nghệ");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0009-0000-0000-000000000000"), column: "CategoryName", value: "Môi trường");
            migrationBuilder.UpdateData(table: "MediaCategories", keyColumn: "Id", keyValue: new Guid("cccccccc-0010-0000-0000-000000000000"), column: "CategoryName", value: "Quảng cáo");

            migrationBuilder.CreateIndex(name: "IX_MediaCategories_OrganizationId_CategoryName", table: "MediaCategories", columns: new[] { "OrganizationId", "CategoryName" }, unique: true);
        }
    }
}
