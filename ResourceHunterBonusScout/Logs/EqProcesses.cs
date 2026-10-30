using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ResourceHunterBonusScout.Logs;

static class EqProcesses
{
    const uint ProcessQueryLimitedInformation = 0x1000;

    public static IReadOnlyList<EqProcess> Find()
    {
        var found = new List<EqProcess>();
        foreach (var process in Process.GetProcessesByName("eqgame"))
        {
            using (process)
            {
                var path = ExePath(process.Id);
                if (path is not null)
                {
                    found.Add(new EqProcess(process.Id, path));
                }
            }
        }

        return found;
    }

    /// <summary><c>QueryFullProcessImageName</c> works even when EverQuest runs elevated, unlike <c>Process.MainModule</c>.</summary>
    static string? ExePath(int processId)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var buffer = new StringBuilder(1024);
            var size = buffer.Capacity;
            if (!QueryFullProcessImageName(handle, 0, buffer, ref size))
            {
                return null;
            }

            return buffer.ToString(0, size);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW")]
    static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder exeName, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr handle);
}
