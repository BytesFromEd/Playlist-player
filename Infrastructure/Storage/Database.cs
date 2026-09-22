using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Storage;

internal class Database : DbContext
{
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<Tool> Tools { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var outputPath = Path.Combine(AppSettings.GetInstance().AppFolder, "database.db");

        options.UseSqlite($"Data Source={outputPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Playlist>()
            .HasMany(p => p.Songs)
            .WithMany();

        modelBuilder.Entity<Playlist>().ToTable("Playlist");
        modelBuilder.Entity<Song>().ToTable("Song");
        modelBuilder.Entity<Tool>().ToTable("Tool");

        modelBuilder.Entity<Playlist>().HasKey(t => t.Id);
        modelBuilder.Entity<Song>().HasKey(t => t.Id);
        modelBuilder.Entity<Tool>().HasKey(t => t.Name);
    }
}