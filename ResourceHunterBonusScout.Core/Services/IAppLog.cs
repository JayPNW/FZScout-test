namespace ResourceHunterBonusScout.Services;

/// <summary>The app's own log. Never write the API key to it.</summary>
public interface IAppLog
{
    void LogInformation(string message);
    void LogWarning(string message) => LogInformation($"WARNING: {message}");
    void LogError(string message) => LogInformation($"ERROR: {message}");
}
