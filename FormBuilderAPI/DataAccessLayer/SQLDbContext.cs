using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Model.SQLModel;

namespace FormBuilderAPI.DataAccessLayer
{
    public class SQLDbContext : DbContext
    {
        public SQLDbContext(DbContextOptions<SQLDbContext> options) : base(options)
        {
        }

        public DbSet<FormResponse> FormResponses { get; set; }
        public DbSet<FormResponseAnswer> FormResponseAnswers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ResponseFile> ResponseFiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FormResponse configuration
            modelBuilder.Entity<FormResponse>(entity =>
            {
                entity.HasKey(fr => fr.ResponseId);
                entity.Property(fr => fr.FormId).IsRequired();
                entity.Property(fr => fr.SubmittedBy).IsRequired();
            });

            // FormResponseAnswer configuration
            modelBuilder.Entity<FormResponseAnswer>(entity =>
            {
                entity.HasKey(a => a.AnswerId);
                entity.Property(a => a.QuestionId).IsRequired();
                entity.Property(a => a.AnswerText).HasMaxLength(2000);

                entity.HasOne(a => a.FormResponse)
                    .WithMany(fr => fr.Answers)
                    .HasForeignKey(a => a.ResponseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // User configuration   
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.Username).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired();
            });

            // ResponseFile configuration
            modelBuilder.Entity<ResponseFile>(entity =>
            {
                entity.HasKey(rf => rf.FileId);

                entity.Property(rf => rf.QuestionId).IsRequired();
                entity.Property(rf => rf.FileName).IsRequired().HasMaxLength(255);
                entity.Property(rf => rf.FileType).IsRequired().HasMaxLength(50);
                entity.Property(rf => rf.FileMaxSize).IsRequired();
                entity.Property(rf => rf.Base64Content).IsRequired();

                entity.HasOne(rf => rf.FormResponse)
                      .WithMany() // Not using navigation property to answers here
                      .HasForeignKey(rf => rf.ResponseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
