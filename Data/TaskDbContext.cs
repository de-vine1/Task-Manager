using Microsoft.EntityFrameworkCore;
using TaskManager.Model;

namespace TaskManager.Data
{
    public class TaskDbContext : DbContext    
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
             //Link TaskItem to User
             //modelBuilder.Entity<TaskItem>()
             //    .HasOne<User>()
             //    .WithMany()
             //    .HasForeignKey(t => t.UserId)
             //    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
