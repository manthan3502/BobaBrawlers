# Testing and evidence

## Previously executed full-game checks — 23 October 2026

The existing local release was tested before repository preparation. Its retained results report:

| Verification | Result |
| --- | --- |
| Windows production executable | 102 passed, 0 failed; 0 recorded Unity runtime errors/warnings; exit 0 |
| Executable from freshly extracted Windows ZIP | 102 passed, 0 failed; 0 recorded Unity runtime errors/warnings; exit 0 |
| Release build | Succeeded, 0 build errors/warnings |
| Actual Unity Editor Play Mode | PASS; 91 frames advanced; countdown advanced; player alive |
| Bounded 1080p Final Brawl offscreen scenario | 1,197 samples; observed 59.8 FPS; median 16.65 ms, p95 19.46 ms |

These are integration/smoke results from the completed local game, **not newly rerun gameplay tests, NUnit results or a guarantee for every PC**. The measured host used a Ryzen AI 7 350 and Radeon 860M. Repository preparation does not rebuild the player or alter gameplay.

The 102 checks cover production input/charge/dash/braking, real contact/knockback, recent/expired/duplicate attribution, physical hole/edge falls, lives/respawns/protection, all level outcomes/unlocks, AI telegraph/attack, navigation/settings/pause, both requested resolutions, all three character rigs and persistence, rendered portrait pixels, and real AudioSource sample progression/output/muting.

The full local reaction audio was tested beyond event dispatch. The exact supplied clip played for both player fall types, once per elimination, through final-heart/menu transitions, and did not play for ordinary enemy KOs. Decoded demonstration audio matched its waveform. This does **not** claim human listening or physical-speaker verification.

## What a clean Git copy can verify

`Tools/repository_audit.py` inspects the staged file set, local Markdown references, Unity metadata/GUID references, dependency manifest/lockfile, forbidden artifacts, privacy patterns and oversized files. Its detailed findings are written to ignored local QA storage; raw secret values are never printed.

The reaction clip is intentionally omitted for licensing. That is the only intentionally omitted project asset GUID. Eleven existing legacy debug/test GUID fields in two Unity settings assets are also reported separately; ten occur in pinned upstream URP templates and one is a deprecated blend field now replaced by ResourcePath. They are retained unchanged rather than silently treated as resolved. Other project-owned references must resolve. A clean import check uses a fresh source export with no original Library cache and an editor-only observer in that disposable copy to load the enabled scene and inspect gameplay components. It does not build a player, enter a match or modify original source assets. Current results are summarized in the local preparation report; the audit script is reproducible.

The full `--final-qa` suite expects the complete assigned reaction recording, including clip-specific assertions. It will not pass all audio checks in the rights-filtered source as shipped. Assign a cleared clip and adapt clip-specific expectations only after authorizing the audio change; do not mislabel omitted audio as tested.

## Media provenance

Nine documentation WebP images are derived from actual Unity scene/Canvas screenshots. Character portraits are exact crops of the selected cards, not rendered concepts. No image generation or retouching was used. [provenance.json](media/provenance.json) records source filenames, hashes, dimensions and transformations. Files were inspected for metadata and visible private information.

A 75.73-second 1280×720, 30 FPS demonstration remains local. It uses real Unity frames, production controllers/physics and synchronized engine audio, with **scripted input, positioned actors and disabled AI during staged combat**. It is not human gameplay or a real-time performance benchmark. A separate silent copy is prepared for a future approved demonstration; the voiced original remains blocked by recording rights. No video is committed and no external video has been uploaded.

## Repository preparation checks — 24 October 2026

The rights-filtered staged source was exported into a new folder with **no original Library cache**. Unity 6000.6.6f1 imported and compiled it, opened the saved game scene and passed **14/14 saved-source checks**: scene/system references, all level rules, opponent prefabs, three distinct playable meshes, original music/effects, both fonts, no missing scene MonoBehaviours and valid scene post-processing components. The reaction AudioClip was absent as documented. No player was built and no gameplay/audio-playback test was rerun.

The index audit covers 322 source/documentation/media files, 146 Unity metadata GUIDs, 42 direct and 56 locked dependencies, and all local Markdown links. The original 501 game/project/build/media files have matching pre/post hashes. Known legacy URP fields and the excluded reaction are reported explicitly rather than counted as resolved assets. Detailed machine-local reports and logs remain excluded from Git.

## Remaining coverage

Human playtesting, long-session stability, assistive-technology access and broad hardware compatibility are not established by these checks. Procedural artwork is a practical interpretation of the reference direction. Source licensing and the supplied recording's redistribution permission are separate publication decisions.
