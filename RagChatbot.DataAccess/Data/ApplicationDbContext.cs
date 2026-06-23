using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
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
        public DbSet<Department> Departments { get; set; }
        public DbSet<HodTerm> HodTerms { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<ContactMessage> ContactMessages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");


            // AppUser
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Subject
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Code)
                .IsUnique();

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

            // Department
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Department -> Subject
            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Department)
                .WithMany(d => d.Subjects)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);


            // AuditLog
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Actor)
                .WithMany()
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Data Seeding
            // Hash passwords using SHA256 for simplicity in DAL
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Công nghệ Thông tin", Description = "Khoa CNTT" }
            );

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, Email = "admin@gmail.com", PasswordHash = HashPassword("@Admin1"), Role = "Admin", FirstName = "Quản trị", LastName = "Hệ thống" },
                new AppUser { Id = 3, Email = "student1@gmail.com", PasswordHash = HashPassword("@Cus1"), Role = "Student", FirstName = "Học", LastName = "Sinh 1" },
                new AppUser { Id = 4, Email = "student2@gmail.com", PasswordHash = HashPassword("@Cus2"), Role = "Student", FirstName = "Học", LastName = "Sinh 2" },
                new AppUser { Id = 100, Email = "hod@gmail.com", PasswordHash = HashPassword("@Hod1"), Role = "HeadOfDepartment", FirstName = "Trưởng", LastName = "Khoa CNTT", DepartmentId = 1 }
            );

            modelBuilder.Entity<HodTerm>().HasData(
                new HodTerm { Id = 1, AppUserId = 100, DepartmentId = 1, StartAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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
