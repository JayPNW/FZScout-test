namespace ResourceHunterBonusScout.Tests.Logs;

/// <summary>A real folder on disk for a test, deleted afterward.</summary>
sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"RHBS-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(this.Path, name);

    public void Dispose() => Directory.Delete(this.Path, recursive: true);
}
