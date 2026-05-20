using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Reloaded.Memory.Sigscan;

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

    static void Main(string[] args)
    {
        var output = new StringBuilder();
        void Log(string s) { output.AppendLine(s); Console.WriteLine(s); }

        var procs = Process.GetProcessesByName("forzahorizon6");
        if (procs.Length == 0) procs = Process.GetProcessesByName("ForzaHorizon6");
        if (procs.Length == 0) { Log("ERROR: FH6 not found."); File.WriteAllText("scan_results.txt", output.ToString()); return; }

        var proc = procs[0];
        Log($"FH6 PID {proc.Id}");
        var handle = OpenProcess(PROCESS_ALL_ACCESS, false, proc.Id);
        if (handle == IntPtr.Zero) { Log("ERROR: OpenProcess failed. Run as admin."); File.WriteAllText("scan_results.txt", output.ToString()); return; }

        try
        {
            var mainModule = proc.MainModule!;
            var baseAddr = mainModule.BaseAddress;
            var modSize = mainModule.ModuleMemorySize;
            Log($"Base 0x{baseAddr.ToInt64():X}, {modSize / 1024 / 1024} MB");

            var buf = new byte[modSize];
            if (!ReadProcessMemory(handle, baseAddr, buf, (IntPtr)modSize, out var bytesRead))
            { Log($"ReadProcessMemory failed: {Marshal.GetLastWin32Error()}"); return; }
            Log($"Read {bytesRead / 1024 / 1024} MB\n");

            // === STEP 1: Scan all known patterns ===
            var patterns = new (string Name, string Pattern)[]
            {
                ("Credits",              "E8 ?? ?? ?? ?? 89 84 ?? ?? ?? ?? ?? 4C 8D ?? ?? ?? ?? ?? 48 8B"),
                ("Wheelspins",           "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 10"),
                ("SuperWheelspins",      "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 18"),
                ("SkillPoints",          "85 D2 78 32 48 89 5C 24 08 57 48 83 EC 20 8B DA 48 8B F9 48 8B 49 48"),
                ("DriftScore",           "E8 ?? ?? ?? ?? F3 0F ?? ?? 0F 28 ?? ?? ?? 0F 28"),
                ("NoSkillBreak",         "0F B6 ?? 40 38 ?? ?? ?? ?? ?? 74 ?? 84 C0"),
                ("SellFactor",           "44 8B ?? ?? ?? ?? ?? 33 D2 48 8B ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 90"),
                ("FreezeAI",            "F3 0F ?? ?? ?? ?? ?? ?? F3 0F ?? ?? F3 0F ?? ?? 0F 57 ?? F3 0F ?? ?? ?? ?? ?? ?? F3 0F ?? ?? C3"),
                ("Teleport",            "0F 10 ?? ?? ?? ?? ?? 0F 28 ?? 0F C2 ?? 00 0F 50"),
                ("NoClip",              "48 8B ?? 4C 89 ?? ?? 56 41 ?? 41"),
                ("Gravity",             "F3 0F ?? ?? ?? F3 0F ?? ?? ?? ?? ?? ?? F3 0F ?? ?? ?? ?? ?? ?? 45 84 ?? 74"),
                ("NoWaterDrag",         "48 8B ?? F3 0F ?? ?? ?? 53 55"),
                ("TimeOfDay",           "44 0F ?? ?? ?? ?? F2 0F ?? ?? ?? 48 83 C4"),
                ("SkillScoreMultiplier","8B 78 ?? 48 8B ?? ?? 48 85 ?? 74 ?? 41 8B"),
                ("PrizeScale",          "F3 0F ?? ?? ?? 33 D2 48 8B ?? ?? E8 ?? ?? ?? ?? 90 48 85 ?? 74 ?? 8B C5"),
                ("RemoveBuildCap",      "E8 ?? ?? ?? ?? F3 0F ?? ?? ?? 48 8B ?? ?? ?? 48 8B"),
                ("RaceTimeScale",       "40 ?? 48 83 EC ?? 48 8B ?? 48 8B ?? 0F 29 ?? ?? ?? 0F 28 ?? FF 50 ?? 0F 57"),
                ("Acceleration",        "F3 0F ?? ?? ?? 41 0F ?? ?? 0F C6 DB ?? 41 0F"),
                ("SpeedZone",           "F3 41 ?? ?? ?? ?? ?? ?? ?? 0F 28 ?? 0F 28 ?? ?? ?? 48 83 C4"),
                ("SpeedTrap",           "0F 29 ?? ?? ?? 48 8B ?? 48 8B ?? ?? ?? ?? ?? 48 85 ?? 74"),
                ("MissionTimeScale",    "F3 0F ?? ?? F3 0F ?? ?? ?? ?? ?? ?? 0F 2F ?? 0F 87 ?? ?? ?? ?? C7 ?? ?? ?? ?? ?? 00 00 00 00"),
                ("FreeClothing",        "48 8B ?? ?? ?? 8B 88 ?? ?? ?? ?? 39 4B"),
                ("CDatabase_1",         "48 8B 0D ?? ?? ?? ?? 48 8B 01 4C 8D 45 ?? 48 8D 55 ?? FF 50 48 90 48 8B 4D ?? 48 85 C9"),
                ("CDatabase_4",         "48 8B 35 ?? ?? ?? ?? 48 85 F6 74"),
            };

            using var scanner = new Scanner(buf);
            int found = 0, missing = 0;

            Log($"{"CHEAT",-28} {"STATUS",-8} {"ADDRESS"}");
            Log(new string('-', 70));

            foreach (var (name, pattern) in patterns)
            {
                var result = scanner.FindPattern(pattern);
                var hit = result.Found;
                if (hit) found++; else missing++;
                var offset = hit ? (int)result.Offset : -1;
                var addr = hit ? $"0x{baseAddr.ToInt64() + offset:X}" : "---";
                Log($"{name,-28} {(hit ? "OK" : "MISS"),-8} {addr}");

                if (hit)
                {
                    // Dump 80 bytes at match for verification
                    var len = Math.Min(80, buf.Length - offset);
                    var hex = Convert.ToHexString(buf.AsSpan(offset, len));
                    Log($"  {hex}");
                }
                Log("");
            }

            Log($"SUMMARY: {found} FOUND, {missing} MISSING / {patterns.Length} total");

            // === STEP 2: Broader search for missing patterns ===
            // Try alternate/shorter patterns for the 7 misses
            Log("\n\n=== BROADER SEARCH FOR MISSING PATTERNS ===\n");

            var broadSearches = new (string Name, string Pattern)[]
            {
                // Teleport: search for movups with large offsets (0x230, 0x240)
                ("Teleport_alt1", "0F 10 ?? 30 02 00 00"),
                ("Teleport_alt2", "0F 10 ?? 40 02 00 00"),
                ("Teleport_alt3", "0F 10 ?? 20 02 00 00"),
                // SkillScore: search for mov edi,[rax+8]
                ("SkillScore_alt1", "8B 78 08 48 8B"),
                // PrizeScale: search for movss xmm6,[rbx+10]
                ("PrizeScale_alt1", "F3 0F 10 73 10"),
                // SpeedZone: divss xmm6,[r14+E8]
                ("SpeedZone_alt1", "F3 41 0F 5E B6 E8 00 00 00"),
                // FreeClothing: mov ecx,[rax+A4]
                ("FreeClothing_alt1", "8B 88 A4 00 00 00"),
                // Shorter CDatabase patterns
                ("CDatabase_alt1", "48 8B 0D ?? ?? ?? ?? 48 8B 01 4C 8D 45"),
            };

            foreach (var (name, pattern) in broadSearches)
            {
                var result = scanner.FindPattern(pattern);
                var hit = result.Found;
                var offset = hit ? (int)result.Offset : -1;
                Log($"{name,-28} {(hit ? "OK" : "MISS"),-8} {(hit ? $"0x{baseAddr.ToInt64() + offset:X}" : "---")}");
                if (hit)
                {
                    // Dump surrounding context: 32 bytes before, 80 after
                    var start = Math.Max(0, offset - 32);
                    var len = Math.Min(144, buf.Length - start);
                    var hex = Convert.ToHexString(buf.AsSpan(start, len));
                    // Mark the match position
                    var marker = new string(' ', (offset - start) * 2) + "^MATCH";
                    Log($"  {hex}");
                    Log($"  {marker}");
                }
                Log("");
            }

            // === STEP 3: Dump ExpectedOriginal verification ===
            Log("\n=== EXPECTEDORIGINAL VERIFICATION ===\n");
            var verifications = new (string Name, string Pattern, byte[] Expected)[]
            {
                ("Credits", "E8 ?? ?? ?? ?? 89 84 ?? ?? ?? ?? ?? 4C 8D ?? ?? ?? ?? ?? 48 8B", [72, 139, 79, 8, 51, 210]),
                ("DriftScore", "E8 ?? ?? ?? ?? F3 0F ?? ?? 0F 28 ?? ?? ?? 0F 28", [243, 15, 88, 247, 15, 40, 124, 36, 32]),
                ("NoSkillBreak", "0F B6 ?? 40 38 ?? ?? ?? ?? ?? 74 ?? 84 C0", [15, 182, 240, 64, 56, 171, 116, 2, 0, 0]),
            };

            foreach (var (name, pattern, expected) in verifications)
            {
                var result = scanner.FindPattern(pattern);
                if (!result.Found) { Log($"{name}: pattern not found, skipping"); continue; }
                var offset = (int)result.Offset + 24; // MatchOffset for Credits
                if (name == "DriftScore") offset = (int)result.Offset + 5;
                if (name == "NoSkillBreak") offset = (int)result.Offset;
                var actual = buf.Skip(offset).Take(expected.Length).ToArray();
                var match = actual.SequenceEqual(expected);
                Log($"{name}: offset +{(int)result.Offset}, hookAt +{offset}");
                Log($"  Expected: {Convert.ToHexString(expected)}");
                Log($"  Actual:   {Convert.ToHexString(actual)}");
                Log($"  Match: {(match ? "YES" : "NO - NEEDS UPDATE")}\n");
            }
        }
        finally { CloseHandle(handle); }

        File.WriteAllText("scan_results.txt", output.ToString());
        Console.WriteLine($"\nSaved to scan_results.txt");
    }
}
