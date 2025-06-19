using DocSpider.Models;
using Microsoft.EntityFrameworkCore;

namespace DocSpider.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Documento> Documentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Documento>()
                .HasIndex(d => d.Titulo)
                .IsUnique();
        }
    }
}
