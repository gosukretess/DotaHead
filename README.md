# DotaHead

Simple Discord bot to get Dota2 match details

## Prerequisities

Clone https://github.com/gosukretess/OpenDota-API repository to Libraries/OpenDota folder.

## Configuration

**docker-compose.yml**

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

## Honorable mentions

Dota2 items and heroes database used: https://github.com/mdiller/dotabase
Inspiration from: https://github.com/mdiller/MangoByte
