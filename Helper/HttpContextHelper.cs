using Microsoft.AspNetCore.Http;

namespace Helper;

public static class HttpContextHelper
{
    private static IHttpContextAccessor _accessor;

    public static void Configure(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    
    public static string? GetSecretKey() {
        return "AIzaSyBM4971wh8XNp5FBfSvu92FYx1-w-P2pBY";
    }
}
