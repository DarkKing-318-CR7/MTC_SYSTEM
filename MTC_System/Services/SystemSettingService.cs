using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Models;

namespace MTC_System.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private const string KEY_BROADCAST_MODE = "BROADCAST_MODE";
        private const string KEY_TIMEZONE = "TIMEZONE";
        private const string PREFIX_USER_SETTING = "USER_SETTING_";

        private readonly MyDbContext _context;

        public SystemSettingService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<SystemSettingResponseDto>>> GetByOrganizationAsync(Guid organizationId)
        {
            var settings = await _context.SystemSettings
                .AsNoTracking()
                .Where(s => s.OrganizationId == organizationId && !s.IsDeleted)
                .Select(s => new SystemSettingResponseDto
                {
                    Id = s.Id,
                    OrganizationId = s.OrganizationId,
                    SettingKey = s.SettingKey,
                    SettingValue = s.SettingValue,
                    Description = s.Description
                })
                .ToListAsync();

            return ApiResponse<List<SystemSettingResponseDto>>.Ok(settings);
        }

        public async Task<ApiResponse<OrganizationBroadcastSettingDto>> GetBroadcastSettingAsync(Guid organizationId)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted);
            if (org == null)
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail("Tổ chức không tồn tại!");
            }

            var settings = await _context.SystemSettings
                .AsNoTracking()
                .Where(s => s.OrganizationId == organizationId && !s.IsDeleted &&
                           (s.SettingKey == KEY_BROADCAST_MODE || s.SettingKey == KEY_TIMEZONE))
                .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

            var result = new OrganizationBroadcastSettingDto
            {
                BroadcastMode = settings.TryGetValue(KEY_BROADCAST_MODE, out var mode) ? mode : "Phát theo cơ sở",
                Timezone = settings.TryGetValue(KEY_TIMEZONE, out var tz) ? tz : "SE Asia Standard Time"
            };

            return ApiResponse<OrganizationBroadcastSettingDto>.Ok(result);
        }

        public async Task<ApiResponse<OrganizationBroadcastSettingDto>> UpdateBroadcastSettingAsync(Guid organizationId, OrganizationBroadcastSettingDto dto)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted);
            if (org == null)
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail("Tổ chức không tồn tại!");
            }

            if (dto.BroadcastMode != "Phát theo cơ sở" && dto.BroadcastMode != "Phát theo thiết bị")
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail("Chế độ phát chỉ chấp nhận 'Phát theo cơ sở' hoặc 'Phát theo thiết bị'!");
            }

            // 1. Cập nhật BROADCAST_MODE
            var modeSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.SettingKey == KEY_BROADCAST_MODE && !s.IsDeleted);

            if (modeSetting == null)
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    OrganizationId = organizationId,
                    SettingKey = KEY_BROADCAST_MODE,
                    SettingValue = dto.BroadcastMode,
                    Description = "Chế độ phát bản tin: Phát theo cơ sở hoặc Phát theo thiết bị (Issue #12)",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                modeSetting.SettingValue = dto.BroadcastMode;
                modeSetting.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // 2. Cập nhật TIMEZONE
            var tzSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.SettingKey == KEY_TIMEZONE && !s.IsDeleted);

            if (tzSetting == null)
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    OrganizationId = organizationId,
                    SettingKey = KEY_TIMEZONE,
                    SettingValue = dto.Timezone,
                    Description = "Múi giờ mặc định của tổ chức (Issue #12)",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                tzSetting.SettingValue = dto.Timezone;
                tzSetting.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return await GetBroadcastSettingAsync(organizationId);
        }

        public async Task<ApiResponse<UserPersonalSettingDto>> GetUserPersonalSettingAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserPersonalSettingDto>.Fail("Người dùng không tồn tại!");
            }

            var settingKey = $"{PREFIX_USER_SETTING}{userId}";
            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && !s.IsDeleted);

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<UserPersonalSettingDto>(setting.SettingValue);
                    if (parsed != null)
                    {
                        parsed.UserId = userId;
                        return ApiResponse<UserPersonalSettingDto>.Ok(parsed);
                    }
                }
                catch
                {
                    // Fallback to default
                }
            }

            return ApiResponse<UserPersonalSettingDto>.Ok(new UserPersonalSettingDto
            {
                UserId = userId,
                Timezone = "SE Asia Standard Time",
                BroadcastViewMode = "Table"
            });
        }

        public async Task<ApiResponse<UserPersonalSettingDto>> UpdateUserPersonalSettingAsync(UserPersonalSettingDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserPersonalSettingDto>.Fail("Người dùng không tồn tại!");
            }

            if (dto.BroadcastViewMode != "Table" && dto.BroadcastViewMode != "Calendar")
            {
                return ApiResponse<UserPersonalSettingDto>.Fail("Chế độ xem bản tin chỉ chấp nhận 'Table' (dạng bảng) hoặc 'Calendar' (dạng lịch)!");
            }

            var orgId = user.OrganizationId ?? Guid.Empty;
            if (orgId == Guid.Empty)
            {
                // Nếu là System Admin, lấy tổ chức đầu tiên hoặc Guid.Empty
                var firstOrg = await _context.Organizations.FirstOrDefaultAsync(o => !o.IsDeleted);
                if (firstOrg != null) orgId = firstOrg.Id;
            }

            var settingKey = $"{PREFIX_USER_SETTING}{dto.UserId}";
            var jsonValue = JsonSerializer.Serialize(dto);

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && !s.IsDeleted);

            if (setting == null)
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    OrganizationId = orgId,
                    SettingKey = settingKey,
                    SettingValue = jsonValue,
                    Description = $"Cấu hình cá nhân người dùng {user.Username} (Issue #12 - USR-SET)",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                setting.SettingValue = jsonValue;
                setting.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<UserPersonalSettingDto>.Ok(dto, "Cập nhật cấu hình cá nhân thành công");
        }
    }
}
