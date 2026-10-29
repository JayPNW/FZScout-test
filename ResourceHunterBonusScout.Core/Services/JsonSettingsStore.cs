using System.Text.Json;

namespace ResourceHunterBonusScout.Services;

/// <summary>Keeps settings in a JSON file. A missing or unreadable file gives the defaults.</summary>
public class JsonSettingsStore : ISettingsStore
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    readonly string _path;
    StoredSettings? _cached;

    public JsonSettingsStore(string path)
    {
        this._path = path;
    }

    public StoredSettings Read() => this._cached ??= Load();

    public void Write(StoredSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this._path)!);
        File.WriteAllText(this._path, JsonSerializer.Serialize(settings, Options));
        this._cached = settings;
    }

    StoredSettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(this._path), Options) ?? new();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }
}
