using Microsoft.Extensions.Configuration;

namespace Helper
{
    public static class WritingApiManager
    {
        private static readonly List<string> _apiKeys = new();
        private static readonly List<string> _models = new();
        private static int _keyIndex = 0;
        private static int _modelIndex = 0;

        public static void Configure(IConfiguration config)
        {
            var section = config.GetSection("GeminiWriting");
            var keys = section.GetSection("ApiKeys").Get<List<string>>() ?? new List<string>();
            var models = section.GetSection("Models").Get<List<string>>() ?? new List<string>();

            if (keys.Count == 0)
                throw new Exception("⚠️ No GeminiWriting.ApiKeys found in appsettings.json");

            _apiKeys.AddRange(keys);
            if (models.Count > 0)
                _models.AddRange(models);
        }

        public static string GetCurrentKey() => _apiKeys[_keyIndex];
        public static string GetCurrentModel() =>
            _models.Count > 0 ? _models[_modelIndex] : "Gemini_15_Flash";

        public static void RotateKey() => _keyIndex = (_keyIndex + 1) % _apiKeys.Count;
        public static void RotateModel()
        {
            if (_models.Count > 1)
                _modelIndex = (_modelIndex + 1) % _models.Count;
        }
    }
}
