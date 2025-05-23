using Microsoft.EntityFrameworkCore;
using GroupExpenseApp.Models;

namespace GroupExpenseApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Group> Groups { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Split> Splits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>()
                .HasOne(m => m.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Split>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Split>()
                .HasOne(s => s.Transaction)
                .WithMany(t => t.Splits)
                .HasForeignKey(s => s.TransactionId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Payer)
                .WithMany()
                .HasForeignKey(t => t.PayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne<Group>()
                .WithMany(g => g.Transactions)
                .HasForeignKey("GroupId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Split>()
                .Property(s => s.Amount)
                .HasPrecision(18, 2);
        }
    }
}