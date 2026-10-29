# Spec

A Windows tray app that watches EverQuest log files and reports each zone's Resource Hunter bonus to the community site. See [API.md](API.md) for the server API, [everquest log analysis.md](everquest%20log%20analysis.md) for log formats, and [QUESTIONS.md](QUESTIONS.md) for open questions.

## Tech stack

- C# and WPF on .NET 10.
- [H.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon) for the tray icon.
- **MVVM:** ViewModels hold only observable properties and commands, with no logic. Display (text, formatting, icons, show/hide toggles) belongs in the WPF views. Business rules (validation, defaults, app state) belong in Core, outside the ViewModels.
- Published as a self-contained, single-file x64 exe.
- [Velopack](https://velopack.io/) for the installer and auto-update, with releases hosted on GitHub Releases. It installs per user, with no admin prompt. The app checks for updates at startup and every few hours, and applies them on the next restart.
- **Code signing:** none at first. SmartScreen warns on the first install only, because Velopack's own update downloads aren't marked as coming from the internet. The README explains how to get past the warning (**More info**, then **Run anyway**). If people outside the group start installing it, sign with [Azure Trusted Signing](https://learn.microsoft.com/azure/trusted-signing/) (about $10 a month) using `vpk pack --azureTrustedSignFile`. Signing builds SmartScreen trust across releases, instead of starting over with every new file.

## App behavior

- **Single instance:** a named mutex. A second copy tells the first to show its settings window, then exits.
- **Start with Windows:** a value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **API key:** one key works on both sites. Store it in Windows Credential Manager (`CredWrite` / `CredRead`). Never write it to a file or to the log.
- **Settings:** `%APPDATA%\ResourceHunterBonusScout\settings.json`.
- **App log:** `%APPDATA%\ResourceHunterBonusScout\logs\`, one file per day, kept for 14 days. It records discovery, reports, responses, and errors.

## Settings window

It opens on its own when no API key is stored, and from the tray menu after that. It contains:

1. A short explanation of what the app does.
1. **Turn on logging:** type `/log on` in EverQuest.
1. **Get an API key:** links to http://fangbreaker.zone and http://frostreaver.zone. Log in with Discord, open the menu under your name, and choose **Account** ([screenshot](../docs/assets/account-menu.png)). The page is titled **Client API Key**. Click **Generate API Key** ([screenshot](../docs/assets/api-key-generate.png)), then **Copy** ([screenshot](../docs/assets/api-key-copy.png)). Include these screenshots.
   - An account has only one key, shared with fzClient. Once a key exists, the page shows it masked and offers **Rotate API Key** and **Revoke API Key**. If the user already has a key in fzClient, they can paste that one. **Rotate** reveals a new key, but the old one stops working in fzClient.
1. **API key:** a masked text box with a show/hide toggle. It must start with `fz_live_`, or the window shows "The API key must start with fz_live_" and won't save. Editing the key clears the error.
1. **Start with Windows** checkbox, checked by default.
1. **Save** and **Cancel**. Both hide the window. The app keeps running in the tray.

## Tray icon

| State  | When                                                                  |
| ------ | --------------------------------------------------------------------- |
| Idle   | No Fangbreaker or Frostreaver log is being tailed.                    |
| Active | At least one is being tailed.                                         |
| Error  | The last report, data load, or zone lookup failed, or there's no key. |

- The icon goes back to Idle or Active after the next success.
- The tooltip starts with "Resource Hunter Bonus Scout", then lists each character with its server and current zone, like `Bowslinger (Fangbreaker): The Halls of Honor`. If the zone isn't known, it says "unknown zone". While waiting to report `none`, after a zone entry or a kill, it adds when, like `Bowslinger (Fangbreaker): Crushbone, waiting for bonus until 3:42:10 PM`. If EverQuest isn't running, it says "EverQuest is not running." Otherwise, with no characters, it says "No EverQuest log is being watched."
- The menu has **Settings…**, **Open log** (today's log file), and **Exit**.

## Startup windows

- **Tray icon hidden:** explain how to pin the icon to the taskbar. Windows has no API for this, but two undocumented Windows 11 registry values give a hint:
  - `HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify`, DWORD `SystemTrayChevronVisibility`: 1 if the overflow menu is on.
  - `HKCU\Control Panel\NotifyIconSettings\<id>`, where `ExecutablePath` is this app's exe: `IsPromoted` = 1 if the icon is pinned.

  Show the explanation at startup when the overflow menu is on and the icon isn't pinned. Also show it if the values are missing, for example on first run, before Windows has created the icon's entry. There's no "don't show again" option. Pinning the icon makes it stop.
- **Can't connect:** say the app can't reach the server. This covers network errors and 5xx responses only.
- **Key rejected:** the `types` and `zones` endpoints are public, so startup can't check the key. A bad key shows up as a 401 on the first report. That shows the "API key rejected" notification (see [Responses](#responses)), not the can't-connect window.

## Logging hint

If an EQ process has run for 5 minutes and no `eqlog_*.txt` in its `Logs` folder has grown, show a tray notification: "No EverQuest log is growing. Type `/log on` in EverQuest." Show it once per EQ process. This includes logs for other servers, because any growing log means logging is on.

## Finding logs

- Every few seconds, look for `eqgame.exe` processes. Get each exe path with `QueryFullProcessImageName`, which works even when EQ runs elevated.
- In the `Logs` folder next to the exe, a file `eqlog_{CharacterName}_{server}.txt` is active when it grows while EQ is running from that folder.
- Tail each active log once, even if several EQ processes share the folder. Start at the end of the file. Stop when no EQ process from that folder is running.

## Servers

The `{server}` in the log file name picks the site, ignoring case. Other servers are ignored.

| Log file      | Site                     | `serverName` |
| ------------- | ------------------------ | ------------ |
| `fangbreaker` | https://fangbreaker.zone | `Fangbreaker` |
| `frostreaver` | https://frostreaver.zone | `Frostreaver` |

When a server's first log becomes active, load its `types` and `zones` lists. Reload them when they're more than an hour old. The lists differ: Frostreaver has no `tribute` or `item-exp`.

## Log parsing

Strip the timestamp (`[Mon Sep 14 16:10:29 2026] `) from each line.

### Zone entry

- `You have entered {zone}.` changes the current zone, except lines that start with `You have entered an area` (like `You have entered an area where levitation effects do not function.`).
- Look `{zone}` up in the server's `zones` list, ignoring case. If there's no match, try again without a leading `The `, then try the alias table below. Report the site's spelling.
- Instances follow their parent zone. If `{zone}` still doesn't match and contains `: `, look up the part before it the same way. For example, `Tirranun's Delve: Grounding the Drakes` reports as `Tirranun's Delve`. Instances that are on the list, like `Deepest Guk: The Rescue`, match exactly first.
- If it's still not on the list, the current zone is unknown. Don't report anything until the next zone entry. Some zones that aren't on the list still show bonus messages (see [everquest log analysis.md](everquest%20log%20analysis.md)), so keeping the previous zone would report its bonus for the wrong zone. Log it as an error and show the Error icon.

Aliases are built in. An alias is used only if its site name is on that server's list.

| Log                            | Site                  |
| ------------------------------ | --------------------- |
| Ruins of Illsalin              | The Ruins of Illsalin |
| Riftseekers' Sanctum           | Riftseeker's Sanctum  |
| The City of Thurgadin          | Thurgadin             |
| Kael Drakkel                   | Kael Drakkal          |
| Cabilis East                   | Cabilis               |
| Torden, the Bastion of Thunder | Bastion of Thunder    |
| Drunder, the Fortress of Zek   | Plane of Tactics      |
| Ruins of Lxanvom               | Crypt of Decay        |

### Bonus message

A bonus message applies to the current zone. Ignore it if the current zone is unknown, or if its key isn't in the server's `types` list.

| Message ends with…                                                        | `bonusKey`   |
| ------------------------------------------------------------------------- | ------------ |
| improving the experiences of all who venture here.                        | `experience` |
| improving the experience tied to abilities for all who venture here.      | `aa`         |
| multiplying the loot that may be found here.                              | `loot`       |
| multiplying the coin that may be found here.                              | `coin`       |
| which has drawn many creatures of exceptional rarity here.                | `rare`       |
| magnifying the recognition one may receive from their adventures here.    | `faction`    |
| which has drawn numerous mundane creatures here.                          | `respawn`    |
| enhancing the benefits of offerings for all who venture here.             | `tribute`    |
| enhancing the potency of any offerings made here.                         | `tribute`    |
| improving the rate at which proficiency is gained while adventuring here. | `tradeskill` |

Fangbreaker's `types` list includes `item-exp`, but the game has no message for it yet.

## Reporting

- Report when a bonus message appears.
- Report `none` 5 minutes after a zone entry with no bonus message. Cancel the timer on any new zone entry, including one to an unknown zone, or when EQ exits.
- Report `none` sooner if `You have slain {mob}!` isn't followed by a bonus message within 10 seconds. Ignore `{mob} was slain by {name}!`: it includes kills by NPCs and other players, which often don't trigger the bonus. `{name} has been slain by {mob}!` is a player or pet dying, not a kill.
- Pause the timer on `It will take you about 30 seconds to prepare your camp.` and resume it on `You abandon your preparations to camp.`
- On the daily reset message (`Fonts of unstable magic are erupting…`), restart the 5-minute timer for the current zone. A new day's bonus may follow.
- If a bonus appears after `none` was reported, report the bonus too.
- Before reporting `none`, GET `/api/servers/{server}/bonuses/today`. If the site already shows a bonus for the zone (anything but `none` or `unconfirmed`), don't report: assume the site is right. If that check fails, log it and skip the report.
- POST `{serverName, zoneName, bonusKey}` to `/api/client/bonuses` with the `X-API-Key` header. Duplicates are fine.

### Responses

Every failure is logged and shows the Error icon.

| Response              | Action                                                                     |
| --------------------- | -------------------------------------------------------------------------- |
| 200                   | Clear the Error icon.                                                      |
| 400 bad payload       | Don't retry.                                                               |
| 401 bad key           | Notify "API key rejected" (click opens settings). Stop until the key changes. |
| 403 banned            | Notify. Stop reporting to that site until restart.                        |
| 404 zone not found    | Reload the zones list. Don't retry.                                        |
| 500, other, network   | Retry once after 1 minute, then drop.                                      |

A waiting retry is replaced by a newer report for the same character and zone. Waiting retries are lost on exit.

If loading `types` or `zones` fails, retry once after 1 minute. If that fails too, try again the next time the list is needed.
