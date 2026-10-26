using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 1. Controllers & Chống lỗi vòng lặp JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 2. PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// 3. Đăng ký Services (Dependency Injection)
// [Dev 1]
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();

// [Dev 3]
builder.Services.AddScoped<IBroadcastService, BroadcastService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();

// 4. Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
