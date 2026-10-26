using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.Services;
using MTC_System.Services.Implementations;
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
// 2. Đăng ký PostgreSQL DbContext
// 2. PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Dev 4 - CRUD Thiết bị, nhóm, nhà phân phối, mã lỗi và nhật ký.
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IDeviceGroupService, DeviceGroupService>();
builder.Services.AddScoped<IDeviceVendorService, DeviceVendorService>();
builder.Services.AddScoped<IDeviceErrorCodeService, DeviceErrorCodeService>();
builder.Services.AddScoped<IDeviceActivityLogService, DeviceActivityLogService>();
builder.Services.AddScoped<IDevicePlayHistoryService, DevicePlayHistoryService>();

// 3. Đăng ký Services (Dependency Injection)
// [Dev 1]
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// [Dev 3]
builder.Services.AddScoped<IBroadcastService, BroadcastService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();

// 4. Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
