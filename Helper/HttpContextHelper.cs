using Microsoft.AspNetCore.Http;

namespace Helper
{
    public static class HttpContextHelper
    {
        private static IHttpContextAccessor _accessor;

        public static void Configure(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        
        public static string? GetSecretKey() {
            return "AIzaSyCvrkTrEA4QrDhOkY9M_e3T_tycKAjOBdY";
        }
    }
}
