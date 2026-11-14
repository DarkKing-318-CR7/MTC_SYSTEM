using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Templates;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implements;

public sealed class TemplateService : ITemplateService
{
    private static readonly string[] AllowedZoneMediaTypes = ["Video", "Audio", "Image", "URL"];
    private readonly MyDbContext _db;

    public TemplateService(MyDbContext db) => _db = db;

    public async Task<IReadOnlyList<TemplateResponse>> GetAllAsync(
        UserScopeContext userScope,
        Guid? organizationId,
        Guid? facilityId,
        CancellationToken cancellationToken)
    {
        userScope.EnsureCanAccess();
        if (!userScope.IsSystemAdministrator)
        {
            if (organizationId.HasValue && organizationId != userScope.OrganizationId)
                throw DeviceModuleException.Forbidden("Không được phép truy vấn dữ liệu của tổ chức khác.");
            organizationId = userScope.OrganizationId;
        }

        var query = _db.Templates.AsNoTracking().Where(x => !x.IsDeleted);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (facilityId.HasValue)
            query = query.Where(x => x.FacilityId == facilityId.Value);
        if (!userScope.IsSystemAdministrator && userScope.FacilityId.HasValue)
        {
            var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, organizationId!.Value, cancellationToken);
            if (facilityId.HasValue && facilityScope is not null && !facilityScope.Contains(facilityId.Value))
                throw DeviceModuleException.Forbidden("Không được phép truy cập dữ liệu của cơ sở này.");
            if (facilityScope is not null)
                query = query.Where(x => facilityScope.Contains(x.FacilityId));
        }

