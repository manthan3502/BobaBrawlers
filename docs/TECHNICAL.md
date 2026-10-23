# Technical guide

All paths below are relative to the repository root and were checked against the existing source. The enabled game scene is `Assets/BobaBrawlers/Scenes/BobaArena.unity`.

## Input, movement and dash

- `Assets/BobaBrawlers/Scripts/PlayerController.cs` reads the Input System keyboard. WASD/arrows produce a clamped movement intent; Space starts/releases a charge. Normal focus loss pauses an active match.
- `Assets/BobaBrawlers/Scripts/Fighter.cs` applies movement in `FixedUpdate` through a Rigidbody. Horizontal velocity approaches the intent at 27 units/s²; player speed is 5.3 units/s. Charging scales ordinary movement to 55%. Falling fighters keep their momentum rather than steering back onto the floor.
- `Assets/BobaBrawlers/Scripts/DashController.cs` charges over 1.15 seconds, dashes for 0.23 seconds at 12–21 units/s and applies a 1-second cooldown after the dash. A per-dash target set prevents repeated hits. Contact knockback scales from 8.5 to 16.5; end-of-dash braking returns horizontal speed to the movement limit.

`Assets/BobaBrawlers/Prefabs/PlayerPearl.prefab` uses a mass-1 Rigidbody and radius-0.56 SphereCollider. Rotation constraints and continuous detection support stable contact. The visual children scale/animate independently of this shared physics root. The arena uses an annular MeshCollider with a real central opening. Decorative rim geometry does not prevent outer-edge falls; higher rounds enable limited ice-bumper colliders.

## Enemy AI

`Assets/BobaBrawlers/Scripts/EnemyAIController.cs` uses approach, reposition, charge, attack, recovery and knocked-out states. It checks whether direct pursuit would cross the hole, follows an annular route when needed, steers inward near the edge, avoids active bumpers and separates from allies. It creates room for a telegraphed dash rather than endlessly pushing at contact distance. `LevelDefinition` sets speed, reaction/charge and attack intervals for each difficulty.

## Knockouts, scoring and respawn

`Assets/BobaBrawlers/Scripts/KnockoutZone.cs` reports trigger entry. `GameManager.cs` also catches alive fighters below y = -1.8. `Fighter.Eliminate` and match-state checks make repeated callbacks harmless.

`Assets/BobaBrawlers/Scripts/ScoreManager.cs` deducts one heart for a player fall. An enemy KO counts only if the player hit it within the four-second attribution window. `GameManager.cs` adds 100 points times the current streak for credited KOs within seven seconds, plus remaining-heart/time bonuses on victory.

Respawn tickets wait 1.8 seconds. Twelve candidate points around radius 5.3 are checked against alive fighters, obstacles and the real floor. `Fighter.Respawn` resets velocity, attribution, dash and intent, then grants 1.5 seconds of attack protection. The final heart, timeout or KO target finishes the round and clears pending tickets. Pause freezes the clock and cancels charge/dash.

## Three playable visual rigs

`Assets/BobaBrawlers/Scripts/FighterPresentation.cs` selects one of three visual children in the player prefab. `GamePreferences.cs` saves the choice using PlayerPrefs.

| Rig | Silhouette and accessories | Presentation |
| --- | --- | --- |
| Boba Bruiser | Sphere, sporty headband, gloves, rounded feet | Energetic bounce; leading punch; raised arms |
| Mango Mayhem | Rounded jelly cube, mango leaf, asymmetric eyebrows | Squishy wobble; elongated dash; victory wiggle |
| Matcha Ninja | Tapered cup, clear lid, short straw, dark ribbons/shoes | Side sway; forward lean; one-arm salute |

All rigs reuse the same movement, dash, health and collision components. `Assets/BobaBrawlers/Scripts/CharacterPortrait.cs` renders the real player prefab in isolated cameras/render textures for selection cards, then releases its stage/textures when navigation removes the cards.

## Scene, UI and effects

`Assets/BobaBrawlers/Scripts/ArenaPresentation.cs` handles menu/gameplay camera positions, stage theme, vortex motion, menu showcases, optional shake and obstacle avoidance. `ArenaObstacle.cs` supplies bumper presentation. Materials, original tea texture and saved meshes live under `Assets/BobaBrawlers/Materials/` and `Meshes/`.

`Assets/BobaBrawlers/Scripts/UIManager.cs` constructs a uGUI Canvas with a 1920×1080 reference resolution and scaling. It provides main menu, live character cards, level locks, how-to, HUD, pause/settings and victory/defeat/champion views. `ArcMeter.cs` draws the curved charge/cooldown meter; `ButtonMotion.cs` provides interaction feedback. `GamePreferences.cs` persists character, settings, unlocks and records. Display/UI fonts are Lilita One and Barlow Bold, with included licenses.

`FighterPresentation.cs` handles breathing, movement, charge squash, dash stretch, impact response, blinking protection and result poses. `FallingEcho.cs` animates a separate visual fall. These effects do not resize the collider. Particle bursts use a reusable pool rather than per-hit particle-system creation.

## Audio

`Assets/BobaBrawlers/Scripts/FeedbackController.cs` owns scene-level AudioSources for music, UI, charge, combat and the player reaction. Original WAV clips load from `Assets/BobaBrawlers/Resources/Audio/`. Separate settings control music and SFX. The player reaction has high priority, survives character deactivation/results/menu changes and ducks music to 32% of its normal level while playing. Enemy KOs use their regular effect.

The local game assigns the exact supplied MP3. That file is deliberately **absent from Git pending redistribution permission**; a clean source copy has an empty optional reaction reference. Runtime code tolerates the missing clip. No synthesized fallback was substituted. The regeneration command requires an assigned/imported reaction clip; normal opening and building the saved scene are separate operations. See [setup](SETUP.md) and [notices](../THIRD_PARTY_NOTICES.md).

## Level progression

`Assets/BobaBrawlers/Scripts/LevelDefinition.cs` defines the ScriptableObject schema. The saved assets are:

| Asset | Enemies / KOs / seconds | Enemy speed | Reaction / attack interval |
| --- | --- | ---: | --- |
| `Assets/BobaBrawlers/Levels/01_FirstSip.asset` | 1 / 2 / 90 | 3.1 | 1.1 / 2.8 s |
| `Assets/BobaBrawlers/Levels/02_SugarRush.asset` | 2 / 3 / 90 | 3.65 | 0.85 / 2.3 s |
| `Assets/BobaBrawlers/Levels/03_FinalBrawl.asset` | 3 / 5 / 100 | 4.1 | 0.65 / 1.9 s |

`GameManager.cs` owns menu/playing/paused/victory/defeat transitions and moves between unlocked levels. `GamePreferences.cs` records completion and per-round best scores. Final Brawl's win uses the champion presentation.

## Authoring and verification boundaries

`Assets/BobaBrawlers/Editor/FinalUpgrade.cs` builds the saved game or explicitly regenerates procedural presentation. Regeneration overwrites generated visual children; it is unnecessary for opening or building the saved source. `MilestoneBuilder.cs` is historical authoring code, not a required setup step.

The opt-in `FinalVerification.cs`, `PerformanceSmoke.cs`, `DemoRecorder.cs` and editor-side `EditorVerification.cs` provide runtime checks, bounded rendering measurements, disclosed staged recording and Editor Play Mode inspection. They run only through explicit flags/methods. See [testing](TESTING.md) for what was actually measured and what is excluded from the public source.
