using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Serilog;

namespace KoruMsSqlYedek.Service.SelfUpdate
{
    /// <summary>
    /// SYSTEM oturumundan (Session 0) aktif kullanıcının masaüstü oturumunda process başlatır.
    /// WTSQueryUserToken + CreateProcessAsUser P/Invoke pattern kullanır.
    /// Başarısız olursa explorer.exe fallback yöntemi dener.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static partial class UserSessionLauncher
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(UserSessionLauncher));

        /// <summary>
        /// Aktif konsol oturumundaki kullanıcı olarak belirtilen uygulamayı başlatır.
        /// İlk olarak CreateProcessAsUser dener, başarısız olursa explorer.exe fallback kullanır.
        /// </summary>
        public static bool LaunchInUserSession(string exePath, string arguments = null)
        {
            if (string.IsNullOrWhiteSpace(exePath))
            {
                Log.Error("LaunchInUserSession: exePath boş veya null.");
                return false;
            }

            Log.Information("LaunchInUserSession başlatılıyor. Exe: {ExePath}, Args: {Args}",
                exePath, arguments ?? "(yok)");

            // Yöntem 1: CreateProcessAsUser ile doğrudan başlat
            bool launched = TryCreateProcessAsUser(exePath, arguments);

            if (launched)
                return true;

            // Yöntem 2: explorer.exe üzerinden başlat (fallback)
            Log.Warning("CreateProcessAsUser başarısız, explorer.exe fallback deneniyor...");
            return TryLaunchViaExplorer(exePath);
        }

        /// <summary>
        /// CreateProcessAsUser ile kullanıcı oturumunda process başlatır.
        /// </summary>
        private static bool TryCreateProcessAsUser(string exePath, string arguments = null)
        {
            nint userToken = nint.Zero;
            nint duplicateToken = nint.Zero;
            nint environment = nint.Zero;
            nint cmdLinePtr = nint.Zero;
            nint desktopPtr = nint.Zero;
            nint workDirPtr = nint.Zero;

            try
            {
                // 1. Aktif konsol oturumunu bul
                uint sessionId = WTSGetActiveConsoleSessionId();

                if (sessionId == 0xFFFFFFFF)
                {
                    Log.Error("Aktif konsol oturumu bulunamadı (0xFFFFFFFF).");
                    return false;
                }

                Log.Information("Aktif konsol oturumu: {SessionId}", sessionId);

                // 2. Oturumdaki kullanıcının token'ını al
                if (!WTSQueryUserToken(sessionId, out userToken))
                {
                    int error = Marshal.GetLastWin32Error();
                    Log.Error("WTSQueryUserToken başarısız. Session: {Session}, Win32: {Error}",
                        sessionId, error);
                    return false;
                }

                // 3. Token'ı çoğalt (Primary token gerekli)
                if (!DuplicateTokenEx(userToken, MAXIMUM_ALLOWED, nint.Zero,
                        SecurityImpersonationLevel.SecurityImpersonation,
                        TokenType.TokenPrimary, out duplicateToken))
                {
                    int error = Marshal.GetLastWin32Error();
                    Log.Error("DuplicateTokenEx başarısız. Win32: {Error}", error);
                    return false;
                }

                // 4. Kullanıcı environment bloğu oluştur
                if (!CreateEnvironmentBlock(out environment, duplicateToken, false))
                {
                    Log.Warning("CreateEnvironmentBlock başarısız, ortam değişkenleri eksik olabilir.");
                    environment = nint.Zero;
                }

                // 5. STARTUPINFO hazırla — WinSta0\Default masaüstüne yönlendir
                string desktopStr = "WinSta0\\Default";
                desktopPtr = Marshal.StringToHGlobalUni(desktopStr);

                var si = new STARTUPINFO
                {
                    cb = Marshal.SizeOf<STARTUPINFO>(),
                    lpDesktop = desktopPtr,
                    dwFlags = STARTF_USESHOWWINDOW,
                    wShowWindow = SW_SHOWNORMAL
                };

                // 6. Komut satırı oluştur — writable buffer gerekli
                string cmdLine = string.IsNullOrEmpty(arguments)
                    ? $"\"{exePath}\""
                    : $"\"{exePath}\" {arguments}";
                cmdLinePtr = Marshal.StringToHGlobalUni(cmdLine);

                // 7. Çalışma dizini
                string workDir = System.IO.Path.GetDirectoryName(exePath) ?? "";
                workDirPtr = Marshal.StringToHGlobalUni(workDir);

                // 8. Process oluştur
                uint flags = CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE;

                var pi = default(PROCESS_INFORMATION);
                bool created = CreateProcessAsUserW(
                    duplicateToken,
                    nint.Zero,         // lpApplicationName — cmdLine'da belirtildi
                    cmdLinePtr,
                    nint.Zero,
                    nint.Zero,
                    false,
                    flags,
                    environment,
                    workDirPtr,
                    ref si,
                    out pi);

                if (!created)
                {
                    int error = Marshal.GetLastWin32Error();
                    Log.Error("CreateProcessAsUser başarısız. Win32: {Error}", error);
                    return false;
                }

                Log.Information("Process kullanıcı oturumunda başlatıldı. PID: {PID}", pi.dwProcessId);

                // Handle'ları kapat
                if (pi.hProcess != nint.Zero) CloseHandle(pi.hProcess);
                if (pi.hThread != nint.Zero) CloseHandle(pi.hThread);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CreateProcessAsUser ile başlatma hatası.");
                return false;
            }
            finally
            {
                if (workDirPtr != nint.Zero) Marshal.FreeHGlobal(workDirPtr);
                if (desktopPtr != nint.Zero) Marshal.FreeHGlobal(desktopPtr);
                if (cmdLinePtr != nint.Zero) Marshal.FreeHGlobal(cmdLinePtr);
                if (environment != nint.Zero) DestroyEnvironmentBlock(environment);
                if (duplicateToken != nint.Zero) CloseHandle(duplicateToken);
                if (userToken != nint.Zero) CloseHandle(userToken);
            }
        }

        /// <summary>
        /// Explorer.exe üzerinden process başlatma (fallback yöntem).
        /// </summary>
        private static bool TryLaunchViaExplorer(string exePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{exePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(psi);
                Log.Information("Explorer.exe fallback ile başlatma komutu gönderildi: {Path}", exePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Explorer.exe fallback ile başlatma başarısız: {Path}", exePath);
                return false;
            }
        }

        #region P/Invoke Declarations

        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const uint CREATE_NEW_CONSOLE = 0x00000010;
        private const uint MAXIMUM_ALLOWED = 0x02000000;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const short SW_SHOWNORMAL = 1;

        private enum SecurityImpersonationLevel
        {
            SecurityImpersonation = 2
        }

        private enum TokenType
        {
            TokenPrimary = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public nint lpReserved;
            public nint lpDesktop;
            public nint lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public nint lpReserved2;
            public nint hStdInput;
            public nint hStdOutput;
            public nint hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public nint hProcess;
            public nint hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [LibraryImport("kernel32.dll")]
        private static partial uint WTSGetActiveConsoleSessionId();

        [LibraryImport("wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool WTSQueryUserToken(uint sessionId, out nint phToken);

        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DuplicateTokenEx(
            nint hExistingToken,
            uint dwDesiredAccess,
            nint lpTokenAttributes,
            SecurityImpersonationLevel impersonationLevel,
            TokenType tokenType,
            out nint phNewToken);

        [LibraryImport("advapi32.dll", EntryPoint = "CreateProcessAsUserW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateProcessAsUserW(
            nint hToken,
            nint lpApplicationName,
            nint lpCommandLine,
            nint lpProcessAttributes,
            nint lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            nint lpEnvironment,
            nint lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [LibraryImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateEnvironmentBlock(
            out nint lpEnvironment,
            nint hToken,
            [MarshalAs(UnmanagedType.Bool)] bool bInherit);

        [LibraryImport("userenv.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DestroyEnvironmentBlock(nint lpEnvironment);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(nint hObject);

        #endregion
    }
}
