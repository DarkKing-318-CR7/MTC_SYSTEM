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

        public async Task<ApiResponse<OrganizationBroadcastSettingDto>> GetBroadcastSettingAsync(
            Guid organizationId,
            Guid? requestingFacilityId = null,
            bool isSystemAdmin = false)
        {
            // QUY TẮC: "Chỉ hiển thị ở cấp Tổ chức cao nhất và áp dụng cho toàn bộ tổ chức"
            if (requestingFacilityId.HasValue && !isSystemAdmin)
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail(
                    "Thiết lập hệ thống chỉ hiển thị ở cấp Tổ chức cao nhất và áp dụng cho toàn bộ tổ chức. Tài khoản cấp cơ sở không có quyền truy cập!");
            }

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
                OrganizationId = org.Id,
                OrganizationName = org.OrganizationName,
                BroadcastMode = settings.TryGetValue(KEY_BROADCAST_MODE, out var mode) ? mode : "Phát theo cơ sở",
                Timezone = settings.TryGetValue(KEY_TIMEZONE, out var tz) ? tz : "SE Asia Standard Time",
                IsRootOrganizationLevel = true,
                CanEdit = true
            };

            return ApiResponse<OrganizationBroadcastSettingDto>.Ok(result);
        }

        public async Task<ApiResponse<OrganizationBroadcastSettingDto>> UpdateBroadcastSettingAsync(
            Guid organizationId,
            OrganizationBroadcastSettingDto dto,
            Guid? requestingFacilityId = null,
            bool isSystemAdmin = false)
        {
            // QUY TẮC: "Chỉ hiển thị ở cấp Tổ chức cao nhất và áp dụng cho toàn bộ tổ chức"
            if (requestingFacilityId.HasValue && !isSystemAdmin)
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail(
                    "Thiết lập hệ thống chỉ được phép cấu hình ở cấp Tổ chức cao nhất. Tài khoản cấp cơ sở không có quyền cập nhật!");
            }

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted);
            if (org == null)
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail("Tổ chức không tồn tại!");
            }

            var rawMode = dto.BroadcastMode?.Trim() ?? string.Empty;
            string normalizedMode;
            if (rawMode.Equals("Phát theo cơ sở", StringComparison.OrdinalIgnoreCase))
            {
                normalizedMode = "Phát theo cơ sở";
            }
            else if (rawMode.Equals("Phát theo thiết bị", StringComparison.OrdinalIgnoreCase))
            {
                normalizedMode = "Phát theo thiết bị";
            }
            else
            {
                return ApiResponse<OrganizationBroadcastSettingDto>.Fail(
                    "Chế độ tạo bản tin không hợp lệ! Chỉ chấp nhận 'Phát theo cơ sở' hoặc 'Phát theo thiết bị'.");
            }

            var timezone = dto.Timezone?.Trim();
            if (string.IsNullOrWhiteSpace(timezone))
            {
                timezone = "SE Asia Standard Time";
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
                    SettingValue = normalizedMode,
                    Description = "Chế độ tạo bản tin: Phát theo cơ sở hoặc Phát theo thiết bị (Cấp tổ chức cao nhất)",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                modeSetting.SettingValue = normalizedMode;
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
                    SettingValue = timezone,
                    Description = "Múi giờ chuẩn của tổ chức (Cấp tổ chức cao nhất)",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                tzSetting.SettingValue = timezone;
                tzSetting.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();

            var updatedResult = new OrganizationBroadcastSettingDto
            {
                OrganizationId = org.Id,
                OrganizationName = org.OrganizationName,
                BroadcastMode = normalizedMode,
                Timezone = timezone,
                IsRootOrganizationLevel = true,
                CanEdit = true
            };

            return ApiResponse<OrganizationBroadcastSettingDto>.Ok(updatedResult, "Cập nhật thiết lập hệ thống tổ chức thành công");
        }

        public async Task<ApiResponse<UserPersonalSettingDto>> GetUserPersonalSettingAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserPersonalSettingDto>.Fail("Người dùng không tồn tại!");
            }

            var displayName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Username;

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
                        parsed.Username = displayName;
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
                Username = displayName,
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

            // Chuẩn hóa chế độ xem bản tin: dạng bảng / dạng lịch
            var rawViewMode = dto.BroadcastViewMode?.Trim() ?? "Table";
            string normalizedViewMode;

            if (rawViewMode.Equals("Table", StringComparison.OrdinalIgnoreCase) ||
                rawViewMode.Equals("Bảng", StringComparison.OrdinalIgnoreCase) ||
                rawViewMode.Equals("Dạng bảng", StringComparison.OrdinalIgnoreCase) ||
                rawViewMode.Equals("Grid", StringComparison.OrdinalIgnoreCase))
            {
                normalizedViewMode = "Table";
            }
            else if (rawViewMode.Equals("Calendar", StringComparison.OrdinalIgnoreCase) ||
                     rawViewMode.Equals("Lịch", StringComparison.OrdinalIgnoreCase) ||
                     rawViewMode.Equals("Dạng lịch", StringComparison.OrdinalIgnoreCase))
            {
                normalizedViewMode = "Calendar";
            }
            else
            {
                return ApiResponse<UserPersonalSettingDto>.Fail(
                    "Chế độ xem bản tin không hợp lệ! Chỉ chấp nhận dạng bảng ('Table' / 'Dạng bảng') hoặc dạng lịch ('Calendar' / 'Dạng lịch').");
            }

            dto.BroadcastViewMode = normalizedViewMode;
            if (string.IsNullOrWhiteSpace(dto.Timezone))
            {
                dto.Timezone = "SE Asia Standard Time";
            }

            var orgId = user.OrganizationId ?? Guid.Empty;
            if (orgId == Guid.Empty)
            {
                var firstOrg = await _context.Organizations.FirstOrDefaultAsync(o => !o.IsDeleted);
                if (firstOrg != null) orgId = firstOrg.Id;
            }

            var settingKey = $"{PREFIX_USER_SETTING}{dto.UserId}";
            var jsonValue = JsonSerializer.Serialize(new UserPersonalSettingDto
            {
                UserId = dto.UserId,
                Username = user.FullName ?? user.Username,
                Timezone = dto.Timezone,
                BroadcastViewMode = normalizedViewMode
            });

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && !s.IsDeleted);

            if (setting == null)
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    OrganizationId = orgId,
                    SettingKey = settingKey,
                    SettingValue = jsonValue,
                    Description = $"Cấu hình cá nhân người dùng {user.Username} (Múi giờ & Chế độ xem bản tin)",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                setting.SettingValue = jsonValue;
                setting.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();

            dto.Username = user.FullName ?? user.Username;
            return ApiResponse<UserPersonalSettingDto>.Ok(dto, "Cập nhật cấu hình cá nhân thành công");
        }

        public Task<ApiResponse<List<TimezoneItemDto>>> GetAvailableTimezonesAsync()
        {
            var popularTimezones = new List<TimezoneItemDto>
            {
                new() { Id = "SE Asia Standard Time", DisplayName = "(UTC+07:00) Bangkok, Hanoi, Jakarta", UtcOffset = "+07:00" },
                new() { Id = "Asia/Ho_Chi_Minh", DisplayName = "(UTC+07:00) Vietnam (Hanoi, Ho Chi Minh)", UtcOffset = "+07:00" },
                new() { Id = "Singapore Standard Time", DisplayName = "(UTC+08:00) Singapore, Kuala Lumpur", UtcOffset = "+08:00" },
                new() { Id = "Tokyo Standard Time", DisplayName = "(UTC+09:00) Osaka, Sapporo, Tokyo", UtcOffset = "+09:00" },
                new() { Id = "UTC", DisplayName = "(UTC+00:00) Coordinated Universal Time", UtcOffset = "+00:00" },
                new() { Id = "Korea Standard Time", DisplayName = "(UTC+09:00) Seoul", UtcOffset = "+09:00" },
                new() { Id = "China Standard Time", DisplayName = "(UTC+08:00) Beijing, Chongqing, Hong Kong", UtcOffset = "+08:00" },
                new() { Id = "India Standard Time", DisplayName = "(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi", UtcOffset = "+05:30" },
                new() { Id = "GMT Standard Time", DisplayName = "(UTC+00:00) Dublin, Edinburgh, Lisbon, London", UtcOffset = "+00:00" },
                new() { Id = "W. Europe Standard Time", DisplayName = "(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Paris", UtcOffset = "+01:00" },
                new() { Id = "Eastern Standard Time", DisplayName = "(UTC-05:00) Eastern Time (US & Canada)", UtcOffset = "-05:00" },
                new() { Id = "Pacific Standard Time", DisplayName = "(UTC-08:00) Pacific Time (US & Canada)", UtcOffset = "-08:00" }
            };

            return Task.FromResult(ApiResponse<List<TimezoneItemDto>>.Ok(popularTimezones));
        }
    }
}
