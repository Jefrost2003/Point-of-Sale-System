using IT15_INATO_POS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IT15_INATO_POS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariation> ProductVariations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SalesItem> SalesItems { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SecurityLog> SecurityLogs { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Archive> Archive { get; set; }
        public DbSet<ArchivedUser> ArchivedUsers { get; set; }

#pragma warning disable S927
        protected override void OnModelCreating(ModelBuilder modelBuilder)
#pragma warning restore S927
        {
            base.OnModelCreating(modelBuilder);

            // Product - ApplicationUser (Creator)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Creator)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - ProductVariation (One-to-Many)
            modelBuilder.Entity<ProductVariation>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariations)
                .HasForeignKey(pv => pv.ProductID)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction - SalesItem (One-to-Many)
            modelBuilder.Entity<SalesItem>()
                .HasOne(si => si.Transaction)
                .WithMany(t => t.SalesItems)
                .HasForeignKey(si => si.TransID)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductVariation - SalesItem (One-to-Many)
            modelBuilder.Entity<SalesItem>()
                .HasOne(si => si.ProductVariation)
                .WithMany(pv => pv.SalesItems)
                .HasForeignKey(si => si.VariationID)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser - Transaction (One-to-Many)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Cashier)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.CashierID)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser - AuditLog (One-to-Many)
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // SalesItem - ApplicationUser (Creator)
            modelBuilder.Entity<SalesItem>()
                .HasOne(si => si.Creator)
                .WithMany()
                .HasForeignKey(si => si.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Archive - ApplicationUser (ArchivedBy)
            modelBuilder.Entity<Archive>()
                .HasOne(a => a.ArchivedByUser)
                .WithMany()
                .HasForeignKey(a => a.ArchivedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ArchivedUser - ApplicationUser (ArchivedBy)
            modelBuilder.Entity<ArchivedUser>()
                .HasOne(a => a.ArchivedByUser)
                .WithMany()
                .HasForeignKey(a => a.ArchivedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.TransactionDate);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.PayMongoReference);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Timestamp);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CreatedBy);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CreatedDate);
        }
    }
}