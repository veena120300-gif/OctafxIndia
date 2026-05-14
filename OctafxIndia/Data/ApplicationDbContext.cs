// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OctafxIndia.Models;
using System;

namespace OctafxIndia.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<SiteData> SiteData { get; set; }

        // TradingAccounts DbSet for the balance box
        public DbSet<TradingAccount> TradingAccounts { get; set; }

        public DbSet<UserTradingAccount> UserTradingAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 👇 Make BirthDate use PostgreSQL "date" (no time zone)
            modelBuilder.Entity<ApplicationUser>()
                        .Property(u => u.BirthDate)
                        .HasColumnType("date");

            // Configure SiteData index (optional)
            modelBuilder.Entity<SiteData>()
                        .HasIndex(s => s.Key)
                        .IsUnique(false);

            // Configure TradingAccount for PostgreSQL
            modelBuilder.Entity<TradingAccount>(entity =>
            {
                entity.ToTable("trading_accounts"); // explicit table name

                // Map CLR property names to exact PostgreSQL column names (lowercase)
                entity.HasKey(e => e.id).HasName("pk_trading_accounts");
                entity.Property(e => e.id).HasColumnName("id");

                entity.Property(e => e.AccountNumber)
                      .HasColumnName("account_number")
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.AccountName)
                      .HasColumnName("account_name")
                      .HasMaxLength(200);

                // Numeric precision for monetary columns
                entity.Property(e => e.Balance)
                      .HasColumnName("balance")
                      .HasColumnType("numeric(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.FreeMargin)
                      .HasColumnName("free_margin")
                      .HasColumnType("numeric(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.Equity)
                      .HasColumnName("equity")
                      .HasColumnType("numeric(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.Leverage)
                      .HasColumnName("leverage")
                      .HasMaxLength(50)
                      .HasDefaultValue("1:1000");

                entity.Property(e => e.Server)
                      .HasColumnName("server")
                      .HasMaxLength(200);

                entity.Property(e => e.NoSwap)
                      .HasColumnName("no_swap")
                      .HasDefaultValue(false);

                // last_updated: default to NOW() on insert.
                entity.Property(e => e.LastUpdated)
                      .HasColumnName("last_updated")
                      .HasColumnType("timestamp")
                      .HasDefaultValueSql("NOW()")
                      .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<UserTradingAccount>(entity =>
            {
                entity.ToTable("user_trading_accounts");

                entity.HasKey(e => e.Id).HasName("pk_user_trading_accounts");
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.UserId)
                      .HasColumnName("user_id")
                      .HasMaxLength(450);

                entity.Property(e => e.AccountNumber)
                      .HasColumnName("account_number")
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.AccountType)
                      .HasColumnName("account_type")
                      .HasMaxLength(50);

                entity.Property(e => e.Server)
                      .HasColumnName("server")
                      .HasMaxLength(200);

                entity.Property(e => e.Balance)
                      .HasColumnName("balance")
                      .HasColumnType("numeric(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.Equity)
                      .HasColumnName("equity")
                      .HasColumnType("numeric(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.Status)
                      .HasColumnName("status")
                      .HasMaxLength(100);

                entity.Property(e => e.LastUpdated)
                      .HasColumnName("last_updated")
                      .HasColumnType("timestamp")
                      .HasDefaultValueSql("NOW()");
            });
        }
    }

    public class SiteData
    {
        public int Id { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class TradingAccount
    {
        // Use PascalCase property name for CLR (EF conventions), map to "id" column above
        public int id { get; set; }

        // e.g. "12345678"
        public string AccountNumber { get; set; } = null!;

        // e.g. "OctaFX-Real7"
        public string? AccountName { get; set; }

        public decimal Balance { get; set; }

        public decimal FreeMargin { get; set; }

        public decimal Equity { get; set; }

        // e.g. "1:500"
        public string? Leverage { get; set; }

        // e.g. "OctaFX-Real7"
        public string? Server { get; set; }

        public bool NoSwap { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
