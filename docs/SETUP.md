# Open, build and play

## Unity source

1. Use Unity Hub to install **Unity 6000.6.6f1**, with Windows build support for a Windows player.
2. Clone `https://github.com/manthan3502/BobaBrawlers.git`. Add its root folder in Unity Hub; it contains `Assets`, `Packages` and `ProjectSettings`.
3. Let Unity restore the pinned packages and create a new Library cache. No existing machine cache is required in version control.
4. Open `Assets/BobaBrawlers/Scenes/BobaArena.unity` and enter Play Mode.

The committed project includes scene, prefab rigs, level definitions, scripts, materials, meshes, original WAV audio, UI assets and fonts with licenses. Unity package sources are resolved from the manifest/lockfile. Generated IDE files, caches, private reports, large captures and builds are deliberately excluded.

## The intentionally omitted reaction clip

The existing local game still has its exact supplied FAHHH recording. Redistribution rights have not been established, so its separate `Assets/Audio/` tree and metadata are excluded from Git. A clean clone may show a missing optional AudioClip reference on `FeedbackController.playerFallVocal`; the runtime checks for null and continues without that reaction. It does not substitute another recording. Original music and other effects remain available.

To prepare your own authorized copy, work in a **separate clone**:

1. Obtain an audio recording you have permission to use and redistribute.
2. Import it using Unity. If you intend to run the existing procedural regeneration tool, use the expected path `Assets/Audio/SFX/fahhh.mp3` and configure a preloaded/decompressed import.
3. Select the scene's Game systems object, assign the imported AudioClip to `FeedbackController.playerFallVocal`, and save the scene.
4. Check player hole/edge falls, final-heart playback, SFX volume and the intended license/credits before creating a distributable.

A new asset receives a new GUID, so importing the same filename alone does not restore the existing serialized assignment. The repository owner must explicitly admit a cleared recording and its `.meta` file to Git; the current ignore rules intentionally quarantine that tree. Do not force-add the supplied recording without resolving permission.

`FinalUpgrade.Upgrade` / `UpgradeAndBuild` require the reaction file and regenerate visuals; they are **not initial setup commands**. Building the existing saved scene does not require regeneration. The full runtime verification suite contains exact-clip audio assertions that require the cleared full audio setup; see [testing](TESTING.md).

## Build a Windows player

Repository preparation did not rebuild the game. For a future authorized build, open Unity Build Profiles, select Windows x86-64 and include the enabled `BobaArena` scene. The saved `FinalUpgrade.Build` editor method is another route to build that scene without regenerating assets. Output is intentionally ignored under `Builds/`.

Use project-relative or your own environment paths when automating Unity; no developer-specific installation path is required by the source. Record the Unity version and test any newly built package independently.

## Install an approved Windows release

1. Download the Windows ZIP from an actual published release when available.
2. Extract the entire ZIP into one folder.
3. Run `BobaBrawlers.exe` from the extracted folder.
4. Keep `BobaBrawlers_Data`, `MonoBleedingEdge`, `D3D12` and the supplied DLLs alongside the executable.

Unity does not need to be installed to play. WASD/arrows move and aim; hold/release Space to charge/dash; Escape pauses. Start First Sip with Play or choose an unlocked round. Settings and records are stored locally through PlayerPrefs.

The present local Windows candidate contains an uncleared recording and is **not yet approved for public release**. Installation instructions do not imply that a public download already exists.
