using Celery.CeleryAPI;
using Celery.Utils;
using System;
using System.Collections.Generic;

namespace CeleryApp.CeleryAPI
{
    // External yara bypass I wrote last year
    public class Yara : ProcessUtil
    {
        public static int codeCaveAt;
        public static int codeCaveStart;
        public static int codeCaveEnd;

        public static void SetCave(ProcInfo pinfo, int start, int end)
        {
            codeCaveStart = start;
            codeCaveEnd = end;
            codeCaveAt = codeCaveStart;

            byte[] bytes = new byte[end - start];
            Array.Clear(bytes, 0, bytes.Length);
            pinfo.WriteBytes(codeCaveStart, bytes, bytes.Length);
        }

        public static int SilentAlloc(ProcInfo pinfo, int size, uint protect)
        {
            if (codeCaveStart == 0 || codeCaveAt == 0)
            {
                return Imports.VirtualAllocEx(pinfo.handle, 0, size, Imports.MEM_COMMIT, protect);
            }

            int at = codeCaveAt;
            pinfo.SetPageProtect(codeCaveAt, size, protect);
            codeCaveAt += size + (size % 4);
            return at;
        }

        public static List<int> GetRbxWow64Clone(ProcInfo pinfo)
        {
            List<int> results = new List<int>();
            int start = 0;

            /*
            07770000 - FF 25 06007707        - jmp dword ptr [07770006] { ->0777000A }
            07770006 - 0A 00                 - or al,[eax]
            07770008 - 77 07                 - ja 07770011
            0777000A - EA 09708877 3300      - jmp 0033:77887009
            07770011 - 00 00                 - add [eax],al
            07770013 - 00 00                 - add [eax],al
            */

            while (Imports.VirtualQueryEx(pinfo.handle, start, out Imports.MEMORY_BASIC_INFORMATION page, 0x1C) != 0)
            {
                if (page.AllocationProtect == Imports.PAGE_EXECUTE_READWRITE && page.State == Imports.MEM_COMMIT)
                {
                    if (pinfo.ReadByte(start + 0xA) == 0xEA && pinfo.ReadByte(start + 0x65) != 0xE9)
                    {
                        results.Add(start);
                        //if (results.Count == 2) break; 
                    }
                }

                start += page.RegionSize;
            }

            return results;
        }

        // "stackRef" must be a small allocated memory with READ/WRITE
        // privileges. It enables us to watch what YARA is reading by
        // writing the information there
        public static byte[] MakePayload(int stackRef)
        {
            /*
            hNtQueryVirtualMemory(
	            HANDLE                   ProcessHandle,
	            PVOID                    BaseAddress,
	            PVOID					 MemoryInformationClass,
	            PVOID                    MemoryInformation,
	            SIZE_T                   MemoryInformationLength,
	            PSIZE_T                  ReturnLength
            )
            */
            byte[] b = BitConverter.GetBytes(stackRef);
            return new byte[]
            {
                0xFF, 0xD2,
                0x56,
                0x57,
                0x53,
                0x52,
                0xBF, b[0], b[1], b[2], b[3], // mov edi,REFERENCE_STACK
                0x8B, 0x3F,
                0x8B, 0x74, 0x24, 0x14,  // HANDLE ProcessHandle
                0x89, 0x37,
                0x8B, 0x74, 0x24, 0x18,  // PVOID BaseAddress
                0x89, 0x77, 0x04,
                0x8B, 0x74, 0x24, 0x1C,  // PVOID MemoryInformationClass
                0x89, 0x77, 0x08,
                0x8B, 0x74, 0x24, 0x20,  // PVOID MemoryInformation
                0x89, 0x77, 0x0C,
                0x8B, 0x74, 0x24, 0x24,  // SIZE_T MemoryInformationLength
                0x89, 0x77, 0x10,
                0xBF, b[0], b[1], b[2], b[3], // mov edi,REFERENCE_STACK
                0x83, 0x7C, 0x24, 0x18, 0x00, // cmp dword ptr [esp+18],00
                0x77, 0x07,
                0xC7, 0x47, 0x04, 0x00, 0x00, 0x00, 0x00, // mov [edi+04],00000000
                0x8B, 0x74, 0x24, 0x20,                   // esi = (PVOID) MemoryInformation
                0x81, 0x7E, 0x10, 0x00, 0x10, 0x00, 0x00, // cmp [esi+10],00001000  // MEM_COMMIT
                0x74, 0x19,
                0x81, 0x7E, 0x10, 0x00, 0x20, 0x00, 0x00, // cmp [esi+10],00002000  // MEM_RESERVE
                0x74, 0x10,
                0x81, 0x7E, 0x10, 0x00, 0x30, 0x00, 0x00, // cmp [esi+10],00003000  // (MEM_COMMIT | MEM_RESERVE)
                0x74, 0x07,
                0x5A,
                0x5B,
                0x5F,
                0x5E,
                0xC2, 0x18, 0x00,
                0x90, 0x90, 0x90, 0x90,
                0x90, 0x90,
                0x90, 0x90, 0x90, 0x90,
                0x90, 0x90,
                0x83, 0x7E, 0x14, 0x40, // cmp dword ptr [esi+14],40 { 64 } (PAGE_EXECUTE_READWRITE)
                0x74, 0x07,
                0x5A,
                0x5B,
                0x5F,
                0x5E,
                0xC2, 0x18, 0x00,
                0x8B, 0x74, 0x24, 0x20, 
                // mov dword ptr[esi + 0xC], 0x2000000; // Modify the page size (skip the whole thing)
                0xC7, 0x46, 0x08, 0x01, 0x00, 0x00, 0x00, // mov [esi+08],00000001  // Modify the base AllocationProtect
                0xC7, 0x46, 0x10, 0x00, 0x00, 0x01, 0x00, // mov [esi+10],00010000  // Modify the page state
                0xC7, 0x46, 0x14, 0x01, 0x00, 0x00, 0x00, // mov [esi+14],00000001  // Modify the page protection
                0x8B, 0x74, 0x24, 0x18,
                0xBF, b[0], b[1], b[2], b[3], // mov edi,REFERENCE_STACK
                0x83, 0x7F, 0x04, 0x20,
                0x77, 0x0E,
                0x83, 0x47, 0x04, 0x04,
                0x8B, 0xDF,
                0x03, 0x5F, 0x04,
                0x83, 0xC3, 0x04,
                0x89, 0x33,
                0x5A,
                0x5B,
                0x5F,
                0x5E,
                0xC2, 0x18, 0x00
            };
        }

