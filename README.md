# DotaHead

Simple Discord bot to get Dota2 match details

**docker-compose.yml**

```yaml
version: "3.8"
services:
  dotahead:
    image: kapu1500/dotahead:latest
    volumes:
      - ./sqlite:/app/sqlite
    environment:
      - DOTAHEAD_DiscordToken=YOUR_DISCORD_BOT_TOKEN
      - DOTAHEAD_SteamToken=YOUR_STEAM_TOKEN
```
