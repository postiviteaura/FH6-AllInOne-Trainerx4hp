# FH6 All-in-One Trainer

An all-in-one trainer for **Forza Horizon 6** — car/physics cheats, live SQL access to the game's in-memory database, and crash-free data-only profile writes. Self-contained `.exe`, no .NET install needed.

> **Offline mode only.** This trainer modifies game memory. Online play (Rivals, Eventlab, Multiplayer, leaderboards) will not work and may result in a ban. Run FH6 in offline mode before using.

## Status

The current release is the **v7.0.0** pre-release.

- **Crash-free data-only writes.** Reverse engineering on v382.893 confirmed why every previous version crashed: the old cheats wrote inline hooks into the game's code (`.text`), and FH6's integrity scanner detects any code modification and kills the game. The SQL cheats (Free Cars, Autoshow, etc.) never crashed because they use a **different mechanism** — a shellcode executed in-process that never touches the game's code. v7.0.0 uses that same proven-safe mechanism for **Wheelspins and Skill Points** via the new **Data-Only (Crash-Free)** section. Zero code modification, no crash. (FH6 does not use Denuvo.)
- **How it works:** the data-only path finds your profile data in memory by scanning for your current value (a plain memory read — safe), then writes the new value through a shellcode (CreateRemoteThread — the same mechanism the SQL cheats have always used). The address is cached after the first scan, so subsequent writes are instant.
- **Old toggle cheats** (Credits, Drift, etc.) are still present but use the old hook method and **will still crash**. Use the **Data-Only** section for the crash-free experience. More cheats are being moved to the data-only method.

## Download

Latest release: **[GitHub Releases](../../releases)** — download the `.zip`, extract, and run `FH6AllInOneTrainer.exe` as Administrator.

## How to use

### Data-Only cheats (crash-free, recommended)
1. Start Forza Horizon 6 and **load fully into the world** (be driving, not in a menu).
2. Note your **current** Wheelspins and Super Wheelspins counts (shown in-game).
3. Launch the trainer as Administrator and attach.
4. In the **Data-Only (Crash-Free)** section, enter: current Wheelspins, current Super Wheelspins (for verification), and your desired Wheelspins count.
5. Click **Set**. Open the wheelspin screen in-game to confirm.
6. The address is now **cached** — subsequent Sets are instant (no re-entry needed).

### SQL cheats (always crash-free)
Attach, then use the Database tab: Free Cars, Autoshow Unlock, Add All Cars, Free Upgrades, Free Wheels, etc. These write via the in-process shellcode and have never crashed.

> Enable cheats only once you are fully in-game. Attaching or toggling during loading or intro screens is more likely to cause issues.

## Features

### Data-Only Profile Values (crash-free)
- **Wheelspins** — set any count. Enter your current value once; the address is cached for the session.
- **Skill Points** — set any count. Same scan-once mechanism.

### SQL Database (in-memory SQLite, crash-free)
- **Unlock Everything** — all SQL cheats in one click
- Free Cars (BaseCost=0), Autoshow Unlock, Install Flags
- Add All Cars (CarBuckets approach), Free Upgrades (47 tables), Free Wheels, Full Autoshow
- Unlock Upgrade Presets, Clear "NEW!" Tag

### Physics & Performance (SQL, crash-free)
- Drift Score 10x, Max Traction, Torque 2x, Reduce Drag 0.5x

### Legacy Hook Cheats (may crash — use Data-Only instead)
- Credits, Super Wheelspins, Sell Payout, Drift Score Multiplier, No Skill Break, and other toggle-based cheats. These use the old code-hook method that the game's integrity scanner detects. They are retained for reference but are not recommended on current game builds.

## Known Limitations

- **Legacy hook cheats crash on v382.893.** Use the Data-Only section instead. More cheats are being migrated to the data-only method.
- **Data-only cheats require your current value** (entered once per session) so the scan can locate the profile struct. The address is cached after the first successful scan.
- **XP / Level modding** is not yet supported.
- **Game build:** cheats are tested against Forza build v382.893. On other builds, signatures may not resolve.

## Build from Source

Requires **.NET 10 SDK** on Windows:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Credits

| Who | Contribution |
|-----|-------------|
| **[paris' club](https://discord.gg/WSd3bRNJuJ)** | Core profile cheats (CALL-resolution approach), SQL features |
| **[ForzaMods](https://github.com/ForzaMods/Forza-Mods-AIO)** | AOB signatures reference |
| **[matkhl](https://www.unknowncheats.me/forum/other-games/752793)** | Free Upgrades SQL (47 tables), CarBuckets approach, database dumper |
| **[Omkmakwana](https://github.com/Omkmakwana/FH6Trainer)** | Add All Cars reference |
| **[Chaarkor](https://github.com/Chaarkoor)** | Original Avalonia UI shell, MVVM architecture |
| **[changcheng967](https://github.com/changcheng967)** | All-in-one integration, physics SQL cheats, data-only writes, UI |

## License

GPL-3.0 — see [LICENSE](LICENSE).
