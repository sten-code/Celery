using Celery.CeleryAPI;
using System;
using System.Collections.Generic;
using static Celery.Utils.OP_TYPES;

// EyeStep was written entirely by me, jayyy#5764.
// 
// EyeStep was a project focused on translating 
// x86 machine code to a readable intel syntax with the ability to
// easily expand and support x64 disassembly.
//
// Nothing in here was taken from other sources, and is based entirely
// on my own research, with references to the intel x86 manual.
// This project was finished 4 years ago, and took me 3 full days to make.
// 
// To give credits, I'd appreciate this comment stays.

// The following comment is a rant. It can be removed, if you wish:
//
// Many, and I mean many, claim that this was a rip off from the publicly acknowledged 
// hde32 micro disassembler, however that is the most dumbfounded bullcrap
// claim I've ever seen. Apart from not being a micro disassembler,
// note these 6 other reasons:
//
// 1. This is a 2,000+ line project. HDE is 200 lines I think (I would have no idea,
// because I never looked at it until after these claims)
// 
// 2. Disassemblers for x86 are going to look identical, because they both do
// THE SAME FUCKING THING, which speed in mind.
// There's no alternative way to go about it.
// 
// 3. EyeStep includes FULL text translation and details of opcodes, with a list of
// easy-to-access properties associated with every instruction,
// taken straight from the intel x86 reference manual. It is geared
// towards being the most user-friendly option, while consisting
// of only 2 files.
//
// 4. Every bit of code in this project shares no resemblance to HDE32
// in any way, whatsoever. Many have claimed that my switch case
// for the prefix flag is identical. Maybe that's cause it's a good f'in idea
// to use a switch case for the prefix flag? Just a hunch! :-)
// If your judgement is clouded by 10 lines in a switch case out of this 2000 line
// project, I think there are more issues in your personal life than there are
// limitations in the HDE32 disassembler*.
// *No offense, HDE32 is an incredible piece
// of work and a great tool, and I have no intention to talk bad about it.
// Many people should know about it, and should also acknowledge the originality
// of both these projects (both HDE32 and EyeCrawl/EyeStep)
// 
// 5. This is designed with future support for x64 in mind,
// and offers expansion and portability beyond HDE32.
// 
// 6. This is written in C#, HDE32 is written in C++.
//


namespace Celery.Utils
{
    // See http://ref.x86asm.net/coder32.html documentation
    // These are all the operand-types currently supported
    // on the x86 chip set
    public enum OP_TYPES : byte
    {
        AL,
        AH,
        AX,
        EAX,
        ECX,
        EDX,
        ESP,
        EBP,
        CL,
        CX,
        DX,
        Sreg,
        ptr16_32,
        Flags,
        EFlags,
        ES,
        CS,
        DS,
        SS,
        FS,
        GS,
        one,
        r8,
        r16,
        r16_32,
        r32,
        r64,
        r_m8,
        r_m16,
        r_m16_32,
        r_m16_m32,
        r_m32,
        moffs8,
        moffs16_32,
        m16_32_and_16_32,
        m,
        m8,
        m14_28,
        m16,
        m16_32,
        m16_int,
        m32,
        m32_int,
        m32real,
        m64,
        m64real,
        m80real,
        m80dec,
        m94_108,
        m128,
        m512,
        rel8,
        rel16,
        rel16_32,
        rel32,
        imm8,
        imm16,
        imm16_32,
        imm32,
        mm,
        mm_m64,
        xmm,
        xmm0,
        xmm_m32,
        xmm_m64,
        xmm_m128,
        STi,
        ST1,
        ST2,
        ST,
        LDTR,
        GDTR,
        IDTR,
        PMC,
        TR,
        XCR,
        MSR,
        MSW,
        CRn,
        DRn,
        CR0,
        DR0,
        DR1,
        DR2,
        DR3,
        DR4,
        DR5,
        DR6,
        DR7,
        IA32_TIMESTAMP_COUNTER,
        IA32_SYS,
        IA32_BIOS,
        NONE
    };

    public class EyeStep
    {
        public const int MOD_NOT_FIRST = 255;
        public const int N_X86_OPCODES = 919;

        public static OP_INFO[] OP_TABLE = null;

        public const uint PRE_REPNE = 0x0001;
        public const uint PRE_REPE = 0x0002;
        public const uint PRE_66 = 0x0004;
        public const uint PRE_67 = 0x0008;
        public const uint PRE_LOCK = 0x0010;
        public const uint PRE_SEG_CS = 0x0020;
        public const uint PRE_SEG_SS = 0x0040;
        public const uint PRE_SEG_DS = 0x0080;
        public const uint PRE_SEG_ES = 0x0100;
        public const uint PRE_SEG_FS = 0x0200;
        public const uint PRE_SEG_GS = 0x0400;

        public const byte OP_LOCK = 0xF0;
        public const byte OP_REPNE = 0xF2;
        public const byte OP_REPE = 0xF3;
        public const byte OP_66 = 0x66;
        public const byte OP_67 = 0x67;
        public const byte OP_SEG_CS = 0x2E;
        public const byte OP_SEG_SS = 0x36;
        public const byte OP_SEG_DS = 0x3E;
        public const byte OP_SEG_ES = 0x26;
        public const byte OP_SEG_FS = 0x64;
        public const byte OP_SEG_GS = 0x65;

        public const uint OP_NONE = 0x00000000;    // undefined or blank opcode
        public const uint OP_SINGLE = 0x00000001;    // single operand in opcode (only Source)
        public const uint OP_SRC_DEST = 0x00000002;    // two operands in opcode (Typical) (Source and Destination)
        public const uint OP_EXTENDED = 0x00000004;    // more than two operands in the opcode
        public const uint OP_IMM8 = 0x00000010;    // this operand has an 8-bit offset
        public const uint OP_IMM16 = 0x00000020;    // this operand has a 16-bit offset
        public const uint OP_IMM32 = 0x00000040;    // this operand has a 32-bit offset
        public const uint OP_DISP8 = 0x00000080;    // this operand has an 8-bit constant value
        public const uint OP_DISP16 = 0x00000100;    // this operand has a 16-bit constant value
        public const uint OP_DISP32 = 0x00000200;    // this operand has a 32-bit constant value
        public const uint OP_R8 = 0x00000400;
        public const uint OP_R16 = 0x00000800;
        public const uint OP_R32 = 0x00001000;
        public const uint OP_R64 = 0x00002000;
        public const uint OP_XMM = 0x00004000;
        public const uint OP_MM = 0x00008000;
        public const uint OP_ST = 0x00010000;
        public const uint OP_SREG = 0x00020000;
        public const uint OP_DR = 0x00040000;
        public const uint OP_CR = 0x00080000;

        public const byte R8_AL = 0;
        public const byte R8_CL = 1;
        public const byte R8_DL = 2;
        public const byte R8_BL = 3;
        public const byte R8_AH = 4;
        public const byte R8_CH = 5;
        public const byte R8_DH = 6;
        public const byte R8_BH = 7;

        public const byte R16_AX = 0;
        public const byte R16_CX = 1;
        public const byte R16_DX = 2;
        public const byte R16_BX = 3;
        public const byte R16_SP = 4;
        public const byte R16_BP = 5;
        public const byte R16_SI = 6;
        public const byte R16_DI = 7;

        public const byte R32_EAX = 0;
        public const byte R32_ECX = 1;
        public const byte R32_EDX = 2;
        public const byte R32_EBX = 3;
        public const byte R32_ESP = 4;
        public const byte R32_EBP = 5;
        public const byte R32_ESI = 6;
        public const byte R32_EDI = 7;


        public class Mnemonics
        {
            public static string[] r8_names =
            {
                "al",
                "cl",
                "dl",
                "bl",
                "ah",
                "ch",
                "dh",
                "bh"
            };

            public static string[] r16_names =
            {
                "ax",
                "cx",
                "dx",
                "bx",
                "sp",
                "bp",
                "si",
                "di"
            };

            public static string[] r32_names =
            {
                "eax",
                "ecx",
                "edx",
                "ebx",
                "esp",
                "ebp",
                "esi",
                "edi"
            };

            public static string[] r64_names =
            {
                "rax",
                "rcx",
                "rdx",
                "rbx",
                "rsp",
                "rbp",
                "rsi",
                "rdi"
            };

            public static string[] xmm_names =
            {
                "xmm0",
                "xmm1",
                "xmm2",
                "xmm3",
                "xmm4",
                "xmm5",
                "xmm6",
                "xmm7"
            };

            public static string[] mm_names =
            {
                "mm0",
                "mm1",
                "mm2",
                "mm3",
                "mm4",
                "mm5",
                "mm6",
                "mm7"
            };

            public static string[] sreg_names =
            {
                "es",
                "cs",
                "ss",
                "ds",
                "fs",
                "gs",
                "hs",
                "is"
            };

            public static string[] dr_names = // debug register
			{
                "dr0",
                "dr1",
                "dr2",
                "dr3",
                "dr4",
                "dr5",
                "dr6",
                "dr7"
            };

            public static string[] cr_names = // control register
			{
                "cr0",
                "cr1",
                "cr2",
                "cr3",
                "cr4",
                "cr5",
                "cr6",
                "cr7"
            };

            public static string[] st_names = // control register
			{
                "st(0)",
                "st(1)",
                "st(2)",
                "st(3)",
                "st(4)",
                "st(5)",
                "st(6)",
                "st(7)"
            };
        }

        public static byte[] multipliers =
        {
            0,
            2,
            4,
            8
        };

        public struct OP_INFO
        {
            public string code;
            public string opcode_name;
            public OP_TYPES[] operands;
            public string description;

            public OP_INFO(string a, string b, OP_TYPES[] c, string d)
            {
                code = a;
                opcode_name = b;
                operands = c;
                description = d;
            }
        };

        public class Operand
        {
            public Operand()
            {
                reg = new List<byte>();
                rel8 = 0;
                rel16 = 0;
                rel32 = 0;
                imm8 = 0;
                imm16 = 0;
                imm32 = 0;
                disp8 = 0;
                disp16 = 0;
                disp32 = 0;
                mul = 0;
                opmode = 0;
                flags = 0;
            }

            ~Operand()
            {
            }

            public uint flags;

            public OP_TYPES opmode;

            public List<byte> reg;

            public byte mul; // single multiplier

            public byte rel8;
            public ushort rel16;
            public uint rel32;

            public byte imm8;
            public ushort imm16;
            public uint imm32;

            public byte disp8;
            public ushort disp16;
            public uint disp32;

            public byte Append_reg(byte reg_type)
            {
                reg.Add(reg_type);
                return reg_type;
            }
        };

        public class Inst
        {
            public string data;
            public OP_INFO info;

            public uint flags;
            public int address;
            public byte[] bytes;
            public int len;
            public List<Operand> operands;

            public Inst()
            {
                bytes = new byte[16];

                // operands should contain 4 blank operands by default
                operands = new List<Operand>
                {
                    new Operand(),
                    new Operand(),
                    new Operand(),
                    new Operand()
                };

                address = 0;
                flags = 0;
                len = 0;
            }

            ~Inst()
            {
                operands.Clear();
            }

            public Operand Src()
            {
                if (operands.Count <= 0) return new Operand();
                return operands[0];
            }

            public Operand Dest()
            {
                if (operands.Count <= 1) return new Operand();
                return operands[1];
            }
        };

        public static int Getm20(byte x)
        {
            return x % 32;
        }

        public static int Getm40(byte x)
        {
            return x % 64;
        }

        public static int Finalreg(byte x)
        {
            return (x % 64) % 8;
        }

        public static int Longreg(byte x)
        {
            return (x % 64) / 8;
        }

