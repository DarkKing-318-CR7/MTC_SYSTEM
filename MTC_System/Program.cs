using System;
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
});

builder.Services.AddAuthorization();

// 4. Đăng ký Services (Dependency Injection)
// [Dev 1]
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// [Dev 2]
builder.Services.AddScoped<IChunkedMediaUploadSvc, ChunkedMediaUploadSvc>();

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
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = exception switch
        {
            DeviceModuleException deviceException => deviceException.Message,
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

// BẮT BUỘC: UseAuthentication phải đứng TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Tự động khởi tạo tài khoản Quản trị viên (admin / Admin@123456) nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    try
    {
        if (context.Database.CanConnect() && !context.Users.Any())
        {
            var adminUser = new MTC_System.Models.User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MTC_System] Khởi tạo dữ liệu seed: {ex.Message}");
    }
}

app.MapControllers();

app.Run();
