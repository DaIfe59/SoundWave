using System.ComponentModel.DataAnnotations;

namespace SoundWaveServer.Models;

public class Track
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Artist { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Album { get; set; } = string.Empty;
    
    public int DurationSeconds { get; set; }
    
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string AudioFormat { get; set; } = "MP3";
    
    public int Bitrate { get; set; } = 320;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
}
