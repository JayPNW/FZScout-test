namespace ResourceHunterBonusScout.Services;

/// <summary>Tray notifications.</summary>
public interface INotifier
{
    /// <summary>Clicking it opens settings.</summary>
    void ApiKeyRejected();

    void Banned(Server server);

    void LoggingOff();
}
