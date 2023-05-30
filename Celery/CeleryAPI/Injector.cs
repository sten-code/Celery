using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Security.Principal;
using Celery.Utils;
using System.Threading.Tasks;

namespace Celery.CeleryAPI
{
    public class Injector : ProcessUtil
    {
        public static bool ConsoleLoaded = false;
        public static string InjectProcessName = "Windows10Universal.exe";
        public static string InjectFileName = "celeryuwp.bin";

        public static void ShowConsole()
        {
            if (!ConsoleLoaded)
            {
                ConsoleLoaded = true;
                Imports.ConsoleHelper.Initialize();
            }
            else
            {
                Imports.ShowWindow(Imports.GetConsoleWindow(), Imports.SW_SHOW);
            }
        }

        public static void HideConsole()
        {
            Imports.ConsoleHelper.Clear();
            Imports.ShowWindow(Imports.GetConsoleWindow(), Imports.SW_HIDE);
        }

        public bool FindProcess(ref ProcInfo outPInfo)
        {
            foreach (ProcInfo pinfo in OpenProcessesByName(InjectProcessName))
            {
                outPInfo = pinfo;
                return true;
            }

            return false;
        }

        public static bool IsInjected(ProcInfo pinfo)
        {
            if (pinfo.IsOpen())
                if (pinfo.ReadByte(Imports.GetProcAddress(Imports.GetModuleHandle("USER32.dll"), "DrawIcon") + 3) == 0x43)
                    return true;

            return false;
        }


        public static void SendScript(ProcInfo pinfo, string source)
        {
            if (!IsInjected(pinfo)) return;

            int functionPtr = Imports.GetProcAddress(Imports.GetModuleHandle("USER32.dll"), "DrawIcon");

            // to-do:
            // if the last script hasn't been executed yet
            // or the dll slacked on freeing memory, we will
            // do it ourselves
            /*if (sourcePtr == pinfo.readInt(functionPtr + 12))
                Imports.VirtualFreeEx(pinfo.handle, sourcePtr, 0, MEM_RELEASE);
            */

            int dontGoToInf = 0;

            while (pinfo.ReadUInt32(functionPtr + 8) > 0)
            {
                Thread.Sleep(10);
                if (dontGoToInf++ > 100) return;
            }

            if (!IsInjected(pinfo)) return;
            // VVV this check is causing the run button to act slow ??? VVV

            // Wait for a script to finish executing and old source deallocated ...
            //if (!(util.readUInt(function_ptr + 8) > 0 && util.readUInt(function_ptr + 8) < 0xA))

            //var oldProtect = pinfo.setPageProtect(functionPtr, 0x20, Imports.PAGE_READWRITE);

            int nothing = 0;
            char[] chars = source.ToCharArray(0, source.Length);
            byte[] bytesUtf8 = Encoding.UTF8.GetBytes(chars);
            int sourcePtr = Imports.VirtualAllocEx(pinfo.handle, 0, bytesUtf8.Length, Imports.MEM_RESERVE | Imports.MEM_COMMIT, Imports.PAGE_READWRITE);
            Imports.WriteProcessMemory(pinfo.handle, sourcePtr, bytesUtf8, bytesUtf8.Length, ref nothing);

            pinfo.WriteUInt32(functionPtr + 8, 1); // Type `1` = script was sent to be executed
            pinfo.WriteInt32(functionPtr + 12, sourcePtr);
            pinfo.WriteInt32(functionPtr + 16, bytesUtf8.Length);

            //pinfo.setPageProtect(functionPtr, 0x20, oldProtect);
        }

        public static List<ProcInfo> postInjectedPlayer = new List<ProcInfo>();
        private static bool isInjectingPlayer = false;
        private static bool skipAdministrative = false;

