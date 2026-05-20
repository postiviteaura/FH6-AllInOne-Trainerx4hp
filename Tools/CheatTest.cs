// Quick test: verify all 21 trainer cheat signatures against live FH6 binary
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Reloaded.Memory.Sigscan;

namespace FH6Scanner;

class CheatTest
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
            var mainModule = proc.MainModule!;
            var baseAddr = mainModule.BaseAddress;
            var modSize = mainModule.ModuleMemorySize;

            var buf = new byte[modSize];
            ReadProcessMemory(handle, baseAddr, buf, (IntPtr)modSize, out var bytesRead);
            Console.WriteLine($"Read {bytesRead / 1024 / 1024} MB\n");

            using var scanner = new Scanner(buf);

            // All 21 cheat signatures from RuntimeProfileHookDescriptor.cs
            var cheats = new (string Name, string Sig, string ExpectedHex, int MatchOffset, int HookSize)[]
            {
                ("Credits",             "E8 ? ? ? ? 89 84 ? ? ? ? ? 4C 8D ? ? ? ? ? 48 8B",
                                        "48 8B 4F 08 33 D2", 0, 6),
                ("Wheelspins",          "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 10",
                                        "33 D2 8B 5F 08", 28, 5),
                ("SuperWheelspins",     "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 18",
                                        "33 D2 8B 5F 10", 28, 5),
                ("SkillPoints",         "85 D2 78 32 48 89 5C 24 08 57 48 83 EC 20 8B DA 48 8B F9 48 8B 49 48",
                                        "33 D2 89 5F 40", 34, 5),
                ("DriftScore",          "E8 ? ? ? ? F3 0F ? ? 0F 28 ? ? ? 0F 28",
                                        "F3 0F 58 F7 0F 28 7C 24 20", 5, 9),
                ("NoSkillBreak",        "0F B6 ? 40 38 ? ? ? ? ? 74 ? 84 C0",
                                        "0F B6 F0 40 38 AB 02 00 00 00", 0, 10),
                ("SellFactor",          "44 8B ? ? ? ? ? 33 D2 48 8B ? ? ? ? ? E8 ? ? ? ? 90",
                                        "44 8B B7 08 01 00 00", 0, 7),
                ("FreezeAI",            "F3 0F ? ? ? ? ? ? F3 0F ? ? F3 0F ? ? 0F 57 ? F3 0F ? ? ? ? ? ? F3 0F ? ? C3",
                                        "F3 0F 88 81 54 01 00 00", 0, 8),
                ("Teleport",            "0F 10 8B 30 02 00 00 0F 11 8F 30 02 00 00",
                                        "0F 10 8B 30 02 00 00", 0, 7),
                ("NoClip",              "48 8B ? 4C 89 ? ? 56 41 ? 41",
                                        "48 8B C4 4C 89 48 20", 0, 7),
                ("Gravity",             "F3 0F ? ? ? F3 0F ? ? ? ? ? ? F3 0F ? ? ? ? ? ? 45 84 ? 74",
                                        "F3 0F 59 4B 08", 0, 5),
                ("NoWaterDrag",         "48 8B ? F3 0F ? ? ? 53 55",
                                        "48 8B C4 F3 0F 11 48 10", 0, 8),
                ("TimeOfDay",           "44 0F ? ? ? ? F2 0F ? ? ? 48 83 C4",
                                        "F2 0F 11 43 08", 6, 5),
                ("SkillScoreMultiplier","8B 78 08 48 8B 18 48 3B DF",
                                        "8B 78 08 48 8B 4D 60", 0, 7),
                ("PrizeScale",          "F3 0F 10 73 10 44 0F 29 40",
                                        "F3 0F 10 73 10", 0, 5),
                ("RemoveBuildCap",      "E8 ? ? ? ? F3 0F ? ? ? 48 8B ? ? ? 48 8B",
                                        "F3 0F 11 43 44", 5, 5),
                ("RaceTimeScale",       "40 ? 48 83 EC ? 48 8B ? 48 8B ? 0F 29 ? ? ? 0F 28 ? FF 50 ? 0F 57",
                                        "F3 0F 5A CE F2 0F 58 C8", 29, 8),
                ("Acceleration",        "F3 0F ? ? ? 41 0F ? ? 0F C6 DB ? 41 0F",
                                        "F3 0F 10 5D 0C", 0, 5),
                ("SpeedTrapMult",       "0F 29 ? ? ? 48 8B ? 48 8B ? ? ? ? ? 48 85 ? 74",
                                        "0F 29 44 24 30", 0, 5),
                ("MissionTimeScale",    "F3 0F ? ? F3 0F ? ? ? ? ? ? 0F 2F ? 0F 87 ? ? ? ? C7 ? ? ? ? ? 00 00 00 00",
                                        "F3 0F 5C C7 F3 0F 11 87 0C 04 00 00", 0, 12),
                ("FreeClothing",        "8B 88 A4 00 00 00 89 4D",
                                        "8B 88 A4 00 00 00", 0, 6),
            };

            Console.WriteLine($"{"CHEAT",-24} {"SIG",-6} {"EXPECTED",-10} {"ADDR"}");
            Console.WriteLine(new string('-', 70));

            int sigOk = 0, sigMiss = 0, expOk = 0, expMiss = 0;

            foreach (var (name, sig, expHex, matchOff, hookSize) in cheats)
            {
                var result = scanner.FindPattern(sig);
                var sigHit = result.Found;
                if (sigHit) sigOk++; else sigMiss++;

                if (!sigHit)
                {
                    Console.WriteLine($"{name,-24} {"MISS",6} {"---",10} ---");
                    expMiss++;
                    continue;
                }

                // Check ExpectedOriginal at the hook address
                var hookOffset = (int)result.Offset + matchOff;
                var expected = ParseHex(expHex);
                var actual = new byte[Math.Min(hookSize, buf.Length - hookOffset)];
                Array.Copy(buf, hookOffset, actual, 0, actual.Length);

                bool expMatch = BytesStartWith(actual, expected);
                if (expMatch) expOk++; else expMiss++;

                var addr = $"0x{baseAddr.ToInt64() + hookOffset:X}";
                Console.WriteLine($"{name,-24} {"OK",6} {(expMatch ? "OK" : "FAIL"),10} {addr}");

                if (!expMatch)
                {
                    Console.WriteLine($"  Expected: {expHex}");
                    Console.WriteLine($"  Actual:   {BitConverter.ToString(actual).Replace("-", " ")}");
                }
            }

            Console.WriteLine($"\nSignature: {sigOk} OK, {sigMiss} MISS");
            Console.WriteLine($"ExpectedOriginal: {expOk} OK, {expMiss} FAIL");
        }
        finally { CloseHandle(handle); }
    }

    static byte[] ParseHex(string hex)
    {
        var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new byte[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            result[i] = byte.Parse(parts[i], System.Globalization.NumberStyles.HexNumber);
        return result;
    }

    static bool BytesStartWith(byte[] current, byte[] expected)
    {
        if (current.Length < expected.Length) return false;
        for (int i = 0; i < expected.Length; i++)
            if (current[i] != expected[i]) return false;
        return true;
    }
}
