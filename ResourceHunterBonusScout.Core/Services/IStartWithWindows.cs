namespace ResourceHunterBonusScout.Services;

/// <summary>The app's value under <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>.</summary>
public interface IStartWithWindows
{
    bool IsEnabled { get; set; }
}
