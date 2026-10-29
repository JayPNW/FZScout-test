using System.ComponentModel;
using System.Runtime.InteropServices;
using ResourceHunterBonusScout.Services;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace ResourceHunterBonusScout.Windows;

/// <summary>Keeps the API key in Windows Credential Manager.</summary>
class CredentialApiKeyStore : IApiKeyStore
{
    const string Target = "ResourceHunterBonusScout/ApiKey";
    const uint CRED_TYPE_GENERIC = 1;
    const uint CRED_PERSIST_LOCAL_MACHINE = 2;

    public string? Read()
    {
        if (!CredRead(Target, CRED_TYPE_GENERIC, 0, out var credential))
            return null;
        try
        {
            var value = Marshal.PtrToStructure<CREDENTIAL>(credential);
            return Marshal.PtrToStringUni(value.CredentialBlob, (int)value.CredentialBlobSize / sizeof(char));
        }
        finally
        {
            CredFree(credential);
        }
    }

    public void Write(string apiKey)
    {
        var blob = Marshal.StringToCoTaskMemUni(apiKey);
        try
        {
            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = Target,
                CredentialBlob = blob,
                CredentialBlobSize = (uint)(apiKey.Length * sizeof(char)),
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = Environment.UserName,
            };
            if (!CredWrite(ref credential, 0))
                throw new Win32Exception();
        }
        finally
        {
            Marshal.ZeroFreeCoTaskMemUnicode(blob);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr buffer);
}
