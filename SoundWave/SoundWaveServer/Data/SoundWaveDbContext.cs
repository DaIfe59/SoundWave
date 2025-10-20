using Microsoft.EntityFrameworkCore;
using SoundWaveServer.Models;

namespace SoundWaveServer.Data;

public class SoundWaveDbContext : DbContext
{
    public SoundWaveDbContext(DbContextOptions<SoundWaveDbContext> options) : base(options)
    {
    }

    public DbSet<Track> Tracks { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Track entity
        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Artist).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Album).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.AudioFormat).HasMaxLength(50);
            entity.HasIndex(e => new { e.Title, e.Artist });
        });

        // Configure Playlist entity
        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // Configure PlaylistTrack entity
        modelBuilder.Entity<PlaylistTrack>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Playlist)
                  .WithMany(p => p.PlaylistTracks)
                  .HasForeignKey(e => e.PlaylistId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Track)
                  .WithMany(t => t.PlaylistTracks)
                  .HasForeignKey(e => e.TrackId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.PlaylistId, e.Order });
        });
    }
}
