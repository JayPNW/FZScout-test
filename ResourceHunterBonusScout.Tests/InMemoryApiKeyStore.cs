using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class InMemoryApiKeyStore : IApiKeyStore
{
    public string? ApiKey { get; set; }
    public string? Read() => ApiKey;
    public void Write(string apiKey) => ApiKey = apiKey;
}
