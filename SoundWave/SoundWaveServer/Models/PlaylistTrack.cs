using System.ComponentModel.DataAnnotations;

namespace SoundWaveServer.Models;

public class PlaylistTrack
{
    public int Id { get; set; }
    
    public int PlaylistId { get; set; }
    
    public int TrackId { get; set; }
    
    public int Order { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Playlist Playlist { get; set; } = null!;
    
    public virtual Track Track { get; set; } = null!;
}
