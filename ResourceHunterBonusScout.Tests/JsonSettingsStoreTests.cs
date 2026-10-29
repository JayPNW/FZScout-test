using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), $"RHBS-{Guid.NewGuid()}");

    string SettingsFile => Path.Combine(this._folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(this._folder))
        {
            Directory.Delete(this._folder, recursive: true);
        }
    }

    [Fact]
    public void MissingFileGivesDefaults()
    {
        Assert.Equal(new StoredSettings(), new JsonSettingsStore(SettingsFile).Read());
        Assert.True(new StoredSettings().IdleTimer);
    }

    [Fact]
    public void WrittenSettingsCanBeReadBack()
    {
        new JsonSettingsStore(SettingsFile).Write(new(IdleTimer: false));

        Assert.False(new JsonSettingsStore(SettingsFile).Read().IdleTimer);
    }

    [Fact]
    public void MissingValuesGetDefaults()
    {
        Directory.CreateDirectory(this._folder);
        File.WriteAllText(SettingsFile, "{}");

        Assert.True(new JsonSettingsStore(SettingsFile).Read().IdleTimer);
    }

    [Fact]
    public void UnreadableFileGivesDefaults()
    {
        Directory.CreateDirectory(this._folder);
        File.WriteAllText(SettingsFile, "not json");

        Assert.Equal(new StoredSettings(), new JsonSettingsStore(SettingsFile).Read());
    }
}
