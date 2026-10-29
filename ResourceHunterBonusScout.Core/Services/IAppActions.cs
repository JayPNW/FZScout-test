namespace ResourceHunterBonusScout.Services;

/// <summary>What the tray menu can ask the app to do.</summary>
public interface IAppActions
{
    void ShowSettings();
    void OpenTodaysLog();
    void Exit();
}
