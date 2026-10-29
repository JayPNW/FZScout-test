using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class InMemorySettingsStore : ISettingsStore
{
    public StoredSettings Settings { get; set; } = new();
    public StoredSettings Read() => Settings;
    public void Write(StoredSettings settings) => Settings = settings;
}
