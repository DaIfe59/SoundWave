namespace SoundWaveWPF.Models;

public class SelectedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string Status { get; set; } = "Готов к загрузке";
    public bool IsUploaded { get; set; } = false;
}