        public static async Task<InjectionStatus> InjectPlayer(ProcInfo pinfo, bool notify)
        {
            if (isInjectingPlayer)
                return InjectionStatus.ALREADY_INJECTING;

            if (IsInjected(pinfo))
                return InjectionStatus.ALREADY_INJECTED;

            if (!skipAdministrative)
            {
                AppDomain myDomain = Thread.GetDomain();
                myDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                WindowsPrincipal myPrincipal = (WindowsPrincipal)Thread.CurrentPrincipal;
                if (!myPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    if (notify)
                    {
                        MessageBoxResult result = await MessageBoxUtils.ShowMessage("Admin rights", "Celery is not running in administrative mode...There may be issues while injecting. Continue?", false, MessageBoxButtons.YesNo);
                        if (result == MessageBoxResult.No)
                        {
                            return InjectionStatus.FAILED_ADMINISTRATOR_ACCESS;
                        }
                    }
                    skipAdministrative = true;
                }
            }

            isInjectingPlayer = true;

            List<byte> bytes1 = new List<byte>();
            List<byte> bytes2 = new List<byte>();

            // Prepare our location for sending script inputs
            // 
            int functionPtr = Imports.GetProcAddress(Imports.GetModuleHandle("USER32.dll"), "DrawIcon");
            //pinfo.writeUInt32(functionPtr + 8, 0);
            //pinfo.writeInt32(functionPtr + 12, 0);
            //pinfo.writeInt32(functionPtr + 16, 0);
            //pinfo.setPageProtect(functionPtr, 0x20, oldProtect);
            var read = pinfo.ReadBytes(functionPtr + 8, 512);
            for (int i = 0; i < 512 - 2; i++)
            {
                if (read[i + 1] == 0x8B && read[i + 2] == 0xFF)
                    break;

                bytes1.Add(0);
            }
            pinfo.SetPageProtect(functionPtr, bytes1.Count + 8, Imports.PAGE_EXECUTE_READWRITE);
            pinfo.WriteBytes(functionPtr + 8, bytes1.ToArray());

            // ### comment this out if revert
            functionPtr = Imports.GetProcAddress(Imports.GetModuleHandle("USER32.dll"), "DrawIconEx");
            read = pinfo.ReadBytes(functionPtr, 512);
            for (int i = 0; i < 512 - 2; i++)
            {
                if (read[i + 1] == 0x8B && read[i + 2] == 0xFF)
                    break;

                bytes2.Add(0);
            }
            pinfo.SetPageProtect(functionPtr, bytes2.Count, Imports.PAGE_EXECUTE_READWRITE);
            pinfo.WriteBytes(functionPtr, bytes2.ToArray());
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "update.txt"))
            {
                if (File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "update.txt") == "true")
                {
                    //mapResult = MapInject.ManualMap(pinfo.processRef, "C:\\Users\\javan\\Desktop\\Celery2.0\\dll\\celery.bin");

                    return InjectionStatus.FAILED; // this is personal >:(
                }
            }

            Logger.Log("Manual mapping...", true);
            //pinfo.writeUInt32(functionPtr + (13 * sizeof(int)), homePathPointer);
            //pinfo.writeInt32(functionPtr + (14 * sizeof(int)), homePathSize);
            //pinfo.setPageProtect(functionPtr, 0x20, oldProtect);

            bool mapResult = MapInject.ManualMap(pinfo.processRef, AppDomain.CurrentDomain.BaseDirectory + "dll/" + InjectFileName);
            if (mapResult)
            {
                while (pinfo.IsOpen() && !IsInjected(pinfo))
                {
                    Thread.Sleep(10);
                }

                Logger.Log("Manual mapping done", true);
                // Easier access to processes injected, for when we run scripts and etc.
                postInjectedPlayer.Add(pinfo);

                isInjectingPlayer = false;
                //ShowConsole();

                /*
                Imports.AllocConsole();
                Imports.SetConsoleTitle("TEST");
                IntPtr stdHandle = Imports.GetStdHandle(Imports.STD_OUTPUT_HANDLE);
                SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
                FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                fileStream.Write(new byte[] { 0x66, 0x6C, 0x65, 0x65, 0x6C }, 0, 5);
                Encoding encoding = System.Text.Encoding.GetEncoding(Imports.MY_CODE_PAGE);
                StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
                standardOutput.AutoFlush = true;
                standardOutput.WriteLine("TEST");
                standardOutput.Write("LOL");
                Console.SetOut(standardOutput);
                */

                return InjectionStatus.SUCCESS;
            }
            else
            {
                isInjectingPlayer = false;

                return InjectionStatus.FAILED;
            }
        }

        public static List<ProcInfo> GetInjectedProcesses()
        {
            List<ProcInfo> results = new List<ProcInfo>();
            foreach (ProcInfo pinfo in OpenProcessesByName(InjectProcessName))
            {
                if (IsInjected(pinfo))
                {
                    results.Add(pinfo);
                }
            }
            return results;
        }
    }

    public enum InjectionStatus
    {
        FAILED,
        FAILED_ADMINISTRATOR_ACCESS,
        ALREADY_INJECTING,
        ALREADY_INJECTED,
        SUCCESS
    };
}
