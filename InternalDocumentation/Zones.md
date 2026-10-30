# Zones

## Zones that never get a bonus

- The Plane of Tranquility
- The Plane of Knowledge
- The Guild Lobby
- Guild Hall

## Instanced zones

Some zones are instances of another, for example `Lavaspinner's Lair: Lavaspinner's Locals` and `Lavaspinner's Lair: The Drake Menace` are instances of `Lavaspinner's Lair`.

But not all zones with a ':' in their name are instances. For example `Chardok: The Halls of Betrayal` is a distinct zone and not an instance of `Chardok`.

## Zone names

Many zones are recognized by different names in different contexts.

For each of these, the display name and the `zoneName` the POST API expects are the same string. **EQ log name** is filled in only where the log's `You have entered {zone}.` text differs from the site's spelling (see the alias table in [SPEC.md](SPEC.md#log-parsing)).

| EQ log message name               | Site zone name         |
|-----------------------------------|------------------------|
|                                   | Felwithe               |
|                                   | Kaladim                |
|                                   | Neriak                 |
|                                   | Plane of Mischief      |
|                                   | Qeynos Catacombs       |
|                                   | Sleeper's Tomb         |
|                                   | The Hole               |
|                                   | Veeshan's Peak         |
| <same>                            | Acrylia Caverns        |
| <same>                            | Ak'Anon                |
| <same>                            | Befallen               |
| <same>                            | Blackburrow            |
| <same>                            | Butcherblock Mountains |
| <same>                            | Chardok                |
| <same>                            | Cobalt Scar            |
| <same>                            | Crystal Caverns        |
| <same>                            | Dagnor's Cauldron      |
| <same>                            | Dawnshroud Peaks       |
| <same>                            | Dragon Necropolis      |
| <same>                            | East Freeport          |
| <same>                            | Eastern Wastes         |
| <same>                            | Echo Caverns           |
| <same>                            | Erud's Crossing        |
| <same>                            | Erudin                 |
| <same>                            | Erudin Palace          |
| <same>                            | Everfrost Peaks        |
| <same>                            | Firiona Vie            |
| <same>                            | Freeport Sewers        |
| <same>                            | Frontier Mountains     |
| <same>                            | Grieg's End            |
| <same>                            | Grimling Forest        |
| <same>                            | Grobb                  |
| <same>                            | Halas                  |
| <same>                            | High Keep              |
| <same>                            | Highpass Hold          |
| <same>                            | Hollowshade Moor       |
| <same>                            | Howling Stones         |
| <same>                            | Icewell Keep           |
| <same>                            | Innothule Swamp        |
| <same>                            | Jaggedpine Forest      |
| <same>                            | Kaesora                |
| <same>                            | Karnor's Castle        |
| <same>                            | Katta Castellum        |
| <same>                            | Kedge Keep             |
| <same>                            | Kithicor Forest        |
| <same>                            | Kurn's Tower           |
| <same>                            | Lake of Ill Omen       |
| <same>                            | Lake Rathetear         |
| <same>                            | Marus Seru             |
| <same>                            | Misty Thicket          |
| <same>                            | Mons Letalis           |
| <same>                            | Najena                 |
| <same>                            | Nektulos Forest        |
| <same>                            | Netherbian Lair        |
| <same>                            | North Qeynos           |
| <same>                            | Oggok                  |
| <same>                            | Paineel                |
| <same>                            | Paludal Caverns        |
| <same>                            | Permafrost Keep        |
| <same>                            | Qeynos Hills           |
| <same>                            | Rivervale              |
| <same>                            | Sanctus Seru           |
| <same>                            | Shadeweaver's Thicket  |
| <same>                            | Shadow Haven           |
| <same>                            | Shar Vahl              |
| <same>                            | Siren's Grotto         |
| <same>                            | Skyshrine              |
| <same>                            | Solusek's Eye          |
| <same>                            | South Qeynos           |
| <same>                            | Ssraeshza Temple       |
| <same>                            | Surefall Glade         |
| <same>                            | The Bazaar             |
| <same>                            | The Deep               |
| <same>                            | The Feerrott           |
| <same>                            | The Grey               |
| <same>                            | The Nexus              |
| <same>                            | The Overthere          |
| <same>                            | The Warrens            |
| <same>                            | Timorous Deep          |
| <same>                            | Toxxulia Forest        |
| <same>                            | Trakanon's Teeth       |
| <same>                            | Velketor's Labyrinth   |
| <same>                            | Vex Thal               |
| <same>                            | West Freeport          |
| <same>                            | Western Wastes         |
| Cabilis East                      | Cabilis                |
| Chardok: The Halls of Betrayal    | Halls of Betrayal      |
| Clan Crushbone                    | Crushbone              |
| Kael Drakkel                      | Kael Drakkal           |
| Nagafen's Lair                    | Nagafen's Lair (SolB)  |
| North Desert of Ro                | North Ro               |
| South Desert of Ro                | South Ro               |
| The Akheva Ruins                  | Akheva Ruins           |
| The Burning Woods                 | Burning Woods          |
| The Castle of Mistmoore           | Castle Mistmoore       |
| The Caverns of Exile              | Caverns of Exile       |
| The City of Guk                   | Upper Guk              |
| The City of Mist                  | City of Mist           |
| The City of Thurgadin             | Thurgadin              |
| The Commonlands                   | Commonlands            |
| The Crypt of Dalnir               | Dalnir                 |
| The Dreadlands                    | Dreadlands             |
| The Eastern Plains of Karana      | East Karana            |
| The Emerald Jungle                | Emerald Jungle         |
| The Estate of Unrest              | Estate of Unrest       |
| The Field of Bone                 | Field of Bone          |
| The Fungus Grove                  | Fungus Grove           |
| The Gorge of King Xorbb           | Gorge of King Xorbb    |
| The Great Divide                  | Great Divide           |
| The Greater Faydark               | Greater Faydark        |
| The Iceclad Ocean                 | Iceclad Ocean          |
| The Lavastorm Mountains           | Lavastorm Mountains    |
| The Lesser Faydark                | Lesser Faydark         |
| The Liberated Citadel of Runnyeye | Clan Runnyeye          |
| The Maiden's Eye                  | Maiden's Eye           |
| The Mines of Nurga                | Mines of Nurga         |
| The Northern Plains of Karana     | North Karana           |
| The Ocean of Tears                | Ocean of Tears         |
| The Plane of Fear                 | Plane of Fear          |
| The Plane of Growth               | Plane of Growth        |
| The Plane of Hate                 | Plane of Hate          |
| The Plane of Sky                  | Plane of Sky           |
| The Rathe Mountains               | Rathe Mountains        |
| The Ruins of Old Guk              | Lower Guk              |
| The Ruins of Sebilis              | Old Sebilis            |
| The Scarlet Desert                | Scarlet Desert         |
| The Skyfire Mountains             | Skyfire Mountains      |
| The Southern Plains of Karana     | South Karana           |
| The Steamfont Mountains           | Steamfont Mountains    |
| The Stonebrunt Mountains          | Stonebrunt Mountains   |
| The Swamp of No Hope              | Swamp of No Hope       |
| The Temple of Droga               | Temple of Droga        |
| The Temple of Solusek Ro          | Temple of Solusek Ro   |
| The Temple of Veeshan             | Temple of Veeshan      |
| The Tenebrous Mountains           | Tenebrous Mountains    |
| The Tower of Frozen Shadow        | Tower of Frozen Shadow |
| The Twilight Sea                  | Twilight Sea           |
| The Umbral Plains                 | Umbral Plains          |
| The Wakening Land                 | Wakening Land          |
| The Warsliks Woods                | Warsliks Wood          |
| The Western Plains of Karana      | West Karana            |

