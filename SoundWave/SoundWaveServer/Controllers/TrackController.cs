using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundWaveServer.Data;
using SoundWaveServer.Models;
using SoundWaveShared.Dtos;

namespace SoundWaveServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackController : ControllerBase
{
    private readonly SoundWaveDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public TrackController(SoundWaveDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrackDto>>> GetTracks([FromQuery] string? search = null)
    {
        var query = _context.Tracks.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.Title.Contains(search) || t.Artist.Contains(search) || t.Album.Contains(search));
        }

        var tracks = await query
            .OrderBy(t => t.Title)
            .Select(t => new TrackDto
            {
                Id = t.Id,
                Title = t.Title,
                Artist = t.Artist,
                Album = t.Album,
                DurationSeconds = t.DurationSeconds,
                FilePath = t.FilePath,
                AudioFormat = t.AudioFormat,
                Bitrate = t.Bitrate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return Ok(tracks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TrackDto>> GetTrack(int id)
    {
        var track = await _context.Tracks.FindAsync(id);

        if (track == null)
        {
            return NotFound();
        }

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

        return Ok(trackDto);
    }

    [HttpPost]
    public async Task<ActionResult<TrackDto>> CreateTrack([FromBody] TrackDto trackDto)
    {
        var track = new Track
        {
            Title = trackDto.Title,
            Artist = trackDto.Artist,
            Album = trackDto.Album,
            DurationSeconds = trackDto.DurationSeconds,
            FilePath = trackDto.FilePath,
            AudioFormat = trackDto.AudioFormat,
            Bitrate = trackDto.Bitrate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();

        trackDto.Id = track.Id;
        trackDto.CreatedAt = track.CreatedAt;
        trackDto.UpdatedAt = track.UpdatedAt;

        return CreatedAtAction(nameof(GetTrack), new { id = track.Id }, trackDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrack(int id, [FromBody] TrackDto trackDto)
    {
        if (id != trackDto.Id)
        {
            return BadRequest();
        }

        var track = await _context.Tracks.FindAsync(id);
        if (track == null)
        {
            return NotFound();
        }

        track.Title = trackDto.Title;
        track.Artist = trackDto.Artist;
        track.Album = trackDto.Album;
        track.DurationSeconds = trackDto.DurationSeconds;
        track.FilePath = trackDto.FilePath;
        track.AudioFormat = trackDto.AudioFormat;
        track.Bitrate = trackDto.Bitrate;
        track.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TrackExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrack(int id)
    {
        var track = await _context.Tracks.FindAsync(id);
        if (track == null)
        {
            return NotFound();
        }

        _context.Tracks.Remove(track);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/audio")]
    public async Task<IActionResult> GetTrackAudio(int id)
    {
        var track = await _context.Tracks.FindAsync(id);
        if (track == null)
        {
            return NotFound();
        }

        var audioFolder = Path.Combine(_environment.ContentRootPath, "AudioFiles");
        var filePath = Path.Combine(audioFolder, track.FilePath);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Аудиофайл не найден на диске");
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var contentType = GetContentType(track.FilePath);

        return File(fileBytes, contentType, $"{track.Title}.{track.AudioFormat}");
    }

    private bool TrackExists(int id)
    {
        return _context.Tracks.Any(e => e.Id == id);
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
}