        public static bool Bypass(ProcInfo pinfo)
        {
            var caveStart = Imports.GetProcAddress(Imports.GetModuleHandle("combase.dll"), "ObjectStublessClient3");

            if (pinfo.GetPage(caveStart).Protect == Imports.PAGE_EXECUTE_READWRITE)
            {
                return true;
            }

            SetCave(pinfo, caveStart, caveStart + 0x800);

            var ntQueryVirtualMemory = Imports.GetProcAddress(Imports.GetModuleHandle("ntdll.dll"), "NtQueryVirtualMemory");
            var hookLocation1 = ntQueryVirtualMemory + 0xA;

            var fnLocation = SilentAlloc(pinfo, 0x400, Imports.PAGE_EXECUTE_READWRITE);
            var dataLocation = fnLocation + 0x300;
            var watchLocation = dataLocation + 0;

            pinfo.WriteInt32(watchLocation + 0, watchLocation - 0x20); // place to write registers for debugging
            pinfo.WriteInt32(watchLocation + 4, 0);
            //util.writeInt(dataLocation + 4, 0);// util.getSection(".vmp1").end);
            //util.writeInt(dataLocation + 8, 0x7FFFFFFF);

            var payload = MakePayload(watchLocation);
            pinfo.WriteBytes(fnLocation, payload, payload.Length);

            pinfo.PlaceJmp(hookLocation1, fnLocation);//, 5);

            foreach (var rbxWow64 in GetRbxWow64Clone(pinfo))
            {
                //MessageBox.Show("Found rbxWow64 clone: " + rbxWow64.ToString("X8"));
                var rbxQueryVirtualMemoryStart = rbxWow64 + 0x50;

                pinfo.PlaceJmp(rbxQueryVirtualMemoryStart, ntQueryVirtualMemory);
            }

            // IF YOU WANT TO WATCH WHAT YARA IS READING EVERY X SECONDS
            // UNCOMMENT THE FOLLOWING:
            /*
            watchLocation += 8; // we start reading output here
            //MessageBox.Show("WATCH location: " + watchLocation.ToString("X8"));

            while (true)
            {
                var msg = "";

                List<uint> found = new List<uint>();

                for (int i = 0; i < 0x20; i += 4)
                {
                    var spoofed = pinfo.readUInt(watchLocation + i);
                    if (spoofed == 0) break;

                    if (!found.Contains(spoofed))
                    {
                        found.Add(spoofed);
                        //yaraStarted = true;
                        msg += ("Spoofed QUERY location [" + spoofed.ToString("X8") + "]\n");
                    }
                }

                if (msg.Length > 0)
                {
                    MessageBox.Show(msg);
                }

                System.Threading.Thread.Sleep(1000);
            }
            // */

            return true;
        }
    }
}
