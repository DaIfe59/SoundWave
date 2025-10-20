using Microsoft.AspNetCore.Mvc;
using SoundWaveShared.Dtos;

namespace SoundWaveServer.Controllers;

[ApiController]
[Route("status")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public ActionResult<StatusUpdateDto> Get()
    {
        var dto = new StatusUpdateDto
        {
            ServerTimeUtc = DateTime.UtcNow
        };
        return Ok(dto);
    }
}