        public static void Init()
        {
            // The reason I say EyeStep offers extended portability (you probably
            // beg to differ by looking at this) is because this can all
            // be moved to an XML or JSON file and interpreted from there.
            // It could even be fetched online.
            // Although sometimes, we just want it hardcoded.
            OP_TABLE = new OP_INFO[]
            {
                new OP_INFO( "00", "add", new OP_TYPES[]{ r_m8, r8 },                       "Add" ),
                new OP_INFO( "01", "add", new OP_TYPES[]{ r_m16_32, r16_32 },               "Add" ),
                new OP_INFO( "02", "add", new OP_TYPES[]{ r8, r_m8 },                       "Add" ),
                new OP_INFO( "03", "add", new OP_TYPES[]{ r16_32, r_m16_32 },               "Add" ),
                new OP_INFO( "04", "add", new OP_TYPES[]{ AL, imm8 },                       "Add" ),
                new OP_INFO( "05", "add", new OP_TYPES[]{ EAX, imm16_32 },                  "Add" ),
                new OP_INFO( "06", "push", new OP_TYPES[]{ ES },                            "Push Extra Segment onto the stack" ),
                new OP_INFO( "07", "pop", new OP_TYPES[]{ ES },                             "Pop Extra Segment off of the stack" ),
                new OP_INFO( "08", "or", new OP_TYPES[]{ r_m8, r8 },                        "Logical Inclusive OR" ),
                new OP_INFO( "09", "or", new OP_TYPES[]{ r_m16_32, r16_32 },                "Logical Inclusive OR" ),
                new OP_INFO( "0A", "or", new OP_TYPES[]{ r8, r_m8 },                        "Logical Inclusive OR" ),
                new OP_INFO( "0B", "or", new OP_TYPES[]{ r16_32, r_m16_32 },                "Logical Inclusive OR" ),
                new OP_INFO( "0C", "or", new OP_TYPES[]{ AL, imm8 },                        "Logical Inclusive OR" ),
                new OP_INFO( "0D", "or", new OP_TYPES[]{ EAX, imm16_32 },                   "Logical Inclusive OR" ),
                new OP_INFO( "0E", "push", new OP_TYPES[]{ CS },                            "Push Code Segment onto the stack" ),
                new OP_INFO( "0F+00+m0", "sldt", new OP_TYPES[]{ r_m16_32 },                "Store Local Descriptor Table Register" ),
                new OP_INFO( "0F+00+m1", "str", new OP_TYPES[]{ r_m16 },                    "Store Task Register" ),
                new OP_INFO( "0F+00+m2", "lldt", new OP_TYPES[]{ r_m16 },                   "Load Local Descriptor Table Register" ),
                new OP_INFO( "0F+00+m3", "ltr", new OP_TYPES[]{ r_m16 },                    "Load Task Register" ),
                new OP_INFO( "0F+00+m4", "verr", new OP_TYPES[]{ r_m16 },                   "Verify a Segment for Reading" ),
                new OP_INFO( "0F+00+m5", "verw", new OP_TYPES[]{ r_m16 },                   "Verify a Segment for Writing" ),
                new OP_INFO( "0F+01+C1", "vmcall", new OP_TYPES[]{  },                      "Call to VM Monitor" ),
                new OP_INFO( "0F+01+C2", "vmlaunch", new OP_TYPES[]{  },                    "Launch Virtual Machine" ),
                new OP_INFO( "0F+01+C3", "vmresume", new OP_TYPES[]{  },                    "Resume Virtual Machine" ),
                new OP_INFO( "0F+01+C4", "vmxoff", new OP_TYPES[]{  },                      "Leave VMX Operation" ),
                new OP_INFO( "0F+01+C8", "monitor", new OP_TYPES[]{  },                     "Set Up Monitor Address" ),
                new OP_INFO( "0F+01+C9", "mwait", new OP_TYPES[]{  },                       "Monitor Wait" ),
                new OP_INFO( "0F+01+CA", "clac", new OP_TYPES[]{  },                        "Clear AC flag in EFLAGS register" ),
                new OP_INFO( "0F+01+m0", "sgdt", new OP_TYPES[]{ r_m16_32 },                "Store Global Descriptor Table Register" ),
                new OP_INFO( "0F+01+m1", "sidt", new OP_TYPES[]{ r_m16_32 },                "Store Interrupt Descriptor Table Register" ),
                new OP_INFO( "0F+01+m2", "lgdt", new OP_TYPES[]{ r_m16_32 },                "Load Global Descriptor Table Register" ),
                new OP_INFO( "0F+01+m3", "lidt", new OP_TYPES[]{ r_m16_32 },                "Load Interrupt Descriptor Table Register" ),
                new OP_INFO( "0F+01+m4", "smsw", new OP_TYPES[]{ r_m16_32 },                "Store Machine Status Word" ),
                new OP_INFO( "0F+01+m5", "smsw", new OP_TYPES[]{ r_m16_32 },                "Store Machine Status Word" ),
                new OP_INFO( "0F+01+m6", "lmsw", new OP_TYPES[]{ r_m16_32 },                "Load Machine Status Word" ),
                new OP_INFO( "0F+01+m7", "invplg", new OP_TYPES[]{ r_m16_32 },              "Invalidate TLB Entry" ),
                new OP_INFO( "0F+02", "lar", new OP_TYPES[]{ r16_32, m16 },                 "Load Access Rights Byte" ), // potentially m8 or m16_32 ..
				new OP_INFO( "0F+03", "lsl", new OP_TYPES[]{ r16_32, m16 },                 "Load Segment Limit" ),  // potentially m8 or m16_32 ..
				new OP_INFO( "0F+04", "ud", new OP_TYPES[]{  },                             "Undefined Instruction" ),
                new OP_INFO( "0F+05", "syscall", new OP_TYPES[]{  },                        "Fast System Call" ),
                new OP_INFO( "0F+06", "clts", new OP_TYPES[]{ CR0 },                        "Clear Task-Switched Flag in CR0" ),
                new OP_INFO( "0F+07", "sysret", new OP_TYPES[]{  },                         "Return form fast system call" ),
                new OP_INFO( "0F+08", "invd", new OP_TYPES[]{  },                           "Invalidate Internal Caches" ),
                new OP_INFO( "0F+09", "wbinvd", new OP_TYPES[]{  },                         "Write Back and Invalidate Cache" ),
                new OP_INFO( "0F+0B", "ud2", new OP_TYPES[]{  },                            "Undefined Instruction" ),
                new OP_INFO( "0F+0D", "nop", new OP_TYPES[]{ r_m16_32 },                    "No Operation" ),
                new OP_INFO( "0F+10", "movups", new OP_TYPES[]{ xmm, xmm_m128 },            "Move Unaligned Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+10", "movss", new OP_TYPES[]{ xmm, xmm_m32 },           "Move Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+10", "movupd", new OP_TYPES[]{ xmm, xmm_m128 },         "Move Unaligned Packed Double-FP Value" ),
                new OP_INFO( "F2+0F+10", "movsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Move Scalar Double-FP Value" ),
                new OP_INFO( "0F+11", "movups", new OP_TYPES[]{ xmm_m128, xmm },            "Move Unaligned Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+11", "movss", new OP_TYPES[]{ xmm_m32, xmm },           "Move Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+11", "movupd", new OP_TYPES[]{ xmm_m128, xmm },         "Move Unaligned Packed Double-FP Value" ),
                new OP_INFO( "F2+0F+11", "movsd", new OP_TYPES[]{ xmm_m64, xmm },           "Move Scalar Double-FP Value" ),
                new OP_INFO( "0F+12", "movhlps", new OP_TYPES[]{ xmm, xmm },                "Move Packed Single-FP Values High to Low" ),
                new OP_INFO( "0F+12", "movlps", new OP_TYPES[]{ xmm, m64 },                 "Move Low Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+12", "movlpd", new OP_TYPES[]{ xmm, m64 },              "Move Low Packed Double-FP Value" ),
                new OP_INFO( "66+0F+12", "movddup", new OP_TYPES[]{ xmm, xmm_m64 },         "Move One Double-FP and Duplicate" ),
                new OP_INFO( "F2+0F+12", "movsldup", new OP_TYPES[]{ xmm, xmm_m64 },        "Move Packed Single-FP Low and Duplicate" ),
                new OP_INFO( "0F+13", "movlps", new OP_TYPES[]{ m64, xmm },                 "Move Low Packed Single-FP Values" ),
                new OP_INFO( "66+0F+13", "movlpd", new OP_TYPES[]{ m64, xmm },              "Move Low Packed Double-FP Value" ),
                new OP_INFO( "0F+14", "unpcklps", new OP_TYPES[]{ xmm, xmm_m64 },           "Unpack and Interleave Low Packed Single-FP Values" ),
                new OP_INFO( "66+0F+14", "unpcklpd", new OP_TYPES[]{ xmm, xmm_m128 },       "Unpack and Interleave Low Packed Double-FP Values" ),
                new OP_INFO( "0F+15", "unpckhps", new OP_TYPES[]{ xmm, xmm_m64 },           "Unpack and Interleave High Packed Single-FP Values" ),
                new OP_INFO( "66+0F+15", "unpckhpd", new OP_TYPES[]{ xmm, xmm_m128 },       "Unpack and Interleave High Packed Double-FP Values" ),
                new OP_INFO( "0F+16", "movlhps", new OP_TYPES[]{ xmm, xmm },                "Move Packed Single-FP Values Low to High" ),
                new OP_INFO( "0F+16", "movhps", new OP_TYPES[]{ xmm, m64 },                 "Move High Packed Single-FP Values" ),
                new OP_INFO( "66+0F+16", "movhpd", new OP_TYPES[]{ xmm, m64 },              "Move High Packed Double-FP Value" ),
                new OP_INFO( "F3+0F+16", "movshdup", new OP_TYPES[]{ xmm, xmm_m64 },        "Move Packed Single-FP High and Duplicate" ),
                new OP_INFO( "0F+17", "movhps", new OP_TYPES[]{ m64, xmm },                 "Move High Packed Single-FP Values" ),
                new OP_INFO( "66+0F+17", "movhpd", new OP_TYPES[]{ m64, xmm },              "Move High Packed Double-FP Value" ),
                new OP_INFO( "0F+18+m0", "prefetchnta", new OP_TYPES[]{ m8 },               "Prefetch Data Into Caches" ),
                new OP_INFO( "0F+18+m1", "prefetcht0", new OP_TYPES[]{ m8 },                "Prefetch Data Into Caches" ),
                new OP_INFO( "0F+18+m2", "prefetcht1", new OP_TYPES[]{ m8 },                "Prefetch Data Into Caches" ),
                new OP_INFO( "0F+18+m3", "prefetcht2", new OP_TYPES[]{ m8 },                "Prefetch Data Into Caches" ),
                new OP_INFO( "0F+18+m4", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+18+m5", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+18+m6", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+18+m7", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+19", "hint_nop", new OP_TYPES[]{ r_m16_32 },               "Hintable NOP" ),
                new OP_INFO( "0F+1A", "hint_nop", new OP_TYPES[]{ r_m16_32 },               "Hintable NOP" ),
                new OP_INFO( "0F+1B", "hint_nop", new OP_TYPES[]{ r_m16_32 },               "Hintable NOP" ),
                new OP_INFO( "0F+1C", "hint_nop", new OP_TYPES[]{ r_m16_32 },               "Hintable NOP" ),
                new OP_INFO( "0F+1D", "hint_nop", new OP_TYPES[]{ r_m16_32 },               "Hintable NOP" ),
                new OP_INFO( "0F+1E", "hint_nop", new OP_TYPES[]{ r_m16_32 },               "Hintable NOP" ),
                new OP_INFO( "0F+1F+m0", "nop", new OP_TYPES[]{ r_m16_32 },                 "No Operation" ),
                new OP_INFO( "0F+1F+m1", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+1F+m2", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+1F+m3", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+1F+m4", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+1F+m5", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+1F+m6", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+1F+m7", "hint_nop", new OP_TYPES[]{ r_m16_32 },            "Hintable NOP" ),
                new OP_INFO( "0F+20", "mov", new OP_TYPES[]{ r_m32, CRn },                  "Move to/from Control Registers" ),
                new OP_INFO( "0F+21", "mov", new OP_TYPES[]{ r_m32, DRn },                  "Move to/from Debug Registers" ),
                new OP_INFO( "0F+22", "mov", new OP_TYPES[]{ CRn, r_m32 },                  "Move to/from Control Registers" ),
                new OP_INFO( "0F+23", "mov", new OP_TYPES[]{ DRn, r_m32,  },                "Move to/from Debug Registers" ),
                new OP_INFO( "0F+28", "movaps", new OP_TYPES[]{ xmm, xmm_m128,  },          "Move Aligned Packed Single-FP Values" ),
                new OP_INFO( "66+0F+28", "movapd", new OP_TYPES[]{ xmm, xmm_m128,  },       "Move Aligned Packed Double-FP Values" ),
                new OP_INFO( "0F+29", "movaps", new OP_TYPES[]{ xmm_m128, xmm,  },          "Move Aligned Packed Single-FP Values" ),
                new OP_INFO( "66+0F+29", "movapd", new OP_TYPES[]{ xmm_m128, xmm,  },       "Move Aligned Packed Double-FP Values" ),
                new OP_INFO( "0F+2A", "cvtpi2ps", new OP_TYPES[]{ xmm, mm_m64 },            "Convert Packed DW Integers to Single-FP Values" ),
                new OP_INFO( "F3+0F+2A", "cvtpi2ss", new OP_TYPES[]{ xmm, r_m32 },          "Convert DW Integer to Scalar Single-FP Value" ),
                new OP_INFO( "66+0F+2A", "cvtpi2pd", new OP_TYPES[]{ xmm, mm_m64 },         "Convert Packed DW Integers to Double-FP Values" ),
                new OP_INFO( "F2+0F+2A", "cvtpi2sd", new OP_TYPES[]{ xmm, r_m32 },          "Convert DW Integer to Scalar Double-FP Value" ),
                new OP_INFO( "0F+2B", "movntps", new OP_TYPES[]{ m128, xmm },               "Store Packed Single-FP Values Using Non-Temporal Hint" ),
                new OP_INFO( "66+0F+2B", "movntpd", new OP_TYPES[]{ m128, xmm },            "Store Packed Double-FP Values Using Non-Temporal Hint" ),
                new OP_INFO( "0F+2C", "cvttps2pi", new OP_TYPES[]{ mm, xmm_m64 },           "Convert with Trunc. Packed Single-FP Values to DW Integers" ),
                new OP_INFO( "F3+0F+2C", "cvttss2si", new OP_TYPES[]{ r32, xmm_m32 },       "Convert with Trunc. Scalar Single-FP Value to DW Integer" ),
                new OP_INFO( "66+0F+2C", "cvttpd2pi", new OP_TYPES[]{ mm, xmm_m128 },       "Convert with Trunc. Packed Double-FP Values to DW Integers" ),
                new OP_INFO( "F2+0F+2C", "cvttsd2si", new OP_TYPES[]{ r32, xmm_m64 },       "Convert with Trunc. Scalar Double-FP Value to Signed DW Int" ),
                new OP_INFO( "0F+2D", "cvtps2pi", new OP_TYPES[]{ mm, xmm_m64 },            "Convert Packed Single-FP Values to DW Integers" ),
                new OP_INFO( "F3+0F+2D", "cvtss2si", new OP_TYPES[]{ r32, xmm_m32 },        "Convert Scalar Single-FP Value to DW Integer" ),
                new OP_INFO( "66+0F+2D", "cvtpd2pi", new OP_TYPES[]{ mm, xmm_m128 },        "Convert Packed Double-FP Values to DW Integers" ),
                new OP_INFO( "F2+0F+2D", "cvtsd2si", new OP_TYPES[]{ r32, xmm_m64 },        "Convert Scalar Double-FP Value to DW Integer" ),
                new OP_INFO( "0F+2E", "ucomiss", new OP_TYPES[]{ xmm, xmm_m32 },            "Unordered Compare Scalar Ordered Single-FP Values and Set EFLAGS" ),
                new OP_INFO( "66+0F+2E", "ucomisd", new OP_TYPES[]{ xmm, xmm_m64 },         "Unordered Compare Scalar Ordered Double-FP Values and Set EFLAGS" ),
                new OP_INFO( "0F+2F", "comiss", new OP_TYPES[]{ xmm, xmm_m32 },             "Compare Scalar Ordered Single-FP Values and Set EFLAGS" ),
                new OP_INFO( "66+0F+2F", "comisd", new OP_TYPES[]{ xmm, xmm_m64 },          "Compare Scalar Ordered Double-FP Values and Set EFLAGS" ),
                new OP_INFO( "0F+30", "wrmsr", new OP_TYPES[]{  },                          "Write to Model Specific Register" ),
                new OP_INFO( "0F+31", "rdtsc", new OP_TYPES[]{  },                          "Read Time-Stamp Counter" ),
                new OP_INFO( "0F+32", "rdmsr", new OP_TYPES[]{  },                          "Read from Model Specific Register" ),
                new OP_INFO( "0F+33", "rdpmc", new OP_TYPES[]{  },                          "Read Performance-Monitoring Counters" ),
                new OP_INFO( "0F+34", "sysenter", new OP_TYPES[]{  },                       "Fast System Call" ),
                new OP_INFO( "0F+35", "sysexit", new OP_TYPES[]{  },                        "Fast Return from Fast System Call" ),
                new OP_INFO( "0F+37", "getsec", new OP_TYPES[]{  },                         "GETSEC Leaf Functions" ),
                new OP_INFO( "0F+38+00", "pshufb", new OP_TYPES[]{ mm, mm_m64 },            "Packed Shuffle Bytes" ),
                new OP_INFO( "66+0F+38+00", "pshufb", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed Shuffle Bytes" ),
                new OP_INFO( "0F+38+01", "phaddw", new OP_TYPES[]{ mm, mm_m64 },            "Packed Horizontal Add" ),
                new OP_INFO( "66+0F+38+01", "phaddw", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed Horizontal Add" ),
                new OP_INFO( "0F+38+02", "phaddd", new OP_TYPES[]{ mm, mm_m64 },            "Packed Horizontal Add" ),
                new OP_INFO( "66+0F+38+02", "phaddd", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed Horizontal Add" ),
                new OP_INFO( "0F+38+03", "phaddsw", new OP_TYPES[]{ mm, mm_m64 },           "Packed Horizontal Add and Saturate" ),
                new OP_INFO( "66+0F+38+03", "phaddsw", new OP_TYPES[]{ xmm, xmm_m128 },     "Packed Horizontal Add and Saturate" ),
                new OP_INFO( "0F+38+04", "pmaddubsw", new OP_TYPES[]{ mm, mm_m64 },         "Multiply and Add Packed Signed and Unsigned Bytes" ),
                new OP_INFO( "66+0F+38+04", "pmaddubsw", new OP_TYPES[]{ xmm, xmm_m128 },   "Multiply and Add Packed Signed and Unsigned Bytes" ),
                new OP_INFO( "0F+38+05", "phsubw", new OP_TYPES[]{ mm, mm_m64 },            "Packed Horizontal Subtract" ),
                new OP_INFO( "66+0F+38+05", "phsubw", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed Horizontal Subtract" ),
                new OP_INFO( "0F+38+06", "phsubd", new OP_TYPES[]{ mm, mm_m64 },            "Packed Horizontal Subtract" ),
                new OP_INFO( "66+0F+38+06", "phsubd", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed Horizontal Subtract" ),
                new OP_INFO( "0F+38+07", "phsubsw", new OP_TYPES[]{ mm, mm_m64 },           "Packed Horizontal Subtract and Saturate" ),
                new OP_INFO( "66+0F+38+07", "phsubsw", new OP_TYPES[]{ xmm, xmm_m128 },     "Packed Horizontal Subtract and Saturate" ),
                new OP_INFO( "0F+38+08", "psignb", new OP_TYPES[]{ mm, mm_m64 },            "Packed SIGN" ),
                new OP_INFO( "66+0F+38+08", "psignb", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed SIGN" ),
                new OP_INFO( "0F+38+09", "psignw", new OP_TYPES[]{ mm, mm_m64 },            "Packed SIGN" ),
                new OP_INFO( "66+0F+38+09", "psignw", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed SIGN" ),
                new OP_INFO( "0F+38+0A", "psignd", new OP_TYPES[]{ mm, mm_m64 },            "Packed SIGN" ),
                new OP_INFO( "66+0F+38+0A", "psignd", new OP_TYPES[]{ xmm, xmm_m128 },      "Packed SIGN" ),
                new OP_INFO( "0F+38+0B", "pmulhrsw", new OP_TYPES[]{ mm, mm_m64 },          "Packed Multiply High with Round and Scale" ),
                new OP_INFO( "66+0F+38+0B", "pmulhrsw", new OP_TYPES[]{ xmm, xmm_m128 },    "Packed Multiply High with Round and Scale" ),
                new OP_INFO( "66+0F+38+10", "pblendvb", new OP_TYPES[]{ xmm, xmm_m128,xmm0},"Variable Blend Packed Bytes" ),
                new OP_INFO( "66+0F+38+14", "blendvps", new OP_TYPES[]{ xmm, xmm_m128,xmm0},"Variable Blend Packed Single-FP Values" ),
                new OP_INFO( "66+0F+38+15", "blendvpd", new OP_TYPES[]{ xmm, xmm_m128,xmm0},"Variable Blend Packed Double-FP Values" ),
                new OP_INFO( "66+0F+38+17", "ptest", new OP_TYPES[]{ xmm, xmm_m128 },       "Logical Compare" ),
                new OP_INFO( "0F+38+1C", "pabsb", new OP_TYPES[]{ mm, mm_m64 },             "Packed Absolute Value" ),
                new OP_INFO( "66+0F+38+1C", "pabsb", new OP_TYPES[]{ xmm, xmm_m128 },       "Packed Absolute Value" ),
                new OP_INFO( "0F+38+1D", "pabsw", new OP_TYPES[]{ mm, mm_m64 },             "Packed Absolute Value" ),
                new OP_INFO( "66+0F+38+1D", "pabsw", new OP_TYPES[]{ xmm, xmm_m128 },       "Packed Absolute Value" ),
                new OP_INFO( "0F+38+1E", "pabsd", new OP_TYPES[]{ mm, mm_m64 },             "Packed Absolute Value" ),
                new OP_INFO( "66+0F+38+1E", "pabsd", new OP_TYPES[]{ xmm, xmm_m128 },       "Packed Absolute Value" ),
                new OP_INFO( "66+0F+38+20", "pmovsxbw", new OP_TYPES[]{ xmm, m64 },         "Packed Move with Sign Extend" ),
                new OP_INFO( "66+0F+38+21", "pmovsxbd", new OP_TYPES[]{ xmm, m32 },         "Packed Move with Sign Extend" ),
                new OP_INFO( "66+0F+38+22", "pmovsxbq", new OP_TYPES[]{ xmm, m16 },         "Packed Move with Sign Extend" ),
                new OP_INFO( "66+0F+38+23", "pmovsxbd", new OP_TYPES[]{ xmm, m64 },         "Packed Move with Sign Extend" ),
                new OP_INFO( "66+0F+38+24", "pmovsxbq", new OP_TYPES[]{ xmm, m32 },         "Packed Move with Sign Extend" ),
                new OP_INFO( "66+0F+38+25", "pmovsxdq", new OP_TYPES[]{ xmm, m64 },         "Packed Move with Sign Extend" ),
                new OP_INFO( "66+0F+38+28", "pmuldq", new OP_TYPES[]{ xmm, xmm_m128 },      "Multiply Packed Signed Dword Integers" ),
                new OP_INFO( "66+0F+38+29", "pcmpeqq", new OP_TYPES[]{ xmm, xmm_m128 },     "Compare Packed Qword Data for Equal" ),
                new OP_INFO( "66+0F+38+2A", "movntdqa", new OP_TYPES[]{ xmm, m128 },        "Load Double Quadword Non-Temporal Aligned Hint" ),
                new OP_INFO( "66+0F+38+2B", "packusdw", new OP_TYPES[]{ xmm, xmm_m128 },    "Pack with Unsigned Saturation" ),
                new OP_INFO( "66+0F+38+30", "pmovzxbw", new OP_TYPES[]{ xmm, m64 },         "Packed Move with Zero Extend" ),
                new OP_INFO( "66+0F+38+31", "pmovzxbd", new OP_TYPES[]{ xmm, m32 },         "Packed Move with Zero Extend" ),
                new OP_INFO( "66+0F+38+32", "pmovzxbq", new OP_TYPES[]{ xmm, m16 },         "Packed Move with Zero Extend" ),
                new OP_INFO( "66+0F+38+33", "pmovzxbd", new OP_TYPES[]{ xmm, m64 },         "Packed Move with Zero Extend" ),
                new OP_INFO( "66+0F+38+34", "pmovzxbq", new OP_TYPES[]{ xmm, m32 },         "Packed Move with Zero Extend" ),
                new OP_INFO( "66+0F+38+35", "pmovzxbq", new OP_TYPES[]{ xmm, m64 },         "Packed Move with Zero Extend" ),
                new OP_INFO( "66+0F+38+37", "pcmpgtq", new OP_TYPES[]{ xmm, xmm_m128 },     "Compare Packed Qword Data for Greater Than" ),
                new OP_INFO( "66+0F+38+38", "pminsb", new OP_TYPES[]{ xmm, xmm_m128 },      "Minimum of Packed Signed Byte Integers" ),
                new OP_INFO( "66+0F+38+39", "pminsd", new OP_TYPES[]{ xmm, xmm_m128 },      "Minimum of Packed Signed Dword Integers" ),
                new OP_INFO( "66+0F+38+3A", "pminuw", new OP_TYPES[]{ xmm, xmm_m128 },      "Minimum of Packed Unsigned Word Integers" ),
                new OP_INFO( "66+0F+38+3B", "pminud", new OP_TYPES[]{ xmm, xmm_m128 },      "Minimum of Packed Unsigned Dword Integers" ),
                new OP_INFO( "66+0F+38+3C", "pmaxsb", new OP_TYPES[]{ xmm, xmm_m128 },      "Maximum of Packed Signed Byte Integers" ),
                new OP_INFO( "66+0F+38+3D", "pmaxsd", new OP_TYPES[]{ xmm, xmm_m128 },      "Maximum of Packed Signed Dword Integers" ),
                new OP_INFO( "66+0F+38+3E", "pmaxuw", new OP_TYPES[]{ xmm, xmm_m128 },      "Maximum of Packed Unsigned Word Integers" ),
                new OP_INFO( "66+0F+38+3F", "pmaxud", new OP_TYPES[]{ xmm, xmm_m128 },      "Maximum of Packed Unsigned Dword Integers" ),
                new OP_INFO( "66+0F+38+40", "pmulld", new OP_TYPES[]{ xmm, xmm_m128 },      "Multiply Packed Signed Dword Integers and Store Low Result" ),
                new OP_INFO( "66+0F+38+41", "phminposuw", new OP_TYPES[]{ xmm, xmm_m128 },  "Packed Horizontal Word Minimum" ),
                new OP_INFO( "66+0F+38+80", "invept", new OP_TYPES[]{ r32, m128 },          "Invalidate Translations Derived from EPT" ),
                new OP_INFO( "66+0F+38+81", "invvpid", new OP_TYPES[]{ r32, m128 },         "Invalidate Translations Based on VPID" ),
                new OP_INFO( "0F+38+F0", "movbe", new OP_TYPES[]{ r16_32, m16_32 },         "Move Data After Swapping Bytes" ),
                new OP_INFO( "F2+0F+38+F0", "crc32", new OP_TYPES[]{ r32, r_m8 },           "Accumulate CRC32 Value" ),
                new OP_INFO( "0F+38+F1", "movbe", new OP_TYPES[]{ m16_32, r16_32 },         "Move Data After Swapping Bytes" ),
                new OP_INFO( "F2+0F+38+F1", "crc32", new OP_TYPES[]{ r32, r_m16_32 },       "Accumulate CRC32 Value" ),
                new OP_INFO( "66+0F+3A+08","roundps", new OP_TYPES[]{ xmm, xmm_m128, imm8 },"Round Packed Single-FP Values" ),
                new OP_INFO( "66+0F+3A+09","roundpd", new OP_TYPES[]{ xmm, xmm_m128, imm8 },"Round Packed Double-FP Values" ),
                new OP_INFO( "66+0F+3A+0A", "roundss", new OP_TYPES[]{ xmm, xmm_m32, imm8 },"Round Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+3A+0B", "roundsd", new OP_TYPES[]{ xmm, xmm_m64, imm8 },"Round Scalar Double-FP Values" ),
                new OP_INFO( "66+0F+3A+0C","blendps", new OP_TYPES[]{ xmm, xmm_m128, imm8 },"Round Packed Single-FP Values" ),
                new OP_INFO( "66+0F+3A+0D","blendpd", new OP_TYPES[]{ xmm, xmm_m128, imm8 },"Round Packed Double-FP Values" ),
                new OP_INFO( "66+0F+3A+0E","pblendw", new OP_TYPES[]{ xmm, xmm_m128, imm8 },"Blend Packed Words" ),
                new OP_INFO( "0F+3A+0F", "palignr", new OP_TYPES[]{ mm, mm_m64 },           "Packed Align Right" ),
                new OP_INFO( "66+0F+3A+0F", "palignr", new OP_TYPES[]{ mm, xmm_m128 },      "Packed Align Right" ),
                new OP_INFO( "66+0F+3A+14", "pextrb", new OP_TYPES[]{ m8, xmm, imm8 },      "Extract Byte" ),
                new OP_INFO( "66+0F+3A+15", "pextrw", new OP_TYPES[]{ m16, xmm, imm8 },     "Extract Word" ),
                new OP_INFO( "66+0F+3A+16", "pextrd", new OP_TYPES[]{ m32, xmm, imm8 },     "Extract Dword/Qword" ),
                new OP_INFO( "66+0F+3A+17", "extractps", new OP_TYPES[]{ m64, xmm, imm8 },  "Extract Packed Single-FP Value" ),
                new OP_INFO( "66+0F+3A+20", "pinsrb", new OP_TYPES[]{ xmm, m8, imm8 },      "Insert Byte" ),
                new OP_INFO( "66+0F+3A+21", "insertps", new OP_TYPES[]{ xmm, m32, imm8 },   "Insert Packed Single-FP Value" ),
                new OP_INFO( "66+0F+3A+22", "pinsrd", new OP_TYPES[]{ xmm, m64, imm8 },     "Insert Dword/Qword" ),
                new OP_INFO( "66+0F+3A+40", "dpps", new OP_TYPES[]{ xmm, xmm_m128 },        "Dot Product of Packed Single-FP Values" ),
                new OP_INFO( "66+0F+3A+41", "dppd", new OP_TYPES[]{ xmm, xmm_m128 },        "Dot Product of Packed Double-FP Values" ),
                new OP_INFO( "66+0F+3A+42","mpsadbw", new OP_TYPES[]{xmm, xmm_m128, imm8},  "Compute Multiple Packed Sums of Absolute Difference" ),
                new OP_INFO( "66+0F+3A+60","pcmpestrm", new OP_TYPES[]{xmm0, xmm, xmm_m128},"Packed Compare Explicit Length Strings, Return Mask" ),
                new OP_INFO( "66+0F+3A+61","pcmpestri", new OP_TYPES[]{ECX, xmm, xmm_m128}, "Packed Compare Explicit Length Strings, Return Index" ),
                new OP_INFO( "66+0F+3A+62","pcmpistrm", new OP_TYPES[]{xmm0, xmm, xmm_m128, imm8},  "Packed Compare Implicit Length Strings, Return Mask" ),
                new OP_INFO( "66+0F+3A+63","pcmpistri", new OP_TYPES[]{ECX, xmm, xmm_m128, imm8},   "Packed Compare Implicit Length Strings, Return Index" ),
                new OP_INFO( "0F+40", "cmovo", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - overflow (OF=1)" ),
                new OP_INFO( "0F+41", "cmovno", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - not overflow (OF=0)" ),
                new OP_INFO( "0F+42", "cmovb", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - below/not above or equal/carry (CF=1)" ),
                new OP_INFO( "0F+43", "cmovnb", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - onot below/above or equal/not carry (CF=0)" ),
                new OP_INFO( "0F+44", "cmove", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - zero/equal (ZF=1)" ),
                new OP_INFO( "0F+45", "cmovne", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - not zero/not equal (ZF=0)" ),
                new OP_INFO( "0F+46", "cmovbe", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - below or equal/not above (CF=1 OR ZF=1)" ),
                new OP_INFO( "0F+47", "cmova", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - not below or equal/above (CF=0 AND ZF=0)" ),
                new OP_INFO( "0F+48", "cmovs", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - sign (SF=1)" ),
                new OP_INFO( "0F+49", "cmovns", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - not sign (SF=0)" ),
                new OP_INFO( "0F+4A", "cmovp", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - parity/parity even (PF=1)" ),
                new OP_INFO( "0F+4B", "cmovnp", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - not parity/parity odd (PF=0)" ),
                new OP_INFO( "0F+4C", "cmovl", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - less/not greater (SF!=OF)" ),
                new OP_INFO( "0F+4D", "cmovge", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - not less/greater or equal (SF=OF)" ),
                new OP_INFO( "0F+4E", "cmovng", new OP_TYPES[]{ r16_32, r_m16_32 },         "Conditional Move - less or equal/not greater ((ZF=1) OR (SF!=OF))" ),
                new OP_INFO( "0F+4F", "cmovg", new OP_TYPES[]{ r16_32, r_m16_32 },          "Conditional Move - not less nor equal/greater ((ZF=0) AND (SF=OF))" ),
                new OP_INFO( "0F+50", "movmskps", new OP_TYPES[]{ r32, xmm },               "Extract Packed Single-FP Sign Mask" ),
                new OP_INFO( "66+0F+50", "movmskpd", new OP_TYPES[]{ r32, xmm },            "Extract Packed Double-FP Sign Mask" ),
                new OP_INFO( "66+0F+50", "movmskpd", new OP_TYPES[]{ r32, xmm },            "Extract Packed Double-FP Sign Mask" ),
                new OP_INFO( "0F+51", "sqrtps", new OP_TYPES[]{ xmm, xmm_m128 },            "Compute Square Roots of Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+51", "sqrtss", new OP_TYPES[]{ xmm, xmm_m32 },          "Compute Square Root of Scalar Single-FP Value" ),
                new OP_INFO( "66+0F+51", "sqrtpd", new OP_TYPES[]{ xmm, xmm_m128 },         "Compute Square Roots of Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+51", "sqrtsd", new OP_TYPES[]{ xmm, xmm_m64 },          "Compute Square Root of Scalar Double-FP Value" ),
                new OP_INFO( "0F+52", "rsqrtps", new OP_TYPES[]{ xmm, xmm_m128 },           "Compute Recipr. of Square Roots of Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+52", "rsqrtss", new OP_TYPES[]{ xmm, xmm_m32 },         "Compute Recipr. of Square Root of Scalar Single-FP Value" ),
                new OP_INFO( "0F+53", "rcpps", new OP_TYPES[]{ xmm, xmm_m128 },             "Compute Reciprocals of Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+53", "rcpss", new OP_TYPES[]{ xmm, xmm_m32 },           "Compute Reciprocal of Scalar Single-FP Values" ),
                new OP_INFO( "0F+54", "andps", new OP_TYPES[]{ xmm, xmm_m128 },             "Bitwise Logical AND of Packed Single-FP Values" ),
                new OP_INFO( "66+0F+54", "andpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Bitwise Logical AND of Packed Double-FP Values" ),
                new OP_INFO( "0F+55", "andnps", new OP_TYPES[]{ xmm, xmm_m128 },            "Bitwise Logical AND NOT of Packed Single-FP Values" ),
                new OP_INFO( "66+0F+55", "andnpd", new OP_TYPES[]{ xmm, xmm_m128 },         "Bitwise Logical AND NOT of Packed Double-FP Values" ),
                new OP_INFO( "0F+56", "orps", new OP_TYPES[]{ xmm, xmm_m128 },              "Bitwise Logical OR of Packed Single-FP Values" ),
                new OP_INFO( "66+0F+56", "orpd", new OP_TYPES[]{ xmm, xmm_m128 },           "Bitwise Logical OR of Packed Double-FP Values" ),
                new OP_INFO( "0F+57", "xorps", new OP_TYPES[]{ xmm, xmm_m128 },             "Bitwise Logical XOR of Packed Single-FP Values" ),
                new OP_INFO( "66+0F+57", "xorpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Bitwise Logical XOR of Packed Double-FP Values" ),
                new OP_INFO( "0F+58", "addps", new OP_TYPES[]{ xmm, xmm_m128 },             "Add Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+58", "addss", new OP_TYPES[]{ xmm, xmm_m32 },           "Add Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+58", "addpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Add Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+58", "addsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Add Scalar Double-FP Values" ),
                new OP_INFO( "0F+59", "mulps", new OP_TYPES[]{ xmm, xmm_m128 },             "Multiply Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+59", "mulss", new OP_TYPES[]{ xmm, xmm_m32 },           "Multiply Scalar Single-FP Value" ),
                new OP_INFO( "66+0F+59", "mulpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Multiply Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+59", "addsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Multiply Scalar Double-FP Values" ),
                new OP_INFO( "0F+5A", "cvtps2pd", new OP_TYPES[]{ xmm, xmm_m128 },          "Convert Packed Single-FP Values to Double-FP Values" ),
                new OP_INFO( "F3+0F+5A", "cvtpd2ps", new OP_TYPES[]{ xmm, xmm_m128 },       "Convert Packed Double-FP Values to Single-FP Values" ),
                new OP_INFO( "66+0F+5A", "cvtss2sd", new OP_TYPES[]{ xmm, xmm_m32 },        "Convert Scalar Single-FP Value to Scalar Double-FP Value" ),
                new OP_INFO( "F2+0F+5A", "cvtsd2ss", new OP_TYPES[]{ xmm, xmm_m64 },        "Convert Scalar Double-FP Value to Scalar Single-FP Value" ),
                new OP_INFO( "0F+5B", "cvtdq2ps", new OP_TYPES[]{ xmm, xmm_m128 },          "Convert Packed DW Integers to Single-FP Values" ),
                new OP_INFO( "66+0F+5B", "cvtps2dq", new OP_TYPES[]{ xmm, xmm_m128 },       "Convert Packed Single-FP Values to DW Integers" ),
                new OP_INFO( "F3+0F+5B", "cvttps2dq", new OP_TYPES[]{ xmm, xmm_m128 },      "Convert with Trunc. Packed Single-FP Values to DW Integers" ),
                new OP_INFO( "0F+5C", "subps", new OP_TYPES[]{ xmm, xmm_m128 },             "Subtract Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+5C", "subss", new OP_TYPES[]{ xmm, xmm_m32 },           "Subtract Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+5C", "subpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Subtract Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+5C", "subsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Subtract Scalar Double-FP Values" ),
                new OP_INFO( "0F+5D", "minps", new OP_TYPES[]{ xmm, xmm_m128 },             "Return Minimum Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+5D", "minss", new OP_TYPES[]{ xmm, xmm_m32 },           "Return Minimum Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+5D", "minpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Return Minimum Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+5D", "minsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Return Minimum Scalar Double-FP Values" ),
                new OP_INFO( "0F+5E", "divps", new OP_TYPES[]{ xmm, xmm_m128 },             "Divide Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+5E", "divss", new OP_TYPES[]{ xmm, xmm_m32 },           "Divide Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+5E", "divpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Divide Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+5E", "divsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Divide Scalar Double-FP Values" ),
                new OP_INFO( "0F+5F", "maxps", new OP_TYPES[]{ xmm, xmm_m128 },             "Return Maximum Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+5F", "maxss", new OP_TYPES[]{ xmm, xmm_m32 },           "Return Maximum Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+5F", "maxpd", new OP_TYPES[]{ xmm, xmm_m128 },          "Return Maximum Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+5F", "maxsd", new OP_TYPES[]{ xmm, xmm_m64 },           "Return Maximum Scalar Double-FP Values" ),
                new OP_INFO( "0F+60", "punpcklbw", new OP_TYPES[]{ mm, mm_m64 },            "Unpack Low Data" ),
                new OP_INFO( "66+0F+60", "punpcklbw", new OP_TYPES[]{ xmm, xmm_m128 },      "Unpack Low Data" ),
                new OP_INFO( "0F+61", "punpcklbd", new OP_TYPES[]{ mm, mm_m64 },            "Unpack Low Data" ),
                new OP_INFO( "66+0F+61", "punpcklbd", new OP_TYPES[]{ xmm, xmm_m128 },      "Unpack Low Data" ),
                new OP_INFO( "0F+62", "punpcklbq", new OP_TYPES[]{ mm, mm_m64 },            "Unpack Low Data" ),
                new OP_INFO( "66+0F+62", "punpcklbq", new OP_TYPES[]{ xmm, xmm_m128 },      "Unpack Low Data" ),
                new OP_INFO( "0F+63", "packsswb", new OP_TYPES[]{ mm, mm_m64 },             "Pack with Signed Saturation" ),
                new OP_INFO( "66+0F+63", "packsswb", new OP_TYPES[]{ xmm, xmm_m128 },       "Pack with Signed Saturation" ),
                new OP_INFO( "0F+64", "pcmpgtb", new OP_TYPES[]{ mm, mm_m64 },              "Compare Packed Signed Integers for Greater Than" ),
                new OP_INFO( "66+0F+64", "pcmpgtb", new OP_TYPES[]{ xmm, xmm_m128 },        "Compare Packed Signed Integers for Greater Than" ),
                new OP_INFO( "0F+65", "pcmpgtw", new OP_TYPES[]{ mm, mm_m64 },              "Compare Packed Signed Integers for Greater Than" ),
                new OP_INFO( "66+0F+65", "pcmpgtw", new OP_TYPES[]{ xmm, xmm_m128 },        "Compare Packed Signed Integers for Greater Than" ),
                new OP_INFO( "0F+66", "pcmpgtd", new OP_TYPES[]{ mm, mm_m64 },              "Compare Packed Signed Integers for Greater Than" ),
                new OP_INFO( "66+0F+66", "pcmpgtd", new OP_TYPES[]{ xmm, xmm_m128 },        "Compare Packed Signed Integers for Greater Than" ),
                new OP_INFO( "0F+67", "packuswb", new OP_TYPES[]{ mm, mm_m64 },             "Pack with Unsigned Saturation" ),
                new OP_INFO( "66+0F+67", "packuswb", new OP_TYPES[]{ xmm, xmm_m128 },       "Pack with Unsigned Saturation" ),
                new OP_INFO( "0F+68", "punpckhbw", new OP_TYPES[]{ mm, mm_m64 },            "Unpack High Data" ),
                new OP_INFO( "66+0F+68", "punpckhbw", new OP_TYPES[]{ xmm, xmm_m128 },      "Unpack High Data" ),
                new OP_INFO( "0F+69", "punpckhwd", new OP_TYPES[]{ mm, mm_m64 },            "Unpack High Data" ),
                new OP_INFO( "66+0F+69", "punpckhwd", new OP_TYPES[]{ xmm, xmm_m128 },      "Unpack High Data" ),
                new OP_INFO( "0F+6A", "punpckhdq", new OP_TYPES[]{ mm, mm_m64 },            "Unpack High Data" ),
                new OP_INFO( "66+0F+6A", "punpckhdq", new OP_TYPES[]{ xmm, xmm_m128 },      "Unpack High Data" ),
                new OP_INFO( "0F+6B", "packssdw", new OP_TYPES[]{ mm, mm_m64 },             "Pack with Signed Saturation" ),
                new OP_INFO( "66+0F+6B", "packssdw", new OP_TYPES[]{ xmm, xmm_m128 },       "Pack with Signed Saturation" ),
                new OP_INFO( "66+0F+6C", "punpcklqdq", new OP_TYPES[]{ xmm, xmm_m128 },     "Unpack Low Data" ),
                new OP_INFO( "66+0F+6D", "punpckhqdq", new OP_TYPES[]{ xmm, xmm_m128 },     "Unpack High Data" ),
                new OP_INFO( "0F+6E", "movd", new OP_TYPES[]{ xmm, r_m32 },                 "Move Doubleword" ),
                new OP_INFO( "66+0F+6E", "movd", new OP_TYPES[]{ xmm, r_m32 },              "Move Doubleword" ),
                new OP_INFO( "0F+6F", "movq", new OP_TYPES[]{ xmm, mm_m64 },                "Move Quadword" ),
                new OP_INFO( "66+0F+6F", "movdqa", new OP_TYPES[]{ xmm, xmm_m128 },         "Move Aligned Double Quadword" ),
                new OP_INFO( "F3+0F+6F", "movdqu", new OP_TYPES[]{ xmm, xmm_m128 },         "Move Unaligned Double Quadword" ),
                new OP_INFO( "0F+70", "pshufw", new OP_TYPES[]{ mm_m64, imm8 },             "Shuffle Packed Words" ),
                new OP_INFO( "F3+0F+70", "pshuflw", new OP_TYPES[]{ xmm_m128, imm8 },       "Shuffle Packed Low Words" ),
                new OP_INFO( "66+0F+70", "pshufhw", new OP_TYPES[]{ xmm_m128, imm8 },       "Shuffle Packed High Words" ),
                new OP_INFO( "F2+0F+70", "pshufd", new OP_TYPES[]{ xmm_m128, imm8 },        "Shuffle Packed Doublewords" ),
                new OP_INFO( "0F+71+m2", "psrlw", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Right Logical" ),
                new OP_INFO( "66+0F+71+m2", "psrlw", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Right Logical" ),
                new OP_INFO( "0F+71+m4", "psraw", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "66+0F+71+m4", "psraw", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "0F+71+m6", "psllw", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Left Logical" ),
                new OP_INFO( "66+0F+71+m6", "psllw", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Left Logical" ),
                new OP_INFO( "0F+72+m2", "psrld", new OP_TYPES[]{ mm, imm8 },               "Shift Double Quadword Right Logical" ),
                new OP_INFO( "66+0F+72+m2", "psrld", new OP_TYPES[]{ xmm, imm8 },           "Shift Double Quadword Right Logical" ),
                new OP_INFO( "0F+72+m4", "psrad", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "66+0F+72+m4", "psrad", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "0F+72+m6", "pslld", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Left Logical" ),
                new OP_INFO( "66+0F+72+m6", "pslld", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Left Logical" ),
                new OP_INFO( "0F+73+m2", "psrld", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Right Logical" ),
                new OP_INFO( "66+0F+73+m2", "psrld", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Right Logical" ),
                new OP_INFO( "0F+73+m3", "psrad", new OP_TYPES[]{ mm, imm8 },               "Shift Double Quadword Right Logical" ),
                new OP_INFO( "66+0F+73+m6", "psrad", new OP_TYPES[]{ xmm, imm8 },           "Shift Packed Data Left Logical" ),
                new OP_INFO( "0F+73+m6", "pslld", new OP_TYPES[]{ mm, imm8 },               "Shift Packed Data Left Logical" ),
                new OP_INFO( "66+0F+73+m7", "pslld", new OP_TYPES[]{ xmm, imm8 },           "Shift Double Quadword Left Logical" ),
                new OP_INFO( "0F+74", "pcmpeqb", new OP_TYPES[]{ mm, mm_m64 },              "Compare Packed Data for Equal" ),
                new OP_INFO( "66+0F+74", "pcmpeqb", new OP_TYPES[]{ xmm, xmm_m128 },        "Compare Packed Data for Equal" ),
                new OP_INFO( "0F+75", "pcmpeqw", new OP_TYPES[]{ mm, mm_m64 },              "Compare Packed Data for Equal" ),
                new OP_INFO( "66+0F+75", "pcmpeqw", new OP_TYPES[]{ xmm, xmm_m128 },        "Compare Packed Data for Equal" ),
                new OP_INFO( "0F+76", "pcmpeqd", new OP_TYPES[]{ mm, mm_m64 },              "Compare Packed Data for Equal" ),
                new OP_INFO( "66+0F+76", "pcmpeqd", new OP_TYPES[]{ xmm, xmm_m128 },        "Compare Packed Data for Equal" ),
                new OP_INFO( "0F+77", "emms", new OP_TYPES[]{  },                           "Empty MMX Technology State" ),
                new OP_INFO( "0F+78", "vmread", new OP_TYPES[]{  },                         "Read Field from Virtual-Machine Control Structure" ),
                new OP_INFO( "0F+79", "vmwrite", new OP_TYPES[]{  },                        "Write Field to Virtual-Machine Control Structure" ),
                new OP_INFO( "66+0F+7C", "haddpd", new OP_TYPES[]{ xmm, xmm_m128 },         "Packed Double-FP Horizontal Add" ),
                new OP_INFO( "F2+0F+7C", "haddps", new OP_TYPES[]{ xmm, xmm_m128 },         "Packed Single-FP Horizontal Add" ),
                new OP_INFO( "66+0F+7D", "hsubpd", new OP_TYPES[]{ xmm, xmm_m128 },         "Packed Double-FP Horizontal Subtract" ),
                new OP_INFO( "F2+0F+7D", "hsubps", new OP_TYPES[]{ xmm, xmm_m128 },         "Packed Single-FP Horizontal Subtract" ),
                new OP_INFO( "0F+7E", "movd", new OP_TYPES[]{ r_m32, mm },                  "Move Doubleword" ),
                new OP_INFO( "66+0F+7E", "movd", new OP_TYPES[]{ r_m32, xmm },              "Move Doubleword" ),
                new OP_INFO( "F3+0F+7E", "movq", new OP_TYPES[]{ xmm, xmm_m64 },            "Move Quadword" ),
                new OP_INFO( "0F+7F", "movq", new OP_TYPES[]{ xmm_m64, mm },                "Move Quadword" ),
                new OP_INFO( "66+0F+7F", "movdqa", new OP_TYPES[]{ xmm_m128, xmm },         "Move Aligned Double Quadword" ),
                new OP_INFO( "F3+0F+7F", "movdqu", new OP_TYPES[]{ xmm_m128, xmm },         "Move Unaligned Double Quadword" ),
                new OP_INFO( "0F+80", "long jo", new OP_TYPES[]{ rel16_32 },                "Jump far if overflow (OF=1)" ),
                new OP_INFO( "0F+81", "long jno", new OP_TYPES[]{ rel16_32 },               "Jump far if not overflow (OF=0)" ),
                new OP_INFO( "0F+82", "long jb", new OP_TYPES[]{ rel16_32 },                "Jump far if below/not above or equal/carry (CF=1)" ),
                new OP_INFO( "0F+83", "long jnb", new OP_TYPES[]{ rel16_32 },               "Jump far if not below/above or equal/not carry (CF=0)" ),
                new OP_INFO( "0F+84", "long je", new OP_TYPES[]{ rel16_32 },                "Jump far if zero/equal (ZF=1)" ),
                new OP_INFO( "0F+85", "long jne", new OP_TYPES[]{ rel16_32 },               "Jump far if not zero/not equal (ZF=0)" ),
                new OP_INFO( "0F+86", "long jna", new OP_TYPES[]{ rel16_32 },               "Jump far if below or equal/not above (CF=1 OR ZF=1)" ),
                new OP_INFO( "0F+87", "long ja", new OP_TYPES[]{ rel16_32 },                "Jump far if not below or equal/above (CF=0 AND ZF=0)" ),
                new OP_INFO( "0F+88", "long js", new OP_TYPES[]{ rel16_32 },                "Jump far if sign (SF=1)" ),
                new OP_INFO( "0F+89", "long jns", new OP_TYPES[]{ rel16_32 },               "Jump far if not sign (SF=0)" ),
                new OP_INFO( "0F+8A", "long jp", new OP_TYPES[]{ rel16_32 },                "Jump far if parity/parity even (PF=1)" ),
                new OP_INFO( "0F+8B", "long jnp", new OP_TYPES[]{ rel16_32 },               "Jump far if not parity/parity odd (PF=0)" ),
                new OP_INFO( "0F+8C", "long jl", new OP_TYPES[]{ rel16_32 },                "Jump far if less/not greater (SF!=OF)" ),
                new OP_INFO( "0F+8D", "long jnl", new OP_TYPES[]{ rel16_32 },               "Jump far if not less/greater or equal (SF=OF)" ),
                new OP_INFO( "0F+8E", "long jng", new OP_TYPES[]{ rel16_32 },               "Jump far if less or equal/not greater ((ZF=1) OR (SF!=OF))" ),
                new OP_INFO( "0F+8F", "long jg", new OP_TYPES[]{ rel16_32 },                "Jump far if not less nor equal/greater ((ZF=0) AND (SF=OF))" ),
                new OP_INFO( "0F+90", "seto", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - overflow (OF=1)" ),
                new OP_INFO( "0F+91", "setno", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - not overflow (OF=0)" ),
                new OP_INFO( "0F+92", "setb", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - below/not above or equal/carry (CF=1)" ),
                new OP_INFO( "0F+93", "setnb", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - not below/above or equal/not carry (CF=0)" ),
                new OP_INFO( "0F+94", "sete", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - zero/equal (ZF=1)" ),
                new OP_INFO( "0F+95", "setne", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - not zero/not equal (ZF=0)" ),
                new OP_INFO( "0F+96", "setna", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - below or equal/not above (CF=1 OR ZF=1)" ),
                new OP_INFO( "0F+97", "seta", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - not below or equal/above (CF=0 AND ZF=0)" ),
                new OP_INFO( "0F+98", "sets", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - sign (SF=1)" ),
                new OP_INFO( "0F+99", "setns", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - not sign (SF=0)" ),
                new OP_INFO( "0F+9A", "setp", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - parity/parity even (PF=1)" ),
                new OP_INFO( "0F+9B", "setnp", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - not parity/parity odd (PF=0)" ),
                new OP_INFO( "0F+9C", "setl", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - less/not greater (SF!=OF)" ),
                new OP_INFO( "0F+9D", "setnl", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - not less/greater or equal (SF=OF)" ),
                new OP_INFO( "0F+9E", "setng", new OP_TYPES[]{ r_m8 },                      "Set Byte on Condition - less or equal/not greater ((ZF=1) OR (SF!=OF))" ),
                new OP_INFO( "0F+9F", "setg", new OP_TYPES[]{ r_m8 },                       "Set Byte on Condition - not less nor equal/greater ((ZF=0) AND (SF=OF))" ),
                new OP_INFO( "0F+A0", "push", new OP_TYPES[]{ FS },                         "Push Word, Doubleword or Quadword Onto the Stack" ),
                new OP_INFO( "0F+A1", "pop", new OP_TYPES[]{ FS },                          "Pop a Value from the Stack" ),
                new OP_INFO( "0F+A2", "cpuid", new OP_TYPES[]{ IA32_BIOS },                 "CPU Identification" ),
                new OP_INFO( "0F+A3", "bt", new OP_TYPES[]{ r_m16_32, r16_32 },             "Bit Test" ),
                new OP_INFO( "0F+A4", "shld", new OP_TYPES[]{ r_m16_32, r16_32, imm8 },     "Double Precision Shift Left" ),
                new OP_INFO( "0F+A5", "shld", new OP_TYPES[]{ r_m16_32, r16_32, CL },       "Double Precision Shift Left" ),
                new OP_INFO( "0F+A8", "push", new OP_TYPES[]{ GS },                         "Push Word, Doubleword or Quadword Onto the Stack" ),
                new OP_INFO( "0F+A9", "pop", new OP_TYPES[]{ GS },                          "Pop a Value from the Stack" ),
                new OP_INFO( "0F+AA", "rsm", new OP_TYPES[]{  },                            "Resume from System Management Mode" ),
                new OP_INFO( "0F+AB", "bts", new OP_TYPES[]{ r_m16_32, r16_32 },            "Bit Test and Set" ),
                new OP_INFO( "0F+AC", "shrd", new OP_TYPES[]{ r_m16_32, r16_32, imm8 },     "Double Precision Shift Right" ),
                new OP_INFO( "0F+AD", "shrd", new OP_TYPES[]{ r_m16_32, r16_32, CL },       "Double Precision Shift Right" ),
                new OP_INFO( "0F+AE+m0", "fxsave", new OP_TYPES[]{ m512, ST, ST1 },         "Save x87 FPU, MMX, XMM, and MXCSR State" ),
                new OP_INFO( "0F+AE+m1", "fxrstor", new OP_TYPES[]{ ST, ST1, ST2 },         "Restore x87 FPU, MMX, XMM, and MXCSR State" ),
                new OP_INFO( "0F+AE+m2", "ldmxcsr", new OP_TYPES[]{ m32 },                  "Load MXCSR Register" ),
                new OP_INFO( "0F+AE+m3", "stmxcsr", new OP_TYPES[]{ m32 },                  "Store MXCSR Register State" ),
                new OP_INFO( "0F+AE+m4", "xsave", new OP_TYPES[]{ m, EDX, EAX },            "Save Processor Extended States" ),
                new OP_INFO( "0F+AE+m5", "lfence", new OP_TYPES[]{  },                      "Load Fence" ),
                new OP_INFO( "0F+AE+m5", "xrstor", new OP_TYPES[]{ ST, ST1, ST2 },          "Restore Processor Extended States" ),
                new OP_INFO( "0F+AE+m6", "mfence", new OP_TYPES[]{  },                      "Memory Fence" ),
                new OP_INFO( "0F+AE+m7", "sfence", new OP_TYPES[]{  },                      "Store Fence" ),
                new OP_INFO( "0F+AE+m7", "clflush", new OP_TYPES[]{ m8 },                   "Flush Cache Line" ),
                new OP_INFO( "0F+AF", "imul", new OP_TYPES[]{ r16_32, r_m16_32 },           "Signed Multiply" ),
                new OP_INFO( "0F+B0", "cmpxchg", new OP_TYPES[]{ r_m8, AL, r8 },            "Compare and Exchange" ),
                new OP_INFO( "0F+B1", "cmpxchg", new OP_TYPES[]{ r_m16_32, EAX, r16_32 },   "Compare and Exchange" ),
                new OP_INFO( "0F+B2", "lss", new OP_TYPES[]{ SS, r16_32, m16_32_and_16_32}, "Load Far Pointer" ),
                new OP_INFO( "0F+B3", "btr", new OP_TYPES[]{ r_m16_32, r16_32 },            "Bit Test and Reset" ),
                new OP_INFO( "0F+B4", "lfs", new OP_TYPES[]{ FS,r_m16_32,m16_32_and_16_32}, "Load Far Pointer" ),
                new OP_INFO( "0F+B5", "lgs", new OP_TYPES[]{ GS,r_m16_32,m16_32_and_16_32}, "Load Far Pointer" ),
                new OP_INFO( "0F+B6", "movzx", new OP_TYPES[]{ r16_32, r_m8 },              "Move with Zero-Extend" ),
                new OP_INFO( "0F+B7", "movzx", new OP_TYPES[]{ r16_32, r_m16 },             "Move with Zero-Extend" ),
                new OP_INFO( "F3+0F+B8", "popcnt", new OP_TYPES[]{ r16_32, r_m16_32 },      "Bit Population Count" ),
                new OP_INFO( "0F+B9", "ud", new OP_TYPES[]{  },                             "Undefined Instruction" ),
                new OP_INFO( "0F+BA+m4", "bt", new OP_TYPES[]{ r_m16_32, imm8 },            "Bit Test" ),
                new OP_INFO( "0F+BA+m5", "bts", new OP_TYPES[]{ r_m16_32, imm8 },           "Bit Test and Set" ),
                new OP_INFO( "0F+BA+m6", "btr", new OP_TYPES[]{ r_m16_32, imm8 },           "Bit Test and Reset" ),
                new OP_INFO( "0F+BA+m7", "btc", new OP_TYPES[]{ r_m16_32, imm8 },           "Bit Test and Complement" ),
                new OP_INFO( "0F+BB", "btc", new OP_TYPES[]{ r_m16_32, r16_32 },            "Bit Test and Complement" ),
                new OP_INFO( "0F+BC", "bsf", new OP_TYPES[]{ r16_32, r_m16_32 },            "Bit Scan Forward" ),
                new OP_INFO( "0F+BD", "bsr", new OP_TYPES[]{ r16_32, r_m16_32 },            "Bit Scan Reverse" ),
                new OP_INFO( "0F+BE", "movsx", new OP_TYPES[]{ r16_32, r_m8 },              "Move with Sign-Extension" ),
                new OP_INFO( "0F+BF", "movsx", new OP_TYPES[]{ r16_32, r_m16 },             "Move with Sign-Extension" ),
                new OP_INFO( "0F+C0", "xadd", new OP_TYPES[]{ r_m8, r8 },                   "Exchange and Add" ),
                new OP_INFO( "0F+C1", "xadd", new OP_TYPES[]{ r_m16_32, r16_32 },           "Exchange and Add" ),
                new OP_INFO( "0F+C2", "cmpps", new OP_TYPES[]{ xmm, xmm_m128, imm8 },       "Compare Packed Single-FP Values" ),
                new OP_INFO( "F3+0F+C2", "cmpss", new OP_TYPES[]{ xmm, xmm_m32, imm8 },     "Compare Scalar Single-FP Values" ),
                new OP_INFO( "66+0F+C2", "cmppd", new OP_TYPES[]{ xmm, xmm_m128, imm8 },    "Compare Packed Double-FP Values" ),
                new OP_INFO( "F2+0F+C2", "cmpsd", new OP_TYPES[]{ xmm, xmm_m64, imm8 },     "Compare Scalar Double-FP Values" ),
                new OP_INFO( "0F+C3", "movnti", new OP_TYPES[]{ m32, r32 },                 "Store Doubleword Using Non-Temporal Hint" ),
                new OP_INFO( "0F+C4", "pinsrw", new OP_TYPES[]{ mm, m16, imm8 },            "Insert Word" ),
                new OP_INFO( "66+0F+C4", "pinsrw", new OP_TYPES[]{ xmm, m16, imm8 },        "Insert Word" ),
                new OP_INFO( "0F+C5", "pextrw", new OP_TYPES[]{ r32, mm, imm8 },            "Extract Word" ),
                new OP_INFO( "66+0F+C5", "pextrw", new OP_TYPES[]{ r32, xmm, imm8 },        "Extract Word" ),
                new OP_INFO( "0F+C6", "shufps", new OP_TYPES[]{ xmm, xmm_m128, imm8 },      "Shuffle Packed Single-FP Values" ),
                new OP_INFO( "66+0F+C6", "shufpd", new OP_TYPES[]{ xmm, xmm_m128, imm8 },   "Shuffle Packed Double-FP Values" ),
                new OP_INFO( "0F+C7+m1", "cmpxchg8b", new OP_TYPES[]{ m64, EAX, EDX },      "Compare and Exchange Bytes" ),
                new OP_INFO( "0F+C7+m6", "vmptrld", new OP_TYPES[]{ m64 },                  "Load Pointer to Virtual-Machine Control Structure" ),
                new OP_INFO( "66+0F+C7+m6", "vmclean", new OP_TYPES[]{ m64 },               "Clear Virtual-Machine Control Structure" ),
                new OP_INFO( "F3+0F+C7+m6", "vmxon", new OP_TYPES[]{ m64 },                 "Enter VMX Operation" ),
                new OP_INFO( "0F+C7+m7", "vmptrst", new OP_TYPES[]{ m64 },                  "Store Pointer to Virtual-Machine Control Structure" ),
                new OP_INFO( "0F+C8+r", "bswap", new OP_TYPES[]{ r16_32 },                  "Byte Swap" ),
                new OP_INFO( "66+0F+D0", "addsubpd", new OP_TYPES[]{ xmm, xmm_m128 },       "Packed Double-FP Add/Subtract" ),
                new OP_INFO( "F2+0F+D0", "addsubpd", new OP_TYPES[]{ xmm, xmm_m128 },       "Packed Single-FP Add/Subtract" ),
                new OP_INFO( "0F+D1", "psrlw", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Right Logical" ),
                new OP_INFO( "66+0F+D1", "psrlw", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Right Logical" ),
                new OP_INFO( "0F+D2", "psrld", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Right Logical" ),
                new OP_INFO( "66+0F+D2", "psrld", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Right Logical" ),
                new OP_INFO( "0F+D3", "psrlq", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Right Logical" ),
                new OP_INFO( "66+0F+D3", "psrlq", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Right Logical" ),
                new OP_INFO( "0F+D4", "paddq", new OP_TYPES[]{ mm, mm_m64 },                "Add Packed Quadword Integers" ),
                new OP_INFO( "66+0F+D4", "paddq", new OP_TYPES[]{ xmm, xmm_m128 },          "Add Packed Quadword Integers" ),
                new OP_INFO( "0F+D5", "pmullw", new OP_TYPES[]{ mm, mm_m64 },               "Multiply Packed Signed Integers and Store Low Result" ),
                new OP_INFO( "66+0F+D5", "pmullw", new OP_TYPES[]{ xmm, xmm_m128 },         "Multiply Packed Signed Integers and Store Low Result" ),
                new OP_INFO( "66+0F+D6", "movq", new OP_TYPES[]{ xmm_m64, xmm },            "Move Quadword" ),
                new OP_INFO( "F3+0F+D6", "movq2dq", new OP_TYPES[]{ xmm, mm },              "Move Quadword from MMX Technology to XMM Register" ),
                new OP_INFO( "F2+0F+D6", "movdq2q", new OP_TYPES[]{ mm, xmm },              "Move Quadword from XMM to MMX Technology Register" ),
                new OP_INFO( "0F+D7", "pmovmskb", new OP_TYPES[]{ r32, mm },                "Move Byte Mask" ),
                new OP_INFO( "66+0F+D7", "pmovmskb", new OP_TYPES[]{ r32, xmm },            "Move Byte Mask" ),
                new OP_INFO( "0F+D8", "psubusb", new OP_TYPES[]{ mm, mm_m64 },              "Subtract Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "66+0F+D8", "psubusb", new OP_TYPES[]{ xmm, xmm_m128 },        "Subtract Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "0F+D9", "psubusw", new OP_TYPES[]{ mm, mm_m64 },              "Subtract Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "66+0F+D9", "psubusw", new OP_TYPES[]{ xmm, xmm_m128 },        "Subtract Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "0F+DA", "pminub", new OP_TYPES[]{ mm, mm_m64 },               "Minimum of Packed Unsigned Byte Integers" ),
                new OP_INFO( "66+0F+DA", "pminub", new OP_TYPES[]{ xmm, xmm_m128 },         "Minimum of Packed Unsigned Byte Integers" ),
                new OP_INFO( "0F+DB", "pand", new OP_TYPES[]{ mm, mm_m64 },                 "Logical AND" ),
                new OP_INFO( "66+0F+DB", "pand", new OP_TYPES[]{ xmm, xmm_m128 },           "Logical AND" ),
                new OP_INFO( "0F+DC", "paddusb", new OP_TYPES[]{ mm, mm_m64 },              "Add Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "66+0F+DC", "paddusb", new OP_TYPES[]{ xmm, xmm_m128 },        "Add Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "0F+DD", "paddusw", new OP_TYPES[]{ mm, mm_m64 },              "Add Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "66+0F+DD", "paddusw", new OP_TYPES[]{ xmm, xmm_m128 },        "Add Packed Unsigned Integers with Unsigned Saturation" ),
                new OP_INFO( "0F+DE", "pmaxub", new OP_TYPES[]{ mm, mm_m64 },               "Maximum of Packed Unsigned Byte Integers" ),
                new OP_INFO( "66+0F+DE", "pmaxub", new OP_TYPES[]{ xmm, xmm_m128 },         "Maximum of Packed Unsigned Byte Integers" ),
                new OP_INFO( "0F+DF", "pandn", new OP_TYPES[]{ mm, mm_m64 },                "Logical AND NOT" ),
                new OP_INFO( "66+0F+DF", "pandn", new OP_TYPES[]{ xmm, xmm_m128 },          "Logical AND NOT" ),
                new OP_INFO( "0F+E0", "pavgb", new OP_TYPES[]{ mm, mm_m64 },                "Average Packed Integers" ),
                new OP_INFO( "66+0F+E0", "pavgb", new OP_TYPES[]{ xmm, xmm_m128 },          "Average Packed Integers" ),
                new OP_INFO( "0F+E1", "psraw", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "66+0F+E1", "psraw", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "0F+E2", "psrad", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "66+0F+E2", "psrad", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Right Arithmetic" ),
                new OP_INFO( "0F+E3", "pavgw", new OP_TYPES[]{ mm, mm_m64 },                "Average Packed Integers" ),
                new OP_INFO( "66+0F+E3", "pavgw", new OP_TYPES[]{ xmm, xmm_m128 },          "Average Packed Integers" ),
                new OP_INFO( "0F+E4", "pmulhuw", new OP_TYPES[]{ mm, mm_m64 },              "Multiply Packed Unsigned Integers and Store High Result" ),
                new OP_INFO( "66+0F+E4", "pmulhuw", new OP_TYPES[]{ xmm, xmm_m128 },        "Multiply Packed Unsigned Integers and Store High Result" ),
                new OP_INFO( "0F+E5", "pmulhw", new OP_TYPES[]{ mm, mm_m64 },               "Multiply Packed Signed Integers and Store High Result" ),
                new OP_INFO( "66+0F+E5", "pmulhw", new OP_TYPES[]{ xmm, xmm_m128 },         "Multiply Packed Signed Integers and Store High Result" ),
                new OP_INFO( "F2+0F+E6", "cvtpd2dq", new OP_TYPES[]{ xmm, xmm_m128 },       "Convert Packed Double-FP Values to DW Integers" ),
                new OP_INFO( "66+0F+E6", "cvttpd2dq", new OP_TYPES[]{ xmm, xmm_m128 },      "Convert with Trunc. Packed Double-FP Values to DW Integers" ),
                new OP_INFO( "F3+0F+E6", "cvtdq2pd", new OP_TYPES[]{ xmm, xmm_m128 },       "Convert Packed DW Integers to Double-FP Values" ),
                new OP_INFO( "0F+E7", "movntq", new OP_TYPES[]{ m64, mm },                  "Store of Quadword Using Non-Temporal Hint" ),
                new OP_INFO( "66+0F+E7", "movntdq", new OP_TYPES[]{ m128, xmm },            "Store Double Quadword Using Non-Temporal Hint" ),
                new OP_INFO( "0F+E8", "psubsb", new OP_TYPES[]{ mm, mm_m64 },               "Subtract Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "66+0F+E8", "psubsb", new OP_TYPES[]{ xmm, xmm_m128 },         "Subtract Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "0F+E9", "psubsw", new OP_TYPES[]{ mm, mm_m64 },               "Subtract Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "66+0F+E9", "psubsw", new OP_TYPES[]{ xmm, xmm_m128 },         "Subtract Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "0F+EA", "pminsw", new OP_TYPES[]{ mm, mm_m64 },               "Minimum of Packed Signed Word Integers" ),
                new OP_INFO( "66+0F+EA", "pminsw", new OP_TYPES[]{ xmm, xmm_m128 },         "Minimum of Packed Signed Word Integers" ),
                new OP_INFO( "0F+EB", "por", new OP_TYPES[]{ mm, mm_m64 },                  "Bitwise Logical OR" ),
                new OP_INFO( "66+0F+EB", "por", new OP_TYPES[]{ xmm, xmm_m128 },            "Bitwise Logical OR" ),
                new OP_INFO( "0F+EC", "paddsb", new OP_TYPES[]{ mm, mm_m64 },               "Add Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "66+0F+EC", "paddsb", new OP_TYPES[]{ xmm, xmm_m128 },         "Add Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "0F+ED", "paddsw", new OP_TYPES[]{ mm, mm_m64 },               "Add Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "66+0F+ED", "paddsw", new OP_TYPES[]{ xmm, xmm_m128 },         "Add Packed Signed Integers with Signed Saturation" ),
                new OP_INFO( "0F+EE", "pmaxsw", new OP_TYPES[]{ mm, mm_m64 },               "Maximum of Packed Signed Word Integers" ),
                new OP_INFO( "66+0F+EE", "pmaxsw", new OP_TYPES[]{ xmm, xmm_m128 },         "Maximum of Packed Signed Word Integers" ),
                new OP_INFO( "0F+EF", "pxor", new OP_TYPES[]{ mm, mm_m64 },                 "Logical Exclusive OR" ),
                new OP_INFO( "66+0F+EF", "pxor", new OP_TYPES[]{ xmm, xmm_m128 },           "Logical Exclusive OR" ),
                new OP_INFO( "F2+0F+F0", "lddqu", new OP_TYPES[]{ xmm, m128 },              "Load Unaligned Integer 128 Bits" ),
                new OP_INFO( "0F+F1", "psllw", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Left Logical" ),
                new OP_INFO( "66+0F+F1", "psllw", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Left Logical" ),
                new OP_INFO( "0F+F2", "pslld", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Left Logical" ),
                new OP_INFO( "66+0F+F2", "pslld", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Left Logical" ),
                new OP_INFO( "0F+F3", "psllq", new OP_TYPES[]{ mm, mm_m64 },                "Shift Packed Data Left Logical" ),
                new OP_INFO( "66+0F+F3", "psllq", new OP_TYPES[]{ xmm, xmm_m128 },          "Shift Packed Data Left Logical" ),
                new OP_INFO( "0F+F4", "pmuludq", new OP_TYPES[]{ mm, mm_m64 },              "Multiply Packed Unsigned DW Integers" ),
                new OP_INFO( "66+0F+F4", "pmuludq", new OP_TYPES[]{ xmm, xmm_m128 },        "Multiply Packed Unsigned DW Integers" ),
                new OP_INFO( "0F+F5", "pmaddwd", new OP_TYPES[]{ mm, mm_m64 },              "Multiply and Add Packed Integers" ),
                new OP_INFO( "66+0F+F5", "pmaddwd", new OP_TYPES[]{ xmm, xmm_m128 },        "Multiply and Add Packed Integers" ),
                new OP_INFO( "0F+F6", "psadbw", new OP_TYPES[]{ mm, mm_m64 },               "Compute Sum of Absolute Differences" ),
                new OP_INFO( "66+0F+F6", "psadbw", new OP_TYPES[]{ xmm, xmm_m128 },         "Compute Sum of Absolute Differences" ),
                new OP_INFO( "0F+F7", "maskmovq", new OP_TYPES[]{ m64, mm, mm },            "Store Selected Bytes of Quadword" ),
                new OP_INFO( "66+0F+F7", "maskmovdqu", new OP_TYPES[]{ m128, xmm, xmm },    "Store Selected Bytes of Double Quadword" ),
                new OP_INFO( "0F+F8", "psubb", new OP_TYPES[]{ mm, mm_m64 },                "Subtract Packed Integers" ),
                new OP_INFO( "66+0F+F8", "psubb", new OP_TYPES[]{ xmm, xmm_m128 },          "Subtract Packed Integers" ),
                new OP_INFO( "0F+F9", "psubw", new OP_TYPES[]{ mm, mm_m64 },                "Subtract Packed Integers" ),
                new OP_INFO( "66+0F+F9", "psubw", new OP_TYPES[]{ xmm, xmm_m128 },          "Subtract Packed Integers" ),
                new OP_INFO( "0F+FA", "psubd", new OP_TYPES[]{ mm, mm_m64 },                "Subtract Packed Integers" ),
                new OP_INFO( "66+0F+FA", "psubd", new OP_TYPES[]{ xmm, xmm_m128 },          "Subtract Packed Integers" ),
                new OP_INFO( "0F+FB", "psubq", new OP_TYPES[]{ mm, mm_m64 },                "Subtract Packed Quadword Integers" ),
                new OP_INFO( "66+0F+FB", "psubq", new OP_TYPES[]{ xmm, xmm_m128 },          "Subtract Packed Quadword Integers" ),
                new OP_INFO( "0F+FC", "paddb", new OP_TYPES[]{ mm, mm_m64 },                "Add Packed Integers" ),
                new OP_INFO( "66+0F+FC", "paddb", new OP_TYPES[]{ xmm, xmm_m128 },          "Add Packed Integers" ),
                new OP_INFO( "0F+FD", "paddw", new OP_TYPES[]{ mm, mm_m64 },                "Add Packed Integers" ),
                new OP_INFO( "66+0F+FD", "paddw", new OP_TYPES[]{ xmm, xmm_m128 },          "Add Packed Integers" ),
                new OP_INFO( "0F+FE", "paddd", new OP_TYPES[]{ mm, mm_m64 },                "Add Packed Integers" ),
                new OP_INFO( "66+0F+FE", "paddd", new OP_TYPES[]{ xmm, xmm_m128 },          "Add Packed Integers" ),
                new OP_INFO( "10", "adc", new OP_TYPES[]{ r_m8, r8 },                       "Add with Carry" ),
                new OP_INFO( "11", "adc", new OP_TYPES[]{ r_m16_32, r16_32 },               "Add with Carry" ),
                new OP_INFO( "12", "adc", new OP_TYPES[]{ r8, r_m8 },                       "Add with Carry" ),
                new OP_INFO( "13", "adc", new OP_TYPES[]{ r16_32, r_m16_32 },               "Add with Carry" ),
                new OP_INFO( "14", "adc", new OP_TYPES[]{ AL, imm8 },                       "Add with Carry" ),
                new OP_INFO( "15", "adc", new OP_TYPES[]{ EAX, imm16_32 },                  "Add with Carry" ),
                new OP_INFO( "16", "push", new OP_TYPES[]{ SS },                            "Push Stack Segment onto the stack" ),
                new OP_INFO( "17", "pop", new OP_TYPES[]{ SS },                             "Pop Stack Segment off of the stack" ),
                new OP_INFO( "18", "sbb", new OP_TYPES[]{ r_m8, r8 },                       "Integer Subtraction with Borrow" ),
                new OP_INFO( "19", "sbb", new OP_TYPES[]{ r_m16_32, r16_32 },               "Integer Subtraction with Borrow" ),
                new OP_INFO( "1A", "sbb", new OP_TYPES[]{ r8, r_m8 },                       "Integer Subtraction with Borrow" ),
                new OP_INFO( "1B", "sbb", new OP_TYPES[]{ r16_32, r_m16_32 },               "Integer Subtraction with Borrow" ),
                new OP_INFO( "1C", "sbb", new OP_TYPES[]{ AL, imm8 },                       "Integer Subtraction with Borrow" ),
                new OP_INFO( "1D", "sbb", new OP_TYPES[]{ EAX, imm16_32 },                  "Integer Subtraction with Borrow" ),
                new OP_INFO( "1E", "push", new OP_TYPES[]{ DS },                            "Push Data Segment onto the stack" ),
                new OP_INFO( "1F", "pop", new OP_TYPES[]{ DS },                             "Pop Data Segment off of the stack" ),
                new OP_INFO( "20", "and", new OP_TYPES[]{ r_m8, r8 },                       "Logical AND" ),
                new OP_INFO( "21", "and", new OP_TYPES[]{ r_m16_32, r16_32 },               "Logical AND" ),
                new OP_INFO( "22", "and", new OP_TYPES[]{ r8, r_m8 },                       "Logical AND" ),
                new OP_INFO( "23", "and", new OP_TYPES[]{ r16_32, r_m16_32 },               "Logical AND" ),
                new OP_INFO( "24", "and", new OP_TYPES[]{ AL, imm8 },                       "Logical AND" ),
                new OP_INFO( "25", "and", new OP_TYPES[]{ EAX, imm16_32 },                  "Logical AND" ),
                new OP_INFO( "27", "daa", new OP_TYPES[]{ AL },                             "Decimal Adjust AL after Addition" ),
                new OP_INFO( "28", "sub", new OP_TYPES[]{ r_m8, r8 },                       "Subtract" ),
                new OP_INFO( "29", "sub", new OP_TYPES[]{ r_m16_32, r16_32 },               "Subtract" ),
                new OP_INFO( "2A", "sub", new OP_TYPES[]{ r8, r_m8 },                       "Subtract" ),
                new OP_INFO( "2B", "sub", new OP_TYPES[]{ r16_32, r_m16_32 },               "Subtract" ),
                new OP_INFO( "2C", "sub", new OP_TYPES[]{ AL, imm8 },                       "Subtract" ),
                new OP_INFO( "2D", "sub", new OP_TYPES[]{ EAX, imm16_32 },                  "Subtract" ),
                new OP_INFO( "2F", "das", new OP_TYPES[]{ AL },                             "Decimal Adjust AL after Subtraction" ),
                new OP_INFO( "30", "xor", new OP_TYPES[]{ r_m8, r8 },                       "Logical Exclusive OR" ),
                new OP_INFO( "31", "xor", new OP_TYPES[]{ r_m16_32, r16_32 },               "Logical Exclusive OR" ),
                new OP_INFO( "32", "xor", new OP_TYPES[]{ r8, r_m8 },                       "Logical Exclusive OR" ),
                new OP_INFO( "33", "xor", new OP_TYPES[]{ r16_32, r_m16_32 },               "Logical Exclusive OR" ),
                new OP_INFO( "34", "xor", new OP_TYPES[]{ AL, imm8 },                       "Logical Exclusive OR" ),
                new OP_INFO( "35", "xor", new OP_TYPES[]{ EAX, imm16_32 },                  "Logical Exclusive OR" ),
                new OP_INFO( "37", "aaa", new OP_TYPES[]{ AL, AH },                         "ASCII Adjust After Addition" ),
                new OP_INFO( "38", "cmp", new OP_TYPES[]{ r_m8, r8 },                       "Compare Two Operands" ),
                new OP_INFO( "39", "cmp", new OP_TYPES[]{ r_m16_32, r16_32 },               "Compare Two Operands" ),
                new OP_INFO( "3A", "cmp", new OP_TYPES[]{ r8, r_m8 },                       "Compare Two Operands" ),
                new OP_INFO( "3B", "cmp", new OP_TYPES[]{ r16_32, r_m16_32 },               "Compare Two Operands" ),
                new OP_INFO( "3C", "cmp", new OP_TYPES[]{ AL, imm8 },                       "Compare Two Operands" ),
                new OP_INFO( "3D", "cmp", new OP_TYPES[]{ EAX, imm16_32 },                  "Compare Two Operands" ),
                new OP_INFO( "3F", "aas", new OP_TYPES[]{ AL, AH },                         "ASCII Adjust AL After Subtraction" ),
                new OP_INFO( "40+r", "inc", new OP_TYPES[]{ r16_32 },                       "Increment by 1" ),
                new OP_INFO( "48+r", "dec", new OP_TYPES[]{ r16_32 },                       "Decrement by 1" ),
                new OP_INFO( "50+r", "push", new OP_TYPES[]{ r16_32 },                      "Push Word, Doubleword or Quadword Onto the Stack" ),
                new OP_INFO( "58+r", "pop", new OP_TYPES[]{ r16_32 },                       "Pop a Value from the Stack" ),
                new OP_INFO( "60", "pushad", new OP_TYPES[]{  },                            "Push All General-Purpose Registers" ),
                new OP_INFO( "61", "popad", new OP_TYPES[]{  },                             "Pop All General-Purpose Registers" ),
                new OP_INFO( "62", "bound", new OP_TYPES[]{ r16_32, m16_32_and_16_32 },     "Check Array Index Against Bounds" ),
                new OP_INFO( "63", "arpl", new OP_TYPES[]{ r_m16, r16 },                    "Adjust RPL Field of Segment Selector" ),
                new OP_INFO( "63", "arpl", new OP_TYPES[]{ r_m16, r16 },                    "Adjust RPL Field of Segment Selector" ),
                new OP_INFO( "68", "push", new OP_TYPES[]{ imm16_32 },                      "Push Word, Doubleword or Quadword Onto the Stack" ),
                new OP_INFO( "69", "imul", new OP_TYPES[]{ r16_32, r_m16_32, imm16_32 },    "Signed Multiply" ),
                new OP_INFO( "6A", "push", new OP_TYPES[]{ imm8 },                          "Push Word, Doubleword or Quadword Onto the Stack" ),
                new OP_INFO( "6B", "imul", new OP_TYPES[]{ r16_32, r_m16_32, imm8 },        "Signed Multiply" ),
                new OP_INFO( "6C", "insb", new OP_TYPES[]{  },                              "Input from Port to String" ),
                new OP_INFO( "6D", "insd", new OP_TYPES[]{  },                              "Input from Port to String" ),
                new OP_INFO( "6E", "outsb", new OP_TYPES[]{  },                             "Output String to Port" ),
                new OP_INFO( "6F", "outsd", new OP_TYPES[]{  },                             "Output String to Port" ),
                new OP_INFO( "70", "jo short", new OP_TYPES[]{ rel8 },                      "Jump short if overflow (OF=1)" ),
                new OP_INFO( "71", "jno short", new OP_TYPES[]{ rel8 },                     "Jump short if not overflow (OF=0))" ),
                new OP_INFO( "72", "jb short", new OP_TYPES[]{ rel8 },                      "Jump short if below/not above or equal/carry (CF=1)" ),
                new OP_INFO( "73", "jae short", new OP_TYPES[]{ rel8 },                     "Jump short if not below/above or equal/not carry (CF=0))" ),
                new OP_INFO( "74", "je short", new OP_TYPES[]{ rel8 },                      "Jump short if zero/equal (ZF=1)" ),
                new OP_INFO( "75", "jne short", new OP_TYPES[]{ rel8 },                     "Jump short if not zero/not equal (ZF=0)" ),
                new OP_INFO( "76", "jna short", new OP_TYPES[]{ rel8 },                     "Jump short if below or equal/not above (CF=1 OR ZF=1)" ),
                new OP_INFO( "77", "ja short", new OP_TYPES[]{ rel8 },                      "Jump short if not below or equal/above (CF=0 AND ZF=0)" ),
                new OP_INFO( "78", "js short", new OP_TYPES[]{ rel8 },                      "Jump short if sign (SF=1)" ),
                new OP_INFO( "79", "jns short", new OP_TYPES[]{ rel8 },                     "Jump short if not sign (SF=0)" ),
                new OP_INFO( "7A", "jp short", new OP_TYPES[]{ rel8 },                      "Jump short if parity/parity even (PF=1)" ),
                new OP_INFO( "7B", "jnp short", new OP_TYPES[]{ rel8 },                     "Jump short if not parity/parity odd (PF=0)" ),
                new OP_INFO( "7C", "jl short", new OP_TYPES[]{ rel8 },                      "Jump short if less/not greater (SF!=OF)" ),
                new OP_INFO( "7D", "jge short", new OP_TYPES[]{ rel8 },                     "Jump short if not less/greater or equal (SF=OF)" ),
                new OP_INFO( "7E", "jle short", new OP_TYPES[]{ rel8 },                     "Jump short if less or equal/not greater ((ZF=1) OR (SF!=OF))" ),
                new OP_INFO( "7F", "jg short", new OP_TYPES[]{ rel8 },                      "Jump short if not less nor equal/greater ((ZF=0) AND (SF=OF))" ),
                new OP_INFO( "80+m0", "add", new OP_TYPES[]{ r_m8, imm8 },                  "Add" ),
                new OP_INFO( "80+m1", "or", new OP_TYPES[] { r_m8, imm8 },                  "Logical Inclusive OR" ),
                new OP_INFO( "80+m2", "adc", new OP_TYPES[]{ r_m8, imm8 },                  "Add with Carry" ),
                new OP_INFO( "80+m3", "sbb", new OP_TYPES[]{ r_m8, imm8 },                  "Integer Subtraction with Borrow" ),
                new OP_INFO( "80+m4", "and", new OP_TYPES[]{ r_m8, imm8 },                  "Logical AND" ),
                new OP_INFO( "80+m5", "sub", new OP_TYPES[]{ r_m8, imm8 },                  "Subtract" ),
                new OP_INFO( "80+m6", "xor", new OP_TYPES[]{ r_m8, imm8 },                  "Logical Exclusive OR" ),
                new OP_INFO( "80+m7", "cmp", new OP_TYPES[]{ r_m8, imm8 },                  "Compare Two Operands" ),
                new OP_INFO( "81+m0", "add", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Add" ),
                new OP_INFO( "81+m1", "or", new OP_TYPES[]{ r_m16_32, imm16_32 },           "Logical Inclusive OR" ),
                new OP_INFO( "81+m2", "adc", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Add with Carry" ),
                new OP_INFO( "81+m3", "sbb", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Integer Subtraction with Borrow" ),
                new OP_INFO( "81+m4", "and", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Logical AND" ),
                new OP_INFO( "81+m5", "sub", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Subtract" ),
                new OP_INFO( "81+m6", "xor", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Logical Exclusive OR" ),
                new OP_INFO( "81+m7", "cmp", new OP_TYPES[]{ r_m16_32, imm16_32 },          "Compare Two Operands" ),
                new OP_INFO( "82+m0", "add", new OP_TYPES[]{ r_m8, imm8 },                  "Add" ),
                new OP_INFO( "82+m1", "or", new OP_TYPES[]{ r_m8, imm8 },                   "Logical Inclusive OR" ),
                new OP_INFO( "82+m2", "adc", new OP_TYPES[]{ r_m8, imm8 },                  "Add with Carry" ),
                new OP_INFO( "82+m3", "sbb", new OP_TYPES[]{ r_m8, imm8 },                  "Integer Subtraction with Borrow" ),
                new OP_INFO( "82+m4", "and", new OP_TYPES[]{ r_m8, imm8 },                  "Logical AND" ),
                new OP_INFO( "82+m5", "sub", new OP_TYPES[]{ r_m8, imm8 },                  "Subtract" ),
                new OP_INFO( "82+m6", "xor", new OP_TYPES[]{ r_m8, imm8 },                  "Logical Exclusive OR" ),
                new OP_INFO( "82+m7", "cmp", new OP_TYPES[]{ r_m8, imm8 },                  "Compare Two Operands" ),
                new OP_INFO( "83+m0", "add", new OP_TYPES[]{ r_m16_32, imm8 },              "Add" ),
                new OP_INFO( "83+m1", "or", new OP_TYPES[]{ r_m16_32, imm8 },               "Logical Inclusive OR" ),
                new OP_INFO( "83+m2", "adc", new OP_TYPES[]{ r_m16_32, imm8 },              "Add with Carry" ),
                new OP_INFO( "83+m3", "sbb", new OP_TYPES[]{ r_m16_32, imm8 },              "Integer Subtraction with Borrow" ),
                new OP_INFO( "83+m4", "and", new OP_TYPES[]{ r_m16_32, imm8 },              "Logical AND" ),
                new OP_INFO( "83+m5", "sub", new OP_TYPES[]{ r_m16_32, imm8 },              "Subtract" ),
                new OP_INFO( "83+m6", "xor", new OP_TYPES[]{ r_m16_32, imm8 },              "Logical Exclusive OR" ),
                new OP_INFO( "83+m7", "cmp", new OP_TYPES[]{ r_m16_32, imm8 },              "Compare Two Operands" ),
                new OP_INFO( "84", "test", new OP_TYPES[]{ r_m8, r8 },                      "Logical Compare" ),
                new OP_INFO( "85", "test", new OP_TYPES[]{ r_m16_32, r16_32 },              "Logical Compare" ),
                new OP_INFO( "86", "xchg", new OP_TYPES[]{ r_m8, r8 },                      "Exchange Register/Memory with Register" ),
                new OP_INFO( "87", "xchg", new OP_TYPES[]{ r_m16_32, r16_32 },              "Exchange Register/Memory with Register" ),
                new OP_INFO( "88", "mov", new OP_TYPES[]{ r_m8, r8 },                       "Move" ),
                new OP_INFO( "89", "mov", new OP_TYPES[]{ r_m16_32, r16_32 },               "Move" ),
                new OP_INFO( "8A", "mov", new OP_TYPES[]{ r8, r_m8 },                       "Move" ),
                new OP_INFO( "8B", "mov", new OP_TYPES[]{ r16_32, r_m16_32 },               "Move" ),
                new OP_INFO( "8C", "mov", new OP_TYPES[]{ m16, Sreg },                      "Move" ),
                new OP_INFO( "8D", "lea", new OP_TYPES[]{ r16_32, m32 },                    "Load Effective Address" ),
                new OP_INFO( "8E", "mov", new OP_TYPES[]{ Sreg, r_m16 },                    "Move" ),
                new OP_INFO( "8F", "pop", new OP_TYPES[]{ r_m16_32 },                       "Pop a Value from the Stack" ),
                new OP_INFO( "90", "nop", new OP_TYPES[]{  },                               "No Operation" ),
                new OP_INFO( "90+r", "xchg", new OP_TYPES[]{ EAX, r16_32 },                 "Exchange Register/Memory with Register" ),
                new OP_INFO( "98", "cbw", new OP_TYPES[]{ AX, AL },                         "Convert Byte to Word" ),
                new OP_INFO( "99", "cwd", new OP_TYPES[]{ AX, AL },                         "Convert Doubleword to Quadword" ),
                new OP_INFO( "9A", "callf", new OP_TYPES[]{ ptr16_32 },                     "Call Procedure" ),
                new OP_INFO( "9B", "fwait", new OP_TYPES[]{  },                             "Check pending unmasked floating-point exceptions" ),
                new OP_INFO( "9C", "pushfd", new OP_TYPES[]{  },                            "Push EFLAGS Register onto the Stack" ),
                new OP_INFO( "9D", "popfd", new OP_TYPES[]{  },                             "Pop Stack into EFLAGS Register" ),
                new OP_INFO( "9E", "sahf", new OP_TYPES[]{ AH },                            "Store AH into Flags" ),
                new OP_INFO( "9F", "lahf", new OP_TYPES[]{ AH },                            "Load Status Flags into AH Register" ),
                new OP_INFO( "A0", "mov", new OP_TYPES[]{ AL, moffs8 },                     "Move" ),
                new OP_INFO( "A1", "mov", new OP_TYPES[]{ EAX, moffs16_32 },                "Move" ),
                new OP_INFO( "A2", "mov", new OP_TYPES[]{ moffs8, AL },                     "Move" ),
                new OP_INFO( "A3", "mov", new OP_TYPES[]{ moffs16_32, EAX },                "Move" ),
                new OP_INFO( "A4", "movsb", new OP_TYPES[]{  },                             "Move Data from String to String" ),
                new OP_INFO( "A5", "movsw", new OP_TYPES[]{  },                             "Move Data from String to String" ),
                new OP_INFO( "A6", "cmpsb", new OP_TYPES[]{  },                             "Compare String Operands" ),
                new OP_INFO( "A7", "cmpsw", new OP_TYPES[]{  },                             "Compare String Operands" ),
                new OP_INFO( "A8", "test", new OP_TYPES[]{ AL, imm8 },                      "Logical Compare" ),
                new OP_INFO( "A9", "test", new OP_TYPES[]{ EAX, imm16_32 },                 "Logical Compare" ),
                new OP_INFO( "AA", "stosb", new OP_TYPES[]{  },                             "Store String" ),
                new OP_INFO( "AB", "stosw", new OP_TYPES[]{  },                             "Store String" ),
                new OP_INFO( "AC", "lodsb", new OP_TYPES[]{  },                             "Load String" ),
                new OP_INFO( "AD", "lodsw", new OP_TYPES[]{  },                             "Load String" ),
                new OP_INFO( "AE", "scasb", new OP_TYPES[]{  },                             "Scan String" ),
                new OP_INFO( "AF", "scasw", new OP_TYPES[]{  },                             "Scan String" ),
                new OP_INFO( "B0+r", "mov", new OP_TYPES[]{ r8, imm8 },                     "Move" ),
                new OP_INFO( "B8+r", "mov", new OP_TYPES[]{ r16_32, imm16_32 },             "Move" ),
                new OP_INFO( "C0+m0", "rol", new OP_TYPES[]{ r_m8, imm8 },                  "Rotate" ),
                new OP_INFO( "C0+m1", "ror", new OP_TYPES[]{ r_m8, imm8 },                  "Rotate" ),
                new OP_INFO( "C0+m2", "rcl", new OP_TYPES[]{ r_m8, imm8 },                  "Rotate" ),
                new OP_INFO( "C0+m3", "rcr", new OP_TYPES[]{ r_m8, imm8 },                  "Rotate" ),
                new OP_INFO( "C0+m4", "shl", new OP_TYPES[]{ r_m8, imm8 },                  "Shift" ),
                new OP_INFO( "C0+m5", "shr", new OP_TYPES[]{ r_m8, imm8 },                  "Shift" ),
                new OP_INFO( "C0+m6", "sal", new OP_TYPES[]{ r_m8, imm8 },                  "Shift" ),
                new OP_INFO( "C0+m7", "sar", new OP_TYPES[]{ r_m8, imm8 },                  "Shift" ),
                new OP_INFO( "C1+m0", "rol", new OP_TYPES[]{ r_m16_32, imm8 },              "Rotate" ),
                new OP_INFO( "C1+m1", "ror", new OP_TYPES[]{ r_m16_32, imm8 },              "Rotate" ),
                new OP_INFO( "C1+m2", "rcl", new OP_TYPES[]{ r_m16_32, imm8 },              "Rotate" ),
                new OP_INFO( "C1+m3", "rcr", new OP_TYPES[]{ r_m16_32, imm8 },              "Rotate" ),
                new OP_INFO( "C1+m4", "shl", new OP_TYPES[]{ r_m16_32, imm8 },              "Shift" ),
                new OP_INFO( "C1+m5", "shr", new OP_TYPES[]{ r_m16_32, imm8 },              "Shift" ),
                new OP_INFO( "C1+m6", "sal", new OP_TYPES[]{ r_m16_32, imm8 },              "Shift" ),
                new OP_INFO( "C1+m7", "sar", new OP_TYPES[]{ r_m16_32, imm8 },              "Shift" ),
                new OP_INFO( "C2", "ret", new OP_TYPES[]{ imm16 },                          "Return from procedure" ),
                new OP_INFO( "C3", "retn", new OP_TYPES[]{  },                              "Return from procedure" ),
                new OP_INFO( "C4", "les", new OP_TYPES[]{ ES, r16_32, m16_32_and_16_32 },   "Load Far Pointer" ),
                new OP_INFO( "C5", "lds", new OP_TYPES[]{ DS, r16_32, m16_32_and_16_32 },   "Load Far Pointer" ),
                new OP_INFO( "C6", "mov", new OP_TYPES[]{ r_m8, imm8 },                     "Move" ),
                new OP_INFO( "C7", "mov", new OP_TYPES[]{ r_m16_32, imm16_32 },             "Move" ),
                new OP_INFO( "66+C7", "mov", new OP_TYPES[]{ r_m16_32, imm16 },             "Move" ),
                new OP_INFO( "C8", "enter", new OP_TYPES[]{ EBP, imm16, imm8 },             "Make Stack Frame for Procedure Parameters" ),
                new OP_INFO( "C9", "leave", new OP_TYPES[]{ EBP },                          "High Level Procedure Exit" ),
                new OP_INFO( "CA", "retf", new OP_TYPES[]{ imm16 },                         "Return from procedure" ),
                new OP_INFO( "CB", "retf", new OP_TYPES[]{  },                              "Return from procedure" ),
                new OP_INFO( "CC", "int 3", new OP_TYPES[]{  },                             "Call to Interrupt Procedure" ),
                new OP_INFO( "CD", "int", new OP_TYPES[]{ imm8 },                           "Call to Interrupt Procedure" ),
                new OP_INFO( "CE", "into", new OP_TYPES[]{  },                              "Call to Interrupt Procedure" ),
                new OP_INFO( "CF", "iretd", new OP_TYPES[]{  },                             "Interrupt Return" ),
                new OP_INFO( "D0+m0", "rol", new OP_TYPES[]{ r_m8, one },                   "Rotate" ),
                new OP_INFO( "D0+m1", "ror", new OP_TYPES[]{ r_m8, one },                   "Rotate" ),
                new OP_INFO( "D0+m2", "rcl", new OP_TYPES[]{ r_m8, one },                   "Rotate" ),
                new OP_INFO( "D0+m3", "rcr", new OP_TYPES[]{ r_m8, one },                   "Rotate" ),
                new OP_INFO( "D0+m4", "shl", new OP_TYPES[]{ r_m8, one },                   "Shift" ),
                new OP_INFO( "D0+m5", "shr", new OP_TYPES[]{ r_m8, one },                   "Shift" ),
                new OP_INFO( "D0+m6", "shl", new OP_TYPES[]{ r_m8, one },                   "Shift" ),
                new OP_INFO( "D0+m7", "shr", new OP_TYPES[]{ r_m8, one },                   "Shift" ),
                new OP_INFO( "D1+m0", "rol", new OP_TYPES[]{ r_m16_32, one },               "Rotate" ),
                new OP_INFO( "D1+m1", "ror", new OP_TYPES[]{ r_m16_32, one },               "Rotate" ),
                new OP_INFO( "D1+m2", "rcl", new OP_TYPES[]{ r_m16_32, one },               "Rotate" ),
                new OP_INFO( "D1+m3", "rcr", new OP_TYPES[]{ r_m16_32, one },               "Rotate" ),
                new OP_INFO( "D1+m4", "shl", new OP_TYPES[]{ r_m16_32, one },               "Shift" ),
                new OP_INFO( "D1+m5", "shr", new OP_TYPES[]{ r_m16_32, one },               "Shift" ),
                new OP_INFO( "D1+m6", "shl", new OP_TYPES[]{ r_m16_32, one },               "Shift" ),
                new OP_INFO( "D1+m7", "shr", new OP_TYPES[]{ r_m16_32, one },               "Shift" ),
                new OP_INFO( "D2+m0", "rol", new OP_TYPES[]{ r_m8, CL },                    "Rotate" ),
                new OP_INFO( "D2+m1", "ror", new OP_TYPES[]{ r_m8, CL },                    "Rotate" ),
                new OP_INFO( "D2+m2", "rcl", new OP_TYPES[]{ r_m8, CL },                    "Rotate" ),
                new OP_INFO( "D2+m3", "rcr", new OP_TYPES[]{ r_m8, CL },                    "Rotate" ),
                new OP_INFO( "D2+m4", "shl", new OP_TYPES[]{ r_m8, CL },                    "Shift" ),
                new OP_INFO( "D2+m5", "shr", new OP_TYPES[]{ r_m8, CL },                    "Shift" ),
                new OP_INFO( "D2+m6", "shl", new OP_TYPES[]{ r_m8, CL },                    "Shift" ),
                new OP_INFO( "D2+m7", "shr", new OP_TYPES[]{ r_m8, CL },                    "Shift" ),
                new OP_INFO( "D3+m0", "rol", new OP_TYPES[]{ r_m16_32, CL },                "Rotate" ),
                new OP_INFO( "D3+m1", "ror", new OP_TYPES[]{ r_m16_32, CL },                "Rotate" ),
                new OP_INFO( "D3+m2", "rcl", new OP_TYPES[]{ r_m16_32, CL },                "Rotate" ),
                new OP_INFO( "D3+m3", "rcr", new OP_TYPES[]{ r_m16_32, CL },                "Rotate" ),
                new OP_INFO( "D3+m4", "shl", new OP_TYPES[]{ r_m16_32, CL },                "Shift" ),
                new OP_INFO( "D3+m5", "shr", new OP_TYPES[]{ r_m16_32, CL },                "Shift" ),
                new OP_INFO( "D3+m6", "shl", new OP_TYPES[]{ r_m16_32, CL },                "Shift" ),
                new OP_INFO( "D3+m7", "shr", new OP_TYPES[]{ r_m16_32, CL },                "Shift" ),
                new OP_INFO( "D4", "aam", new OP_TYPES[]{ AL, AH, imm8 },                   "ASCII Adjust AX After Multiply" ),
                new OP_INFO( "D5", "aad", new OP_TYPES[]{ AL, AH, imm8 },                   "ASCII Adjust AX Before Division" ),
                new OP_INFO( "D6", "setalc", new OP_TYPES[]{ AL },                          "Set AL If Carry" ),
                new OP_INFO( "D7", "xlatb", new OP_TYPES[]{ AL },                           "Table Look-up Translation" ),
                new OP_INFO( "D8+m8", "fadd", new OP_TYPES[]{ ST, STi },                    "Add" ),
                new OP_INFO( "D8+m9", "fmul", new OP_TYPES[]{ ST, STi },                    "Multiply" ),
                new OP_INFO( "D8+mA", "fcom", new OP_TYPES[]{ ST, STi },                    "Compare Real" ),
                new OP_INFO( "D8+mB", "fcomp", new OP_TYPES[]{ ST, STi },                   "Compare Real and Pop" ),
                new OP_INFO( "D8+mC", "fsub", new OP_TYPES[]{ ST, STi },                    "Subtract" ),
                new OP_INFO( "D8+mD", "fsubr", new OP_TYPES[]{ ST, STi },                   "Reverse Subtract" ),
                new OP_INFO( "D8+mE", "fdiv", new OP_TYPES[]{ ST, STi },                    "Divide" ),
                new OP_INFO( "D8+mF", "fdivr", new OP_TYPES[]{ ST, STi },                   "Reverse Divide" ),
                new OP_INFO( "D8+m0", "fadd", new OP_TYPES[]{ STi },                        "Add" ),
                new OP_INFO( "D8+m1", "fmul", new OP_TYPES[]{ STi },                        "Multiply" ),
                new OP_INFO( "D8+m2", "fcom", new OP_TYPES[]{ STi },                        "Compare Real" ),
                new OP_INFO( "D8+m3", "fcomp", new OP_TYPES[]{ STi },                       "Compare Real and Pop" ),
                new OP_INFO( "D8+m4", "fsub", new OP_TYPES[]{ STi },                        "Subtract" ),
                new OP_INFO( "D8+m5", "fsubr", new OP_TYPES[]{ STi },                       "Reverse Subtract" ),
                new OP_INFO( "D8+m6", "fdiv", new OP_TYPES[]{ STi },                        "Divide" ),
                new OP_INFO( "D8+m7", "fdivr", new OP_TYPES[]{ STi },                       "Reverse Divide" ),
                new OP_INFO( "D9+m0", "fld", new OP_TYPES[]{ STi },                         "Load Floating Point Value" ),
                new OP_INFO( "D9+m1", "fxch", new OP_TYPES[]{ STi },                        "Exchange Register Contents" ),
                new OP_INFO( "D9+m2", "fst", new OP_TYPES[]{ STi },                         "Store Floating Point Value" ),
                new OP_INFO( "D9+m3", "fstp", new OP_TYPES[]{ STi },                        "Store Floating Point Value and Pop" ),
                new OP_INFO( "D9+m4", "fldenv", new OP_TYPES[]{ STi },                      "Load x87 FPU Environment" ),
                new OP_INFO( "D9+m5", "fldcw", new OP_TYPES[]{ STi },                       "Load x87 FPU Control Word" ),
                new OP_INFO( "D9+m6", "fnstenv", new OP_TYPES[]{ STi },                     "Store x87 FPU Environment" ),
                new OP_INFO( "D9+m7", "fnstcw", new OP_TYPES[]{ STi },                      "Store x87 FPU Control Word" ),
                new OP_INFO( "DA+m8", "fcmovb", new OP_TYPES[]{ ST, STi },                  "FP Conditional Move - below (CF=1)" ),
                new OP_INFO( "DA+m9", "fcmove", new OP_TYPES[]{ ST, STi },                  "FP Conditional Move - equal (ZF=1)" ),
                new OP_INFO( "DA+mA", "fcmovbe", new OP_TYPES[]{ ST, STi },                 "FP Conditional Move - below or equal (CF=1 or ZF=1)" ),
                new OP_INFO( "DA+mB", "fcmovu", new OP_TYPES[]{ ST, STi },                  "FP Conditional Move - unordered (PF=1)" ),
                new OP_INFO( "DA+mC", "fisub", new OP_TYPES[]{ ST, STi },                   "Subtract" ),
                new OP_INFO( "DA+mD", "fisubr", new OP_TYPES[]{ ST, STi },                  "Reverse Subtract" ),
                new OP_INFO( "DA+mE", "fidiv", new OP_TYPES[]{ ST, STi },                   "Divide" ),
                new OP_INFO( "DA+mF", "fidivr", new OP_TYPES[]{ ST, STi },                  "Reverse Divide" ),
                new OP_INFO( "DA+m0", "fiadd", new OP_TYPES[]{ STi },                       "Add" ),
                new OP_INFO( "DA+m1", "fimul", new OP_TYPES[]{ STi },                       "Multiply" ),
                new OP_INFO( "DA+m2", "ficom", new OP_TYPES[]{ STi },                       "Compare Real" ),
                new OP_INFO( "DA+m3", "ficomp", new OP_TYPES[]{ STi },                      "Compare Real and Pop" ),
                new OP_INFO( "DA+m4", "fisub", new OP_TYPES[]{ STi },                       "Subtract" ),
                new OP_INFO( "DA+m5", "fisubr", new OP_TYPES[]{ STi },                      "Reverse Subtract" ),
                new OP_INFO( "DA+m6", "fidiv", new OP_TYPES[]{ STi },                       "Divide" ),
                new OP_INFO( "DA+m7", "fidivr", new OP_TYPES[]{ STi },                      "Reverse Divide" ),
                new OP_INFO( "DB+m8", "fcmovnb", new OP_TYPES[]{ ST, STi },                 "FP Conditional Move - not below (CF=0)" ),
                new OP_INFO( "DB+m9", "fcmovne", new OP_TYPES[]{ ST, STi },                 "FP Conditional Move - not equal (ZF=0)" ),
                new OP_INFO( "DB+mA", "fcmovnbe", new OP_TYPES[]{ ST, STi },                "FP Conditional Move - below or equal (CF=0 and ZF=0)" ),
                new OP_INFO( "DB+mB", "fcmovnu", new OP_TYPES[]{ ST, STi },                 "FP Conditional Move - not unordered (PF=0)" ),
                new OP_INFO( "DB+m0", "fild", new OP_TYPES[]{ STi },                        "Load Integer" ),
                new OP_INFO( "DB+m1", "fisttp", new OP_TYPES[]{ STi },                      "Store Integer with Truncation and Pop" ),
                new OP_INFO( "DB+m2", "fist", new OP_TYPES[]{ STi },                        "Store Integer" ),
                new OP_INFO( "DB+m3", "fistp", new OP_TYPES[]{ STi },                       "Store Integer and Pop" ),
                new OP_INFO( "DB+m4", "finit", new OP_TYPES[]{ STi },                       "Initialize Floating-Point Unit" ),
                new OP_INFO( "DB+m5", "fucomi", new OP_TYPES[]{ STi },                      "Unordered Compare Floating Point Values and Set EFLAGS" ),
                new OP_INFO( "DB+m6", "fcomi", new OP_TYPES[]{ STi },                       "Compare Floating Point Values and Set EFLAGS" ),
                new OP_INFO( "DB+m7", "fstp", new OP_TYPES[]{ STi },                        "Store Floating Point Value and Pop" ),
                new OP_INFO( "DC+m8", "fadd", new OP_TYPES[]{ STi, ST },                    "Add" ),
                new OP_INFO( "DC+m9", "fmul", new OP_TYPES[]{ STi, ST },                    "Multiply" ),
                new OP_INFO( "DC+mA", "fcom", new OP_TYPES[]{ STi, ST },                    "Compare Real" ),
                new OP_INFO( "DC+mB", "fcomp", new OP_TYPES[]{ STi, ST },                   "Compare Real and Pop" ),
                new OP_INFO( "DC+mC", "fsub", new OP_TYPES[]{ STi, ST },                    "Subtract" ),
                new OP_INFO( "DC+mD", "fsubr", new OP_TYPES[]{ STi, ST },                   "Reverse Subtract" ),
                new OP_INFO( "DC+mE", "fdiv", new OP_TYPES[]{ STi, ST },                    "Divide" ),
                new OP_INFO( "DC+mF", "fdivr", new OP_TYPES[]{ STi, ST },                   "Reverse Divide" ),
                new OP_INFO( "DC+m0", "fadd", new OP_TYPES[]{ STi },                        "Add" ),
                new OP_INFO( "DC+m1", "fmul", new OP_TYPES[]{ STi },                        "Multiply" ),
                new OP_INFO( "DC+m2", "fcom", new OP_TYPES[]{ STi },                        "Compare Real" ),
                new OP_INFO( "DC+m3", "fcomp", new OP_TYPES[]{ STi },                       "Compare Real and Pop" ),
                new OP_INFO( "DC+m4", "fsub", new OP_TYPES[]{ STi },                        "Subtract" ),
                new OP_INFO( "DC+m5", "fsubr", new OP_TYPES[]{ STi },                       "Reverse Subtract" ),
                new OP_INFO( "DC+m6", "fdiv", new OP_TYPES[]{ STi },                        "Divide" ),
                new OP_INFO( "DC+m7", "fdivr", new OP_TYPES[]{ STi },                       "Reverse Divide" ),
                new OP_INFO( "DD+m8", "ffree", new OP_TYPES[]{ STi },                       "Free Floating-Point Register" ),
                new OP_INFO( "DD+m0", "fld", new OP_TYPES[]{ STi },                         "Load Floating Point Value" ),
                new OP_INFO( "DD+m1", "fisttp", new OP_TYPES[]{ STi },                      "Store Integer with Truncation and Pop" ),
                new OP_INFO( "DD+m2", "fst", new OP_TYPES[]{ STi },                         "Store Floating Point Value" ),
                new OP_INFO( "DD+m3", "fstp", new OP_TYPES[]{ STi },                        "Store Floating Point Value and Pop" ),
                new OP_INFO( "DD+m4", "frstor", new OP_TYPES[]{ STi },                      "Restore x87 FPU State" ),
                new OP_INFO( "DD+m5", "fucomp", new OP_TYPES[]{ STi },                      "Unordered Compare Floating Point Values and Pop" ),
                new OP_INFO( "DD+m6", "fnsave", new OP_TYPES[]{ STi },                      "Store x87 FPU State" ),
                new OP_INFO( "DD+m7", "fnstsw", new OP_TYPES[]{ STi },                      "Store x87 FPU Status Word" ),
                new OP_INFO( "DE+m8", "faddp", new OP_TYPES[]{ ST, STi },                   "Add and Pop" ),
                new OP_INFO( "DE+m9", "fmulp", new OP_TYPES[]{ ST, STi },                   "Multiply and Pop" ),
                new OP_INFO( "DE+mA", "ficom", new OP_TYPES[]{ ST, STi },                   "Compare Real" ),
                new OP_INFO( "DE+mB", "ficomp", new OP_TYPES[]{ ST, STi },                  "Compare Real and Pop" ),
                new OP_INFO( "DE+mC", "fsubrp", new OP_TYPES[]{ ST, STi },                  "Reverse Subtract and Pop" ),
                new OP_INFO( "DE+mD", "fsubp", new OP_TYPES[]{ ST, STi },                   "Subtract and Pop" ),
                new OP_INFO( "DE+mE", "fdivrp", new OP_TYPES[]{ ST, STi },                  "Reverse Divide and Pop" ),
                new OP_INFO( "DE+mF", "fdivp", new OP_TYPES[]{ ST, STi },                   "Divide and Pop" ),
                new OP_INFO( "DE+m0", "fiadd", new OP_TYPES[]{ STi },                       "Add" ),
                new OP_INFO( "DE+m1", "fimul", new OP_TYPES[]{ STi },                       "Multiply" ),
                new OP_INFO( "DE+m2", "ficom", new OP_TYPES[]{ STi },                       "Compare Real" ),
                new OP_INFO( "DE+m3", "ficomp", new OP_TYPES[]{ STi },                      "Compare Real and Pop" ),
                new OP_INFO( "DE+m4", "fisub", new OP_TYPES[]{ STi },                       "Subtract" ),
                new OP_INFO( "DE+m5", "fisubr", new OP_TYPES[]{ STi },                      "Reverse Subtract" ),
                new OP_INFO( "DE+m6", "fidiv", new OP_TYPES[]{ STi },                       "Divide" ),
                new OP_INFO( "DE+m7", "fdivr", new OP_TYPES[]{ STi },                       "Reverse Divide" ),
                new OP_INFO( "DF+m8", "ffreep", new OP_TYPES[]{ STi },                      "Free Floating-Point Register and Pop" ),
                new OP_INFO( "DF+m9", "fisttp", new OP_TYPES[]{ r32 },                      "Store Integer with Truncation and Pop" ),
                new OP_INFO( "DF+mA", "fist", new OP_TYPES[]{ STi },                        "Store Integer" ),
                new OP_INFO( "DF+mB", "fistp", new OP_TYPES[]{ STi },                       "Store Integer and Pop" ),
                new OP_INFO( "DF+mC", "fnstsw", new OP_TYPES[]{ STi },                      "Store x87 FPU Status Word" ),
                new OP_INFO( "DF+mD", "fucomip", new OP_TYPES[]{ ST, STi },                 "Unordered Compare Floating Point Values and Set EFLAGS and Pop" ),
                new OP_INFO( "DF+mE", "fcomip", new OP_TYPES[]{ ST, STi },                  "Compare Floating Point Values and Set EFLAGS and Pop" ),
                new OP_INFO( "DF+mF", "fistp", new OP_TYPES[]{ r64 },                       "Store Integer and Pop" ),
                new OP_INFO( "DF+m0", "fild", new OP_TYPES[]{ STi },                        "Load Integer" ),
                new OP_INFO( "DF+m1", "fisttp", new OP_TYPES[]{ STi },                      "Store Integer with Truncation and Pop" ),
                new OP_INFO( "DF+m2", "fist", new OP_TYPES[]{ STi },                        "Store Integer" ),
                new OP_INFO( "DF+m3", "fistp", new OP_TYPES[]{ STi },                       "Store Integer and Pop" ),
                new OP_INFO( "DF+m4", "fbld", new OP_TYPES[]{ STi },                        "Load Binary Coded Decimal" ),
                new OP_INFO( "DF+m5", "fild", new OP_TYPES[]{ STi },                        "Load Integer" ),
                new OP_INFO( "DF+m6", "fbstp", new OP_TYPES[]{ STi },                       "Store BCD Integer and Pop" ),
                new OP_INFO( "DF+m7", "fistp", new OP_TYPES[]{ STi },                       "Store Integer and Pop" ),
                new OP_INFO( "E0", "loopne", new OP_TYPES[]{ ECX, rel8 },                   "Decrement count; Jump short if count!=0 and ZF=0" ),
                new OP_INFO( "E1", "loope", new OP_TYPES[]{ ECX, rel8 },                    "Decrement count; Jump short if count!=0 and ZF=1" ),
                new OP_INFO( "E2", "loop", new OP_TYPES[]{ ECX, rel8 },                     "Decrement count; Jump short if count!=0" ),
                new OP_INFO( "E3", "jecxz", new OP_TYPES[]{ rel8 },                         "Jump short if eCX register is 0" ),
                new OP_INFO( "E4", "in", new OP_TYPES[]{ AL, imm8 },                        "Input from Port" ),
                new OP_INFO( "E5", "in", new OP_TYPES[]{ EAX, imm8 },                       "Input from Port" ),
                new OP_INFO( "E6", "out", new OP_TYPES[]{ imm8, AL },                       "Output to Port" ),
                new OP_INFO( "E7", "out", new OP_TYPES[]{ imm8, EAX },                      "Output to Port" ),
                new OP_INFO( "E8", "call", new OP_TYPES[]{ rel16_32 },                      "Call Procedure" ),
                new OP_INFO( "E9", "jmp", new OP_TYPES[]{ rel16_32 },                       "Jump" ),
                new OP_INFO( "EA", "jmpf", new OP_TYPES[]{ ptr16_32 },                      "Jump" ),
                new OP_INFO( "EB", "jmp short", new OP_TYPES[]{ rel8 },                     "Jump" ),
                new OP_INFO( "EC", "in", new OP_TYPES[]{ AL, DX },                          "Input from Port" ),
                new OP_INFO( "ED", "in", new OP_TYPES[]{ EAX, DX },                         "Input from Port" ),
                new OP_INFO( "EE", "out", new OP_TYPES[]{ DX, AL },                         "Output to Port" ),
                new OP_INFO( "EF", "out", new OP_TYPES[]{ DX, EAX },                        "Output to Port" ),
                new OP_INFO( "F1", "int 1", new OP_TYPES[]{  },                             "Call to Interrupt Procedure" ),
                new OP_INFO( "F4", "hlt", new OP_TYPES[]{  },                               "Halt" ),
                new OP_INFO( "F5", "cmc", new OP_TYPES[]{  },                               "Complement Carry Flag" ),
                new OP_INFO( "F6+m0", "test", new OP_TYPES[]{ r_m8, imm8 },                 "Logical Compare" ),
                new OP_INFO( "F6+m1", "test", new OP_TYPES[]{ r_m8, imm8 },                 "Logical Compare" ),
                new OP_INFO( "F6+m2", "not", new OP_TYPES[]{ r_m8 },                        "One's Complement Negation" ),
                new OP_INFO( "F6+m3", "neg", new OP_TYPES[]{ r_m8 },                        "Two's Complement Negation" ),
                new OP_INFO( "F6+m4", "mul", new OP_TYPES[]{ AX, AL, r_m8 },                "Unsigned Multiply" ),
                new OP_INFO( "F6+m5", "imul", new OP_TYPES[]{ AX, AL, r_m8 },               "Signed Multiply" ),
                new OP_INFO( "F6+m6", "div", new OP_TYPES[]{ AX, AL, AX, r_m8 },            "Unigned Divide" ),
                new OP_INFO( "F6+m7", "idiv", new OP_TYPES[]{ AX, AL, AX, r_m8 },           "Signed Divide" ),
                new OP_INFO( "F7+m0", "test", new OP_TYPES[]{ r_m16_32, imm16_32 },         "Logical Compare" ),
                new OP_INFO( "F7+m1", "test", new OP_TYPES[]{ r_m16_32, imm16_32 },         "Logical Compare" ),
                new OP_INFO( "F7+m2", "not", new OP_TYPES[]{ r_m16_32 },                    "One's Complement Negation" ),
                new OP_INFO( "F7+m3", "neg", new OP_TYPES[]{ r_m16_32 },                    "Two's Complement Negation" ),
                new OP_INFO( "F7+m4", "mul", new OP_TYPES[]{ EDX, EAX, r_m16_32 },          "Unsigned Multiply" ),
                new OP_INFO( "F7+m5", "imul", new OP_TYPES[]{ EDX, EAX, r_m16_32 },         "Signed Multiply" ),
                new OP_INFO( "F7+m6", "div", new OP_TYPES[]{ EDX, EAX, r_m16_32 },          "Unigned Divide" ),
                new OP_INFO( "F7+m7", "idiv", new OP_TYPES[]{ EDX, EAX, r_m16_32 },         "Signed Divide" ),
                new OP_INFO( "F8", "clc", new OP_TYPES[]{  },                               "Clear Carry Flag" ),
                new OP_INFO( "F9", "stc", new OP_TYPES[]{  },                               "Set Carry Flag" ),
                new OP_INFO( "FA", "cli", new OP_TYPES[]{  },                               "Clear Interrupt Flag" ),
                new OP_INFO( "FB", "sti", new OP_TYPES[]{  },                               "Set Interrupt Flag" ),
                new OP_INFO( "FC", "cld", new OP_TYPES[]{  },                               "Clear Direction Flag" ),
                new OP_INFO( "FD", "std", new OP_TYPES[]{  },                               "Set Direction Flag" ),
                new OP_INFO( "FE+m0", "inc", new OP_TYPES[]{ r_m8 },                        "Increment by 1" ),
                new OP_INFO( "FE+m1", "dec", new OP_TYPES[]{ r_m8 },                        "Decrement by 1" ),
                new OP_INFO( "FE+mE", "inc", new OP_TYPES[]{ r_m8 },                        "Increment by 1" ),
                new OP_INFO( "FE+mF", "dec", new OP_TYPES[]{ r_m8 },                        "Decrement by 1" ),
                new OP_INFO( "FF+m0", "inc", new OP_TYPES[]{ r_m16_32 },                    "Increment by 1" ),
                new OP_INFO( "FF+m1", "dec", new OP_TYPES[]{ r_m16_32 },                    "Decrement by 1" ),
                new OP_INFO( "FF+m2", "call", new OP_TYPES[]{ r_m16_32 },                   "Call Procedure" ),
                new OP_INFO( "FF+m3", "callf", new OP_TYPES[]{ m16_32_and_16_32 },          "Call Procedure" ),
                new OP_INFO( "FF+m4", "jmp", new OP_TYPES[]{ r_m16_32 },                    "Jump" ),
                new OP_INFO( "FF+m5", "jmpf", new OP_TYPES[]{ m16_32_and_16_32 },           "Jump" ),
                new OP_INFO( "FF+m6", "push", new OP_TYPES[]{ r_m16_32 },                   "Push Word, Doubleword or Quadword Onto the Stack" ),
            };
        }

        static byte ToByte(string str, int offset)
        {
            int n = 0;

            if (str[offset] == '?' && str[offset + 1] == '?')
                return 0;

            for (int i = offset; i < offset + 2; i++)
            {
                int b = 0;

                if (str[i] >= 0x61)
                    b = (str[i] - 0x57);
                else if (str[i] >= 0x41)
                    b = (str[i] - 0x37);
                else if (str[i] >= 0x30)
                    b = (str[i] - 0x30);

                if (i == offset)
                    n += (b * 16);
                else
                {
                    n += b;
                }
            }

            return (byte)n;
        }

        public static string ToStr(byte b)
        {
            string result = b.ToString("X2");
            return result;
        }

        public static Inst Read(int handle, int address)
        {
            Inst p = new Inst
            {
                address = address
            };

            int nothing = 0;
            Imports.ReadProcessMemory(handle, address, p.bytes, 16, ref nothing);

            for (int opcodeAt = 0; opcodeAt < OP_TABLE.Length; opcodeAt++)
            {
                var opInfo = OP_TABLE[opcodeAt];

                // Reset for each check..
                p.flags = 0;
                p.len = 0;

                byte opcodeByte = ToByte(opInfo.code, 0);
                bool showPrefix = false;

                // identify the instruction prefixes
                switch (p.bytes[p.len])
                {
                    case OP_SEG_CS:
                        p.len++;
                        p.flags |= PRE_SEG_CS;
                        break;
                    case OP_SEG_SS:
                        p.len++;
                        p.flags |= PRE_SEG_SS;
                        break;
                    case OP_SEG_DS:
                        p.len++;
                        p.flags |= PRE_SEG_DS;
                        break;
                    case OP_SEG_ES:
                        p.len++;
                        p.flags |= PRE_SEG_ES;
                        break;
                    case OP_SEG_FS:
                        p.len++;
                        p.flags |= PRE_SEG_FS;
                        break;
                    case OP_SEG_GS:
                        p.len++;
                        p.flags |= PRE_SEG_GS;
                        break;
                    case OP_66:
                        p.flags |= PRE_66;
                        break;
                    case OP_67:
                        p.flags |= PRE_67;
                        break;
                    case OP_LOCK:
                        p.flags |= PRE_LOCK;
                        if (opcodeByte != OP_LOCK)
                        {
                            showPrefix = true;
                        }
                        break;
                    case OP_REPNE:
                        p.flags |= PRE_REPNE;
                        if (opcodeByte != OP_REPNE)
                        {
                            showPrefix = true;
                        }
                        break;
                    case OP_REPE:
                        p.flags |= PRE_REPE;
                        if (opcodeByte != OP_REPE)
                        {
                            showPrefix = true;
                        }
                        break;
                }

                bool opcode_match = (p.bytes[p.len] == opcodeByte);
                bool reg_from_opcode_byte = false;

                // This will check if we've included up to
                // 3 prefixes in the byte string
                for (int i = 2; i < 11; i += 3)
                {
                    if (opInfo.code.Length > i)
                    {
                        // extended byte?
                        if (opInfo.code[i] == '+')
                        {
                            // check if the opcode byte determines the register
                            if (opInfo.code[i + 1] == 'r')
                            {
                                reg_from_opcode_byte = true;

                                // this simple check can simplify instructons like inc/dec/push/pop
                                // in our opcode table so it can do up to 8 combinations
                                // All we have to put is put `40+r` (rather than 40, 41, 42, 43,...)
                                opcode_match = (p.bytes[p.len] >= opcodeByte) && (p.bytes[p.len] < opcodeByte + 8);
                                break;
                            }
                            else if (opInfo.code[i + 1] == 'm' && opcode_match)
                            {
                                string str = "0";
                                str += opInfo.code[i + 2];

                                byte n = ToByte(str, 0);
                                if (n >= 0 && n < 8)
                                {
                                    // for every +8 it switches to a different opcode out of 8
                                    opcode_match = Longreg(p.bytes[p.len + 1]) == n;
                                }
                                else
                                {
                                    // for every +8 it switches to a different opcode out of 8
                                    // IF the mode is 3 / the byte is >= 0xC0
                                    n -= 8;
                                    opcode_match = Longreg(p.bytes[p.len + 1]) == n && p.bytes[p.len + 1] >= 0xC0;
                                }
                                break;
                            }
                            else if (opcode_match)
                            {
                                // in all other cases, it's an extending byte
                                p.len++;

                                opcodeByte = ToByte(opInfo.code, i + 1);
                                opcode_match = (p.bytes[p.len] == opcodeByte);


                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // this byte matches the opcode byte
                if (opcode_match)
                {
                    // move onto the next byte
                    p.len++;

                    if (showPrefix)
                    {
                        switch (p.flags)
                        {
                            case PRE_LOCK:
                                p.data = "lock ";
                                break;
                            case PRE_REPNE:
                                p.data = "repne ";
                                break;
                            case PRE_REPE:
                                p.data = "repe ";
                                break;
                        }
                    }

                    p.data += opInfo.opcode_name;
                    p.data += " ";

                    p.info = opInfo;

                    int noperands = opInfo.operands.Length;

                    switch (noperands)
                    {
                        case 0:
                            break;
                        case 1:
                            p.flags |= OP_SINGLE;
                            break;
                        case 2:
                            p.flags |= OP_SRC_DEST;
                            break;
                        default:
                            p.flags |= OP_EXTENDED;
                            break;
                    }


                    byte prev = MOD_NOT_FIRST;

                    for (int c = 0; c < noperands; c++)
                    {
                        // c = current operand (index)
                        // append this opmode to that of the corresponding operand
                        p.operands[c].opmode = opInfo.operands[c];

                        // Returns the imm8 offset value at `x`
                        // and then increases `at` by imm8 size.
                        void get_imm8(byte x, bool constant) // *x
                        {
                            string s_offset;
                            if (!constant)
                            {
                                p.operands[c].imm8 = x;
                                p.operands[c].flags |= OP_IMM8;

                                if (x > 0x7F)
                                {
                                    s_offset = "-" + ((0xFF + 1) - p.operands[c].imm8).ToString("X2");
                                }
                                else
                                {
                                    s_offset = "+" + p.operands[c].imm8.ToString("X2");
                                }
                            }
                            else
                            {
                                p.operands[c].disp8 = x;
                                p.operands[c].flags |= OP_DISP8;

                                s_offset = p.operands[c].disp8.ToString("X2");
                            }

                            p.data += s_offset;

                            p.len += sizeof(byte);
                        };

                        // Returns the imm16 offset value at `x`
                        // and then increases `at` by imm16 size.
                        void get_imm16(ushort x, bool constant)
                        {
                            string s_offset;
                            if (!constant)
                            {
                                p.operands[c].imm16 = x;
                                p.operands[c].flags |= OP_IMM16;

                                if (x > 0x7FFF)
                                {
                                    s_offset = "-" + ((0xFFFF + 1) - p.operands[c].imm16).ToString("X4");
                                }
                                else
                                {
                                    s_offset = "+" + p.operands[c].imm16.ToString("X4");
                                }
                            }
                            else
                            {
                                p.operands[c].disp16 = x;
                                p.operands[c].flags |= OP_DISP16;

                                s_offset = p.operands[c].disp16.ToString("X4");
                            }

                            p.data += s_offset;

                            p.len += sizeof(ushort);
                        };

                        // Returns the imm32 offset value at `x`
                        // and then increases `at` by imm32 size.
                        void get_imm32(uint x, bool constant)
                        {
                            string s_offset;
                            if (!constant)
                            {
                                p.operands[c].imm32 = x;
                                p.operands[c].flags |= OP_IMM32;

                                if (x > 0x7FFFFFFF)
                                {
                                    s_offset = "-" + (0 - p.operands[c].imm32).ToString("X8");
                                }
                                else
                                {
                                    s_offset = "+" + p.operands[c].imm32.ToString("X8");
                                }
                            }
                            else
                            {
                                p.operands[c].disp32 = x;
                                p.operands[c].flags |= OP_DISP32;

                                s_offset = p.operands[c].disp32.ToString("X8");
                            }

                            p.data += s_offset;

                            p.len += sizeof(uint);
                        };

                        void get_sib(byte imm)
                        {
                            // get the SIB byte based on the operand's MOD byte.
                            // See http://www.c-jump.com/CIS77/CPU/x86/X77_0100_sib_byte_layout.htm
                            // See https://www.cs.uaf.edu/2002/fall/cs301/Encoding%20instructions.htm
                            // 
                            // To-do: Label the values that make up scale, index, and byte
                            // I didn't label too much here so it is pretty indecent atm...
                            // 

                            byte sib_byte = p.bytes[++p.len]; // notice we skip to the next byte for this
                            byte r1 = (byte)Longreg(sib_byte);
                            byte r2 = (byte)Finalreg(sib_byte);

                            if ((sib_byte + 32) / 32 % 2 == 0 && sib_byte % 32 < 8)
                            {
                                // 
                                p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r2)];
                                p.operands[c].flags |= OP_R32;
                            }
                            else
                            {
                                // we need to check the previous byte in this circumstance
                                if (r2 == 5 && p.bytes[p.len - 1] < 64)
                                {
                                    p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r1)];
                                    p.operands[c].flags |= OP_R32;
                                }
                                else
                                {
                                    p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r2)];
                                    p.data += "+"; // + SIB Base
                                    p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r1)];
                                    p.operands[c].flags |= OP_R32;
                                }

                                // SIB Scale
                                if (sib_byte / 64 > 0) // if (sib_byte / 64)
                                {
                                    p.operands[c].mul = multipliers[sib_byte / 64];
                                    string s_multiplier = p.operands[c].mul.ToString("X1");
                                    p.data += "*";
                                    p.data += s_multiplier;
                                }
                            }

                            if (imm == sizeof(byte))
                            {
                                get_imm8(p.bytes[p.len + 1], false);
                            }
                            else if (imm == sizeof(uint) || (imm == 0 && r2 == 5))
                            {
                                get_imm32(BitConverter.ToUInt32(p.bytes, p.len + 1), true);
                            }
                        };

                        // Gets the relative offset value at `x`
                        // and then increases `at` by rel8 size.
                        void get_rel8(byte x)
                        {
                            // get the current address of where `at` is located
                            int location = p.address + p.len;

                            // base the 8-bit relative offset on it
                            p.operands[c].rel8 = x;

                            p.data += ((location + sizeof(byte) + p.operands[c].rel8)).ToString("X8");
                            p.len += sizeof(byte);
                        };

                        // Gets the relative offset value at `x`
                        // and then increases `at` by rel16 size.
                        void get_rel16(ushort x)
                        {
                            // get the current address of where `at` is located
                            int location = p.address + p.len;

                            // base the 8-bit relative offset on it
                            p.operands[c].rel16 = x;

                            p.data += ((location + sizeof(ushort) + p.operands[c].rel16)).ToString("X8");
                            p.len += sizeof(ushort);
                        };

                        // Gets the relative offset value at `x`
                        // and then increases `at` by rel32 size.
                        void get_rel32(uint x)
                        {
                            // get the current address of where `at` is located
                            int location = p.address + p.len;

                            // base the 8-bit relative offset on it
                            p.operands[c].rel32 = x;

                            p.data += ((location + sizeof(uint) + p.operands[c].rel32)).ToString("X8");
                            p.len += sizeof(uint);
                        };

                        byte r = prev;

                        // grab the basic register initially
                        if (prev == MOD_NOT_FIRST)
                        {
                            r = (byte)Longreg(p.bytes[p.len]);
                        }

                        if (reg_from_opcode_byte)
                        {
                            r = (byte)Finalreg(p.bytes[p.len - 1]);
                        }

                        switch (p.operands[c].opmode)
                        {
                            case one:
                                p.operands[c].disp32 = p.operands[c].disp16 = p.operands[c].disp8 = 1;
                                p.data += "1";
                                break;
                            case xmm0:
                                p.operands[c].Append_reg(0);
                                p.data += "xmm0";
                                p.operands[c].flags |= OP_XMM;
                                break;
                            case AL:
                                p.operands[c].Append_reg(R8_AL);
                                p.data += "al";
                                p.operands[c].flags |= OP_R8;
                                break;
                            case AH:
                                p.operands[c].Append_reg(R8_AH);
                                p.data += "ah";
                                p.operands[c].flags |= OP_R8;
                                break;
                            case AX:
                                p.operands[c].Append_reg(R16_AX);
                                p.data += "ax";
                                p.operands[c].flags |= OP_R16;
                                break;
                            case CL:
                                p.operands[c].Append_reg(R8_CL);
                                p.data += "cl";
                                p.operands[c].flags |= OP_R8;
                                break;
                            case ES:
                                p.data += "es";
                                break;
                            case SS:
                                p.data += "ss";
                                break;
                            case DS:
                                p.data += "ds";
                                break;
                            case GS:
                                p.data += "gs";
                                break;
                            case FS:
                                p.data += "fs";
                                break;
                            case EAX:
                                p.operands[c].Append_reg(R32_EAX);
                                p.data += "eax";
                                p.operands[c].flags |= OP_R32;
                                break;
                            case ECX:
                                p.operands[c].Append_reg(R32_ECX);
                                p.data += "ecx";
                                p.operands[c].flags |= OP_R32;
                                break;
                            case EBP:
                                p.operands[c].Append_reg(R32_EBX);
                                p.data += "ebp";
                                p.operands[c].flags |= OP_R32;
                                break;
                            case DRn:
                                p.data += Mnemonics.dr_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_DR;
                                break;
                            case CRn:
                                p.data += Mnemonics.cr_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_CR;
                                break;
                            case ST:
                                p.data += Mnemonics.st_names[p.operands[c].Append_reg(0)];
                                p.operands[c].flags |= OP_ST;
                                break;
                            case Sreg:
                                p.data += Mnemonics.sreg_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_SREG;
                                break;
                            case mm:
                                p.data += Mnemonics.mm_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_MM;
                                break;
                            case xmm:
                                p.data += Mnemonics.xmm_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_XMM;
                                break;
                            case r8:
                                p.data += Mnemonics.r8_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_R8;
                                break;
                            case r16:
                                p.data += Mnemonics.r16_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_R16;
                                break;
                            case r16_32:
                            case r32:
                                p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_R32;
                                break;
                            case r64:
                                p.data += Mnemonics.r64_names[p.operands[c].Append_reg(r)];
                                p.operands[c].flags |= OP_R64;
                                break;
                            case m8:
                            case m16:
                            case m16_32:
                            case m32:
                            case m64real:
                            case r_m8:
                            case r_m16:
                            case r_m16_32:
                            case r_m32:
                            case m16_32_and_16_32:
                            case m128:
                            case mm_m64:
                            case xmm_m32:
                            case xmm_m64:
                            case xmm_m128:
                            case STi:
                                {
                                    // To-do: Apply markers...
                                    if ((p.flags & PRE_SEG_CS) == PRE_SEG_CS)
                                    {
                                        p.data += "cs:";
                                    }
                                    else if ((p.flags & PRE_SEG_DS) == PRE_SEG_DS)
                                    {
                                        p.data += "ds:";
                                    }
                                    else if ((p.flags & PRE_SEG_ES) == PRE_SEG_ES)
                                    {
                                        p.data += "es:";
                                    }
                                    else if ((p.flags & PRE_SEG_SS) == PRE_SEG_SS)
                                    {
                                        p.data += "ss:";
                                    }
                                    else if ((p.flags & PRE_SEG_FS) == PRE_SEG_FS)
                                    {
                                        p.data += "fs:";
                                    }
                                    else if ((p.flags & PRE_SEG_GS) == PRE_SEG_GS)
                                    {
                                        p.data += "gs:";
                                    }

                                    // small edit..
                                    if (p.operands[c].opmode == moffs16_32)
                                    {
                                        p.data += "[";
                                        get_imm32(BitConverter.ToUInt32(p.bytes, p.len + 1), true); // changes to a disp32
                                        p.data += "]";
                                        break;
                                    }

                                    if (c == 0)
                                    {
                                        prev = r;
                                    }

                                    r = (byte)Finalreg(p.bytes[p.len]);

                                    switch (p.bytes[p.len] / 64) // determine mode from `MOD` byte
                                    {
                                        case 3:
                                            switch (p.operands[c].opmode)
                                            {
                                                case r_m8:
                                                case m8:
                                                    p.data += Mnemonics.r8_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_R8;
                                                    break;
                                                case r_m16:
                                                case m16:
                                                    p.data += Mnemonics.r16_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_R16;
                                                    break;
                                                case mm_m64:
                                                    p.data += Mnemonics.mm_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_MM;
                                                    break;
                                                case xmm_m32:
                                                case xmm_m64:
                                                case xmm_m128:
                                                case m128:
                                                    p.data += Mnemonics.xmm_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_XMM;
                                                    break;
                                                case ST:
                                                case STi:
                                                    p.data += Mnemonics.st_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_ST;
                                                    break;
                                                case CRn:
                                                    p.data += Mnemonics.cr_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_CR;
                                                    break;
                                                case DRn:
                                                    p.data += Mnemonics.dr_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_DR;
                                                    break;
                                                default: // Anything else is going to be 32-bit
                                                    p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r)];
                                                    p.operands[c].flags |= OP_R32;
                                                    break;
                                            }
                                            break;
                                        case 0:
                                            {
                                                p.data += "[";

                                                switch (r)
                                                {
                                                    case 4:
                                                        get_sib(0); // Translate SIB byte (no offsets)
                                                        break;
                                                    case 5:
                                                        p.data += (p.operands[c].disp32 = BitConverter.ToUInt32(p.bytes, p.len + 1)).ToString("X8");
                                                        p.operands[c].flags |= OP_DISP32;
                                                        p.len += sizeof(uint);
                                                        break;
                                                    default:
                                                        p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r)];
                                                        p.operands[c].flags |= OP_R32;
                                                        break;
                                                }

                                                p.data += "]";
                                                break;
                                            }
                                        case 1:
                                            p.data += "[";

                                            if (r == 4)
                                                get_sib(sizeof(byte)); // Translate SIB byte (with BYTE offset)
                                            else
                                            {
                                                p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r)];
                                                p.operands[c].flags |= OP_R32;
                                                get_imm8(p.bytes[p.len + 1], false);
                                            }

                                            p.data += "]";
                                            break;
                                        case 2:
                                            p.data += "[";

                                            if (r == 4)
                                                get_sib(sizeof(uint)); // Translate SIB byte (with DWORD offset)
                                            else
                                            {
                                                p.data += Mnemonics.r32_names[p.operands[c].Append_reg(r)];
                                                p.operands[c].flags |= OP_R32;
                                                get_imm32(BitConverter.ToUInt32(p.bytes, p.len + 1), false);
                                            }

                                            p.data += "]";
                                            break;
                                    }

                                    p.len++;
                                    break;
                                }
                            case imm8:
                                get_imm8(p.bytes[p.len], true); // changes to a disp32
                                break;
                            case imm16:
                                get_imm16(BitConverter.ToUInt16(p.bytes, p.len), true); // changes to a disp32
                                break;
                            case imm16_32:
                            case imm32:
                                get_imm32(BitConverter.ToUInt32(p.bytes, p.len), true); // changes to a disp32
                                break;
                            case moffs8:
                                p.data += "[";
                                get_imm32(BitConverter.ToUInt32(p.bytes, p.len), true); // changes to a disp32
                                p.data += "]";
                                break;
                            case moffs16_32:
                                p.data += "[";
                                get_imm32(BitConverter.ToUInt32(p.bytes, p.len), true); // changes to a disp32
                                p.data += "]";
                                break;
                            case rel8:
                                get_rel8(p.bytes[p.len]);
                                break;
                            case rel16:
                                get_rel16(BitConverter.ToUInt16(p.bytes, p.len));
                                break;
                            case rel16_32:
                            case rel32:
                                get_rel32(BitConverter.ToUInt32(p.bytes, p.len));
                                break;
                            case ptr16_32:
                                get_imm32(BitConverter.ToUInt32(p.bytes, p.len), true);
                                p.data += ":";
                                get_imm16(BitConverter.ToUInt16(p.bytes, p.len), true);
                                break;
                        }

                        // move up to the next operand
                        if (c < noperands - 1 && noperands > 1)
                        {
                            p.data += ",";
                        }
                    }

                    break;
                }
            }

            if (p.len == 0)
            {
                p.len = 1;
                p.data = "???";
            }

            return p;
        }

        public static List<Inst> Read(int handle, int address, int count)
        {
            int at = address;
            List<Inst> inst_list = new List<Inst>();

            for (int c = 0; c < count; c++)
            {
                Inst i = Read(handle, at);
                inst_list.Add(i);
                at += i.len;
            }

            return inst_list;
        }

        public static List<Inst> Read_range(int handle, int from, int to)
        {
            int at = from;
            List<Inst> inst_list = new List<Inst>();

            while (at < to)
            {
                Inst i = Read(handle, at);
                inst_list.Add(i);
                at += i.len;
            }

            return inst_list;
        }
    }

}