using Microsoft.Win32;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Windows;

class RegistryStartWithWindows : IStartWithWindows
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "ResourceHunterBonusScout";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is not null;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value)
                key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
