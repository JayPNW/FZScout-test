namespace ResourceHunterBonusScout.Logs;

record EqProcess(int Id, string ExePath)
{
    public string LogsFolder => Path.Combine(Path.GetDirectoryName(ExePath)!, "Logs");
}
