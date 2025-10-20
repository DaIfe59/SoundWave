using System;

namespace SoundWaveShared.Dtos
{
    public class StatusUpdateDto
    {
        public string Application { get; set; } = "SoundWave";
        public string Version { get; set; } = "0.1.0";
        public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "OK";
    }
}
