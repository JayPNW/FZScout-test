namespace ResourceHunterBonusScout.Services;

/// <summary>Where the API key lives. The app stores it in Windows Credential Manager.</summary>
public interface IApiKeyStore
{
    string? Read();
    void Write(string apiKey);
}
