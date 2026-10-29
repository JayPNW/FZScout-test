using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Tests;

class FakeNotifier : INotifier
{
    public List<string> Notifications { get; } = [];
    public void ApiKeyRejected() => Notifications.Add(nameof(ApiKeyRejected));
    public void Banned(Server server) => Notifications.Add($"{nameof(Banned)} {server}");
    public void LoggingOff() => Notifications.Add(nameof(LoggingOff));
}
