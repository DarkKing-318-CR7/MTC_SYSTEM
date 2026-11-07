using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MTC_System.Data;

#nullable disable

namespace MTC_System.Migrations;

[DbContext(typeof(MyDbContext))]
[Migration("20261106090000_AddChangeTypeToSubscriptionChanges")]
public partial class AddChangeTypeToSubscriptionChanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ChangeType",
            table: "SubscriptionChanges",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ChangeType", table: "SubscriptionChanges");
    }
}
