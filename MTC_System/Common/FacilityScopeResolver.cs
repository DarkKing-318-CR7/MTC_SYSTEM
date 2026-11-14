using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Models;
using MTC_System.Services;

namespace MTC_System.Common;

public static class FacilityScopeResolver
{
    public static async Task<HashSet<Guid>?> ResolveAsync(
        MyDbContext db,
        UserScopeContext userScope,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        userScope.EnsureCanAccess();
        if (userScope.IsSystemAdministrator)
            return null;
        if (userScope.OrganizationId != organizationId)
            throw DeviceModuleException.Forbidden("Không được phép truy cập dữ liệu của tổ chức khác.");
        if (!userScope.FacilityId.HasValue)
            return null;

        var facilities = await db.Facilities.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.OrganizationId == organizationId)
            .Select(x => new FacilityNode(x.Id, x.ParentId))
            .ToListAsync(cancellationToken);
        var root = facilities.FirstOrDefault(x => x.Id == userScope.FacilityId.Value);
        if (root is null)
            throw DeviceModuleException.Forbidden("Cơ sở của người dùng không tồn tại, đã ngừng hoạt động hoặc không thuộc tổ chức.");

        var children = facilities.Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(child => child.Id).ToArray());
        var scope = new HashSet<Guid> { root.Id };
        var pending = new Queue<Guid>();
        pending.Enqueue(root.Id);
        while (pending.TryDequeue(out var parentId))
        {
            if (!children.TryGetValue(parentId, out var childIds))
                continue;
            foreach (var childId in childIds)
                if (scope.Add(childId))
                    pending.Enqueue(childId);
        }
        return scope;
    }

    public static async Task ValidateFacilityAsync(
        MyDbContext db,
        UserScopeContext userScope,
        Guid organizationId,
        Guid facilityId,
        IReadOnlySet<Guid>? facilityScope,
        CancellationToken cancellationToken)
    {
        var valid = await db.Facilities.AnyAsync(x => x.Id == facilityId && !x.IsDeleted && x.IsActive && x.OrganizationId == organizationId, cancellationToken);
        if (!valid)
            throw DeviceModuleException.BadRequest("Cơ sở không tồn tại, đã ngừng hoạt động hoặc không thuộc tổ chức.");
        if (!userScope.IsSystemAdministrator && facilityScope is not null && !facilityScope.Contains(facilityId))
            throw DeviceModuleException.Forbidden("Không được phép truy cập dữ liệu của cơ sở này.");
    }

    private sealed record FacilityNode(Guid Id, Guid? ParentId);
}
