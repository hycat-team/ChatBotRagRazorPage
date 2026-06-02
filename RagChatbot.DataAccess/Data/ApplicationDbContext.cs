using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
using System;
using System.Text.Json;
namespace RagChatbot.DataAccess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.HasPostgresExtension("vector");


            // AppUser
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Subject
            modelBuilder.Entity<Subject>()
                .HasIndex(s => new { s.Code, s.UserId })
                .IsUnique();

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subjects)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Document -> AppUser (Uploader)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Uploader)
                .WithMany()
                .HasForeignKey(d => d.UploaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChatSession -> AppUser
            modelBuilder.Entity<ChatSession>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // DocumentChunk Embedding Mapping
            modelBuilder.Entity<DocumentChunk>()
                .Property(c => c.Embedding)
                .HasColumnType("vector(768)");

            // Data Seeding
            // Hash passwords using SHA256 for simplicity in DAL
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, Username = "admin1", PasswordHash = HashPassword("@Admin1"), Role = "Admin", FirstName = "Quản trị", LastName = "Hệ thống" },
                new AppUser { Id = 2, Username = "lecturer1", PasswordHash = HashPassword("@Lecturer1"), Role = "Lecturer", FirstName = "Nguyễn", LastName = "Giảng Viên 1" },
                new AppUser { Id = 3, Username = "cus1", PasswordHash = HashPassword("@Cus1"), Role = "Student", FirstName = "Học", LastName = "Sinh 1" },
                new AppUser { Id = 4, Username = "cus2", PasswordHash = HashPassword("@Cus2"), Role = "Student", FirstName = "Học", LastName = "Sinh 2" }
            );
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
