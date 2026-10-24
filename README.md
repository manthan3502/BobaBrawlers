# BOBA BRAWLERS

**Small Pearls. Big Brawls.**

![Boba Brawlers gameplay in the circular tea arena](docs/media/hero-gameplay.webp)

A colorful **single-player 3D arcade knockout game** made with Unity. Pick a fighter, charge a dash and send rival ingredients flying across a giant bubble-tea arena.

## About the Game

- Pick one of three boba-inspired fighters and battle AI opponents inside a giant bubble-tea arena.
- Move around the bowl and **hold Space to charge a dash**. Release it to knock rivals away.
- Push opponents into the **center hole** or **off the outer edge** to score KOs.
- Start each round with **three hearts**. Reach the KO target before the timer runs out.
- Complete three increasingly challenging levels and unlock the next round as you win.

## Choose Your Fighter

| Boba Bruiser | Mango Mayhem | Matcha Ninja |
| :---: | :---: | :---: |
| <img src="docs/media/boba-bruiser.webp" width="220" alt="Blue Boba Bruiser with headband and boxing gloves"> | <img src="docs/media/mango-mayhem.webp" width="220" alt="Orange Mango Mayhem jelly fighter"> | <img src="docs/media/matcha-ninja.webp" width="220" alt="Green Matcha Ninja cup fighter"> |
| A sporty blue pearl with boxing gloves. | A squishy orange mango-jelly fighter. | A green tea-cup ninja with a lid and straw. |

Each fighter has a different 3D look and animations, with the **same movement and dash abilities**.

## How to Play

1. Open the game and choose your character.
2. Select an unlocked level.
3. Move around the arena and aim toward an opponent.
4. Hold **Space** to charge, then release to dash and knock them into a hazard.
5. Reach the level's KO target before time runs out. Avoid falling and losing all three hearts!

## Controls

| Control | Action |
| --- | --- |
| **WASD** or **Arrow Keys** | Move and aim |
| **Hold Space** | Charge a dash |
| **Release Space** | Dash forward |
| **Escape** | Pause / resume |
| **Mouse** | Navigate menus and select options |

## Three Levels

| Level | AI Opponents | KO Target | Time |
| --- | ---: | ---: | ---: |
| **First Sip** | 1 | 2 | 90 seconds |
| **Sugar Rush** | 2 | 3 | 90 seconds |
| **Final Brawl** | 3 | 5 | 100 seconds |

## Game Screenshots

| Main Menu | Character Selection |
| :---: | :---: |
| <img src="docs/media/main-menu.webp" width="410" alt="Boba Brawlers main menu"> | <img src="docs/media/character-selection.webp" width="410" alt="Live 3D character selection"> |

| Arena / Gameplay | Charged Dash |
| :---: | :---: |
| <img src="docs/media/hero-gameplay.webp" width="410" alt="Boba Brawlers gameplay in the circular tea arena"> | <img src="docs/media/charged-dash.webp" width="410" alt="Charging a dash in the arena"> |

| Knockout | Champion Screen |
| :---: | :---: |
| <img src="docs/media/knockout.webp" width="410" alt="Knocking an opponent out of the arena"> | <img src="docs/media/champion.webp" width="410" alt="Champion result after completing the final round"> |

## Project Structure

```text
Assets/
  BobaBrawlers/
    Scenes/       Main game scene
    Scripts/      Movement, dash, AI, scoring and UI code
    Prefabs/      Reusable player and enemy objects
    Levels/       Level settings
    Meshes/       3D arena and character shapes
    Materials/    Colors and surface appearance
    Resources/    Game audio, UI assets and fonts
    Editor/       Unity editor and build tools
  Settings/       URP graphics settings
Packages/         Unity package dependencies
ProjectSettings/  Unity project settings
docs/media/       Game screenshots used in this README
```

## Open in Unity

1. Clone the repository: `git clone https://github.com/manthan3502/BobaBrawlers.git`
2. Open **Unity Hub** and add the cloned project folder.
3. Use **Unity 6000.6.6f1** and allow the project to import.
4. Open `Assets/BobaBrawlers/Scenes/BobaArena.unity`.
5. Press **Play** to try the game in the Unity Editor.
