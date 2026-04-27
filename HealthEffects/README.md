# HealthEffects

| | |
|-|-|
| **Mod id** | `healtheffects` |
| **Version** | 0.1.4 |
| **Game** | Vintage Story 1.22.0+ |
| **.NET** | 10+ (`net10.0`) |

**Movement and combat** scale with current **health %** (e.g. move 1:1 with HP, combat stats +1% per 10% HP). **Character** screen: **Health & vigor** panel, warning chat under ~10% HP. Standalone.

## Build

- Set **`Directory.Build.props`** (game install path) or `dotnet build HealthEffects.csproj -c Release -p:VintageStoryPath="..."`  
- Optional: `build.ps1` for the same, with the same `VintageStoryPath` story.

**Deploy** targets the project folder, `out/ForMods/`, and (Windows) AppData `VintagestoryData\Mods\HealthEffects\` unless `HealthEffectsNoDeploy=true`.

## Layout

- `src/`, `assets/`, `modinfo.json`, `MOD_PAGE.md` (long-form notes)

## License

[MIT](LICENSE)

**Author:** adams.
