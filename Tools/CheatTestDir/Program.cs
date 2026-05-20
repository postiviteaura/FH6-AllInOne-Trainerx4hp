using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FH6Scanner;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr baseAddr, byte[] buf, IntPtr size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_ALL_ACCESS = 0x001F0FFF;

    static void Main()
    {
        var procs = Process.GetProcessesByName("forzahorizon6");
        if (procs.Length == 0) procs = Process.GetProcessesByName("ForzaHorizon6");
        if (procs.Length == 0) { Console.WriteLine("ERROR: FH6 not found."); return; }

        var proc = procs[0];
        Console.WriteLine($"FH6 PID {proc.Id}");
        var handle = OpenProcess(PROCESS_ALL_ACCESS, false, proc.Id);
        if (handle == IntPtr.Zero) { Console.WriteLine("ERROR: OpenProcess failed."); return; }

        try
        {
            var mm = proc.MainModule!;
            long ba = mm.BaseAddress.ToInt64();
            var buf = new byte[mm.ModuleMemorySize];
            ReadProcessMemory(handle, mm.BaseAddress, buf, (IntPtr)mm.ModuleMemorySize, out var br);
            Console.WriteLine($"Read {br / 1024 / 1024} MB\n");

            var cheats = new[]
            {
                C("Credits", "E8 ?? ?? ?? ?? 89 84 ?? ?? ?? ?? ?? 4C 8D ?? ?? ?? ?? ?? 48 8B", "48 8B 4F 08 33 D2", 0, 6),
                C("Wheelspins", "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 10", "33 D2 8B 5F 08", 28, 5),
                C("SuperWheelspins", "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 18", "33 D2 8B 5F 10", 28, 5),
                C("SkillPoints", "85 D2 78 32 48 89 5C 24 08 57 48 83 EC 20 8B DA 48 8B F9 48 8B 49 48", "33 D2 89 5F 40", 34, 5),
                C("DriftScore", "E8 ?? ?? ?? ?? F3 0F ?? ?? 0F 28 ?? ?? ?? 0F 28", "F3 0F 58 F7 0F 28 7C 24 20", 5, 9),
                C("NoSkillBreak", "0F B6 ?? 40 38 ?? ?? ?? ?? ?? 74 ?? 84 C0", "0F B6 F0 40 38 AB 02 00 00 00", 0, 10),
                C("SellFactor", "44 8B ?? ?? ?? ?? ?? 33 D2 48 8B ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 90", "44 8B B7 08 01 00 00", 0, 7),
                C("FreezeAI", "F3 0F ?? ?? ?? ?? ?? ?? F3 0F ?? ?? F3 0F ?? ?? 0F 57 ?? F3 0F ?? ?? ?? ?? ?? ?? F3 0F ?? ?? C3", "F3 0F 88 81 54 01 00 00", 0, 8),
                C("Teleport", "0F 10 8B 30 02 00 00 0F 11 8F 30 02 00 00", "0F 10 8B 30 02 00 00", 0, 7),
                C("NoClip", "48 8B ?? 4C 89 ?? ?? 56 41 ?? 41", "48 8B C4 4C 89 48 20", 0, 7),
                C("Gravity", "F3 0F ?? ?? ?? F3 0F ?? ?? ?? ?? ?? ?? F3 0F ?? ?? ?? ?? ?? ?? 45 84 ?? 74", "F3 0F 59 4B 08", 0, 5),
                C("NoWaterDrag", "48 8B ?? F3 0F ?? ?? ?? 53 55", "48 8B C4 F3 0F 11 48 10", 0, 8),
                C("TimeOfDay", "44 0F ?? ?? ?? ?? F2 0F ?? ?? ?? 48 83 C4", "F2 0F 11 43 08", 6, 5),
                C("SkillScoreMult", "8B 78 08 48 8B 18 48 3B DF", "8B 78 08 48 8B 4D 60", 0, 7),
                C("PrizeScale", "F3 0F 10 73 10 44 0F 29 40", "F3 0F 10 73 10", 0, 5),
                C("RemoveBuildCap", "E8 ?? ?? ?? ?? F3 0F ?? ?? ?? 48 8B ?? ?? ?? 48 8B", "F3 0F 11 43 44", 5, 5),
                C("RaceTimeScale", "40 ?? 48 83 EC ?? 48 8B ?? 48 8B ?? 0F 29 ?? ?? ?? 0F 28 ?? FF 50 ?? 0F 57", "F3 0F 5A CE F2 0F 58 C8", 29, 8),
                C("Acceleration", "F3 0F ?? ?? ?? 41 0F ?? ?? 0F C6 DB ?? 41 0F", "F3 0F 10 5D 0C", 0, 5),
                C("SpeedTrapMult", "0F 29 ?? ?? ?? 48 8B ?? 48 8B ?? ?? ?? ?? ?? 48 85 ?? 74", "0F 29 44 24 30", 0, 5),
                C("MissionTimeScale", "F3 0F ?? ?? F3 0F ?? ?? ?? ?? ?? ?? 0F 2F ?? 0F 87 ?? ?? ?? ?? C7 ?? ?? ?? ?? ?? 00 00 00 00", "F3 0F 5C C7 F3 0F 11 87 0C 04 00 00", 0, 12),
                C("FreeClothing", "8B 88 A4 00 00 00 89 4D", "8B 88 A4 00 00 00", 0, 6),
            };

            Console.WriteLine($"{"CHEAT",-22} {"SIG",-6} {"BYTES",-8} {"ADDR"}");
            Console.WriteLine(new string('-', 62));

            int sOk = 0, sMi = 0, eOk = 0, eFa = 0;

            foreach (var c in cheats)
            {
                var pat = PH(c.Sig);
                int foundOff = -1;
                for (int i = 0; i <= buf.Length - pat.Length; i++)
                {
                    bool m = true;
                    for (int j = 0; j < pat.Length; j++)
                        if (pat[j] >= 0 && buf[i + j] != pat[j]) { m = false; break; }
                    if (m) { foundOff = i; break; }
                }

                if (foundOff < 0)
                {
                    Console.WriteLine($"{c.Name,-22} {"MISS",6} {"---",8} ---");
                    sMi++; eFa++; continue;
                }
                sOk++;

                int ho = foundOff + c.MOff;
                var exp = PH2(c.Exp);
                var act = new byte[Math.Min(c.HS, buf.Length - ho)];
                Array.Copy(buf, ho, act, 0, act.Length);

                bool em = true;
                for (int i = 0; i < exp.Length && i < act.Length; i++)
                    if (act[i] != exp[i]) { em = false; break; }

                if (em) eOk++; else eFa++;
                Console.WriteLine($"{c.Name,-22} {"OK",6} {(em ? "OK" : "FAIL"),8} 0x{ba + ho:X}");

                if (!em)
                {
                    Console.WriteLine($"  Expected: {c.Exp}");
                    Console.WriteLine($"  Actual:   {BitConverter.ToString(act).Replace("-", " ")}");
                }
            }

            Console.WriteLine($"\nSignature: {sOk} OK, {sMi} MISS");
            Console.WriteLine($"Bytes:     {eOk} OK, {eFa} FAIL");
        }
        finally { CloseHandle(handle); }
    }

    static (string Name, string Sig, string Exp, int MOff, int HS) C(string n, string s, string e, int mo, int hs) => (n, s, e, mo, hs);

    static int[] PH(string sig)
    {
        var p = sig.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var r = new int[p.Length];
        for (int i = 0; i < p.Length; i++)
            r[i] = (p[i] == "??" || p[i] == "?") ? -1 : byte.Parse(p[i], System.Globalization.NumberStyles.HexNumber);
        return r;
    }

    static byte[] PH2(string hex)
    {
        var p = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var r = new byte[p.Length];
        for (int i = 0; i < p.Length; i++)
            r[i] = byte.Parse(p[i], System.Globalization.NumberStyles.HexNumber);
        return r;
    }
}
