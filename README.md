# MMMedical

Medical training game mode mod for **Casualties Unknown Demo**.

## Installation
1. Install [BepInEx](https://github.com/BepInEx/BepInEx) into Casualties Unknown Demo
2. Drop `MMMedical.dll` into `BepInEx/plugins/`

## Features
- Adds **MMM** course to the tutorial list (no unlock required)
- Press **B** to bark → random traumas applied → 5 random medical items spawn on the ground
- **Even rounds**: wish screen pops up (pixel-art black & white UI) — pick 1 of 7 random items, guaranteed to appear in that round's supply
- Treat injuries, then press **B** to verify → 100x speed fast-forward for 5 seconds → score
- Score >5 has 50% chance to increase difficulty (N+1)
- Starting INT=20, hunger/thirst/energy auto-maintained

## Scoring
| Check | Points |
|-------|--------|
| Blood pressure 60–140 | 3.0 |
| No internal bleeding | 0.5 |
| No external bleeding | 0.5 |
| No fibrillation | 1.0 |
| No stroke / hemothorax | 1.0 |
| Consciousness >0.5 | 1.0 |
| Happiness >0.3 | 0.7 |
| Sickness <0.2 | 0.3 |
| Muscle health bonus | muscleHealth/10 |

Death → locked at 0.

## Build
```bash
dotnet build -c Release
```
Requires .NET SDK 9.0+ and `net472` target framework.

## Dependencies
- [BepInEx 5](https://github.com/BepInEx/BepInEx)
- [HarmonyX](https://github.com/pardeike/Harmony)
