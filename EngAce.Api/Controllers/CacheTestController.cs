using EngAce.Api.Cached;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
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

    // POST /api/cache/set
    [HttpPost("set")]
    public async Task<IActionResult> Set([FromQuery] string key, [FromBody] JsonElement data)
    {
        string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        await _cache.SetAsync(key, jsonString, TimeSpan.FromMinutes(10));
        return Ok(new
        {
            message = "✅ JSON cached successfully",
            key,
            cachedValue = jsonString
        });
    }

    // GET /api/cache/get?key=mytest
    [HttpGet("get")]
    public async Task<IActionResult> Get([FromQuery] string key)
    {
        var json = await _cache.GetAsync(key);
        if (string.IsNullOrEmpty(json))
            return NotFound(new { message = "❌ No cache found for this key", key });

        var data = JsonSerializer.Deserialize<JsonElement>(json);
        return Ok(new
        {
            key,
            value = data
        });
    }
}
