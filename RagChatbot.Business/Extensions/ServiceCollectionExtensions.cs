using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.Repositories;
using RagChatbot.DataAccess.Interfaces;
using RagChatbot.Business.Interfaces;
using RagChatbot.Business.Services;
using System.Linq;
using System;

namespace RagChatbot.Business.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureAndBusinessServices(this IServiceCollection services, string connectionString)
        {
            // Setup DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, o =>
                {
                    o.UseVector();
                    o.MigrationsAssembly("RagChatbot.DataAccess");
                });
            });

            // Register Scoped Repositories
            services.AddScoped<IAppUserRepository, AppUserRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
            services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            
            // Register Scoped Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IAppUserService, AppUserService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
            services.AddScoped<ITextChunkingService, TextChunkingService>();
            services.AddScoped<IAiService, AiService>();
            services.AddScoped<IVectorSearchService, VectorSearchService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
            
            // Register Singleton Services
            services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
            services.AddSingleton<ILocalStorageService, LocalStorageService>();
            services.AddSingleton<IEmailQueue, EmailQueue>();

            return services;
        }

        public static void InitializeDatabase(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Auto-migrate on startup
            dbContext.Database.Migrate();

            // Reset stuck documents
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
    }
}
