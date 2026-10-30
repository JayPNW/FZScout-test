namespace ResourceHunterBonusScout.Logs;

sealed record EqProcess(int Id, string ExePath)
{
    public string LogsFolder => Path.Combine(Path.GetDirectoryName(ExePath)!, "Logs");
}
