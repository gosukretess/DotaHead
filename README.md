# DotaHead

Simple Discord bot to get Dota2 match details

Used in project: https://github.com/mdiller/dotabase

Inspiration and emojis from: https://github.com/mdiller/MangoByte

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
