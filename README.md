# FH6 All-in-One Trainer

An improved, all-in-one trainer for **Forza Horizon 6** — runtime hooks for player profile values + live SQL access to the game's in-memory database. Single-file `.exe`, no extra runtime needed.

> ⚠️ **Use at your own risk.** This trainer modifies game memory. Microsoft / Turn 10 can ban your account. **Solo / Free Roam only — never use online (Rivals, Eventlab, Multiplayer, leaderboards).**

## ⬇️ Download

Latest release: **[GitHub Releases](../../releases/latest)** — grab `FH6AllInOneTrainer.exe`. Run as administrator.

## ✅ Working Features

### Quick Start (Dashboard)
- **One-click Quick Start** — 999M Credits + Free Cars + Autoshow Unlock + All Cars in Garage, instantly

### Runtime Hooks (Unlocks Page)
- **Max All** — set Credits 999M, Wheelspins 999, Super Wheelspins 999, Skill Points 999K in one click
- **Credits (CR)** — custom value with presets (10K, 100K, 1M, 100M, 999M), locked
- **Wheelspins** — custom count with presets (10, 50, 100, 999)
- **Super Wheelspins** — custom count with presets (10, 50, 100, 999)
- **Skill Points** — custom value with presets (100, 1K, 10K, 999K), locked
- **Sell Payout x** — multiply car sell price by any factor

### SQL Database (Database Page)
- **Unlock Everything** — applies all 5 SQL cheats at once (one click)
- **Free Cars (LOCK)** — BaseCost stays at 0 forever (re-applied every 10s)
- **Autoshow All Visible (LOCK)** — every car stays in showroom
- **Install Flags (LOCK)** — IsInstalled / IsPurchased / IsDrivable stay at 1
- **Clear NEW Tag** — remove persistent NEW! badges from garage
- **Add All Cars** — grant every car free (reopen game to claim)

Each LOCK toggle re-applies its SQL every 10 seconds. Backup tables are created automatically — toggling OFF restores originals.

## ⚠️ Broken (game patched)

- Drift Score Multiplier
- No Skill Break

## 🛡️ Stability & Safety

- **CRC bypass** auto-armed before any hook (vtable function pointer swap + 10s re-arm timer)
- **Hook self-healing** — every 10s the engine re-applies patches the game tries to roll back
- **ExpectedOriginal sanity check** — refuses to inject if target bytes don't match (no crashes from outdated signatures)
- **Auto-detach** when the game exits or crashes — no writes to dead processes
- **Two-phase CRC dance** — restores originals for 1 second so the game's integrity check passes, then re-applies patches

## 🔧 Build from Source

Requires **.NET 10 SDK** on Windows:

```bash
dotnet publish -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Output: `bin/Release/net10.0-windows/win-x64/publish/FH6AllInOneTrainer.exe`

## 🙏 Credits

This trainer builds on the work of several people:

| Who | Contribution |
|-----|-------------|
| **[paris' club](https://discord.gg/WSd3bRNJuJ)** | All core cheats: runtime hooks (Credits, Wheelspins, Super Wheelspins, Skill Points, Sell Payout), SQL features (Free Cars, Autoshow, Install Flags, Add All Cars, Clear NEW Tag), CRC bypass, code caves, memory injection foundation |
| **[Chaarkor](https://github.com/Chaarkoor)** | Original Avalonia UI shell, MVVM architecture, design system, pattern scanner — [Chaarkors-FH6-Trainer](https://github.com/Chaarkoor/Chaarkors-FH6-Trainer) |
| **[ForzaMods](https://github.com/ForzaMods/Forza-Mods-AIO)** | Upstream AOB signatures and hook techniques — [Forza-Mods-AIO](https://github.com/ForzaMods/Forza-Mods-AIO) |
| **[Reloaded.Memory](https://github.com/Reloaded-Project/Reloaded.Memory.Sigscan)** | SIMD-accelerated AOB scanner |
| **[changcheng967](https://github.com/changcheng967)** | All-in-one improvements: Max All, Unlock Everything, Quick Start, preset buttons, rebrand |

## 📝 License

GPL-3.0 — source must remain open. See [LICENSE](LICENSE).

---

**FH6 All-in-One Trainer** · 2026 · GPL-3.0 · Solo / Free Roam only
