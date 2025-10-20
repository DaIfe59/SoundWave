using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundWaveServer.Data;
using SoundWaveServer.Models;
using SoundWaveShared.Dtos;
using TagLib;

namespace SoundWaveServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly SoundWaveDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadController> _logger;

    public UploadController(SoundWaveDbContext context, IWebHostEnvironment environment, ILogger<UploadController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("audio")]
    public async Task<ActionResult<TrackDto>> UploadAudioFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не выбран или пустой");
            }

            // Проверяем расширение файла
            var allowedExtensions = new[] { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest($"Неподдерживаемый формат файла. Разрешены: {string.Join(", ", allowedExtensions)}");
            }

            // Проверяем размер файла (максимум 100MB)
            if (file.Length > 100 * 1024 * 1024)
            {
                return BadRequest("Файл слишком большой. Максимальный размер: 100MB");
            }

            // Создаем папку для аудиофайлов
            var audioFolder = Path.Combine(_environment.ContentRootPath, "AudioFiles");
            if (!Directory.Exists(audioFolder))
            {
                Directory.CreateDirectory(audioFolder);
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(audioFolder, fileName);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Извлекаем метаданные из аудиофайла
            var trackInfo = ExtractAudioMetadata(filePath, file.FileName);

            // Создаем запись в базе данных
            var track = new Track
            {
                Title = trackInfo.Title,
                Artist = trackInfo.Artist,
                Album = trackInfo.Album,
                DurationSeconds = trackInfo.DurationSeconds,
                FilePath = fileName, // Сохраняем только имя файла, не полный путь
                AudioFormat = fileExtension.TrimStart('.'),
                Bitrate = trackInfo.Bitrate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();

            var trackDto = new TrackDto
            {
                Id = track.Id,
                Title = track.Title,
                Artist = track.Artist,
                Album = track.Album,
                DurationSeconds = track.DurationSeconds,
                FilePath = track.FilePath,
                AudioFormat = track.AudioFormat,
                Bitrate = track.Bitrate,
                CreatedAt = track.CreatedAt,
                UpdatedAt = track.UpdatedAt
            };

            _logger.LogInformation($"Аудиофайл загружен: {file.FileName} -> {fileName}");
            return Ok(trackDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке аудиофайла");
            return StatusCode(500, "Внутренняя ошибка сервера при загрузке файла");
        }
    }

    [HttpPost("multiple")]
    public async Task<ActionResult<List<TrackDto>>> UploadMultipleAudioFiles(List<IFormFile> files)
    {
        var uploadedTracks = new List<TrackDto>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            try
            {
                var result = await UploadAudioFile(file);
                if (result.Result is OkObjectResult okResult)
                {
                    uploadedTracks.Add((TrackDto)okResult.Value!);
                }
                else
                {
                    errors.Add($"Ошибка загрузки {file.FileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке файла {file.FileName}");
                errors.Add($"Ошибка загрузки {file.FileName}: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            return BadRequest(new { 
                Message = "Некоторые файлы не удалось загрузить", 
                Errors = errors,
                UploadedTracks = uploadedTracks 
            });
        }

        return Ok(uploadedTracks);
    }

    [HttpGet("download/{fileName}")]
    public IActionResult DownloadAudioFile(string fileName)
    {
        try
        {
            var audioFolder = Path.Combine(_environment.ContentRootPath, "AudioFiles");
            var filePath = Path.Combine(audioFolder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Файл не найден");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = GetContentType(fileName);

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при скачивании файла {fileName}");
            return StatusCode(500, "Ошибка при скачивании файла");
        }
    }

    [HttpDelete("file/{fileName}")]
    public async Task<IActionResult> DeleteAudioFile(string fileName)
    {
        try
        {
            // Находим трек в базе данных
            var track = await _context.Tracks.FirstOrDefaultAsync(t => t.FilePath == fileName);
            if (track == null)
            {
                return NotFound("Трек не найден");
            }

            // Удаляем файл с диска
            var audioFolder = Path.Combine(_environment.ContentRootPath, "AudioFiles");
            var filePath = Path.Combine(audioFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Удаляем запись из базы данных
            _context.Tracks.Remove(track);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Аудиофайл удален: {fileName}");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при удалении файла {fileName}");
            return StatusCode(500, "Ошибка при удалении файла");
        }
    }

    private AudioMetadata ExtractAudioMetadata(string filePath, string originalFileName)
    {
        try
        {
            using var file = TagLib.File.Create(filePath);
            
            return new AudioMetadata
            {
                Title = !string.IsNullOrEmpty(file.Tag.Title) ? file.Tag.Title : Path.GetFileNameWithoutExtension(originalFileName),
                Artist = !string.IsNullOrEmpty(file.Tag.FirstPerformer) ? file.Tag.FirstPerformer : "Неизвестный исполнитель",
                Album = !string.IsNullOrEmpty(file.Tag.Album) ? file.Tag.Album : "Неизвестный альбом",
                DurationSeconds = (int)file.Properties.Duration.TotalSeconds,
                Bitrate = file.Properties.AudioBitrate
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Не удалось извлечь метаданные из файла {filePath}");
            
            // Возвращаем базовую информацию, если метаданные недоступны
            return new AudioMetadata
            {
                Title = Path.GetFileNameWithoutExtension(originalFileName),
                Artist = "Неизвестный исполнитель",
                Album = "Неизвестный альбом",
                DurationSeconds = 0,
                Bitrate = 0
            };
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }

    private class AudioMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public int Bitrate { get; set; }
    }
}
