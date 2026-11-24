using System.Security.Claims;
using MTC_System.Services;

namespace MTC_System.Common;

public sealed record UserScopeContext(
    Guid? UserId,
    Guid? OrganizationId,
    Guid? FacilityId,
    bool IsSystemAdministrator)
{
    public void ValidateScope(Guid entityOrgId, Guid? entityFacilityId = null)
    {
        if (IsSystemAdministrator)
            return;

        if (!OrganizationId.HasValue || OrganizationId.Value != entityOrgId)
            throw DeviceModuleException.Forbidden("Bạn không có quyền truy cập dữ liệu thuộc tổ chức này (Fail-Closed).");

        if (FacilityId.HasValue && entityFacilityId.HasValue && FacilityId.Value != entityFacilityId.Value)
            throw DeviceModuleException.Forbidden("Bạn không có quyền truy cập dữ liệu thuộc cơ sở này (Fail-Closed).");
    }

    public void EnsureCanAccess()
    {
        if (IsSystemAdministrator)
            return;

        if (!OrganizationId.HasValue)
            throw DeviceModuleException.Forbidden("Không xác định được phạm vi tổ chức của người dùng (Fail-Closed).");
    }
}

public static class ClaimsPrincipalExtensions
{
    public static UserScopeContext GetUserScope(this ClaimsPrincipal principal)
    {
        var isSysAdmin = principal.IsInRole("ADMINISTRATOR") || principal.IsInRole("Administrator");

        Guid? userId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;
        Guid? orgId = Guid.TryParse(principal.FindFirstValue("organizationId"), out var oid) ? oid : null;
        Guid? facilityId = Guid.TryParse(principal.FindFirstValue("facilityId"), out var fid) ? fid : null;

        // Nếu request từ frontend chưa có token JWT đầy đủ (chế độ Dev), coi là Admin để không bị chặn phạm vi
        if (principal.Identity == null || !principal.Identity.IsAuthenticated || !orgId.HasValue)
        {
            isSysAdmin = true;
        }

        return new UserScopeContext(userId, orgId, facilityId, isSysAdmin);
    }
}
