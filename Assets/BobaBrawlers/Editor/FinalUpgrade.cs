using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BobaBrawlers.Editor
{
    // Upgrade the existing scene and prefab visuals. Verified physics roots are retained.
    public static class FinalUpgrade
    {
        const string Root = "Assets/BobaBrawlers/";
        const string ScenePath = Root + "Scenes/BobaArena.unity";
        static Material blue, orange, lime, purple, cream, navy, white, darkPearl, cyan, gold, glass, pink, trailMaterial;
        static Mesh smooth, roundedCube;
        static Font display;
        static readonly List<Renderer> themed = new List<Renderer>();
        static readonly List<Light> accents = new List<Light>();
        static readonly List<GameObject> obstacles = new List<GameObject>();

        [MenuItem("Boba Brawlers/Upgrade existing game to final presentation")]
        public static void Upgrade()
        {
            foreach (var path in new[] { "Materials/Final", "Meshes/Final", "Resources/UI", "Levels", "Prefabs" }) Directory.CreateDirectory(Root + path);
            AssetDatabase.Refresh();
            blue = Material("Cobalt gummy", Palette.Blue, .78f); orange = Material("Mango gummy", Palette.Orange, .78f);
            lime = Material("Matcha gummy", Palette.Lime, .72f); purple = Material("Taro gummy", Palette.Purple, .8f);
            cream = Material("Milk foam", Palette.Cream, .56f); navy = Material("Stadium navy", Palette.Navy * .7f, .4f);
            white = Material("Eye white", Color.white, .65f); darkPearl = Material("Glossy tapioca", Palette.Hex("#24182B"), .92f);
            pink = Material("Peach cheek", Palette.Hex("#F895A2"), .5f);
            cyan = Material("Neon cyan", Palette.Hex("#33CFF5"), .5f, 3.5f); gold = Material("Neon amber", Palette.Orange, .5f, 3.2f);
            glass = Material("Acrylic crystal", new Color(.58f, .82f, 1, .15f), .96f);
            glass.SetFloat("_Surface", 1); glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0); glass.SetFloat("_Cull", 0); glass.SetShaderPassEnabled("ShadowCaster", false); glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.renderQueue = 3000;
            display = AssetDatabase.LoadAssetAtPath<Font>(Root + "Resources/Fonts/LilitaOne.ttf");
            smooth = SaveMesh("Smooth pearl", Superellipsoid(1)); roundedCube = SaveMesh("Soft taro cube", Superellipsoid(.42f));
            MakeSprites(); ConfigureAudio();
            var sparks = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Sparks.mat");
            sparks.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Resources/UI/Star.png"));
            ConfigureParticles(sparks); EditorUtility.SetDirty(sparks);
            string trailPath = Root + "Materials/Final/Dash ribbon.mat";
            trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(trailPath);
            if (!trailMaterial) { trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(trailMaterial, trailPath); }
            ConfigureParticles(trailMaterial); EditorUtility.SetDirty(trailMaterial);
            Fighter player = UpgradeFighter(Root + "Prefabs/PlayerPearl.prefab", 0, true);
            Fighter taro = UpgradeFighter(Root + "Prefabs/TaroOpponent.prefab", 1, false);
            ClonePrefabIfMissing(Root + "Prefabs/TaroOpponent.prefab", Root + "Prefabs/MatchaOpponent.prefab");
            ClonePrefabIfMissing(Root + "Prefabs/TaroOpponent.prefab", Root + "Prefabs/MangoOpponent.prefab");
            Fighter matcha = UpgradeFighter(Root + "Prefabs/MatchaOpponent.prefab", 2, false);
            Fighter mango = UpgradeFighter(Root + "Prefabs/MangoOpponent.prefab", 3, false);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindAnyObjectByType<GameManager>();
            if (!game) throw new Exception("The existing game manager is missing. Upgrade stopped.");
            var old = GameObject.Find("Final presentation"); if (old) Object.DestroyImmediate(old);
            themed.Clear(); accents.Clear(); obstacles.Clear();
            var presentationRoot = new GameObject("Final presentation");
            var art = presentationRoot.AddComponent<ArenaPresentation>();
            art.arenaCamera = Camera.main; art.gameplayPosition = new Vector3(0, 12.4f, -15.4f); art.gameplayTarget = new Vector3(0, -.3f, -1.3f);
            art.menuPosition = new Vector3(0, 8.5f, -14.5f); art.menuTarget = new Vector3(0, .4f, -.1f);
            art.arenaCamera.fieldOfView = 47; art.arenaCamera.backgroundColor = Palette.Hex("#070D24");
            UpgradeArena(presentationRoot.transform, art);
            Stadium(presentationRoot.transform);
            var spots = new List<Transform>();
            foreach (var p in new[] { new Vector3(-4.6f, 1.04f, -3.4f), new Vector3(4.6f, 1.04f, -3.4f), new Vector3(-6.3f, 1.04f, .4f), new Vector3(6.3f, 1.04f, .4f) })
            { var spot = new GameObject("Menu showcase spot").transform; spot.SetParent(presentationRoot.transform); spot.position = p; spots.Add(spot); }
            art.previewPrefabs = new[] { player, taro, matcha, mango }; art.previewSpots = spots.ToArray();
            art.themeRenderers = themed.ToArray(); art.accentLights = accents.ToArray(); art.obstacles = obstacles.ToArray();
            game.Presentation = art; game.playerPrefab = player; game.opponentPrefab = taro; game.ingredientPrefabs = new[] { taro, matcha, mango };
            game.levels = new[] { Level(0, "First Sip", 1, 2, 90, 3.1f, 1.1f, 2.8f, Palette.Blue, "EASY", 0), Level(1, "Sugar Rush", 2, 3, 90, 3.65f, .85f, 2.3f, Palette.Lime, "MEDIUM", 1), Level(2, "Final Brawl", 3, 5, 100, 4.1f, .65f, 1.9f, Palette.Purple, "HARD", 2) };
            game.Level = game.levels[0];
            game.Feedback.sparkMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Sparks.mat");
            game.Feedback.playerFallVocal = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/fahhh.mp3");
            if (!game.Feedback.playerFallVocal) throw new Exception("The supplied fahhh.mp3 must be imported before building.");
            art.arenaCamera.cullingMask &= ~(1 << 30);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.3f, .35f, .5f);
            foreach (var light in Object.FindObjectsByType<Light>())
            {
                if (light.name.StartsWith("Key")) { light.intensity = 1.25f; light.shadowStrength = .68f; light.shadowBias = .035f; }
                if (light.name.StartsWith("Fill")) light.intensity = .62f;
            }
            var volume = GameObject.Find("Studio bloom").GetComponent<Volume>();
            var profile = volume.sharedProfile;
            profile.components.RemoveAll(component => !component);
            var bloom = profile.TryGet<Bloom>(out var existingBloom) ? existingBloom : profile.Add<Bloom>();
            bloom.intensity.Override(.52f); bloom.threshold.Override(1.15f); bloom.scatter.Override(.62f);
            var tonemap = profile.TryGet<Tonemapping>(out var tm) ? tm : profile.Add<Tonemapping>(); tonemap.mode.Override(TonemappingMode.ACES);
            foreach (var component in profile.components)
            { if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile); EditorUtility.SetDirty(component); }
            EditorUtility.SetDirty(profile);
            var stripping = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>(); if (stripping != null) stripping.stripUnusedPostProcessingVariants = false;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"); pipeline.msaaSampleCount = 4; pipeline.renderScale = 1; EditorUtility.SetDirty(pipeline);
            QualitySettings.vSyncCount = 0;
            PlayerSettings.productName = "Boba Brawlers"; PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080; PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); Debug.Log("FINAL UPGRADE: Existing scene and gameplay roots preserved; final assets and campaign saved.");
        }
        static LevelDefinition Level(int i, string title, int enemies, int target, float duration, float speed, float reaction, float interval, Color theme, string difficulty, int obstacleCount)
        {
            string path = Root + "Levels/0" + (i + 1) + "_" + title.Replace(" ", "") + ".asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (!level) { level = ScriptableObject.CreateInstance<LevelDefinition>(); AssetDatabase.CreateAsset(level, path); }
            level.title = title; level.opponents = enemies; level.knockoutTarget = target; level.duration = duration; level.enemySpeed = speed;
            level.enemyReactionTime = reaction; level.enemyAttackInterval = interval; level.theme = theme; level.difficulty = difficulty; level.obstacleCount = obstacleCount;
            level.enemyCharge = i == 0 ? .55f : i == 1 ? .72f : .9f;
            EditorUtility.SetDirty(level); return level;
        }
        static void ClonePrefabIfMissing(string from, string to) { if (!AssetDatabase.LoadAssetAtPath<GameObject>(to)) AssetDatabase.CopyAsset(from, to); }
        static Fighter UpgradeFighter(string path, int kind, bool player)
        {
            var saved = UpgradeIngredient(path, kind, player);
            if (!player) return saved;
            var root = PrefabUtility.LoadPrefabContents(path); var fighter = root.GetComponent<Fighter>(); var visual = fighter.visual;
            for (int i = visual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(visual.GetChild(i).gameObject);
            // Visual scale only: the proven .56 sphere collider and all combat values stay on the unchanged root.
            visual.localScale = Vector3.one * 1.32f; visual.localPosition = Vector3.up * .13f;
            var look = root.GetComponent<FighterPresentation>(); look.characterRigs = new FighterPresentation.Rig[3];
            for (int style = 0; style < 3; style++) look.characterRigs[style] = BuildPlayerRig(visual, style);
            look.tintRenderers = new[] { look.characterRigs[0].body };
            var first = look.characterRigs[0]; look.leftArm = first.leftArm; look.rightArm = first.rightArm; look.leftFoot = first.leftFoot; look.rightFoot = first.rightFoot; look.eyes = first.eyes; look.mouth = first.mouth;
            look.trail.time = .28f; look.trail.widthMultiplier = .82f;
            saved = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<Fighter>(); PrefabUtility.UnloadPrefabContents(root); return saved;
        }
        static FighterPresentation.Rig BuildPlayerRig(Transform parent, int style)
        {
            var rig = new FighterPresentation.Rig(); rig.root = new GameObject(GamePreferences.SkinNames[style]).transform; rig.root.SetParent(parent, false);
            var tint = style == 0 ? blue : style == 1 ? orange : lime;
            Mesh bodyMesh = style == 0 ? smooth : style == 1 ? roundedCube : SaveMesh("Tapered matcha cup", CupMesh());
            rig.body = MeshObject(style == 0 ? "Round pearl body" : style == 1 ? "Squishy jelly cube" : "Tapered tea cup", bodyMesh, rig.root, Vector3.zero, style == 1 ? new Vector3(.94f, .88f, .86f) : Vector3.one, tint).GetComponent<Renderer>();
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = new GameObject(side < 0 ? "Left arm" : "Right arm").transform; arm.SetParent(rig.root, false); arm.localPosition = new Vector3(side * .48f, -.06f, 0);
                MeshObject("Arm", smooth, arm, new Vector3(side * .1f, -.07f, -.03f), new Vector3(.28f, .17f, .2f), tint);
                MeshObject(style == 0 ? "Boxing glove" : "Hand", smooth, arm, new Vector3(side * .17f, -.12f, -.08f), Vector3.one * (style == 0 ? .34f : .2f), style == 0 ? orange : style == 2 ? navy : tint);
                if (style == 0) MeshObject("Glove cuff", smooth, arm, new Vector3(side * .12f, -.08f, -.065f), new Vector3(.18f, .14f, .22f), cream);
                var foot = MeshObject(style == 2 ? "Ninja shoe" : "Rounded sneaker", smooth, rig.root, new Vector3(side * .23f, -.46f, -.08f), new Vector3(.31f, .18f, .4f), style == 2 ? navy : cream).transform;
                Primitive("Sole", PrimitiveType.Cube, foot, new Vector3(0, -.3f, 0), new Vector3(.88f, .18f, .9f), style == 1 ? lime : blue);
                if (side < 0) { rig.leftArm = arm; rig.leftFoot = foot; } else { rig.rightArm = arm; rig.rightFoot = foot; }
            }
            rig.eyes = new GameObject("Expressive eyes").transform; rig.eyes.SetParent(rig.root, false);
            float faceZ = style == 1 ? -.46f : style == 2 ? -.49f : -.49f;
            for (int side = -1; side <= 1; side += 2)
            {
                float eyeHeight = style == 2 ? .21f : .29f;
                MeshObject("Eye white", smooth, rig.eyes, new Vector3(side * .18f, .13f, faceZ), new Vector3(.25f, eyeHeight, .11f), white);
                MeshObject("Pupil", smooth, rig.eyes, new Vector3(side * .18f + (style == 1 ? .025f : 0), .12f, faceZ - .06f), new Vector3(.12f, eyeHeight * .62f, .055f), navy);
                MeshObject("Catchlight", smooth, rig.eyes, new Vector3(side * .18f - .02f, .16f, faceZ - .09f), Vector3.one * .05f, white);
                var brow = Primitive("Brow", PrimitiveType.Capsule, rig.eyes, new Vector3(side * .18f, .31f, faceZ + .015f), new Vector3(.04f, .1f, .04f), navy);
                brow.transform.localRotation = Quaternion.Euler(0, 0, style == 1 ? side < 0 ? 70 : -95 : side * -65);
                if (style != 2) MeshObject("Cheek", smooth, rig.root, new Vector3(side * .33f, -.06f, faceZ + .01f), new Vector3(.12f, .07f, .04f), pink);
            }
            rig.mouth = new GameObject("Smile").transform; rig.mouth.SetParent(rig.root, false); rig.mouth.localPosition = new Vector3(style == 1 ? .04f : 0, -.12f, faceZ - .03f);
            MeshObject("Confident smile", smooth, rig.mouth, Vector3.zero, new Vector3(style == 2 ? .16f : .22f, style == 2 ? .045f : .115f, .04f), navy);
            if (style != 2) MeshObject("Smile tooth", smooth, rig.mouth, new Vector3(-.025f, .025f, -.023f), new Vector3(.1f, .035f, .025f), cream);
            if (style == 0)
            {
                MeshObject("Sport headband", Torus(.46f, .052f), rig.root, new Vector3(0, .28f, 0), Vector3.one, orange);
                Primitive("Headband stripe", PrimitiveType.Cube, rig.root, new Vector3(0, .29f, -.415f), new Vector3(.65f, .055f, .08f), cream);
                var ribbon = Primitive("Sport ribbon", PrimitiveType.Cube, rig.root, new Vector3(.57f, .28f, .08f), new Vector3(.34f, .09f, .045f), orange); ribbon.transform.Rotate(0, 0, -18);
                MeshObject("Knot", smooth, rig.root, new Vector3(.46f, .28f, .05f), Vector3.one * .16f, cream);
            }
            else if (style == 1)
            {
                var leaf = MeshObject("Mango leaf", smooth, rig.root, new Vector3(.13f, .53f, 0), new Vector3(.6f, .11f, .27f), lime); leaf.transform.Rotate(0, 0, 28);
                var stem = Primitive("Mango stem", PrimitiveType.Cylinder, rig.root, new Vector3(-.06f, .52f, 0), new Vector3(.055f, .1f, .055f), navy); stem.transform.Rotate(0, 0, 14);
                // Soft specular jelly highlight, geometry rather than a replacement body tint.
                MeshObject("Jelly shine", smooth, rig.root, new Vector3(-.25f, .26f, -.43f), new Vector3(.19f, .08f, .025f), cream);
            }
            else
            {
                Primitive("Milk foam under lid", PrimitiveType.Cylinder, rig.root, new Vector3(0, .47f, 0), new Vector3(1.01f, .027f, 1.01f), cream);
                Primitive("Clear lid", PrimitiveType.Cylinder, rig.root, new Vector3(0, .53f, 0), new Vector3(1.11f, .035f, 1.11f), glass);
                MeshObject("Clear rolled lid rim", Torus(.54f, .035f), rig.root, new Vector3(0, .52f, 0), Vector3.one, glass);
                var straw = Primitive("Short ninja straw", PrimitiveType.Cylinder, rig.root, new Vector3(.18f, .72f, .06f), new Vector3(.13f, .19f, .13f), lime); straw.transform.Rotate(0, 0, -15);
                MeshObject("Ninja headband", Torus(.49f, .045f), rig.root, new Vector3(0, .31f, 0), Vector3.one, navy);
                Primitive("Ninja band front", PrimitiveType.Cube, rig.root, new Vector3(0, .32f, -.46f), new Vector3(.8f, .08f, .065f), navy);
                for (int i = 0; i < 2; i++) { var ribbon = Primitive("Ninja ribbon", PrimitiveType.Cube, rig.root, new Vector3(.63f, .27f - i * .1f, .12f), new Vector3(.46f, .075f, .04f), navy); ribbon.transform.Rotate(0, 0, -12 - i * 18); }
                for (int i = 0; i < 7; i++) MeshObject("Tea pearl", smooth, rig.root, new Vector3(-.26f + i % 3 * .25f, -.3f + i / 3 * .095f, -.43f), Vector3.one * .075f, darkPearl);
            }
            rig.root.gameObject.SetActive(style == 0); return rig;
        }
        static Mesh CupMesh()
        {
            const int segments = 64; var v = new List<Vector3>(); var t = new List<int>();
            // Open top is closed by the foam and clear lid; bottom cap remains visible during falls.
            for (int y = 0; y <= 1; y++) for (int i = 0; i <= segments; i++)
            { float a = i * Mathf.PI * 2 / segments, r = y == 0 ? .37f : .52f; v.Add(new Vector3(Mathf.Sin(a) * r, y == 0 ? -.44f : .46f, Mathf.Cos(a) * r)); }
            for (int i = 0; i < segments; i++) { int b = i + segments + 1; t.AddRange(new[] { i, i + 1, b, i + 1, b + 1, b }); }
            int center = v.Count; v.Add(new Vector3(0, -.44f, 0));
            for (int i = 0; i < segments; i++) t.AddRange(new[] { center, i + 1, i });
            var mesh = new Mesh { name = "Tapered matcha cup" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Fighter UpgradeIngredient(string path, int kind, bool player)
        {
            var root = PrefabUtility.LoadPrefabContents(path); var fighter = root.GetComponent<Fighter>(); fighter.isPlayer = player;
            Transform visual = fighter.visual;
            for (int i = visual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(visual.GetChild(i).gameObject);
            visual.localPosition = Vector3.up * .06f; visual.localScale = Vector3.one * 1.2f; visual.localRotation = Quaternion.identity;
            var animator = root.GetComponent<FighterPresentation>(); if (!animator) animator = root.AddComponent<FighterPresentation>();
            // Remove only prior presentation children when upgrading again, not physics/controller roots.
            foreach (string name in new[] { "Aim direction", "Charge halo", "Dash trail" }) { var child = root.transform.Find(name); if (child) Object.DestroyImmediate(child.gameObject); }
            var tint = kind == 0 ? blue : kind == 1 ? purple : kind == 2 ? lime : orange;
            var body = MeshObject("Gummy ingredient body", kind == 1 ? roundedCube : smooth, visual, Vector3.zero, kind == 2 ? new Vector3(1, 1.14f, 1) : kind == 3 ? new Vector3(1.05f, 1.04f, .82f) : Vector3.one, tint);
            var tinted = new List<Renderer> { body.GetComponent<Renderer>() };
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = new GameObject(side < 0 ? "Left arm pivot" : "Right arm pivot").transform; arm.SetParent(visual, false); arm.localPosition = new Vector3(side * .48f, -.08f, 0);
                MeshObject("Gummy arm", smooth, arm, new Vector3(side * .09f, -.08f, -.02f), new Vector3(.27f, .19f, .22f), tint);
                MeshObject("Boxing glove", smooth, arm, new Vector3(side * .14f, -.16f, -.06f), new Vector3(.25f, .22f, .24f), player ? orange : tint);
                var foot = MeshObject("Sneaker", smooth, visual, new Vector3(side * .23f, -.45f, -.05f), new Vector3(.28f, .18f, .38f), cream).transform;
                Primitive("Shoe sole", PrimitiveType.Cube, foot, new Vector3(0, -.3f, 0), new Vector3(.9f, .22f, .92f), blue);
                if (side < 0) { animator.leftArm = arm; animator.leftFoot = foot; } else { animator.rightArm = arm; animator.rightFoot = foot; }
            }
            var eyeRoot = new GameObject("Expressive eyes").transform; eyeRoot.SetParent(visual, false); animator.eyes = eyeRoot;
            for (int side = -1; side <= 1; side += 2)
            {
                float z = kind == 3 ? -.405f : -.48f;
                MeshObject("Eye white", smooth, eyeRoot, new Vector3(side * .185f, .13f, z), new Vector3(.235f, .29f, .11f), white);
                MeshObject("Dark pupil", smooth, eyeRoot, new Vector3(side * .185f, .115f, z - .062f), new Vector3(.12f, .17f, .055f), darkPearl);
                MeshObject("Eye sparkle", smooth, eyeRoot, new Vector3(side * .185f - .023f, .16f, z - .091f), Vector3.one * .047f, white);
                var brow = Primitive("Competitive eyebrow", PrimitiveType.Capsule, eyeRoot, new Vector3(side * .18f, .33f, z - .015f), new Vector3(.034f, .085f, .035f), darkPearl); brow.transform.localRotation = Quaternion.Euler(0, 0, side * -70);
                MeshObject("Cheek blush", smooth, visual, new Vector3(side * .325f, -.055f, z + .025f), new Vector3(.12f, .065f, .035f), pink);
            }
            var mouth = new GameObject("Mouth expression").transform; mouth.SetParent(visual, false); mouth.localPosition = new Vector3(0, -.12f, kind == 3 ? -.435f : -.515f); animator.mouth = mouth;
            MeshObject("Open smile", smooth, mouth, Vector3.zero, new Vector3(.18f, .11f, .035f), darkPearl);
            MeshObject("Smile tongue", smooth, mouth, new Vector3(0, -.025f, -.023f), new Vector3(.095f, .04f, .02f), pink);
            if (player)
            {
                MeshObject("Sport headband", Torus(.46f, .055f), visual, new Vector3(0, .28f, 0), Vector3.one, orange);
                Primitive("Headband stripe", PrimitiveType.Cube, visual, new Vector3(0, .29f, -.405f), new Vector3(.67f, .065f, .1f), cream);
                var tail = Primitive("Headband ribbon", PrimitiveType.Cube, visual, new Vector3(.56f, .27f, .08f), new Vector3(.36f, .09f, .04f), orange); tail.transform.Rotate(0, 0, -18);
                MeshObject("Ribbon knot", smooth, visual, new Vector3(.45f, .3f, .04f), Vector3.one * .18f, cream);
            }
            else if (kind == 1)
            {
                Primitive("Taro cap brim", PrimitiveType.Cylinder, visual, new Vector3(0, .49f, 0), new Vector3(.55f, .045f, .55f), gold);
                Primitive("Taro captain hat", PrimitiveType.Cylinder, visual, new Vector3(0, .59f, 0), new Vector3(.38f, .1f, .38f), purple);
                for (int i = 0; i < 7; i++) MeshObject("Taro speck", smooth, visual, new Vector3(-.34f + i % 3 * .3f, -.26f + i / 3 * .19f, -.49f), Vector3.one * .045f, darkPearl);
            }
            else if (kind == 2)
            {
                MeshObject("Matcha foam cap", smooth, visual, new Vector3(0, .48f, 0), new Vector3(.83f, .2f, .8f), cream);
                var straw = Primitive("Matcha straw", PrimitiveType.Cylinder, visual, new Vector3(.18f, .75f, .04f), new Vector3(.12f, .25f, .12f), lime); straw.transform.Rotate(0, 0, -15);
                for (int s = -1; s <= 1; s += 2) { var leaf = MeshObject("Matcha leaf", smooth, visual, new Vector3(s * .18f, .61f, .02f), new Vector3(.39f, .09f, .18f), lime); leaf.transform.Rotate(0, 0, s * 18); }
            }
            else
            {
                var rim = MeshObject("Mango slice rind", Torus(.43f, .035f), visual, new Vector3(0, 0, -.4f), Vector3.one, cream); rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int i = 0; i < 8; i++) { float angle = i * Mathf.PI * 2 / 8; var segment = Primitive("Citrus segment", PrimitiveType.Cube, visual, new Vector3(Mathf.Sin(angle) * .32f, Mathf.Cos(angle) * .32f, -.415f), new Vector3(.02f, .17f, .02f), cream); segment.transform.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg); }
                MeshObject("Mango leaf", smooth, visual, new Vector3(.15f, .56f, 0), new Vector3(.4f, .09f, .2f), lime);
            }
            animator.tintRenderers = tinted.ToArray();
            var arrow = new GameObject("Aim direction").transform; arrow.SetParent(root.transform, false); animator.aimArrow = arrow;
            var arrowMesh = new Mesh { name = "Aim wedge", vertices = new[] { new Vector3(-.19f, 0, .83f), new Vector3(0, 0, 1.17f), new Vector3(.19f, 0, .83f) }, triangles = new[] { 0, 1, 2 } }; arrowMesh.RecalculateNormals();
            MeshObject("Arrow", SaveMesh("Aim wedge", arrowMesh), arrow, Vector3.zero, Vector3.one, gold);
            var halo = MeshObject("Charge halo", Torus(.75f, .025f), root.transform, new Vector3(0, -.5f, 0), Vector3.one, player ? cyan : gold); animator.chargeHalo = halo.transform;
            var trailRoot = new GameObject("Dash trail"); trailRoot.transform.SetParent(root.transform, false); trailRoot.transform.localPosition = Vector3.up * .1f;
            var trail = trailRoot.AddComponent<TrailRenderer>(); trail.sharedMaterial = trailMaterial; trail.time = .2f; trail.minVertexDistance = .04f; trail.widthMultiplier = .6f; trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0); trail.emitting = false; trail.shadowCastingMode = ShadowCastingMode.Off; animator.trail = trail;
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root); return saved.GetComponent<Fighter>();
        }
        static void UpgradeArena(Transform root, ArenaPresentation art)
        {
            var floor = GameObject.Find("Playable annular floor - real open center");
            if (!floor || !floor.GetComponent<MeshCollider>()) throw new Exception("Verified annular collision floor missing");
            var floorMaterial = Material("Marbled milk tea", Color.white, .72f);
            var texture = MakeTeaTexture(); floorMaterial.SetTexture("_BaseMap", texture);
            var mesh = Object.Instantiate(floor.GetComponent<MeshFilter>().sharedMesh); var uv = new Vector2[mesh.vertexCount]; var vertices = mesh.vertices;
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(vertices[i].x / 16f + .5f, vertices[i].z / 16f + .5f);
            mesh.uv = uv; var visualMesh = SaveMesh("Tea surface with planar UV", mesh); floor.GetComponent<MeshFilter>().sharedMesh = visualMesh; floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            // Keep the original proven collider mesh, dimensions and hole unchanged.
            GameObject.Find("Cup body").GetComponent<Renderer>().sharedMaterial = glass;
            var oldRim = GameObject.Find("Cobalt outer lip - cosmetic only"); oldRim.GetComponent<Renderer>().sharedMaterial = blue; themed.Add(oldRim.GetComponent<Renderer>());
            MeshObject("Acrylic rolled upper rim", Torus(8.13f, .16f), root, new Vector3(0, -.02f, 0), Vector3.one, glass);
            MeshObject("Acrylic inner highlight", Torus(8.02f, .025f), root, new Vector3(0, .08f, 0), Vector3.one, white);
            MeshObject("Neon bowl underlight", Torus(7.85f, .06f), root, new Vector3(0, -2.3f, 0), Vector3.one, cyan);
            MeshObject("Cup lower cobalt base", Torus(7.75f, .14f), root, new Vector3(0, -2.45f, 0), Vector3.one, blue);
            var random = new System.Random(92);
            int pearlIndex = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                if (r.name == "Tapioca at tea surface") { r.gameObject.SetActive(pearlIndex++ % 3 == 0); r.sharedMaterial = darkPearl; r.transform.localPosition = new Vector3(r.transform.localPosition.x, .015f, r.transform.localPosition.z); r.transform.localScale = new Vector3(.22f, .12f, .22f); }
            for (int i = 0; i < 65; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, radius = 2.7f + (float)random.NextDouble() * 4.9f;
                MeshObject("Pearl suspended in tea", smooth, root, new Vector3(Mathf.Sin(a) * radius, -1.8f + (float)random.NextDouble() * 1.4f, Mathf.Cos(a) * radius), Vector3.one * (.19f + (float)random.NextDouble() * .15f), darkPearl);
            }
            var vortex = new GameObject("Swirling knockout vortex").transform; vortex.SetParent(root, false); art.vortex = vortex;
            for (int j = 0; j < 3; j++)
            {
                var spiral = MeshObject("Amber spiral " + j, Spiral(j * 120), vortex, Vector3.zero, Vector3.one, j == 1 ? cyan : gold);
                spiral.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48;
                var tick = Primitive("Outer neon dash", PrimitiveType.Cube, root, new Vector3(Mathf.Sin(a) * 7.68f, .025f, Mathf.Cos(a) * 7.68f), new Vector3(.045f, .025f, .17f), i % 2 == 0 ? cyan : gold); tick.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
            }
            for (int i = 0; i < 2; i++)
            {
                var obstacle = new GameObject("Ice bumper " + i); obstacle.transform.SetParent(root, false); obstacle.transform.position = i == 0 ? new Vector3(-4.8f, .32f, 1.1f) : new Vector3(4.8f, .32f, -1.1f);
                var shape = MeshObject("Rounded frozen tea", roundedCube, obstacle.transform, Vector3.zero, new Vector3(.85f, .62f, .85f), glass);
                var frame = MeshObject("Ice luminous core", roundedCube, obstacle.transform, Vector3.zero, new Vector3(.66f, .46f, .66f), cream);
                var box = obstacle.AddComponent<BoxCollider>(); box.size = new Vector3(.82f, .62f, .82f); box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "Materials/PearlPhysics.physicMaterial");
                var controller = obstacle.AddComponent<ArenaObstacle>(); controller.decorativeTop = shape.transform; obstacles.Add(obstacle); obstacle.SetActive(false);
            }
        }
        static void Stadium(Transform root)
        {
            Primitive("Stadium foundation", PrimitiveType.Cylinder, root, new Vector3(0, -3.15f, 0), new Vector3(24, .25f, 24), navy);
            MeshObject("Stadium halo", Torus(11.4f, .07f), root, new Vector3(0, -2.8f, 0), Vector3.one, cyan);
            for (int i = 0; i < 12; i++)
            {
                float a = (i - 5.5f) * Mathf.PI / 12; Vector3 p = new Vector3(Mathf.Sin(a) * 12.5f, -1.2f, Mathf.Cos(a) * 12.5f);
                var arcade = new GameObject("Arcade kiosk " + i).transform; arcade.SetParent(root, false); arcade.position = p; arcade.rotation = Quaternion.LookRotation(-new Vector3(p.x, 0, p.z));
                Primitive("Rounded kiosk cabinet", PrimitiveType.Cube, arcade, new Vector3(0, .8f, 0), new Vector3(1.6f, 3.2f, .9f), navy);
                Primitive("Arcade screen frame", PrimitiveType.Cube, arcade, new Vector3(0, 1.2f, .49f), new Vector3(1.35f, 1.25f, .07f), i % 2 == 0 ? cyan : gold);
                Primitive("Arcade screen", PrimitiveType.Cube, arcade, new Vector3(0, 1.2f, .535f), new Vector3(1.12f, 1.02f, .03f), blue);
                Primitive("Control deck", PrimitiveType.Cube, arcade, new Vector3(0, .35f, .62f), new Vector3(1.4f, .16f, .48f), purple);
                MeshObject("Joystick", smooth, arcade, new Vector3(-.27f, .58f, .65f), Vector3.one * .19f, orange);
                for (int b = 0; b < 3; b++) MeshObject("Arcade button", smooth, arcade, new Vector3(.1f + b * .22f, .46f, .7f), new Vector3(.12f, .06f, .12f), b % 2 == 0 ? lime : pink);
                Sign("PEARL\nPUNCH", arcade, new Vector3(0, 2.1f, .55f), 10, cream, .14f);
                Sign("BB", arcade, new Vector3(0, 1.2f, .565f), 24, white, .14f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var tower = new GameObject("Stadium light rig").transform; tower.SetParent(root, false); tower.position = new Vector3(side * 9, 0, 7.5f);
                Primitive("Truss", PrimitiveType.Cube, tower, new Vector3(0, 2, 0), new Vector3(.22f, 7, .22f), navy);
                var bulb = Material("Stadium softbox", new Color(1, .92f, .8f), .5f, 3);
                for (int row = 0; row < 2; row++) for (int col = 0; col < 5; col++) MeshObject("Softbox bulb", smooth, tower, new Vector3(-.8f + col * .4f, 4.9f + row * .4f, 0), new Vector3(.26f, .26f, .12f), bulb);
                var point = new GameObject("Stadium accent", typeof(Light)).GetComponent<Light>(); point.transform.SetParent(tower, false); point.transform.localPosition = new Vector3(0, 3.5f, -1); point.type = LightType.Point; point.color = side < 0 ? Palette.Blue : Palette.Orange; point.range = 13; point.intensity = 4; point.shadows = LightShadows.None; accents.Add(point);
                var cup = Primitive("Giant tea mascot cup", PrimitiveType.Cylinder, root, new Vector3(side * 10, 2, 3), new Vector3(1.8f, 1.6f, 1.8f), cream);
                Primitive("Mascot lid", PrimitiveType.Cylinder, cup.transform, new Vector3(0, 1.05f, 0), new Vector3(1.13f, .09f, 1.13f), purple);
                Primitive("Mascot straw", PrimitiveType.Cylinder, cup.transform, new Vector3(.15f, 1.5f, 0), new Vector3(.15f, .55f, .15f), orange);
                var floaty = cup.AddComponent<SpinDecoration>(); floaty.spin = new Vector3(0, side * 4, 0); floaty.bob = .12f;
                for (int b = 0; b < 7; b++) MeshObject("Mascot boba", smooth, cup.transform, new Vector3(-.3f + b % 3 * .27f, -.6f + b / 3 * .24f, -.43f), Vector3.one * .13f, darkPearl);
            }
            var banner = new GameObject("Boba neon scoreboard").transform; banner.SetParent(root, false); banner.position = new Vector3(0, 2.5f, 10.5f); banner.rotation = Quaternion.Euler(0, 180, 0);
            Primitive("Scoreboard backing", PrimitiveType.Cube, banner, Vector3.zero, new Vector3(11, 1.35f, .18f), navy);
            Primitive("Scoreboard top neon", PrimitiveType.Cube, banner, new Vector3(0, .67f, -.02f), new Vector3(11.1f, .06f, .25f), cyan);
            Primitive("Scoreboard bottom neon", PrimitiveType.Cube, banner, new Vector3(0, -.67f, -.02f), new Vector3(11.1f, .06f, .25f), gold);
            Sign("BOBA BRAWLERS ARENA", banner, new Vector3(0, .05f, .13f), 48, cream, .13f);
            var random = new System.Random(120);
            for (int i = 0; i < 120; i++)
            {
                float angle = ((float)random.NextDouble() * 250 - 125) * Mathf.Deg2Rad, radius = 10.2f + (float)random.NextDouble() * 1.7f;
                var color = i % 4 == 0 ? blue : i % 4 == 1 ? lime : i % 4 == 2 ? purple : cream;
                MeshObject("Ingredient crowd", smooth, root, new Vector3(Mathf.Sin(angle) * radius, -1.1f + (radius - 10) * .8f, Mathf.Cos(angle) * radius), Vector3.one * .35f, color);
            }
        }
        static void Sign(string title, Transform parent, Vector3 at, int size, Material color, float characterSize)
        {
            var go = new GameObject(title, typeof(TextMesh)); go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = go.GetComponent<TextMesh>(); text.text = title; text.font = display; text.fontSize = size; text.characterSize = characterSize; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = color.GetColor("_BaseColor");
            go.GetComponent<MeshRenderer>().sharedMaterial = display.material;
        }
        static Material Material(string name, Color color, float smoothness, float emission = 0)
        {
            string path = Root + "Materials/Final/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", .08f); m.enableInstancing = true;
            if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * emission); }
            EditorUtility.SetDirty(m); return m;
        }
        static void ConfigureParticles(Material material)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
        }
        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 at, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static GameObject MeshObject(string name, Mesh mesh, Transform parent, Vector3 at, Vector3 scale, Material material)
        {
            if (!AssetDatabase.Contains(mesh)) mesh = SaveMesh(mesh.name, mesh);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = Root + "Meshes/Final/" + name.Replace("/", "-") + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing; }
            mesh.name = name; AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Mesh Superellipsoid(float power)
        {
            const int lat = 28, lon = 48; var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float P(float x) => Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), power);
            for (int y = 0; y <= lat; y++) for (int x = 0; x <= lon; x++)
            { float a = Mathf.PI * y / lat - Mathf.PI / 2, b = Mathf.PI * 2 * x / lon; v.Add(new Vector3(P(Mathf.Cos(a)) * P(Mathf.Cos(b)), P(Mathf.Sin(a)), P(Mathf.Cos(a)) * P(Mathf.Sin(b))) * .52f); uv.Add(new Vector2((float)x / lon, (float)y / lat)); }
            for (int y = 0; y < lat; y++) for (int x = 0; x < lon; x++) { int a = y * (lon + 1) + x, b = a + lon + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var mesh = new Mesh { name = "Rounded ingredient" }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh Torus(float radius, float tube)
        {
            int segments = radius > 2 ? 128 : 64, sides = 10; var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= segments; i++) for (int j = 0; j <= sides; j++) { float a = i * 2 * Mathf.PI / segments, b = j * 2 * Mathf.PI / sides; v.Add(new Vector3(Mathf.Sin(a) * (radius + Mathf.Cos(b) * tube), Mathf.Sin(b) * tube, Mathf.Cos(a) * (radius + Mathf.Cos(b) * tube))); }
            for (int i = 0; i < segments; i++) for (int j = 0; j < sides; j++) { int a = i * (sides + 1) + j, b = a + sides + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var mesh = new Mesh { name = "Torus " + radius.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " " + tube.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh Spiral(float phase)
        {
            const int n = 180; var v = new Vector3[(n + 1) * 2]; var t = new int[n * 6];
            for (int i = 0; i <= n; i++) { float progress = (float)i / n, a = progress * Mathf.PI * 5 + phase * Mathf.Deg2Rad, r = Mathf.Lerp(1.65f, .08f, progress), y = -.12f - progress * 2.6f; for (int j = 0; j < 2; j++) v[i * 2 + j] = new Vector3(Mathf.Sin(a) * (r + (j == 0 ? -.035f : .035f)), y, Mathf.Cos(a) * (r + (j == 0 ? -.035f : .035f))); if (i < n) { int o = i * 6, a0 = i * 2; t[o] = a0; t[o + 1] = a0 + 2; t[o + 2] = a0 + 1; t[o + 3] = a0 + 1; t[o + 4] = a0 + 2; t[o + 5] = a0 + 3; } }
            for (int i = 0; i < t.Length; i += 3) { int swap = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = swap; }
            var mesh = new Mesh { name = "Vortex spiral " + phase, vertices = v, triangles = t }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Texture2D MakeTeaTexture()
        {
            const int n = 1024; var texture = new Texture2D(n, n, TextureFormat.RGB24, false); var colors = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float px = (x - n * .5f) / n, py = (y - n * .5f) / n, r = Mathf.Sqrt(px * px + py * py), a = Mathf.Atan2(py, px);
                float swirl = Mathf.Sin(r * 80 + a * 3 + Mathf.PerlinNoise(px * 7 + 8, py * 7 + 8) * 6) * .5f + .5f;
                Color c = Color.Lerp(Palette.Hex("#B87D53"), Palette.Hex("#F5D4A0"), .5f + swirl * .3f);
                c *= .95f + Mathf.PerlinNoise(x * .014f, y * .014f) * .1f; colors[y * n + x] = c;
            }
            texture.SetPixels(colors); texture.Apply(); string path = Root + "Materials/Final/OriginalMilkTea.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void MakeSprites()
        {
            foreach (var name in new[] { "Rounded", "Circle", "Heart", "Star" })
            {
                const int n = 128; var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float px = (x - 63.5f) / 62f, py = (y - 63.5f) / 62f, alpha;
                    if (name == "Rounded") { Vector2 q = new Vector2(Mathf.Max(Mathf.Abs(px) - .55f, 0), Mathf.Max(Mathf.Abs(py) - .55f, 0)); alpha = Mathf.Clamp01((.44f - q.magnitude) * 62); }
                    else if (name == "Circle") alpha = Mathf.Clamp01((1 - Mathf.Sqrt(px * px + py * py)) * 62);
                    else if (name == "Heart") { float hx = px * 1.15f, hy = py * 1.15f + .15f, value = Mathf.Pow(hx * hx + hy * hy - 1, 3) - hx * hx * hy * hy * hy; alpha = value <= 0 ? 1 : 0; }
                    else { float a = Mathf.Atan2(px, py), r = Mathf.Sqrt(px * px + py * py), boundary = .67f + .28f * Mathf.Cos(a * 5); alpha = Mathf.Clamp01((boundary - r) * 62); }
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                texture.Apply(); string path = Root + "Resources/UI/" + name + ".png"; File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.spriteBorder = name == "Rounded" ? Vector4.one * 32 : Vector4.zero;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
        }
        static void ConfigureAudio()
        {
            var fallImporter = (AudioImporter)AssetImporter.GetAtPath("Assets/Audio/SFX/fahhh.mp3");
            if (!fallImporter) throw new Exception("Missing supplied fahhh.mp3");
            var fallSettings = fallImporter.defaultSampleSettings; fallSettings.loadType = AudioClipLoadType.DecompressOnLoad; fallSettings.compressionFormat = AudioCompressionFormat.PCM;
            fallSettings.preloadAudioData = true; fallImporter.defaultSampleSettings = fallSettings; fallImporter.SaveAndReimport();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "Resources/Audio" }))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)); var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .75f; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
            }
        }
        public static void Build()
        {
            Directory.CreateDirectory("Builds/Windows"); Directory.CreateDirectory("QA/Final");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/BobaBrawlers.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            var details = new StringBuilder($"Result: {result.summary.result}\nErrors: {result.summary.totalErrors}\nWarnings: {result.summary.totalWarnings}\nBytes: {result.summary.totalSize}\nDuration: {result.summary.totalTime}\n");
            foreach (var step in result.steps) foreach (var message in step.messages) if (message.type == LogType.Warning || message.type == LogType.Error) details.AppendLine(message.type + ": " + message.content);
            File.WriteAllText("QA/Final/build-result.txt", details.ToString()); if (result.summary.result != BuildResult.Succeeded) throw new Exception("Final Windows build failed");
        }
        public static void UpgradeAndBuild() { Upgrade(); Build(); }
    }
}
