using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
using Pgvector.EntityFrameworkCore;

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

            // Enable pgvector extension
            modelBuilder.HasPostgresExtension("vector");

            // AppUser
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Subject
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Code)
                .IsUnique();

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subjects)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // DocumentChunk
            modelBuilder.Entity<DocumentChunk>()
                .Property(c => c.Embedding)
                .HasColumnType("vector(768)"); // gemini-embedding-001 / text-embedding-004 dimensions

            modelBuilder.Entity<DocumentChunk>()
                .HasIndex(c => c.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            // Data Seeding
            // Hash passwords using SHA256 for simplicity in DAL
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, Username = "admin1", PasswordHash = HashPassword("@Admin1") },
                new AppUser { Id = 2, Username = "cus1", PasswordHash = HashPassword("@Cus1") },
                new AppUser { Id = 3, Username = "cus2", PasswordHash = HashPassword("@Cus2") }
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
