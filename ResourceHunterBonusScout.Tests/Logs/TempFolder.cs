namespace ResourceHunterBonusScout.Tests.Logs;

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
