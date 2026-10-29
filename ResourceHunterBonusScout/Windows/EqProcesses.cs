using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ResourceHunterBonusScout.Services;

namespace ResourceHunterBonusScout.Windows;

class EqProcesses : IEqProcesses
{
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public IReadOnlyList<EqProcess> Find()
    {
        var found = new List<EqProcess>();
        foreach (var process in Process.GetProcessesByName("eqgame"))
            using (process)
                if (ExePath(process.Id) is { } path)
                    found.Add(new(process.Id, path));
        return found;
    }

    /// <summary>Works even when EQ runs elevated.</summary>
    static string? ExePath(int processId)
    {
        var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (handle == IntPtr.Zero)
            return null;
        try
        {
            var path = new StringBuilder(1024);
            var size = path.Capacity;
            return QueryFullProcessImageName(handle, 0, path, ref size) ? path.ToString(0, size) : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder exeName, ref int size);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
}