        var templates = await query.Include(x => x.Zones.Where(z => !z.IsDeleted))
            .OrderBy(x => x.TemplateName)
            .ToListAsync(cancellationToken);
        return templates.Select(Map).ToList();
    }

    public async Task<TemplateResponse> GetByIdAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var template = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, template.OrganizationId, template.FacilityId, cancellationToken);
        return Map(template);
    }

    public async Task<TemplateResponse> CreateAsync(TemplateUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var organizationId = ResolveOrganizationId(userScope, request.OrganizationId);
        var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, organizationId, cancellationToken);
        await ValidateRequestAsync(request, userScope, organizationId, facilityScope, cancellationToken);

        var template = new Template
        {
            OrganizationId = organizationId,
            FacilityId = request.FacilityId,
            TemplateName = request.TemplateName.Trim(),
            Description = request.Description?.Trim(),
            Width = request.Width,
            Height = request.Height,
            LayoutConfigJson = request.LayoutConfigJson,
            IsActive = request.IsActive,
            CreatedBy = userScope.UserId
        };
        foreach (var zoneRequest in request.Zones)
            template.Zones.Add(MapZone(zoneRequest, userScope.UserId));

        _db.Templates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task<TemplateResponse> UpdateAsync(Guid id, TemplateUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var template = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, template.OrganizationId, template.FacilityId, cancellationToken);
        if (request.OrganizationId.HasValue && request.OrganizationId.Value != template.OrganizationId)
            throw DeviceModuleException.BadRequest("Không được phép thay đổi tổ chức của Template.");

        var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, template.OrganizationId, cancellationToken);
        await ValidateRequestAsync(request, userScope, template.OrganizationId, facilityScope, cancellationToken);

        var incomingIds = request.Zones.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        var existingIds = template.Zones.Select(x => x.Id).ToHashSet();
        if (incomingIds.Except(existingIds).Any())
            throw DeviceModuleException.BadRequest("Một hoặc nhiều TemplateZone không thuộc Template đã chỉ định.");

        template.FacilityId = request.FacilityId;
        template.TemplateName = request.TemplateName.Trim();
        template.Description = request.Description?.Trim();
        template.Width = request.Width;
        template.Height = request.Height;
        template.LayoutConfigJson = request.LayoutConfigJson;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        template.UpdatedBy = userScope.UserId;

        foreach (var zone in template.Zones.Where(x => !incomingIds.Contains(x.Id)))
        {
            zone.IsDeleted = true;
            zone.UpdatedAt = DateTimeOffset.UtcNow;
            zone.UpdatedBy = userScope.UserId;
        }

        foreach (var zoneRequest in request.Zones)
        {
            if (!zoneRequest.Id.HasValue)
            {
                template.Zones.Add(MapZone(zoneRequest, userScope.UserId));
                continue;
            }
            var zone = template.Zones.Single(x => x.Id == zoneRequest.Id.Value);
            ApplyZone(zone, zoneRequest, userScope.UserId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task DeleteAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var template = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, template.OrganizationId, template.FacilityId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        template.IsDeleted = true;
        template.UpdatedAt = now;
        template.UpdatedBy = userScope.UserId;
        foreach (var zone in template.Zones.Where(x => !x.IsDeleted))
        {
            zone.IsDeleted = true;
            zone.UpdatedAt = now;
            zone.UpdatedBy = userScope.UserId;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Template> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Templates.Include(x => x.Zones.Where(z => !z.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
        ?? throw DeviceModuleException.NotFound("Không tìm thấy Template.");

    private async Task EnsureCanAccessAsync(UserScopeContext userScope, Guid organizationId, Guid facilityId, CancellationToken cancellationToken)
    {
        var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, organizationId, cancellationToken);
        if (!userScope.IsSystemAdministrator && facilityScope is not null && !facilityScope.Contains(facilityId))
            throw DeviceModuleException.Forbidden("Không được phép truy cập dữ liệu của cơ sở này.");
    }

    private async Task ValidateRequestAsync(
        TemplateUpsertRequest request,
        UserScopeContext userScope,
        Guid organizationId,
        IReadOnlySet<Guid>? facilityScope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName) || request.TemplateName.Trim().Length > 255)
            throw DeviceModuleException.BadRequest("Tên Template bắt buộc và không vượt quá 255 ký tự.");
        if (request.Width <= 0 || request.Height <= 0)
            throw DeviceModuleException.BadRequest("Chiều rộng và chiều cao Template phải lớn hơn 0.");
        if (!string.IsNullOrWhiteSpace(request.LayoutConfigJson))
        {
            try
            {
                using var _ = JsonDocument.Parse(request.LayoutConfigJson);
            }
            catch (JsonException)
            {
                throw DeviceModuleException.BadRequest("Cấu hình bố cục phải là JSON hợp lệ.");
            }
        }

        await FacilityScopeResolver.ValidateFacilityAsync(_db, userScope, organizationId, request.FacilityId, facilityScope, cancellationToken);
        foreach (var zone in request.Zones)
        {
            if (zone.Id == Guid.Empty || string.IsNullOrWhiteSpace(zone.ZoneName) || zone.ZoneName.Trim().Length > 100)
                throw DeviceModuleException.BadRequest("Mỗi TemplateZone phải có tên hợp lệ.");
            if (zone.X < 0 || zone.Y < 0 || zone.ZIndex < 0 || zone.WidthPercent <= 0 || zone.WidthPercent > 100 || zone.HeightPercent <= 0 || zone.HeightPercent > 100)
                throw DeviceModuleException.BadRequest("Tọa độ, lớp hiển thị và kích thước TemplateZone không hợp lệ.");
            NormalizeAllowedMediaTypes(zone.AllowedMediaTypes);
        }
    }

    private static Guid ResolveOrganizationId(UserScopeContext userScope, Guid? requestedOrganizationId)
    {
        userScope.EnsureCanAccess();
        if (userScope.IsSystemAdministrator)
            return requestedOrganizationId is { } adminOrganizationId && adminOrganizationId != Guid.Empty
                ? adminOrganizationId
                : throw DeviceModuleException.BadRequest("OrganizationId là bắt buộc với quản trị viên hệ thống.");
        if (requestedOrganizationId.HasValue && requestedOrganizationId.Value != userScope.OrganizationId)
            throw DeviceModuleException.Forbidden("Không được phép tạo dữ liệu cho tổ chức khác.");
        return userScope.OrganizationId!.Value;
    }

    private static TemplateZone MapZone(TemplateZoneUpsertRequest request, Guid? userId) => new()
    {
        ZoneName = request.ZoneName.Trim(),
        X = request.X,
        Y = request.Y,
        WidthPercent = request.WidthPercent,
        HeightPercent = request.HeightPercent,
        ZIndex = request.ZIndex,
        AllowedMediaTypes = NormalizeAllowedMediaTypes(request.AllowedMediaTypes),
        CreatedBy = userId
    };

    private static void ApplyZone(TemplateZone zone, TemplateZoneUpsertRequest request, Guid? userId)
    {
        zone.ZoneName = request.ZoneName.Trim();
        zone.X = request.X;
        zone.Y = request.Y;
        zone.WidthPercent = request.WidthPercent;
        zone.HeightPercent = request.HeightPercent;
        zone.ZIndex = request.ZIndex;
        zone.AllowedMediaTypes = NormalizeAllowedMediaTypes(request.AllowedMediaTypes);
        zone.IsDeleted = false;
        zone.UpdatedAt = DateTimeOffset.UtcNow;
        zone.UpdatedBy = userId;
    }

    private static string NormalizeAllowedMediaTypes(string? value)
    {
        var types = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var normalized = new List<string>();
        foreach (var type in types)
        {
            var canonical = AllowedZoneMediaTypes.FirstOrDefault(x => x.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (canonical is null)
                throw DeviceModuleException.BadRequest("TemplateZone chỉ hỗ trợ Video, Audio, Image hoặc URL; không hỗ trợ phát trực tiếp hay tần số.");
            if (!normalized.Contains(canonical, StringComparer.OrdinalIgnoreCase))
                normalized.Add(canonical);
        }
        if (normalized.Count == 0)
            throw DeviceModuleException.BadRequest("TemplateZone phải khai báo ít nhất một loại media được phép.");
        return string.Join(',', normalized);
    }

    private static TemplateResponse Map(Template template) => new()
    {
        Id = template.Id,
        OrganizationId = template.OrganizationId,
        FacilityId = template.FacilityId,
        TemplateName = template.TemplateName,
        Description = template.Description,
        Width = template.Width,
        Height = template.Height,
        LayoutConfigJson = template.LayoutConfigJson,
        IsActive = template.IsActive,
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt,
        Zones = template.Zones.Where(x => !x.IsDeleted).OrderBy(x => x.ZIndex).ThenBy(x => x.Id).Select(x => new TemplateZoneResponse
        {
            Id = x.Id,
            ZoneName = x.ZoneName,
            X = x.X,
            Y = x.Y,
            WidthPercent = x.WidthPercent,
            HeightPercent = x.HeightPercent,
            ZIndex = x.ZIndex,
            AllowedMediaTypes = x.AllowedMediaTypes
        }).ToList()
    };
}
