using System.IO;
using System.Windows;
using System;
using Celery.Utils;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;

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
                foreach (var pinfo in Injector.getInjectedProcesses())
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
                        var ptr = pinfo.readInt32(functionPtr + 0x24);
                        var str = pinfo.readString(ptr, length);

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
                            Console.Out.WriteLine(str);
                        }
                    }
                    else if (transmitType == 2) // PRINT_CONSOLEW (wstring data)
                    {
                        int printSize = pinfo.readInt32(dataStart + 4);
                        int printPointer = pinfo.readInt32(dataStart + 8);
                        if (printSize > 0)
                        {
                            string str = pinfo.readWString(printPointer, printSize);
                            Console.Out.WriteLine(str);
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
                            Console.Out.WriteLine("Reading file path: " + filePath);

                            if (File.Exists(filePath))
                            {
                                Console.Out.WriteLine("File eixsts");
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
                            //Console.Out.WriteLine("Writing file path: " + filePath);

                            byte[] data = pinfo.readBytes(dataPointer, dataSize);
                            //var data = pinfo.readString(dataPointer, dataSize);
                            //Console.Out.WriteLine(data);

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
                            //Console.Out.WriteLine("Reading file path: " + filePath);

                            byte[] data = pinfo.readBytes(dataPointer, dataSize * 2);
                            //var data = pinfo.readWString(dataPointer, dataSize);
                            //Console.Out.WriteLine(data);

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
                            //Console.Out.WriteLine("Reading file path: " + filePath);

                            string data = pinfo.readString(dataPointer, dataSize);
                            //var data = pinfo.readString(dataPointer, dataSize);
                            //Console.Out.WriteLine(data);

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
                            //Console.Out.WriteLine("Reading file path: " + filePath);

                            byte[] bytes = pinfo.readBytes(dataPointer, dataSize * 2);
                            //var data = pinfo.readWString(dataPointer, dataSize);
                            //Console.Out.WriteLine(data);
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
                            //Console.Out.WriteLine("Reading file path: " + filePath);

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
                            //Console.Out.WriteLine("Reading file path: " + filePath);

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
                            //Console.Out.WriteLine("Reading file path: " + filePath);

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
                            //Console.Out.WriteLine("Reading path: " + dirPath);

                            if (Directory.Exists(dirPath))
                            {
                                string contents = "";

                                foreach (string filePath in System.IO.Directory.GetFiles(dirPath))
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
                            Clipboard.SetText(str);
                        }
                    }
                    else if (transmitType == 18) // wstring getclipboard
                    {
                        string contents = Clipboard.GetText();
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
        }

        public async void Inject(bool notify = true)
        {
            foreach (ProcInfo pinfo in ProcessUtil.openProcessesByName("Windows10Universal.exe"))
            {
                Debug.WriteLine("Injecting into pid: " + pinfo.processId);
                if (Injector.isInjected(pinfo) && notify)
                {
                    await MessageBoxUtils.ShowMessage("Already Attached", $"Celery is already attached to Roblox (PID: {pinfo.processId})", true, MessageBoxButtons.Ok);
                    continue;
                }

                InjectionStatus status = await Injector.injectPlayer(pinfo, notify);
                if (notify)
                {
                    switch (status)
                    {
                        case InjectionStatus.SUCCESS:
                            await MessageBoxUtils.ShowMessage("Success", "Celery injected successfully!", true, MessageBoxButtons.Ok);
                            break;
                        case InjectionStatus.ALREADY_INJECTED:
                            await MessageBoxUtils.ShowMessage("Error", "Celery is already injected.", true, MessageBoxButtons.Ok);
                            break;
                        case InjectionStatus.ALREADY_INJECTING:
                            break;
                        case InjectionStatus.FAILED:
                            await MessageBoxUtils.ShowMessage("Error", "Injection failed! Unknown error.", true, MessageBoxButtons.Ok);
                            break;
                        case InjectionStatus.FAILED_ADMINISTRATOR_ACCESS:
                            await MessageBoxUtils.ShowMessage("Admin rights", "Please run Celery as an administrator.", true, MessageBoxButtons.Ok);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public async void Execute(string script)
        {
            List<ProcInfo> procs = Injector.getInjectedProcesses();
            if (procs.Count <= 0)
            {
                await MessageBoxUtils.ShowMessage("Open Roblox", "Roblox isn't openend, make sure you using the Roblox version from the Microsoft Store.", true, MessageBoxButtons.Ok);
                return;
            }

            foreach (ProcInfo pinfo in procs)
            {
                if (!Injector.isInjected(pinfo))
                {
                    await MessageBoxUtils.ShowMessage("Not Attached", $"Celery isn't attached to Roblox (PID: {pinfo.processId})", true, MessageBoxButtons.Ok);
                    continue;
                }
                Injector.sendScript(pinfo, script);
            }
        }

    }
}
