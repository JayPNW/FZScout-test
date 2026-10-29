# About Everquest

## Logs

### Overview

EQ logs live in the eq directory, e.g. `C:\EverQuest\Logs\eqlog_{CHARACTERNAME}_fangbreaker.txt` and contents look like:

```text
[Mon Sep 14 16:10:29 2026] Welcome to EverQuest!
```

Timestamps are in local time. `Welcome to EverQuest!` appears at login and is followed by a zone entry. Logging out writes nothing.

### Zone Entry

When you enter a zone the message will look like:

```text
You have entered The Halls of Honor.
```

The same wording is used for areas inside a zone, which don't change the zone:

```text
You have entered an area where levitation effects do not function.
You have entered an area where Bind Affinity is allowed.
```

Zone names in the log don't always match the site's `zones` list:

| Log Message                               | Site                    |
| ----------------------------------------- | ----------------------- |
| The Halls of Honor                        | `halls-of-honor`        |
| Ruins of Illsalin                         | `the-ruins-of-illsalin` |
| Riftseekers' Sanctum                      | `riftseekers-sanctum`   |
| The City of Thurgadin                     | `thurgadin`             |
| Kael Drakkel                              | `kael-drakkal`          |
| Cabilis East                              | `cabilis`               |
| Torden, the Bastion of Thunder            | `bastion-of-thunder`    |
| Drunder, the Fortress of Zek              | `plane-of-tactics`      |
| Ruins of Lxanvom                          | `crypt-of-decay`        |

### Bonus message

A little while after entering a zone one of the following messages may appear in the log indicating a bonus:

```text
This land is saturated with unstable magic, enhancing the benefits of offerings for all who venture here.
This land is saturated with unstable magic, enhancing the potency of any offerings made here.
This land is saturated with unstable magic, improving the experience tied to abilities for all who venture here.
This land is saturated with unstable magic, improving the experiences of all who venture here.
This land is saturated with unstable magic, improving the rate at which proficiency is gained while adventuring here.
This land is saturated with unstable magic, magnifying the recognition one may receive from their adventures here.
This land is saturated with unstable magic, multiplying the coin that may be found here.
This land is saturated with unstable magic, multiplying the loot that may be found here.
This land is saturated with unstable magic, which has drawn many creatures of exceptional rarity here.
This land is saturated with unstable magic, which has drawn numerous mundane creatures here.
```

The message will appear after your first kill in the zone, or after 3 to 3.5 minutes of being in the zone without a kill.
If you get to that point without a kill and there's still no bonus message, it means the zone has no bonus today.

In a real log from Sep 14 to Oct 27, 2026:

- Without a kill, the message came 178–209 seconds after entry.
- When a kill triggered it, the message came in the same second as the kill.
- Many kills didn't trigger it: kills by NPCs, by other players, and occasionally your own. Of the kills that came first in a visit, 21 of 80 were followed by the message minutes later instead.
- The message appeared once per zone visit, except at the daily reset.

### Kill messages

```text
You have slain a Stillmoon leopard!
A stormrider was slain by Bowslinger!
Kekab was slain by A jord militis!
Kixu has been slain by an elite kyv hunter!
```

`was slain by` is the most common form. The name before it can be blank (`  was slain by Gibarn!`). `has been slain by` is a player or pet dying, not a mob kill.

### Daily reset

When the server resets the day's bonuses, this appears. It came at about midnight Pacific time, but watch for the message, not the clock:

```text
Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources.
```

If the new day's bonus applies to your current zone, its bonus message follows shortly after (20 seconds in the one example seen).

### Camping

```text
It will take you about 30 seconds to prepare your camp.
It will take about 25 more seconds to prepare your camp.
You abandon your preparations to camp.
```

The countdown repeats every 5 seconds.
