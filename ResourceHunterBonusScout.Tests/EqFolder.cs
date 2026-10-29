namespace ResourceHunterBonusScout.Tests;

/// <summary>An EverQuest folder with a <c>Logs</c> folder, deleted on dispose.</summary>
sealed class EqFolder : IDisposable
{
    public EqFolder()
    {
        Logs = Path.Combine(Root, "Logs");
        Directory.CreateDirectory(Logs);
    }

    public string Root { get; } = Directory.CreateTempSubdirectory("RHBS").FullName;
    public string ExePath => Path.Combine(Root, "eqgame.exe");
    public string Logs { get; }

    public string LogPath(string character, string server) => Path.Combine(Logs, $"eqlog_{character}_{server}.txt");

    public void Append(string path, params string[] lines)
        => File.AppendAllLines(path, lines.Select(line => $"[Mon Sep 14 16:10:29 2026] {line}"));

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
