using RagChatbot.Business.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using RagChatbot.Business.Interfaces;
using RagChatbot.Business.Extensions;

var builder = WebApplication.CreateBuilder(args);
// Load .env file if it exists
var envPath1 = Path.Combine(Directory.GetCurrentDirectory(), ".env");
var envPath2 = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath1))
{
    DotNetEnv.Env.Load(envPath1);
}
else if (File.Exists(envPath2))
{
    DotNetEnv.Env.Load(envPath2);
}
else
{
    DotNetEnv.Env.Load(); // Try to load from current directory fallback
}

// Add services to the container.
builder.Services.AddRazorPages();

// Setup DbContext and Services via Business layer
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Services.AddInfrastructureAndBusinessServices(connectionString);

// Add SignalR
builder.Services.AddSignalR();

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);

        // Cáº¤U HÃŒNH THÃŠM DÃ’NG NÃ€Y: Äiá»u hÆ°á»›ng khi bá»‹ cháº·n quyá»n truy cáº­p (VÃ­ dá»¥: Há»c sinh vÃ o trang Admin)
        options.AccessDeniedPath = "/Auth/AccessDenied";

        // KHI TÃ€I KHOáº¢N Bá»Š KHÃ“A Sáº¼ VÄ‚NG RA NGAY
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var appUserService = context.HttpContext.RequestServices.GetRequiredService<IAppUserService>();
                    var user = await appUserService.GetByIdAsync(userId);

                    // Náº¿u tÃ i khoáº£n khÃ´ng tá»“n táº¡i hoáº·c bá»‹ KhÃ³a (IsActive = false)
                    if (user == null || !user.IsActive)
                    {
                        context.RejectPrincipal();
                        await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                            context.HttpContext,
                            CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            }
        };
    });

// (Services are now registered in AddInfrastructureAndBusinessServices)

// Register Background Service
builder.Services.AddHostedService<RagChatbot.PresentationRazorPage.BackgroundJobs.DocumentProcessingJob>();
builder.Services.AddHostedService<RagChatbot.PresentationRazorPage.BackgroundJobs.ChatLogCleanupJob>();
builder.Services.AddHostedService<RagChatbot.PresentationRazorPage.BackgroundJobs.EmailBackgroundService>();

var app = builder.Build();

// Auto-migrate on startup for Docker environments
app.Services.InitializeDatabase();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Map SignalR Hub
app.MapHub<RagChatbot.PresentationRazorPage.Hubs.ChatHub>("/chatHub");
app.MapHub<RagChatbot.PresentationRazorPage.Hubs.AppNotificationHub>("/appNotificationHub");

app.Run();



