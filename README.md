# Tyria Planner HUD · Blish HUD module

In-game popups for upcoming signups and brand-new guild events scheduled on [Tyria Planner](https://tyriaplanner.com). Each notification carries buttons for `/sqjoin`, `/whisper`, and opening the event page in your browser.

A draggable in-game menu (top-bar corner icon) lets you browse your upcoming signups and recent guild events at any time.

## Install

1. Save a GW2 API key on your Tyria Planner profile if you haven't already.
2. Drop `TyriaPlanner.Hud.bhm` into `%userprofile%\Documents\Guild Wars 2\addons\blishhud\modules\`.
3. Enable the module in Blish HUD's manager.
4. Open the module's settings, paste your **GW2 API key** into the field.

The module exchanges your GW2 key for a scoped 90-day token on its first poll. After that, only the scoped bearer is sent on each request.

## Build

```powershell
dotnet build TyriaPlanner.Hud/TyriaPlanner.Hud.csproj -c Release
```

Output: `TyriaPlanner.Hud/bin/Release/TyriaPlanner.Hud.bhm`.

## Settings

| Setting | Default |
|---|---|
| API base URL | `https://tyriaplanner.com` |
| GW2 API key | (empty) |
| Notify upcoming signups | on |
| Notify new guild events | on |
| Poll interval (seconds) | 45 |
| Font size | Medium |

## Scope

The bearer is scoped `addon:read` · it can only read your upcoming events and the guild events you have access to. It cannot modify anything.

## License

MIT · see `LICENSE`.
