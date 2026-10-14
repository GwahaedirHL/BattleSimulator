# Battle Simulator — Unity ECS

A battle between two armies built on **Unity Entities (ECS 1.x)** with **Burst** and the **Job System**; the UI is plain MonoBehaviour MVC.
Test task for Z-Media, completed in 24 hours.

<!-- Add a gameplay GIF here: ![Battle](Docs/battle.gif) -->

## Highlights

- **Gameplay fully in ECS** — spawning, target acquisition, movement, combat, death and the end-of-battle check are all systems.
- **Spatial hashing instead of O(N²)** — targets are found through a grid with an expanding search radius (`TargetAcquireGridSystem`).
- **Burst-compiled jobs** — target search and movement run as `IJobEntity` jobs; allies keep apart via a separate avoidance grid.
- **Stuck detection** — units that stop closing in on their target automatically pick a new one.
- **Player ability** — a meteor with AoE damage, triggered from the UI with a cooldown.
- **UI isolated from ECS** — UI → ECS via one-frame request components, ECS → UI via events.

The architecture is designed to scale to thousands of units on screen.

## Gameplay

The main menu generates two armies. A unit type is a combination of shape (sphere / cube), color and size from `UnitsConfig`; army composition is randomized with a guaranteed number of distinct types.
In the battle scene both armies spawn in grid formation, units lock onto the nearest enemy, close in and attack until one army is destroyed. Army markers are colored per side.

## Architecture

```
UI (MonoBehaviour, MVC)                     ECS (Entities 1.x)
GameUI / MainMenu        (View)
GameUiController / ...   (Controller) ──► request components ──► systems
                                      ◄── battle-end event   ◄── CheckEndBattleSystem
```

### ECS systems

| System | Responsibility |
|---|---|
| `SpawnSystem` | Spawns both armies in grid formation from baked data |
| `ArmyMarkerColorSystem` | Colors army markers once after spawn (URP material property) |
| `TargetAcquireGridSystem` | Builds a spatial grid and finds the nearest enemy with an expanding search (Burst jobs) |
| `BuildAvoidDataSystem` | Builds the ally grid used for separation |
| `MoveToTargetSystem` | Moves units to targets with minimum distance and ally separation (Burst job) |
| `AttackSystem` | Range and cooldown checks, applies damage |
| `DeathSystem` | Destroys dead units and clears targets that pointed at them |
| `RetargetStuckSystem` | Detects units that stopped approaching and forces retargeting |
| `CheckEndBattleSystem` | Counts units per army and raises the battle-end event for the UI |
| `MeteorSpawnSystem` / `MeteorMovementSystem` / `MeteorImpactSystem` | Meteor ability: spawn on request, fall, AoE damage |

### Components

- **Unit data:** `HealthComponent`, `AttackComponent`, `SpeedComponent`, `AttackRangeComponent`, `AttackSpeedComponent`, `AttackCooldownComponent`
- **Tags and state:** `ArmyOneTag`, `ArmyTwoTag`, `GameStartedTag`, `TargetLockedData`, `RetargetData`
- **Requests (one-frame events):** `SpawnRequest`, `MeteorSpawnRequest`, `MeteorMovementRequest`, `MeteorImpactRequest`

### UI (MVC)

- **Views:** `GameUI` (meteor button, cooldown indicator, victory screen, back to menu), `MainMenu` (army generation, composition, battle start)
- **Controllers:** `GameUiController` (creates ECS requests, listens for battle end), `MainMenuController` (builds army data)
- **Cooldown:** a MonoBehaviour timer kept outside ECS — it affects only the UI, not the simulation
- **Entry points:** `EntryPoint` (menu), `GameEntryPoint` (wires game UI to gameplay)

## Project structure

```
Assets/Scripts/
├── Authorings/   bakers for units, spawner, meteor
├── Configs/      UnitsConfig (ScriptableObject), ArmiesConfig
├── Systems/      ECS systems
└── UI/           views, controllers, cooldown
```

## Running

- Unity **6000.3.4f1**, URP, ECS packages (`com.unity.feature.ecs`)
- Open `Assets/Scenes/MainMenuScene.unity` → Play → generate armies → start the battle

## License

MIT
