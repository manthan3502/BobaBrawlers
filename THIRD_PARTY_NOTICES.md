# Asset and third-party notices

## Project-authored content

Procedural character/arena meshes, marbled tea texture, UI shapes, original music compositions and synthesized effects were authored for Boba Brawlers. The art references are not bundled game assets. No blanket source-code or artwork license has been selected; public availability is not itself a redistribution license.

`Tools/generate_audio.py` creates MenuLoop, BrawlLoop, Button, Charge, Dash, Impact, KO, Victory, Defeat and the historical ComicFall effects. ComicFall remains a historical project asset and is not substituted for the supplied player reaction.

## Fonts — SIL Open Font License 1.1

| Font | Upstream | Included license |
| --- | --- | --- |
| Lilita One | [Google Fonts / Lilita One](https://github.com/google/fonts/tree/main/ofl/lilitaone) | [LilitaOne-OFL.txt](Assets/BobaBrawlers/Resources/Fonts/LilitaOne-OFL.txt) |
| Barlow Bold | [Google Fonts / Barlow](https://github.com/google/fonts/tree/main/ofl/barlow) | [Barlow-OFL.txt](Assets/BobaBrawlers/Resources/Fonts/Barlow-OFL.txt) |

Font files and copyright notices are retained unchanged. The public copyright-holder contact in the Lilita license is a required upstream notice, not the project owner's private email.

## Unity and template content

This project uses the Unity editor, URP, uGUI, the Input System and Unity's starter/template files. These are governed by their upstream Unity/package terms, not relicensed by this notice. Dependency versions are in `Packages/manifest.json` and `Packages/packages-lock.json`. Registry package source and its license files are restored by Unity; generated package caches are not committed. Unity's licensed installation produced the existing local Windows runtime files.

## Supplied FAHHH recording — public distribution blocked

The local game uses the exact user-supplied `fahhh.mp3`: 31,901 bytes, stereo MP3 at 44,100 Hz, approximately 1.96 seconds including encoder padding. SHA-256:

```text
b39c7456f35c22e2de99c5c8a06e5f3fc9a911e07d46373e6ee22b092f3fa1b0
```

Its author, source license and redistribution permission have **not been established**. A meme or humorous recording is not assumed to be freely licensed. The original file, scene assignment, audio behavior, Windows build and voiced video are preserved locally.

The separate `Assets/Audio/` tree currently contains only this recording and its Unity metadata. It is excluded from Git, together with builds, archives and voiced video. A clean Git copy therefore has an intentionally unresolved optional `playerFallVocal` reference. Other game assets and original audio remain included. See [setup](docs/SETUP.md) for assigning an independently licensed recording in a separate clone. This is an explicit licensing omission, not a substituted sound.

**Do not publish the supplied clip, the current Windows binary/ZIP or the voiced demonstration until permission is documented.** Alternatively, prepare a future build with an appropriately licensed recording after the owner authorizes that change. No game rebuild or audio replacement was performed during repository preparation.

## Local recording tools

The optional local FFmpeg/imageio tooling is excluded from Git and from the game runtime. Its notices remain with the local tool installation. A future silent demonstration is derived by removing the original recording's audio stream while preserving genuine rendered video frames; the voiced original remains local.
