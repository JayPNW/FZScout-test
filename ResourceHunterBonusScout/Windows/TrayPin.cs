using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ResourceHunterBonusScout.Windows;

/// <summary>Reads the undocumented Windows 11 values that say whether the tray icon is pinned.</summary>
static class TrayPin
{
    public static bool ShouldShowHint() => TrayIconHint.ShouldShow(ChevronVisibility(), IsPromoted());

    static int? ChevronVisibility()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");
        return key?.GetValue("SystemTrayChevronVisibility") as int?;
    }

    static int? IsPromoted()
    {
        using var settings = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings");
        if (settings is null)
        {
            return null;
        }

        foreach (var name in settings.GetSubKeyNames())
        {
            using var key = settings.OpenSubKey(name);
            if (key?.GetValue("ExecutablePath") is string path
                && string.Equals(ExpandKnownFolder(path), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
            {
                return key.GetValue("IsPromoted") as int?;
            }
        }
        return null;
    }

    /// <summary>Windows writes paths under known folders like <c>{F1B32785-6FBA-4FCF-9D55-7B8E7F157091}\…</c>.</summary>
    static string ExpandKnownFolder(string path)
    {
        var end = path.IndexOf('}');
        if (path.StartsWith('{') && end > 0 && Guid.TryParse(path[..(end + 1)], out var folderId)
            && SHGetKnownFolderPath(folderId, 0, IntPtr.Zero, out var folder) == 0)
        {
            return folder + path[(end + 1)..];
        }

        return path;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, uint flags, IntPtr token, out string path);
}
