using System.IO;
using System.Windows;
using System;
using Celery.Utils;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Celery.CeleryAPI
{
    public class CeleryAPI
    {
        public CeleryAPI()
        {
            DispatcherTimer updateTimer = new DispatcherTimer();
            updateTimer.Tick += (s, e) =>
            {
                /*
                 Handle synchronous events remotely, by listening to signals from the DLL.
                Events include: mouse api functions, file IO, clipboard IO, keyboard api functions, etc.

                This bypasses process-restriction security by using this UI
                to handle necessary windows calls

                Information is written to and read from User32.DrawIcon and User32.DrawIconEx.
                This is because, as I've found to be the case, these functions
                are never used even from the onset of the roblox app.
                It's just extra empty storage that can be easily located
                from both the UI (here) and the DLL.

                 */
                foreach (ProcInfo pinfo in Injector.getInjectedProcesses())
                {
                    int functionPtr = Imports.GetProcAddress(Imports.GetModuleHandle("USER32.dll"), "DrawIconEx");

                    // This will enable output on the UI (if enabled in the DLL).
                    // My DLL relays signals to a location each time an output
                    // message is received via LogService.
                    // This is not synchronous, but asynchronous.
                    // It may miss a few outputs, so it's not meant for
                    // high-speed printing or accurate debugging.
                    // It's just for viewing prints/warns/errors here and there
                    int type = pinfo.readInt32(functionPtr + 0x20);
                    int length = pinfo.readInt32(functionPtr + 0x28);
                    if (type == 1 || type == 2 || type == 3 || type == 4) // output type that was sent
                    {
                        int ptr = pinfo.readInt32(functionPtr + 0x24);
                        string str = pinfo.readString(ptr, length);

                        //AddOutput(str, (OutputType)(type - 1));

                        //var oldProtect2 = pinfo.setPageProtect(functionPtr, 0x40, Imports.PAGE_READWRITE);
                        pinfo.writeInt32(functionPtr + 0x20, 0);
                        //ppinfo.setPageProtect(functionPtr, 0x40, oldProtect2);
                    }

                    /*
                     Yeah, I know, this is bad, handling something that requires
                     high speed in a super slow remote-based system.
                     Well it works, until I bypass the security checks for
                     windows10universal.
                     */
                    int mouseDataStart = functionPtr + (10 * sizeof(int));
                    int mouseTransmitType = pinfo.readInt32(mouseDataStart);
                    switch (mouseTransmitType)
                    {
                        case 1:
                            MouseOperations.doMouse1Down();
                            break;
                        case 2:
                            MouseOperations.doMouse1Up();
                            break;
                        case 3:
                            MouseOperations.doMouse1Click();
                            break;
                        case 4:
                            MouseOperations.doMouse2Down();
                            break;
                        case 5:
                            MouseOperations.doMouse2Up();
                            break;
                        case 6:
                            MouseOperations.doMouse2Click();
                            break;
                        case 7:
                            MouseOperations.mouseMoveRel(pinfo.readInt32(mouseDataStart + 4), pinfo.readInt32(mouseDataStart + 8));
                            break;
                        case 8:
                            MouseOperations.mouseMoveAbs(pinfo.readInt32(mouseDataStart + 4), pinfo.readInt32(mouseDataStart + 8));
                            break;
                        case 9:
                            KeyOperations.pressKey(pinfo.readUInt32(mouseDataStart + 4));
                            break;
                        case 10:
                            KeyOperations.releaseKey(pinfo.readUInt32(mouseDataStart + 4));
                            break;
                    }
                    pinfo.writeUInt32(mouseDataStart, 0);


                    int dataStart = functionPtr + (15 * sizeof(int));
                    int transmitType = pinfo.readInt32(dataStart);

                    if (transmitType == 1) // PRINT_CONSOLE (string data)
                    {
                        int printSize = pinfo.readInt32(dataStart + 4);
                        int printPointer = pinfo.readInt32(dataStart + 8);
                        if (printSize > 0)
                        {
                            string str = pinfo.readString(printPointer, printSize);
                            Logger.Log(str);
                        }
                    }
                    else if (transmitType == 2) // PRINT_CONSOLEW (wstring data)
                    {
                        int printSize = pinfo.readInt32(dataStart + 4);
                        int printPointer = pinfo.readInt32(dataStart + 8);
                        if (printSize > 0)
                        {
                            string str = pinfo.readWString(printPointer, printSize);
                            Logger.Log(str);
                        }
                    }
                    else if (transmitType == 3) // string READ_FILE (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            if (File.Exists(filePath))
                            {
                                string contents = File.ReadAllText(filePath);
                                int contentsPointer = Imports.VirtualAllocEx(pinfo.handle, 0, contents.Length + 4, Imports.MEM_COMMIT | Imports.MEM_RESERVE, Imports.PAGE_READWRITE);
                                pinfo.writeString(contentsPointer, contents);
                                pinfo.writeInt32(dataStart + 12, contents.Length);
                                pinfo.writeInt32(dataStart + 16, contentsPointer);
                            }
                            else
                            {
                                pinfo.writeInt32(dataStart + 12, 0);
                                pinfo.writeInt32(dataStart + 16, 0);
                            }
                        }
                    }
                    else if (transmitType == 4) // wstring READ_FILE (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Reading file (Path: {filePath})");

                            if (File.Exists(filePath))
                            {
                                string contents = File.ReadAllText(filePath);
                                int contentsPointer = Imports.VirtualAllocEx(pinfo.handle, 0, (contents.Length * 2) + 4, Imports.MEM_COMMIT | Imports.MEM_RESERVE, Imports.PAGE_READWRITE);

                                pinfo.writeWString(contentsPointer, contents);
                                pinfo.writeInt32(dataStart + 12, contents.Length);
                                pinfo.writeInt32(dataStart + 16, contentsPointer);
                            }
                            else
                            {
                                pinfo.writeInt32(dataStart + 12, 0);
                                pinfo.writeInt32(dataStart + 16, 0);
                            }
                        }
                    }
                    else if (transmitType == 5) // WRITE_FILE (wstring filePath, string data)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        int dataSize = pinfo.readInt32(dataStart + 12);
                        int dataPointer = pinfo.readInt32(dataStart + 16);
                        if (filePathSize > 0 && dataSize >= 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Writing to file (Path: {filePath}");

                            byte[] data = pinfo.readBytes(dataPointer, dataSize);
                            //var data = pinfo.readString(dataPointer, dataSize);
                            //Logger.Log(data);

                            FileUtils.checkCreateFile(filePath);
                            try { File.WriteAllBytes(filePath, data); } catch { }
                        }
                    }
                    else if (transmitType == 6) // WRITE_FILEW (wstring filePath, wstring data)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        int dataSize = pinfo.readInt32(dataStart + 12);
                        int dataPointer = pinfo.readInt32(dataStart + 16);
                        if (filePathSize > 0 && dataSize >= 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Writing to file (Path: {filePath})");

                            byte[] data = pinfo.readBytes(dataPointer, dataSize * 2);
                            //var data = pinfo.readWString(dataPointer, dataSize);
                            //Logger.Log(data);

                            FileUtils.checkCreateFile(filePath);
                            try { File.WriteAllBytes(filePath, data); } catch { }
                        }
                    }
                    else if (transmitType == 7) // sendMessageBox (string)
                    {
                        int printSize = pinfo.readInt32(dataStart + 4);
                        int printPointer = pinfo.readInt32(dataStart + 8);
                        if (printSize > 0)
                        {
                            string str = pinfo.readString(printPointer, printSize);
                            Imports.MessageBoxA(Imports.FindWindow(null, "Roblox"), str, "[Celery]", 0);
                        }
                    }
                    else if (transmitType == 8) // sendMessageBoxW (wstring)
                    {
                        int printSize = pinfo.readInt32(dataStart + 4);
                        int printPointer = pinfo.readInt32(dataStart + 8);
                        if (printSize > 0)
                        {
                            string str = pinfo.readWString(printPointer, printSize);
                            Imports.MessageBoxW(Imports.FindWindow(null, "Roblox"), str, "", 0);
                        }
                    }
                    else if (transmitType == 9) // APPEND_FILE (wstring filePath, string data)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        int dataSize = pinfo.readInt32(dataStart + 12);
                        int dataPointer = pinfo.readInt32(dataStart + 16);
                        if (filePathSize > 0 && dataSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Appending to file (Path: {filePath})");

                            string data = pinfo.readString(dataPointer, dataSize);
                            //var data = pinfo.readString(dataPointer, dataSize);
                            //Logger.Log(data);

                            FileUtils.checkCreateFile(filePath);
                            try { File.AppendAllText(filePath, data); } catch { }
                        }
                    }
                    else if (transmitType == 10) // APPEND_FILEW (wstring filePath, wstring data)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        int dataSize = pinfo.readInt32(dataStart + 12);
                        int dataPointer = pinfo.readInt32(dataStart + 16);
                        if (filePathSize > 0 && dataSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Appending to file (Path: {filePath}");

                            byte[] bytes = pinfo.readBytes(dataPointer, dataSize * 2);
                            //var data = pinfo.readWString(dataPointer, dataSize);
                            //Logger.Log(data);

                            string data = "";
                            foreach (byte b in bytes)
                                data += (char)b;

                            FileUtils.checkCreateFile(filePath);
                            try { File.AppendAllText(filePath, data); } catch { }
                        }
                    }
                    else if (transmitType == 11) // CREATE_DIRECTORY (wstring filePath, wstring data)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Creating folder (Path: {filePath})");

                            try { Directory.CreateDirectory(filePath); } catch { }
                        }
                    }
                    else if (transmitType == 12) // DELFILE (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Deleting file (Path: {filePath})");

                            if (File.Exists(filePath))
                                try { File.Delete(filePath); } catch { }
                        }
                    }
                    else if (transmitType == 13) // DELFOLDER (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Deleting folder (Path: {filePath})");

                            if (Directory.Exists(filePath))
                                try { Directory.Delete(filePath); } catch { }
                        }
                    }
                    else if (transmitType == 14) // LISTFILES (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string dirPath = pinfo.readWString(filePathPointer, filePathSize);
                            Logger.Log($"Listing files (Path: {dirPath})");

                            if (Directory.Exists(dirPath))
                            {
                                string contents = "";
                                foreach (string filePath in Directory.GetFiles(dirPath))
                                {
                                    int wsStart = filePath.LastIndexOf("dll\\workspace") + 14;
                                    if (wsStart < filePath.Length)
                                    {
                                        contents += filePath.Substring(wsStart, filePath.Length - wsStart);
                                        contents += "|";
                                    }
                                }
                                contents.TrimEnd('|');

                                int contentsPointer = Imports.VirtualAllocEx(pinfo.handle, 0, (contents.Length * 2) + 4, Imports.MEM_COMMIT | Imports.MEM_RESERVE, Imports.PAGE_READWRITE);
                                pinfo.writeWString(contentsPointer, contents);
                                pinfo.writeInt32(dataStart + 12, contents.Length);
                                pinfo.writeInt32(dataStart + 16, contentsPointer);
                            }
                            else
                            {
                                pinfo.writeInt32(dataStart + 12, 0);
                                pinfo.writeInt32(dataStart + 16, 0);
                            }
                        }
                    }
                    else if (transmitType == 15) // bool isFile (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            pinfo.writeInt32(dataStart + 12, File.Exists(filePath) ? 1 : 0);
                        }
                    }
                    else if (transmitType == 16) // bool isFolder (wstring filePath)
                    {
                        int filePathSize = pinfo.readInt32(dataStart + 4);
                        int filePathPointer = pinfo.readInt32(dataStart + 8);
                        if (filePathSize > 0)
                        {
                            string filePath = pinfo.readWString(filePathPointer, filePathSize);
                            pinfo.writeInt32(dataStart + 12, Directory.Exists(filePath) ? 1 : 0);
                        }
                    }
                    else if (transmitType == 17) // setclipboard (wstring data)
                    {
                        int printSize = pinfo.readInt32(dataStart + 4);
                        int printPointer = pinfo.readInt32(dataStart + 8);
                        if (printSize > 0)
                        {
                            string str = pinfo.readWString(printPointer, printSize);
                            Logger.Log($"Setting clipboard (Data: {str})");
                            Clipboard.SetText(str);
                        }
                    }
                    else if (transmitType == 18) // wstring getclipboard
                    {
                        string contents = Clipboard.GetText();
                        Logger.Log($"Reading clipboard (Data: {contents})");

                        int contentsPointer = Imports.VirtualAllocEx(pinfo.handle, 0, (contents.Length * 2) + 4, Imports.MEM_COMMIT | Imports.MEM_RESERVE, Imports.PAGE_READWRITE);
                        pinfo.writeWString(contentsPointer, contents);
                        pinfo.writeInt32(dataStart + 4, contents.Length);
                        pinfo.writeInt32(dataStart + 8, contentsPointer);
                    }

                    if (pinfo.isOpen())
                        pinfo.writeInt32(dataStart, 0);
                }
            };
            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            updateTimer.Start();

            if (!Directory.Exists(Config.CeleryTemp))
                Directory.CreateDirectory(Config.CeleryTemp);
            File.WriteAllText(Config.CeleryDir, Config.ApplicationPath + "\\");
            File.WriteAllText(Config.CeleryHome, Path.Combine(Config.ApplicationPath, "dll") + "\\");
        }

        public async void Inject(bool notify = true)
        {
            List<ProcInfo> procs = ProcessUtil.openProcessesByName(Injector.InjectProcessName);
            if (procs.Count <= 0)
            {
                if (notify)
                    Logger.Log("Roblox isn't openend, make sure you using the Roblox version from the Microsoft Store.");
                return;
            }

            foreach (ProcInfo pinfo in procs)
            {
                if (Injector.IsInjected(pinfo))
                {
                    if (notify)
                        Logger.Log($"Already injected (PID: {pinfo.processId})");
                    continue;
                }

                Logger.Log($"Injecting... (PID: {pinfo.processId})");
                InjectionStatus status = await Injector.InjectPlayer(pinfo, notify);
                switch (status)
                {
                    case InjectionStatus.SUCCESS:
                        Logger.Log($"Injected successfully!");
                        break;
                    case InjectionStatus.ALREADY_INJECTED:
                        Logger.Log($"Celery is already injected.");
                        break;
                    case InjectionStatus.ALREADY_INJECTING:
                        break;
                    case InjectionStatus.FAILED:
                        Logger.Log($"Injection failed!");
                        break;
                    case InjectionStatus.FAILED_ADMINISTRATOR_ACCESS:
                        Logger.Log($"Admin rights required.");
                        break;
                    default:
                        break;
                }
            }
        }

        public void Execute(string script)
        {
            List<ProcInfo> procs = ProcessUtil.openProcessesByName(Injector.InjectProcessName);
            if (procs.Count <= 0)
            {
                Logger.Log("Roblox isn't openend, make sure you using the Roblox version from the Microsoft Store.");
                return;
            }

            List<ProcInfo> injectedProcs = procs.Where(p => Injector.IsInjected(p)).ToList();
            if (injectedProcs.Count <= 0)
            {
                Logger.Log("Celery not attached.");
                return;
            }

            Logger.Log($"Executing...");
            foreach (ProcInfo pinfo in injectedProcs)
            {
                Injector.SendScript(pinfo, script);
            }
        }

        public bool IsInjected()
        {
            return Injector.getInjectedProcesses().Count > 0;
        }

    }
}
