namespace ResourceHunterBonusScout.Logs;

sealed record EqProcess(int Id, string ExePath)
{
    public string LogsFolder => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.ExePath)!, "Logs");
}
