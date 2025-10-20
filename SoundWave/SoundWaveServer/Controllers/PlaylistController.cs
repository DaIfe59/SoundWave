using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundWaveServer.Data;
using SoundWaveServer.Models;
using SoundWaveShared.Dtos;

namespace SoundWaveServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaylistController : ControllerBase
{
    private readonly SoundWaveDbContext _context;

    public PlaylistController(SoundWaveDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlaylistDto>>> GetPlaylists()
    {
        var playlists = await _context.Playlists
            .Include(p => p.PlaylistTracks)
            .ThenInclude(pt => pt.Track)
            .OrderBy(p => p.Name)
            .Select(p => new PlaylistDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Tracks = p.PlaylistTracks
                    .OrderBy(pt => pt.Order)
                    .Select(pt => new TrackDto
                    {
                        Id = pt.Track.Id,
                        Title = pt.Track.Title,
                        Artist = pt.Track.Artist,
                        Album = pt.Track.Album,
                        DurationSeconds = pt.Track.DurationSeconds,
                        FilePath = pt.Track.FilePath,
                        AudioFormat = pt.Track.AudioFormat,
                        Bitrate = pt.Track.Bitrate,
                        CreatedAt = pt.Track.CreatedAt,
                        UpdatedAt = pt.Track.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(playlists);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlaylistDto>> GetPlaylist(int id)
    {
        var playlist = await _context.Playlists
            .Include(p => p.PlaylistTracks)
            .ThenInclude(pt => pt.Track)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (playlist == null)
        {
            return NotFound();
        }

        var playlistDto = new PlaylistDto
        {
            Id = playlist.Id,
            Name = playlist.Name,
            Description = playlist.Description,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt,
            Tracks = playlist.PlaylistTracks
                .OrderBy(pt => pt.Order)
                .Select(pt => new TrackDto
                {
                    Id = pt.Track.Id,
                    Title = pt.Track.Title,
                    Artist = pt.Track.Artist,
                    Album = pt.Track.Album,
                    DurationSeconds = pt.Track.DurationSeconds,
                    FilePath = pt.Track.FilePath,
                    AudioFormat = pt.Track.AudioFormat,
                    Bitrate = pt.Track.Bitrate,
                    CreatedAt = pt.Track.CreatedAt,
                    UpdatedAt = pt.Track.UpdatedAt
                })
                .ToList()
        };

        return Ok(playlistDto);
    }

    [HttpPost]
    public async Task<ActionResult<PlaylistDto>> CreatePlaylist([FromBody] PlaylistDto playlistDto)
    {
        var playlist = new Playlist
        {
            Name = playlistDto.Name,
            Description = playlistDto.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Playlists.Add(playlist);
        await _context.SaveChangesAsync();

        playlistDto.Id = playlist.Id;
        playlistDto.CreatedAt = playlist.CreatedAt;
        playlistDto.UpdatedAt = playlist.UpdatedAt;
        playlistDto.Tracks = new List<TrackDto>();

        return CreatedAtAction(nameof(GetPlaylist), new { id = playlist.Id }, playlistDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlaylist(int id, [FromBody] PlaylistDto playlistDto)
    {
        if (id != playlistDto.Id)
        {
            return BadRequest();
        }

        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist == null)
        {
            return NotFound();
        }

        playlist.Name = playlistDto.Name;
        playlist.Description = playlistDto.Description;
        playlist.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PlaylistExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlaylist(int id)
    {
        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist == null)
        {
            return NotFound();
        }

        _context.Playlists.Remove(playlist);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{playlistId}/tracks/{trackId}")]
    public async Task<IActionResult> AddTrackToPlaylist(int playlistId, int trackId)
    {
        var playlist = await _context.Playlists.FindAsync(playlistId);
        if (playlist == null)
        {
            return NotFound("Playlist not found");
        }

        var track = await _context.Tracks.FindAsync(trackId);
        if (track == null)
        {
            return NotFound("Track not found");
        }

        // Check if track is already in playlist
        var existingPlaylistTrack = await _context.PlaylistTracks
            .FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);

        if (existingPlaylistTrack != null)
        {
            return BadRequest("Track is already in playlist");
        }

        // Get the next order number
        var maxOrder = await _context.PlaylistTracks
            .Where(pt => pt.PlaylistId == playlistId)
            .MaxAsync(pt => (int?)pt.Order) ?? 0;

        var playlistTrack = new PlaylistTrack
        {
            PlaylistId = playlistId,
            TrackId = trackId,
            Order = maxOrder + 1,
            AddedAt = DateTime.UtcNow
        };

        _context.PlaylistTracks.Add(playlistTrack);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{playlistId}/tracks/{trackId}")]
    public async Task<IActionResult> RemoveTrackFromPlaylist(int playlistId, int trackId)
    {
        var playlistTrack = await _context.PlaylistTracks
            .FirstOrDefaultAsync(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId);

        if (playlistTrack == null)
        {
            return NotFound("Track not found in playlist");
        }

        _context.PlaylistTracks.Remove(playlistTrack);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool PlaylistExists(int id)
    {
        return _context.Playlists.Any(e => e.Id == id);
    }
}
