namespace MTC_System.Data;

internal static class DefaultMediaRolePermissionSeeder
{
    internal static readonly Guid CreatorMediaViewAssignmentId = Guid.Parse("f1a00000-0000-4000-8000-000000000001");

    public static void AddInitialCreatorMediaViewAssignment(
        MyDbContext context,
        IReadOnlyCollection<Models.Permission> initialPermissions)
    {
        // The permission catalog is the existing marker for first-time initialization.
        // Never restore a permission after the catalog has been initialized and a role
        // administrator has subsequently changed its assignments.
        if (context.Permissions.Any()) return;

        var creatorRole = context.Roles.SingleOrDefault(x => x.RoleCode == "CREATOR" && !x.IsDeleted);
        var mediaViewPermission = initialPermissions.SingleOrDefault(x => x.PermissionCode == "MEDIA_VIEW");
        if (creatorRole is null || mediaViewPermission is null) return;

        if (context.RolePermissions.Local.Any(x => x.RoleId == creatorRole.Id && x.PermissionId == mediaViewPermission.Id) ||
            context.RolePermissions.Any(x => x.RoleId == creatorRole.Id && x.PermissionId == mediaViewPermission.Id)) return;

        context.RolePermissions.Add(new Models.RolePermission
        {
            Id = CreatorMediaViewAssignmentId,
            RoleId = creatorRole.Id,
            PermissionId = mediaViewPermission.Id
        });
    }
}
