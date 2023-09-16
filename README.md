# DotaHead

Simple Discord bot to get Dota2 match details

## Prerequisities

Clone https://github.com/gosukretess/OpenDota-API repository to Libraries/OpenDota folder.

## Configuration and commands

- Create database of your guild players to monitor their matches:

```
/player-add [name] [steamId] [discordId]
/player-remove [name]
/player-add-me [steamId]
/player-list
```

- Configure intervals to check for new matches

```
/set-peak-hours [start] [end]     //24 hours format!
/set-peak-hours-refresh [minutes]
/set-normal-hours-refresh [minutes]
```

- Configure Discord channel where should be posted messages with new matches

```
/set-channel      //run this command in selected channel
```

- Ask for last match, specific match, or display server configuration

```
/last-match
/match [matchId]
/server-config
```

Edit **docker-compose.yml** file to use your bot tokens:

```yaml
version: "3.4"
services:
  dotahead:
    image: kapu1500/dotahead:latest
    volumes:
      - ./sqlite:/app/sqlite
    environment:
      - DOTAHEAD_DiscordToken=YOUR_DISCORD_BOT_TOKEN
      - DOTAHEAD_SteamToken=YOUR_STEAM_TOKEN
```

- How to get Steam Token: https://steamcommunity.com/dev

- How to create Discord Bot Token: https://discord.com/developers/docs/intro

## Honorable mentions

Dota2 items and heroes database used: https://github.com/mdiller/dotabase
Inspiration from: https://github.com/mdiller/MangoByte
