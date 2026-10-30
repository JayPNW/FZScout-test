namespace ResourceHunterBonusScout.Logs;

/// <summary>A running <c>eqgame.exe</c>.</summary>
sealed record EqProcess(int Id, string ExePath)
{
    public string LogsFolder => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.ExePath)!, "Logs");
}
