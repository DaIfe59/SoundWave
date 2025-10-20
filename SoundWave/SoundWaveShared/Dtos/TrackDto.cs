using System;

namespace SoundWaveShared.Dtos
{
    public class TrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string AudioFormat { get; set; } = string.Empty;
        public int Bitrate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
