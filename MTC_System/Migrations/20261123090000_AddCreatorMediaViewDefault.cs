using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTC_System.Migrations;

public partial class AddCreatorMediaViewDefault : Migration
{
    private const string AssignmentId = "f1a00000-0000-4000-8000-000000000002";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            INSERT INTO "RolePermissions" ("Id", "CreatedAt", "RoleId", "PermissionId", "IsDeleted")
            SELECT '{AssignmentId}'::uuid, CURRENT_TIMESTAMP, creator."Id", permission."Id", FALSE
            FROM "Roles" AS creator
            CROSS JOIN "Permissions" AS permission
            WHERE creator."RoleCode" = 'CREATOR'
              AND creator."IsDeleted" = FALSE
              AND permission."PermissionCode" = 'MEDIA_VIEW'
              AND permission."IsDeleted" = FALSE
              AND NOT EXISTS (
                  SELECT 1
                  FROM "RolePermissions" AS existing
                  WHERE existing."RoleId" = creator."Id"
                    AND existing."PermissionId" = permission."Id"
              )
            ON CONFLICT DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            DELETE FROM "RolePermissions"
            WHERE "Id" = '{AssignmentId}'::uuid
              AND "RoleId" = (SELECT "Id" FROM "Roles" WHERE "RoleCode" = 'CREATOR')
              AND "PermissionId" = (SELECT "Id" FROM "Permissions" WHERE "PermissionCode" = 'MEDIA_VIEW');
            """);
    }
}
