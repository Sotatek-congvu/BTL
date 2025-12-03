using Microsoft.Extensions.Configuration;

namespace Helper
{
    public static class HttpContextHelper
    {
        private static IConfiguration? _config;

        public static void Configure(IConfiguration config)
        {
            _config = config;
        }

        public static string? GetSecretKey()
        {
            return _config?["GEMINI_API_KEY"];
        }
    }
}
