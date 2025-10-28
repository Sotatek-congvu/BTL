using EngAce.Api.Cached;
using Microsoft.AspNetCore.Mvc;
namespace EngAce.Api.Controllers;
[ApiController]
[Route("api/cache")]
public class CacheTestController : ControllerBase
{
    private readonly ICacheService _cache;

    public CacheTestController(ICacheService cache)
    {
        _cache = cache;
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCache()
    {
        await _cache.ClearAllAsync();
        return Ok("✅ All Redis cache cleared.");
    }
}
