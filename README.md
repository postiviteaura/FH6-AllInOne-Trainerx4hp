# FH6 All-in-One Trainer

An all-in-one trainer for **Forza Horizon 6** — runtime hooks for player profile values + car/physics cheats + live SQL access to the game's in-memory database. Self-contained `.exe`, no .NET install needed.

> **Offline mode only.** This trainer modifies game memory. Online play (Rivals, Eventlab, Multiplayer, leaderboards) will not work and may result in a ban. Run FH6 in offline mode before using.

## Status

The current release is the **v6.6.0** pre-release.

- **No anti-cheat bypass is used or needed** for offline single-player. Earlier versions shipped a "CRC / integrity bypass" that reverse engineering later showed was not bypassing anything at all — it was corrupting a normal game subsystem and causing the crashes users reported. v6.6.0 removes all of it; the cheats operate through their own hooks only. (FH6 does not use Denuvo.)
- **Known issue:** the game may still crash shortly after enabling a cheat on some builds. This is under active investigation — please see [issue #130](../../issues/130) to test the current build and report what you see.
- **Game build:** cheats are signed against the current Forza build (v379.939). On newer builds (e.g. v382.893) some cheats may not resolve until the signatures are refreshed.

## Download

Latest release: **[GitHub Releases](../../releases)** — download the `.zip`, extract, and run `FH6AllInOneTrainer.exe` as Administrator.

## How to use

1. Start Forza Horizon 6 and **load fully into the world** (be driving, not in a menu or loading screen).
2. Launch the trainer as Administrator and attach.
3. Enable the cheats you want, then play.

> Enable cheats only once you are fully in-game. Attaching or toggling during loading or intro screens is more likely to cause issues.

## Features

### Profile Values (runtime hooks)
- **Credits** — set any amount (10K to 999M). Toggle on, then buy/sell something for the change to take effect.
- **Wheelspins** — set count (10–999). Toggle on, then spin once for it to lock.
- **Super Wheelspins** — set count (10–999). **Enable Wheelspins first**, then toggle SWS on and spin once to activate.
- **Skill Points** — set any amount. Toggle on, then spend a point for the change to take effect.
- **Sell Payout** — multiply car sell prices by any value.
- **Drift Score** — multiply drift score by any value (5x, 10x, 50x, or custom).
- **No Skill Break** — prevents skill chains from breaking on impacts.

> **Tip:** Wheelspins must be enabled for Super Wheelspins (and some other cheats) to take effect. Enable Wheelspins first, then add your other cheats.

Uses inline code-cave hooks with toggle+value slots — based on the paris' club approach (CALL-resolution with string-compare verification).

### Quick Actions
- **Quick Start** — 999M Credits + Free Cars + Autoshow Unlock + Install Flags + All Cars
- **Max All** — max Credits, Wheelspins, Super Wheelspins, Skill Points

### SQL Database (in-memory SQLite)
- **Unlock Everything** — all SQL cheats in one click
- Free Cars (BaseCost=0), Autoshow Unlock, Install Flags — with persistent locks that re-apply every 10s
- Add All Cars (CarBuckets approach), Free Upgrades (47 tables), Free Wheels, Full Autoshow
- Unlock Upgrade Presets, Clear "NEW!" Tag

### Physics & Performance (SQL)
- Drift Score 10x, Max Traction, Torque 2x, Reduce Drag 0.5x

## Known Limitations

- **XP / Level modding** is not yet supported. See [issue #19](../../issues/19) for discussion.
- **Wheelspins dependency** — Super Wheelspins (and possibly Credits) require Wheelspins to be enabled first to take effect.
- **Crash after enabling a cheat** — under investigation, see [issue #130](../../issues/130).

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
| **[changcheng967](https://github.com/changcheng967)** | All-in-one integration, physics SQL cheats, code cave detours, UI |

## License

GPL-3.0 — see [LICENSE](LICENSE).
