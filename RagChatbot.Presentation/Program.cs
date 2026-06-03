using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Data;
using RagChatbot.Business.Services;

using RagChatbot.DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using RagChatbot.DataAccess.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.Business.Interfaces;

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
builder.Services.AddControllersWithViews();

// Setup DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                     ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => 
    {
        o.UseVector();
        o.MigrationsAssembly("RagChatbot.DataAccess");
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Register Scoped Services
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
builder.Services.AddScoped<ITextChunkingService, TextChunkingService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmailService, DummyEmailService>();

// Register Background Service
builder.Services.AddHostedService<DocumentProcessingJob>();
builder.Services.AddHostedService<ChatLogCleanupJob>();

var app = builder.Build();

// Auto-migrate on startup for Docker environments
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();



    var stuckDocs = dbContext.Documents.Where(d => d.Status == "Processing").ToList();
    if (stuckDocs.Any())
    {
        foreach (var doc in stuckDocs)
        {
            doc.Status = "Pending";
        }
        dbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR Hub
app.MapHub<RagChatbot.Presentation.Hubs.ChatHub>("/chatHub");

app.Run();
