using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace UrlShortener.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<UrlRecord> Urls => Set<UrlRecord>();
        public DbSet<UnusedKey> UnusedKeys => Set<UnusedKey>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UrlRecord>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(2048);
                builder.Property(x => x.ShortCode).IsRequired().HasMaxLength(15);
                builder.HasIndex(x => x.ShortCode).IsUnique();
            });

            modelBuilder.Entity<UnusedKey>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.ShortCode).IsRequired().HasMaxLength(15);
                builder.HasIndex(x => x.ShortCode).IsUnique();
            });
        }
    }
}
