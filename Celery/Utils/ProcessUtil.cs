using Celery.CeleryAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Celery.Utils
{
    public class ProcessUtil
    {
        public static List<int> OpenedHandles = new List<int>();

        public static List<ProcInfo> OpenProcessesByName(string processName)
        {
            List<ProcInfo> procList = new List<ProcInfo>();
            foreach (Process process in Process.GetProcessesByName(processName.Replace(".exe", "")))
            {
                try
                {
                    if (process.Id != 0 && !process.HasExited)
                    {
                        if (process.MainModule != null)
                        {
                            if (process.MainModule.ModuleName.Length > 0)
                            {
                                int handle = Imports.OpenProcess(Imports.PROCESS_ALL_ACCESS, false, process.Id);
                                if (handle != 0)
                                {
                                    OpenedHandles.Add(handle);

                                    int baseModule = process.MainModule.BaseAddress.ToInt32();
                                    int baseModuleSize = process.MainModule.ModuleMemorySize;

                                    if (baseModule != 0 && baseModuleSize > 0)
                                    {
                                        ProcInfo procInfo = new ProcInfo
                                        {
                                            processRef = process,
                                            baseModule = baseModule,
                                            handle = handle,
                                            processId = process.Id,
                                            processName = processName,
                                            windowName = ""
                                        };
                                        procList.Add(procInfo);
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Win32Exception)
                { }
            }

            return procList;
        }

        public void Flush()
        {
            foreach (int handle in OpenedHandles)
            {
                Imports.CloseHandle(handle);
            }
        }

    }

    public class ProcInfo
    {
        public ProcInfo()
        {
            processRef = null;
            processId = 0;
            handle = 0;
        }

        public Process processRef;
        public int processId;
        public string processName;
        public string windowName;
        public int handle;
        public int baseModule;
        private int nothing;

        public bool IsOpen()
        {
            try
            {
                if (processRef == null) return false;
                if (processRef.HasExited) return false;
                if (processRef.Id == 0) return false;
                if (processRef.Handle == IntPtr.Zero) return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch
            {
                return false;
            }
            return (processId != 0 && handle != 0);
        }

        public class ProcessRegion
        {
            public int start;
            public int end;
        }

        public ProcessRegion GetSection(string name)
        {
            var start = baseModule + 0x248;
            var region = new ProcessRegion();

            while (true)
            {
                if (ReadString(start, name.Length) == name)
                {
                    region.start = ReadInt32(start + 12);
                    region.end = ReadInt32(start + 12) + ReadInt32(start + 8);

                    break;
                }

                start += 0x28;
            }

            return region;
        }

        public Imports.MEMORY_BASIC_INFORMATION GetPage(int address)
        {
            Imports.VirtualQueryEx(handle, address, out Imports.MEMORY_BASIC_INFORMATION mbi, 0x1C);
            return mbi;
        }

        public bool IsAccessible(int address)
        {
            var page = GetPage(address);
            var pr = page.Protect;
            return page.State == Imports.MEM_COMMIT && (pr == Imports.PAGE_READWRITE || pr == Imports.PAGE_READONLY || pr == Imports.PAGE_EXECUTE_READWRITE || pr == Imports.PAGE_EXECUTE_READ);
        }

        public uint SetPageProtect(int address, int size, uint protect)
        {
            uint oldProtect = 0;
            Imports.VirtualProtectEx(handle, address, size, protect, ref oldProtect);
            return oldProtect;
        }

        public bool WriteByte(int address, byte value)
        {
            byte[] bytes = new byte[sizeof(byte)];
            bytes[0] = value;
            return Imports.WriteProcessMemory(handle, address, bytes, bytes.Length, ref nothing);
        }

        public bool WriteBytes(int address, byte[] bytes, int count = -1)
        {
            return Imports.WriteProcessMemory(handle, address, bytes, (count == -1) ? bytes.Length : count, ref nothing);
        }

        public bool WriteString(int address, string str, int count = -1)
        {
            char[] chars = str.ToCharArray(0, str.Length);
            List<byte> bytes = new List<byte>();

            foreach (char b in chars)
                bytes.Add((byte)b);

            return Imports.WriteProcessMemory(handle, address, bytes.ToArray(), (count == -1) ? bytes.Count : count, ref nothing);
        }

        public bool WriteWString(int address, string str)
        {
            var at = address;
            char[] chars = str.ToCharArray(0, str.Length);
            foreach (char c in chars)
            {
                WriteUInt16(at, Convert.ToUInt16(c));
                at += 2;
            }
            return true;
        }

        public bool WriteInt16(int address, short value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(short), ref nothing);
        }

        public bool WriteUInt16(int address, ushort value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(ushort), ref nothing);
        }

        public bool WriteInt32(int address, int value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(int), ref nothing);
        }

        public bool WriteUInt32(int address, uint value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(uint), ref nothing);
        }

        public bool WriteFloat(int address, float value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(float), ref nothing);
        }

        public bool WriteDouble(int address, double value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(double), ref nothing);
        }

        public bool WriteInt64(int address, long value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(long), ref nothing);
        }

        public bool WriteUInt64(int address, ulong value)
        {
            return Imports.WriteProcessMemory(handle, address, BitConverter.GetBytes(value), sizeof(ulong), ref nothing);
        }

        public byte ReadByte(int address)
        {
            byte[] bytes = new byte[1];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(byte), ref nothing);
            return bytes[0];
        }

        public byte[] ReadBytes(int address, int count)
        {
            byte[] bytes = new byte[count];
            Imports.ReadProcessMemory(handle, address, bytes, count, ref nothing);
            return bytes;
        }

        public string ReadString(int address, int count = -1)
        {
            var result = "";
            var at = address;

            if (count == -1)
            {
                while (at != 512)
                {
                    foreach (var b in ReadBytes(at, 512))
                    {
                        if (!(b == '\n' || b == '\r' || b == '\t' || (b >= 0x20 && b <= 0x7F)))
                        {
                            at = 0;
                            break;
                        }
                        result += (char)b;
                    }

                    at += 512;
                }
            }
            else
            {
                foreach (byte c in ReadBytes(at, count))
                {
                    result += (char)c;
                }
            }

            return result;
        }

        public string ReadWString(int address, int count = -1)
        {
            string result = "";
            var at = address;

            if (count == -1)
            {
                while (at != 512)
                {
                    var bytes = ReadBytes(at, 512);
                    for (int i = 0; i < bytes.Length; i += 2)
                    {
                        char c = Convert.ToChar(BitConverter.ToUInt16(new byte[2] { bytes[0], bytes[1] }, 0));
                        if (c == 0)
                        {
                            at = 0;
                            break;
                        }
                        result += c;
                    }

                    at += 512;
                }
            }
            else
            {
                var bytes = ReadBytes(at, count * 2);

                for (int i = 0; i < bytes.Length; i += 2)
                {
                    result += Convert.ToChar(BitConverter.ToUInt16(new byte[2] { bytes[i], bytes[i + 1] }, 0));
                }
            }

            return result;
        }

        public short ReadInt16(int address)
        {
            byte[] bytes = new byte[sizeof(short)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(short), ref nothing);
            return BitConverter.ToInt16(bytes, 0);
        }

        public ushort ReadUInt16(int address)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(ushort), ref nothing);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public int ReadInt32(int address)
        {
            byte[] bytes = new byte[sizeof(int)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(int), ref nothing);
            return BitConverter.ToInt32(bytes, 0);
        }

        public uint ReadUInt32(int address)
        {
            byte[] bytes = new byte[sizeof(uint)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(uint), ref nothing);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public float ReadFloat(int address)
        {
            byte[] bytes = new byte[sizeof(float)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(float), ref nothing);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble(int address)
        {
            byte[] bytes = new byte[sizeof(double)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(double), ref nothing);
            return BitConverter.ToDouble(bytes, 0);
        }

        public long ReadInt64(int address)
        {
            byte[] bytes = new byte[sizeof(long)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(long), ref nothing);
            return BitConverter.ToInt64(bytes, 0);
        }

        public ulong ReadUInt64(int address)
        {
            byte[] bytes = new byte[sizeof(ulong)];
            Imports.ReadProcessMemory(handle, address, bytes, sizeof(ulong), ref nothing);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public void PlaceJmp(int from, int to)
        {
            int hookSize = 0;
            while (hookSize < 5)
            {
                hookSize += EyeStep.Read(handle, from + hookSize).len;
            }

            var oldProtect = SetPageProtect(from, hookSize, Imports.PAGE_EXECUTE_READWRITE);

            WriteByte(from, 0xE9);
            WriteInt32(from + 1, (to - from) - 5);
            for (int i = 5; i < hookSize; i++)
                WriteByte(from + i, 0x90);

            SetPageProtect(from, hookSize, oldProtect);
        }

        public void PlaceCall(int from, int to)
        {
            int hookSize = 0;
            while (hookSize < 5)
            {
                hookSize += EyeStep.Read(handle, from + hookSize).len;
            }

            var oldProtect = SetPageProtect(from, hookSize, Imports.PAGE_EXECUTE_READWRITE);

            WriteByte(from, 0xE8);
            WriteInt32(from + 1, (to - from) - 5);
            for (int i = 5; i < hookSize; i++)
                WriteByte(from + i, 0x90);

            SetPageProtect(from, hookSize, oldProtect);
        }

        public void PlaceTrampoline(int from, int to, int length)
        {
            PlaceJmp(from, to);
            PlaceJmp(to + length, from + 5);
        }

        public bool IsPrologue(int address)
        {
            var bytes = ReadBytes(address, 3);

            // copy of a dll function?
            if (bytes[0] == 0x8B && bytes[1] == 0xFF && bytes[2] == 0x55)
                return true;

            // now ignore misaligned functions
            if (address % 0x10 != 0)
                return false;

            if (
                // Check for different prologues, with different registers
                ((bytes[0] == 0x52 && bytes[1] == 0x8B && bytes[2] == 0xD4) // push edx + mov edx, esp
              || (bytes[0] == 0x53 && bytes[1] == 0x8B && bytes[2] == 0xDC) // push ebx + mov ebx, esp
              || (bytes[0] == 0x55 && bytes[1] == 0x8B && bytes[2] == 0xEC) // push ebp + mov ebp, esp
              || (bytes[0] == 0x56 && bytes[1] == 0x8B && bytes[2] == 0xF4) // push esi + mov esi, esp
              || (bytes[0] == 0x57 && bytes[1] == 0x8B && bytes[2] == 0xFF)) // push edi + mov edi, esp
            )
                return true;

            // is there a switch table behind this address?
            if ((ReadInt32(address - 4) > address - 0x8000 && ReadInt32(address - 4) < address)
             && (ReadInt32(address - 8) > address - 0x8000 && ReadInt32(address - 8) < address)
            )
                return true;

            return false;
        }

        public bool IsEpilogue(int address)
        {
            byte first = ReadByte(address);

            switch (first)
            {
                case 0xC9: // leave
                    return true;

                case 0xC3: // retn
                case 0xC2: // ret
                case 0xCC: // align / int 3
                    {
                        switch (ReadByte(address - 1))
                        {
                            case 0x5A: // pop edx
                            case 0x5B: // pop ebx
                            case 0x5D: // pop ebp
                            case 0x5E: // pop esi
                            case 0x5F: // pop edi
                                {
                                    if (first == 0xC2)
                                    {
                                        var r = ReadUInt16(address + 1);

                                        // double check for return value
                                        if (r % 4 == 0 && r > 0 && r < 1024)
                                        {
                                            return true;
                                        }
                                    }

                                    return true;
                                }
                        }

                        break;
                    }
            }

            return false;
        }

        private bool IsValidCode(int address)
        {
            return !(ReadUInt64(address) == 0 && ReadUInt64(address + 8) == 0);
        }

        public int GotoPrologue(int address)
        {
            int at = address;

            if (IsPrologue(at)) return at;
            while (!IsPrologue(at) && IsValidCode(address))
            {
                if ((at % 16) != 0)
                    at -= (at % 16);
                else
                    at -= 16;
            }

            return at;
        }

        public int GotoNextPrologue(int address)
        {
            int at = address;

            if (IsPrologue(at)) at += 0x10;
            while (!IsPrologue(at) && IsValidCode(at))
            {
                if ((at % 0x10) == 0)
                    at += 0x10;
                else
                    at += (at % 0x10);
            }

            return at;
        }

    }

}