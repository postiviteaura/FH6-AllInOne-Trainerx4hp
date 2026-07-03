using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FH6Mod.Cheats.RuntimeHook;

/// <summary>
/// Data-only profile value writes — the crash-free path.
///
/// Finds the profile struct by scanning the game's memory for its known field
/// values (external ReadProcessMemory — safe; the trainer already reads memory
/// for signature scanning and it never crashes), then writes the new value via
/// an in-process shellcode executed through CreateRemoteThread — the exact
/// proven-safe mechanism SqlExecutor uses for the SQL cheats.
///
/// This NEVER modifies the game's .text section. No hook, no JMP patch, no
/// thread suspension. There is nothing for the code-integrity scanner to find,
/// which is why the SQL cheats (same mechanism) don't crash while the old
/// code-cave hooks did.
/// </summary>
public sealed class DataOnlyWriter
{
    private readonly RuntimeHookEngine _e;
    public DataOnlyWriter(RuntimeHookEngine e) => _e = e;

    public IntPtr Handle => _e.HandlePublic;

    // ===== thread P/Invoke (same as SqlExecutor) =====
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Scan the game's committed, readable memory for every address holding a
    /// given int32 value. This is a plain memory read (ReadProcessMemory) — the
    /// same operation the trainer already performs for AOB scanning, which has
    /// never triggered a crash.
    /// </summary>
    public List<ulong> ScanInt32(int value, int maxResults = 500)
    {
        var results = new List<ulong>();
        var handle = _e.HandlePublic;
        if (handle == IntPtr.Zero) return results;
        var mbiSize = Marshal.SizeOf<Native.MemoryBasicInformation64>();
        ulong addr = 0x10000;
        while (addr < 0x7FFFFFFFFFFFL && results.Count < maxResults)
        {
            if (Native.VirtualQueryEx(handle, (UIntPtr)addr, out var mbi, (UIntPtr)mbiSize) == UIntPtr.Zero)
                break;
            ulong baseAddr = mbi.BaseAddress;
            ulong size = mbi.RegionSize;
            if (mbi.State == Native.MEM_COMMIT && size > 0 && size < 0x10000000 && Native.IsReadable(mbi.Protect))
            {
                for (ulong off = 0; off < size; off += 0x400000)
                {
                    int chunk = (int)Math.Min(0x400000, size - off);
                    var region = _e.ReadBytesPublic(baseAddr + off, chunk);
                    if (region.Length == 0) continue;
                    for (int i = 0; i <= region.Length - 4; i++)
                    {
                        if (BitConverter.ToInt32(region, i) == value)
                        {
                            results.Add(baseAddr + off + (ulong)i);
                            if (results.Count >= maxResults) break;
                        }
                    }
                    if (results.Count >= maxResults) break;
                }
            }
            addr = baseAddr + size;
            if (size == 0) break;
        }
        return results;
    }

    /// <summary>
    /// Write an int32 to an address via an in-process shellcode (CreateRemoteThread).
    /// The shellcode is: MOV DWORD [RCX], imm32 ; RET — where RCX is the target
    /// address (passed as the thread's lpParameter). The write happens inside the
    /// game process as a normal memory store, identical to how SqlExecutor's SQL
    /// writes happen. No .text is touched.
    /// </summary>
    public bool WriteInt32(ulong addr, int value)
    {
        var handle = _e.HandlePublic;
        if (handle == IntPtr.Zero) return false;
        var codeMem = Native.VirtualAllocEx(handle, IntPtr.Zero, (UIntPtr)4096, Native.MEM_COMMIT | Native.MEM_RESERVE, Native.PAGE_EXECUTE_READWRITE);
        if (codeMem == IntPtr.Zero) return false;
        try
        {
            // C7 01 <imm32> C3  =  mov dword ptr [rcx], imm32 ; ret
            var code = new byte[] { 0xC7, 0x01, 0, 0, 0, 0, 0xC3 };
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, code, 2, 4);
            _e.WriteBytesPublic((ulong)codeMem.ToInt64(), code);
            var t = CreateRemoteThread(handle, IntPtr.Zero, 0, codeMem, new IntPtr((long)addr), 0, out _);
            if (t == IntPtr.Zero) return false;
            WaitForSingleObject(t, 5000);
            CloseHandle(t);
            return true;
        }
        finally { Native.VirtualFreeEx(handle, codeMem, UIntPtr.Zero, Native.MEM_RELEASE); }
    }

    /// <summary>
    /// Cached profile struct base — found once via scan, then reused for all
    /// subsequent writes (no re-scanning). The struct address is stable for the
    /// game session, so the user only enters their current value ONCE.
    /// </summary>
    private ulong _cachedBase;
    private bool _hasCache;

    /// <summary>
    /// Find the profile struct base. If we've found it before (cached), verify the
    /// cache is still valid and return it instantly (no scan). Otherwise scan for
    /// the current value + verify sibling, then cache.
    /// </summary>
    public ulong FindStruct(int fieldOffset, int currentFieldValue, int verifyOffset, int verifyValue)
    {
        // Fast path: cached + still valid
        if (_hasCache && _cachedBase != 0)
        {
            // Verify the cache is still the right struct (read two fields)
            if (_e.ReadInt32Public(_cachedBase + (ulong)fieldOffset) == currentFieldValue ||
                _e.ReadInt32Public(_cachedBase + (ulong)verifyOffset) == verifyValue)
                return _cachedBase;
            // Cache stale — re-scan
            _hasCache = false;
        }

        foreach (var hit in ScanInt32(currentFieldValue))
        {
            if (hit < (ulong)fieldOffset) continue;
            ulong baseAddr = hit - (ulong)fieldOffset;
            if (_e.ReadInt32Public(baseAddr + (ulong)verifyOffset) == verifyValue)
            {
                _cachedBase = baseAddr;
                _hasCache = true;
                return baseAddr;
            }
        }
        return 0;
    }

    /// <summary>
    /// Invalidate the cache (e.g. when the game restarts or the profile changes).
    /// </summary>
    public void InvalidateCache() { _hasCache = false; _cachedBase = 0; }

    /// <summary>
    /// Set a profile field the safe way: find the struct (scan + verify), then
    /// shellcode-write [base+fieldOffset] = desiredValue. Returns true if written.
    /// </summary>
    public bool SetField(int fieldOffset, int currentValue, int verifyOffset, int verifyValue, int desiredValue)
    {
        var baseAddr = FindStruct(fieldOffset, currentValue, verifyOffset, verifyValue);
        if (baseAddr == 0) return false;
        return WriteInt32(baseAddr + (ulong)fieldOffset, desiredValue);
    }
}
