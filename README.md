# Resource Hunter Bonus Scout

A small companion app for EverQuest's Resource Hunter feature. It runs on your PC, watches your EverQuest log file, and reports each zone's daily bonuses to the community site as you travel.

Every player who runs it helps fill in the day's bonus map for everyone else.

## Getting Started

1. Open http://fangbreaker.zone or http://frostreaver.zone in your browser and log in using your Discord account.

    ![Login button](docs/login-button.png)

1. Download and run the Resource Hunter Bonus Scout application.


## How it works

1. It follows your character's Everquest log file.
2. It tracks your current zone from `You have entered <Zone>.` messages.
3. When the log shows a Resource Hunter bonus, it matches the bonus to one of the server's bonus types and sends that zone and bonus to the site.

The app gets bonus types and zone names from the site when it starts, so they are never hard-coded.

## Setup

1. **Turn on logging in EverQuest:** type `/log on` in game.
2. **Get an API key:** sign in to the community site and generate a key (it starts with `fz_live_`). Your key links reports to your account, so the site's usual rules, timeouts and bans all apply.
3. **Configure the app:** give it your API key and the path to your EverQuest folder.
4. **Run it** and play as usual.

