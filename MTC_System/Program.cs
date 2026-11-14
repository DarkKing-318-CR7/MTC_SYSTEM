using System;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.Services;
using MTC_System.Services.Implementations;
using MTC_System.Services.Implements;
using MTC_System.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 1. Controllers & Chống lỗi vòng lặp JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(error => $"{x.Key}: {error.ErrorMessage}"))
            .ToList();
        return new BadRequestObjectResult(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ.", errors));
    };
});

// 2. PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// 3. Đăng ký Cấu hình JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "MTC_SYSTEM_SUPER_SECRET_KEY_FOR_JWT_SECURITY_2026_@_LONG_ENOUGH_KEY";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MTC_Backend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MTC_Clients";
var deviceJwtSecretKey = builder.Configuration["DeviceJwt:SecretKey"] ?? jwtSecretKey;
var deviceJwtIssuer = builder.Configuration["DeviceJwt:Issuer"] ?? "MTC_DeviceGateway";
var deviceJwtAudience = builder.Configuration["DeviceJwt:Audience"] ?? "MTC_Devices";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddJwtBearer("DeviceBearer", options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(deviceJwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = deviceJwtIssuer,
        ValidateAudience = true,
        ValidAudience = deviceJwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/device"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var deviceIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var versionValue = context.Principal?.FindFirstValue("token_version");
            if (!Guid.TryParse(deviceIdValue, out var deviceId) || !int.TryParse(versionValue, out var tokenVersion))
            {
                context.Fail("Device Token không hợp lệ.");
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<MyDbContext>();
            var valid = await dbContext.Devices.AsNoTracking().AnyAsync(
                x => x.Id == deviceId &&
                     !x.IsDeleted &&
                     x.ActiveStatus &&
                     x.IsActivated &&
                     x.DeviceTokenVersion == tokenVersion,
                context.HttpContext.RequestAborted);
            if (!valid)
                context.Fail("Device Token đã bị thu hồi hoặc thiết bị đã bị khóa.");
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DeviceView", policy =>
    {
        policy.AuthenticationSchemes.Add(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("DeviceManage", policy =>
    {
        policy.AuthenticationSchemes.Add(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("ADMINISTRATOR", "Administrator", "IT");
    });

    options.AddPolicy("DeviceControl", policy =>
    {
        policy.AuthenticationSchemes.Add(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("ADMINISTRATOR", "Administrator", "IT");
    });

    options.AddPolicy("DeviceAdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("ADMINISTRATOR", "Administrator");
    });
});

// 4. Đăng ký Services (Dependency Injection)
// [Dev 1]
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<UpgradePackageService>();
builder.Services.AddScoped<PackageChangeService>();

// [Dev 2]
builder.Services.AddScoped<IChunkedMediaUploadSvc, ChunkedMediaUploadSvc>();
builder.Services.AddSingleton<IHlsProcessRunner, HlsProcessRunner>();
builder.Services.AddScoped<HlsProcessingService>();
builder.Services.AddHostedService<HlsProcessingWorker>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaCategoryService, MediaCategoryService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();

// [Dev 3]
builder.Services.AddScoped<IBroadcastService, BroadcastService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();

// [Dev 4] - CRUD Thiết bị, nhóm, nhà phân phối, mã lỗi và nhật ký.
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceGroupService, DeviceGroupService>();
builder.Services.AddScoped<IDeviceVendorService, DeviceVendorService>();
builder.Services.AddScoped<IDeviceErrorCodeService, DeviceErrorCodeService>();
builder.Services.AddScoped<IDeviceActivityLogService, DeviceActivityLogService>();
builder.Services.AddScoped<IDevicePlayHistoryService, DevicePlayHistoryService>();
builder.Services.AddScoped<IDeviceActivationService, DeviceActivationService>();

// SignalR & Realtime Connection Manager
builder.Services.AddSignalR();
builder.Services.AddSingleton<MTC_System.Realtime.IDeviceConnectionManager, MTC_System.Realtime.DeviceConnectionManager>();
builder.Services.AddSingleton<MTC_System.Realtime.IDeviceCommandManager, MTC_System.Realtime.DeviceCommandManager>();
builder.Services.AddSingleton<MTC_System.Realtime.IDispatchOccurrenceManager, MTC_System.Realtime.DispatchOccurrenceManager>();
builder.Services.AddScoped<MTC_System.Realtime.IBroadcastRealtimeDispatcher, MTC_System.Realtime.BroadcastRealtimeDispatcher>();

// Background Hosted Services (Heartbeat Timeout Monitor & Broadcast Scheduler)
builder.Services.AddSingleton<MTC_System.Realtime.DeviceHeartbeatMonitorHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MTC_System.Realtime.DeviceHeartbeatMonitorHostedService>());
builder.Services.AddHostedService<MTC_System.Realtime.BroadcastSchedulerHostedService>();

// 5. Swagger UI với nút ổ khóa nhập Bearer Token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MTC System API",
        Version = "v1",
        Description = "Hệ thống Quản trị & Điều hành Màn hình / Loa tập trung (MTC Backend)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Access Token vào đây (Swagger sẽ tự động đính kèm tiền tố 'Bearer '):",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            DeviceModuleException deviceException => deviceException.StatusCode,
            MediaModuleException mediaException => mediaException.StatusCode,
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = exception switch
        {
            DeviceModuleException deviceException => deviceException.Message,
            MediaModuleException mediaException => mediaException.Message,
            DbUpdateException => "Dữ liệu xung đột với ràng buộc của cơ sở dữ liệu.",
            _ => "Đã xảy ra lỗi trong quá trình xử lý yêu cầu."
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Cấu hình Static Files cho web wwwroot (không public thư mục /storage để bảo vệ media theo tổ chức)
var storagePath = MediaStoragePathResolver.ResolveStorageRoot(app.Configuration, app.Environment);
Directory.CreateDirectory(storagePath);
var ffmpegPath = app.Configuration["MediaProcessing:FfmpegPath"] ?? "ffmpeg";
var ffprobePath = app.Configuration["MediaProcessing:FfprobePath"] ?? "ffprobe";
var timeoutSetting = app.Configuration["MediaProcessing:TimeoutSeconds"];
var timeoutSeconds = int.TryParse(timeoutSetting, out var configuredTimeoutSeconds)
    ? configuredTimeoutSeconds is >= 30 and <= 86400
        ? configuredTimeoutSeconds.ToString()
        : $"invalid ({configuredTimeoutSeconds}; processor rejects values outside 30-86400)"
    : "3600 (default)";
var segmentDurationSetting = app.Configuration["MediaProcessing:SegmentDurationSeconds"];
var segmentDurationSeconds = int.TryParse(segmentDurationSetting, out var configuredSegmentDurationSeconds)
    ? configuredSegmentDurationSeconds is >= 2 and <= 30
        ? configuredSegmentDurationSeconds.ToString()
        : $"invalid ({configuredSegmentDurationSeconds}; processor rejects values outside 2-30)"
    : "6 (default)";
var rawStorageRoot = app.Configuration["MediaUpload:StorageRoot"];
app.Logger.LogInformation(
    "HLS-DIAGNOSTIC Startup Environment={EnvironmentName} ContentRoot={ContentRootPath} CurrentDirectory={CurrentDirectory} BaseDirectory={BaseDirectory} FfmpegPath={FfmpegPath} FfprobePath={FfprobePath} TimeoutSeconds={TimeoutSeconds} SegmentDurationSeconds={SegmentDurationSeconds} StorageRootRaw={StorageRootRaw} StorageRootUsesDefault={StorageRootUsesDefault} ResolvedStorageRoot={ResolvedStorageRoot} FfmpegExists={FfmpegExists} FfprobeExists={FfprobeExists}",
    app.Environment.EnvironmentName,
    app.Environment.ContentRootPath,
    Directory.GetCurrentDirectory(),
    AppContext.BaseDirectory,
    ffmpegPath,
    ffprobePath,
    timeoutSeconds,
    segmentDurationSeconds,
    rawStorageRoot,
    rawStorageRoot is null,
    storagePath,
    File.Exists(ffmpegPath),
    File.Exists(ffprobePath));
app.UseStaticFiles();

// BẮT BUỘC: UseAuthentication phải đứng TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Tự động khởi tạo dữ liệu mặc định: Quyền chức năng, Vai trò Admin và Tài khoản admin / Admin@123456
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    try
    {
        if (context.Database.CanConnect())
        {
            var adminRoleId = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000000");

            // 1. Tự động khởi tạo danh mục Permissions & gán cho ADMINISTRATOR
            if (!context.Permissions.Any())
            {
                var defaultPermissions = new List<MTC_System.Models.Permission>
                {
                    // Nhóm: Người dùng & Phân quyền
                    new() { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000001"), PermissionCode = "USERS_VIEW", PermissionName = "Xem danh sách người dùng", ModuleGroup = "Người dùng & Phân quyền" },
                    new() { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000002"), PermissionCode = "USERS_MANAGE", PermissionName = "Tạo, sửa, khóa người dùng", ModuleGroup = "Người dùng & Phân quyền" },
                    new() { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000003"), PermissionCode = "ROLES_MANAGE", PermissionName = "Quản lý vai trò & ma trận phân quyền", ModuleGroup = "Người dùng & Phân quyền" },

                    // Nhóm: Tổ chức & Cơ sở
                    new() { Id = Guid.Parse("bbbbbbbb-0002-0000-0000-000000000001"), PermissionCode = "ORGS_VIEW", PermissionName = "Xem thông tin tổ chức & cây cơ sở", ModuleGroup = "Tổ chức & Cơ sở" },
                    new() { Id = Guid.Parse("bbbbbbbb-0002-0000-0000-000000000002"), PermissionCode = "ORGS_MANAGE", PermissionName = "Quản lý tổ chức & phân cấp cơ sở", ModuleGroup = "Tổ chức & Cơ sở" },

                    // Nhóm: Media & Bản mẫu
                    new() { Id = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000001"), PermissionCode = "MEDIA_VIEW", PermissionName = "Xem kho media", ModuleGroup = "Media & Bản mẫu" },
                    new() { Id = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000002"), PermissionCode = "MEDIA_UPLOAD", PermissionName = "Tải lên media dung lượng lớn", ModuleGroup = "Media & Bản mẫu" },
                    new() { Id = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000003"), PermissionCode = "MEDIA_APPROVE", PermissionName = "Phê duyệt hoặc từ chối media", ModuleGroup = "Media & Bản mẫu" },
                    new() { Id = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000004"), PermissionCode = "TEMPLATE_MANAGE", PermissionName = "Quản lý Template & Playlist", ModuleGroup = "Media & Bản mẫu" },

                    // Nhóm: Bản tin & Lịch phát
                    new() { Id = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000001"), PermissionCode = "BROADCAST_VIEW", PermissionName = "Xem danh sách bản tin & lịch phát", ModuleGroup = "Bản tin & Lịch phát" },
                    new() { Id = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000002"), PermissionCode = "BROADCAST_CREATE", PermissionName = "Tạo bản tin & thiết lập lịch phát", ModuleGroup = "Bản tin & Lịch phát" },
                    new() { Id = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000003"), PermissionCode = "BROADCAST_APPROVE", PermissionName = "Phê duyệt bản tin", ModuleGroup = "Bản tin & Lịch phát" },
                    new() { Id = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000004"), PermissionCode = "BROADCAST_EMERGENCY", PermissionName = "Phát bản tin khẩn cấp ghi đè", ModuleGroup = "Bản tin & Lịch phát" },

                    // Nhóm: Thiết bị & Mạng
                    new() { Id = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000001"), PermissionCode = "DEVICE_VIEW", PermissionName = "Xem danh sách & trạng thái thiết bị", ModuleGroup = "Thiết bị & Mạng" },
                    new() { Id = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000002"), PermissionCode = "DEVICE_CONTROL", PermissionName = "Điều khiển âm lượng, Reboot từ xa", ModuleGroup = "Thiết bị & Mạng" },
                    new() { Id = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000003"), PermissionCode = "DEVICE_MANAGE", PermissionName = "Thêm, gán nhóm, mã lỗi thiết bị", ModuleGroup = "Thiết bị & Mạng" },

                    // Nhóm: Gói cước & Báo cáo
                    new() { Id = Guid.Parse("bbbbbbbb-0006-0000-0000-000000000001"), PermissionCode = "PACKAGE_MANAGE", PermissionName = "Quản lý gói cước & hợp đồng", ModuleGroup = "Gói cước & Báo cáo" },
                    new() { Id = Guid.Parse("bbbbbbbb-0006-0000-0000-000000000002"), PermissionCode = "REPORT_VIEW", PermissionName = "Xem báo cáo thống kê Dashboard", ModuleGroup = "Gói cước & Báo cáo" },
                    new() { Id = Guid.Parse("bbbbbbbb-0006-0000-0000-000000000003"), PermissionCode = "AUDITLOG_VIEW", PermissionName = "Xem nhật ký kiểm toán hệ thống", ModuleGroup = "Gói cước & Báo cáo" }
                };

                context.Permissions.AddRange(defaultPermissions);
                context.SaveChanges();

                // Gán tất cả quyền cho ADMINISTRATOR
                foreach (var perm in defaultPermissions)
                {
                    context.RolePermissions.Add(new MTC_System.Models.RolePermission
                    {
                        RoleId = adminRoleId,
                        PermissionId = perm.Id
                    });
                }
                context.SaveChanges();
                Console.WriteLine("[MTC_System] Đã tự động khởi tạo danh mục 18 Permissions và Role Matrix.");
            }

            // 2. Tự động khởi tạo tài khoản Quản trị viên (admin / Admin@123456)
            var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var adminUser = context.Users.FirstOrDefault(u => u.Id == adminId || u.Username == "admin");
            if (adminUser == null)
            {
                adminUser = new MTC_System.Models.User
                {
                    Id = adminId,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                    FullName = "Quản trị viên Hệ thống",
                    Email = "admin@mtc.vn",
                    PhoneNumber = "0901234567",
                    IsActive = true
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
                Console.WriteLine("[MTC_System] Đã tự động khởi tạo tài khoản mặc định: admin / Admin@123456");
            }

            // Gán Role ADMINISTRATOR cho adminUser nếu chưa có
            bool hasAdminRole = context.UserRoles.Any(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRoleId && !ur.IsDeleted);
            if (!hasAdminRole)
            {
                context.UserRoles.Add(new MTC_System.Models.UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRoleId
                });
                context.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MTC_System] Khởi tạo dữ liệu seed: {ex.Message}");
    }
}

app.MapControllers();
app.MapHub<MTC_System.Hubs.DeviceHub>("/hubs/device");

app.Run();

public partial class Program { }
