namespace ResourceHunterBonusScout.Services;

/// <summary>The app's own log. Never write the API key to it.</summary>
public interface IAppLog
{
    void Write(string message);
}
