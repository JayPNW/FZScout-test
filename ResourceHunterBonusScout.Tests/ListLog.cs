using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class ListLog : IAppLog
{
    public List<string> Lines { get; } = [];
    public void LogInformation(string message) => Lines.Add(message);
}
