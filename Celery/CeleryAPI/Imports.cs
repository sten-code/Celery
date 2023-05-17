using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;

namespace Celery.CeleryAPI
{
    public class Imports
    {
        public const uint PAGE_NOACCESS = 0x1;
        public const uint PAGE_READONLY = 0x2;
        public const uint PAGE_READWRITE = 0x4;
        public const uint PAGE_WRITECOPY = 0x8;
        public const uint PAGE_EXECUTE = 0x10;
        public const uint PAGE_EXECUTE_READ = 0x20;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        public const uint PAGE_GUARD = 0x100;
        public const uint PAGE_NOCACHE = 0x200;
        public const uint PAGE_WRITECOMBINE = 0x400;

        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_DECOMMIT = 0x4000;
        public const uint MEM_RELEASE = 0x8000;

        public const uint PROCESS_WM_READ = 0x0010;
        public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;


        private const uint GENERIC_WRITE = 0x40000000;
        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 0x00000003;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint ERROR_ACCESS_DENIED = 5;

        private const uint ATTACH_PARENT = 0xFFFFFFFF;

        public const int EXCEPTION_CONTINUE_EXECUTION = -1;
        public const int EXCEPTION_CONTINUE_SEARCH = 0;

        public const uint STD_OUTPUT_HANDLE = 0xFFFFFFF5;//-11;
        public const int MY_CODE_PAGE = 437;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public uint AllocationProtect;
            public int RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }


        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public static extern int FindWindow(string sClass, string sWindow);

        [DllImport("user32.dll", EntryPoint = "ShowWindow")]
        public static extern bool ShowWindow(int hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "MessageBoxA", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int MessageBoxA(int hWnd, string sMessage, string sCaption, uint mbType);

        [DllImport("user32.dll", EntryPoint = "MessageBoxW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(int hWnd, string sMessage, string sCaption, uint mbType);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow")]
        public static extern int GetConsoleWindow();

        [DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
        public static extern int OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", EntryPoint = "WriteProcessMemory")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", EntryPoint = "VirtualProtectEx")]
        public static extern bool VirtualProtectEx(int hProcess, int lpBaseAddress, int dwSize, uint new_protect, ref uint lpOldProtect);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern int VirtualQueryEx(int hProcess, int lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", EntryPoint = "VirtualAllocEx")]
        public static extern int VirtualAllocEx(int hProcess, int lpAddress, int size, uint allocation_type, uint protect);

        [DllImport("kernel32.dll", EntryPoint = "VirtualFreeEx")]
        public static extern int VirtualFreeEx(int hProcess, int lpAddress, int size, uint allocation_type);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Auto)]
        public static extern int GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern int GetProcAddress(int hModule, string procName);

        [DllImport("kernel32.dll", EntryPoint = "GetLastError")]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        public static extern bool CloseHandle(int hObject);

        [DllImport("kernel32.dll", EntryPoint = "GetExitCodeProcess", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(int hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", EntryPoint = "CreateRemoteThread")]
        public static extern int CreateRemoteThread(int hProcess, int lpThreadAttributes, uint dwStackSize, int lpStartAddress, int lpParameter, uint dwCreationFlags, out int lpThreadId);

        [DllImport("kernel32.dll",
                EntryPoint = "GetStdHandle",
                SetLastError = true,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetStdHandle(uint nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "SetStdHandle")]
        public static extern void SetStdHandle(uint nStdHandle, uint handle);

        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll", EntryPoint = "SetConsoleTitle", CharSet = CharSet.Auto)]
        public static extern bool SetConsoleTitle(string lpConsoleTitle);

        [DllImport("kernel32.dll",
            EntryPoint = "AttachConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern uint AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll",
            EntryPoint = "CreateFileW",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern uint CreateFileW(
              string lpFileName,
              uint dwDesiredAccess,
              uint dwShareMode,
              uint lpSecurityAttributes,
              uint dwCreationDisposition,
              uint dwFlagsAndAttributes,
              uint hTemplateFile
            );

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcessId")]
        public static extern uint GetCurrentProcessId();

        [System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "FreeConsole")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", EntryPoint = "CreateFile", SetLastError = true)]
        public static extern uint CreateFile(string lpFileName, uint
        dwDesiredAccess, uint dwShareMode, uint lpSecurityAttributes, uint
        dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

        /*public static class ConsoleHelper
        {
            public static void CreateConsole()
            {
                AllocConsole();

                // stdout's handle seems to always be equal to 7
                uint defaultStdout = 7;
                uint currentStdout = GetStdHandle(STD_OUTPUT_HANDLE);

                if (currentStdout != defaultStdout)
                    // reset stdout
                    SetStdHandle(STD_OUTPUT_HANDLE, defaultStdout);

                // reopen stdout
                TextWriter writer = new StreamWriter(System.Console.OpenStandardOutput())
                { AutoFlush = true };

                System.Console.SetOut(writer);
            }
        }*/

        public static class ConsoleHelper
        {
            public static StreamWriter writer;
            public static FileStream fwriter;

            static public void Initialize(bool alwaysCreateNewConsole = true)
            {
                bool consoleAttached = true;
                if (alwaysCreateNewConsole
                    || (AttachConsole(ATTACH_PARENT) == 0
                    && Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
                {
                    consoleAttached = AllocConsole() != 0;
                }

                if (consoleAttached)
                {
                    InitializeOutStream();
                    InitializeInStream();
                }
            }

            static public void Clear()
            {
                System.Console.Write("\n\n");
            }

            private static void InitializeOutStream()
            {
                fwriter = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);
                if (fwriter != null)
                {
                    writer = new StreamWriter(fwriter) { AutoFlush = true };
                    System.Console.SetOut(writer);
                    System.Console.SetError(writer);
                }
            }

            private static void InitializeInStream()
            {
                var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);
                if (fs != null)
                {
                    System.Console.SetIn(new StreamReader(fs));
                }
            }

            private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
                                    FileAccess dotNetFileAccess)
            {
                var file = new SafeFileHandle((System.IntPtr)CreateFileW(name, win32DesiredAccess, win32ShareMode, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0), true);
                if (!file.IsInvalid)
                {
                    var fs = new FileStream(file, dotNetFileAccess);
                    return fs;
                }
                return null;
            }
        }
    }
}