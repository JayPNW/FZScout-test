namespace ResourceHunterBonusScout.Services;

/// <summary>Where settings other than the API key and Start with Windows live. The app keeps them in <c>settings.json</c>.</summary>
public interface ISettingsStore
{
    StoredSettings Read();
    void Write(StoredSettings settings);
}
