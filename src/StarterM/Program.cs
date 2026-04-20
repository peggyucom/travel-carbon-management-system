using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StarterM.Data;
using StarterM.Middleware;
using StarterM.Models;
using StarterM.Services;
using StarterM.Services.Implementations;
using StarterM.Services.Interfaces;

// Serilog 設定
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// === 資料庫 ===
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Identity ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    //數字.碼數.特殊符號.大寫.小寫
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    //同一個 Email 不能註冊兩次
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
})
.AddErrorDescriber<TraditionalChineseIdentityErrorDescriber>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});

// Cookie 設定
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// === DI 註冊 Services ===
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IDailyTripService, DailyTripService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<ICarbonService, CarbonService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddHttpClient<IDistanceService, OpenStreetMapDistanceService>();

// === Memory Cache ===
builder.Services.AddMemoryCache();

// === MVC ===
builder.Services.AddControllersWithViews();

var app = builder.Build();

// === Middleware Pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<ExceptionHandlingMiddleware>();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 使 Identity 的登入/驗證機制生效
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// === 資料庫初始化與種子資料 ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
