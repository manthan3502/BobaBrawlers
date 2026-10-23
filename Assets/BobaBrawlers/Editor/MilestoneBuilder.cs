using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BobaBrawlers.Editor
{
    public static class MilestoneBuilder
    {
        const string Root = "Assets/BobaBrawlers";
        const string Scene = Root + "/Scenes/BobaArena.unity";
        static Material blue, orange, lime, purple, cream, navy, glowBlue, glowOrange, tea;
        // Historical generator retained for the archived foundation. The final
        // upgrade and Build menu are the supported tools for the finished scene.
        public static void Create()
        {
            foreach (string folder in new[] { "Scenes", "Prefabs", "Materials", "Meshes", "Levels", "Resources/UI" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            blue = Mat("Cobalt", Palette.Blue); orange = Mat("Orange", Palette.Orange); lime = Mat("Lime", Palette.Lime);
            purple = Mat("Taro", Palette.Purple); cream = Mat("Cream", Palette.Cream); navy = Mat("Navy", Palette.Navy);
            tea = Mat("Amber milk tea", Palette.Hex("#BF8051"));
            glowBlue = Mat("Blue illuminated trim", Palette.Hex("#46CFFF"), true);
            glowOrange = Mat("Orange illuminated trim", Palette.Orange, true);
            MakePill();
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Root + "/Levels/01_FirstSip.asset");
            if (!level) { level = ScriptableObject.CreateInstance<LevelDefinition>(); AssetDatabase.CreateAsset(level, Root + "/Levels/01_FirstSip.asset"); }
            level.title = "First Sip"; level.opponents = 1; level.knockoutTarget = 2; level.duration = 90; EditorUtility.SetDirty(level);
            var player = MakeFighter(true); var enemy = MakeFighter(false);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.42f, .46f, .65f);
            RenderSettings.fog = false;
            var cameraObject = new GameObject("Arena camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            var camera = cameraObject.GetComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 12f, -15f); camera.transform.LookAt(new Vector3(0, -.4f, -1.4f));
            camera.fieldOfView = 47; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Palette.Hex("#080E27");
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var stripping = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            if (stripping != null) stripping.stripUnusedPostProcessingVariants = false;
            var pcPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            pcPipeline.msaaSampleCount = 4; EditorUtility.SetDirty(pcPipeline);
            Light("Key / warm softbox", new Vector3(40, -30, 0), new Color(1, .89f, .75f), 1.7f);
            Light("Fill / blue", new Vector3(35, 130, 0), new Color(.42f, .63f, 1f), .8f);
            Light("Rim / orange", new Vector3(65, 30, 0), new Color(1, .51f, .25f), .6f);
            var volume = new GameObject("Studio bloom", typeof(Volume)).GetComponent<Volume>(); volume.isGlobal = true;
            var profile = new VolumeProfile(); var bloom = profile.Add<Bloom>(); bloom.intensity.Override(.35f); bloom.threshold.Override(1.1f);
            AssetDatabase.CreateAsset(profile, UniqueAsset("Materials/StudioVolume.asset")); volume.sharedProfile = profile;
            var stage = new GameObject("Bubble tea arena");
            Ring("Playable annular floor - real open center", stage.transform, 1.8f, 8f, -.32f, 0, tea, true);
            Ring("Cup body", stage.transform, 1.84f, 7.96f, -2.5f, -.34f, cream, false);
            Ring("Cobalt outer lip - cosmetic only", stage.transform, 8.01f, 8.3f, -.28f, -.04f, blue, false);
            Ring("Cyan outer light", stage.transform, 8.29f, 8.36f, -.08f, -.03f, glowBlue, false);
            Ring("Orange inner warning rim", stage.transform, 1.82f, 2.05f, .005f, .035f, glowOrange, false);
            Ring("Cyan inner halo", stage.transform, 2.14f, 2.18f, .005f, .025f, glowBlue, false);
            Ring("Outer running line", stage.transform, 7.48f, 7.53f, .005f, .025f, glowOrange, false);
            // Vortex is well below the collision surface: the aperture remains physically open.
            Primitive("Vortex darkness", PrimitiveType.Cylinder, stage.transform, new Vector3(0, -3f, 0), new Vector3(3.5f, .05f, 3.5f), navy);
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20;
                var tick = Primitive("Hazard tick", PrimitiveType.Cube, stage.transform, new Vector3(Mathf.Sin(a) * 2.3f, .025f, Mathf.Cos(a) * 2.3f), new Vector3(.09f, .02f, .27f), i % 2 == 0 ? orange : navy);
                tick.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
            }
            var random = new System.Random(42);
            for (int i = 0; i < 65; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, r = 2.8f + (float)random.NextDouble() * 4.3f;
                Primitive("Tapioca at tea surface", PrimitiveType.Sphere, stage.transform, new Vector3(Mathf.Sin(a) * r, .008f, Mathf.Cos(a) * r), new Vector3(.24f, .03f, .24f), navy);
            }
            // Low rails and decor are visual only, so the entire circumference permits ring-outs.
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                var pillar = Primitive("Stadium light", PrimitiveType.Cube, stage.transform, new Vector3(Mathf.Sin(a) * 11, -1, Mathf.Cos(a) * 11), new Vector3(.15f, 2.5f, .15f), i % 2 == 0 ? glowBlue : glowOrange);
                pillar.transform.Rotate(0, 0, i % 2 == 0 ? -10 : 10);
            }
            var trigger = new GameObject("Fall catcher - center and outer edge", typeof(BoxCollider), typeof(KnockoutZone));
            trigger.transform.position = new Vector3(0, -2.2f, 0); var box = trigger.GetComponent<BoxCollider>(); box.size = new Vector3(60, .5f, 60); box.isTrigger = true;
            var systems = new GameObject("Game systems"); var game = systems.AddComponent<GameManager>();
            game.Level = level; game.playerPrefab = player; game.opponentPrefab = enemy;
            game.UI = systems.AddComponent<UIManager>(); game.Feedback = systems.AddComponent<FeedbackController>();
            var spark = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); spark.name = "Original comic particles";
            AssetDatabase.CreateAsset(spark, UniqueAsset("Materials/Sparks.mat")); game.Feedback.sparkMaterial = spark;
            PlayerSettings.companyName = "Boba Brawlers Student Project"; PlayerSettings.productName = "Boba Brawlers";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Time.fixedDeltaTime = .02f; Physics.gravity = new Vector3(0, -22, 0);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            AssetDatabase.SaveAssets(); Debug.Log("BOBA: Milestone scene, prefabs, materials and Level 1 saved.");
        }
        static string UniqueAsset(string path)
        {
            string full = Root + "/" + path;
            if (AssetDatabase.LoadAssetAtPath<Object>(full)) AssetDatabase.DeleteAsset(full);
            return full;
        }
        static Material Mat(string name, Color color, bool emission = false)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = name;
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .65f); mat.SetFloat("_Metallic", .1f);
            if (emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2f); }
            AssetDatabase.CreateAsset(mat, UniqueAsset("Materials/" + name + ".mat")); return mat;
        }
        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 at, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static Fighter MakeFighter(bool player)
        {
            var root = new GameObject(player ? "Pearl Pete" : "Taro Trouble");
            var body = root.AddComponent<Rigidbody>(); body.mass = 1; body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = root.AddComponent<SphereCollider>(); collider.radius = .56f;
            var physics = new PhysicsMaterial("Arcade grip") { dynamicFriction = .15f, staticFriction = .15f, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(physics, UniqueAsset("Materials/" + (player ? "Pearl" : "Taro") + "Physics.physicMaterial")); collider.sharedMaterial = physics;
            var fighter = root.AddComponent<Fighter>(); fighter.isPlayer = player;
            var visual = new GameObject("Replaceable visual").transform; visual.SetParent(root.transform, false); fighter.visual = visual;
            var pearl = Primitive("Glossy body", player ? PrimitiveType.Sphere : PrimitiveType.Cube, visual, Vector3.zero, Vector3.one * 1.05f, player ? blue : purple);
            if (!player) pearl.transform.localScale = new Vector3(.95f, .95f, .95f);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Expressive eye", PrimitiveType.Sphere, visual, new Vector3(side * .18f, .12f, -.48f), new Vector3(.12f, .17f, .08f), navy);
                Primitive("Eye glint", PrimitiveType.Sphere, visual, new Vector3(side * .18f - .02f, .155f, -.522f), Vector3.one * .035f, cream);
                Primitive("Cheek", PrimitiveType.Sphere, visual, new Vector3(side * .32f, -.04f, -.43f), new Vector3(.12f, .06f, .04f), orange);
            }
            Primitive("Happy mouth", PrimitiveType.Sphere, visual, new Vector3(0, -.12f, -.51f), new Vector3(.12f, .07f, .035f), navy);
            if (player)
            {
                Ring("Sport headband", visual, .495f, .54f, .23f, .33f, orange, false, false);
                Primitive("Headband knot", PrimitiveType.Sphere, visual, new Vector3(.49f, .27f, .06f), Vector3.one * .2f, cream);
                var tail = Primitive("Headband tail", PrimitiveType.Cube, visual, new Vector3(.65f, .2f, .07f), new Vector3(.32f, .1f, .06f), orange); tail.transform.Rotate(0, 0, -25);
                root.AddComponent<PlayerController>();
            }
            else root.AddComponent<EnemyAIController>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + (player ? "PlayerPearl" : "TaroOpponent") + ".prefab");
            Object.DestroyImmediate(root); return prefab.GetComponent<Fighter>();
        }
        static void Ring(string name, Transform parent, float inner, float outer, float bottom, float top, Material material, bool collider, bool save = true)
        {
            const int n = 128;
            var vertices = new Vector3[n * 16]; var triangles = new int[n * 24];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n, b = (i + 1) * Mathf.PI * 2 / n;
                Vector3 ia = new Vector3(Mathf.Sin(a) * inner, top, Mathf.Cos(a) * inner), ib = new Vector3(Mathf.Sin(b) * inner, top, Mathf.Cos(b) * inner);
                Vector3 oa = new Vector3(Mathf.Sin(a) * outer, top, Mathf.Cos(a) * outer), ob = new Vector3(Mathf.Sin(b) * outer, top, Mathf.Cos(b) * outer);
                Vector3 drop = Vector3.up * (top - bottom);
                Vector3[] v = { ia, ib, ob, oa, oa, ob, ob-drop, oa-drop, ib, ia, ia-drop, ib-drop, ia-drop, oa-drop, ob-drop, ib-drop };
                v.CopyTo(vertices, i * 16);
                for (int face = 0; face < 4; face++)
                {
                    int o = i * 16 + face * 4, t = i * 24 + face * 6;
                    triangles[t] = o; triangles[t+1] = o+2; triangles[t+2] = o+1; triangles[t+3] = o; triangles[t+4] = o+3; triangles[t+5] = o+2;
                }
            }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (collider) mesh.SetPreBakeCollisionMesh(false, true);
            // Persist even the small headband meshes so prefab assets have no transient references.
            string meshPath = "Meshes/" + name.Replace("/", "-") + ".asset";
            AssetDatabase.CreateAsset(mesh, UniqueAsset(meshPath));
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        static void Light(string name, Vector3 angles, Color color, float intensity)
        {
            var light = new GameObject(name, typeof(UnityEngine.Light)).GetComponent<UnityEngine.Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(angles); light.color = color; light.intensity = intensity;
            light.shadows = name.StartsWith("Key") ? LightShadows.Soft : LightShadows.None;
        }
        static void MakePill()
        {
            const int size = 64; var texture = new Texture2D(size, size);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 q = new Vector2(Mathf.Max(Mathf.Abs(x - 31.5f) - 15.5f, 0), Mathf.Max(Mathf.Abs(y - 31.5f) - 15.5f, 0));
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(16 - q.magnitude)));
            }
            texture.Apply(); string path = Root + "/Resources/UI/Pill.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteBorder = new Vector4(20, 20, 20, 20); importer.SaveAndReimport();
        }
        public static void Build()
        {
            Create(); Directory.CreateDirectory("Builds/Windows");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = "Builds/Windows/BobaBrawlers.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            Directory.CreateDirectory("QA");
            var details = new System.Text.StringBuilder($"Result: {result.summary.result}\nErrors: {result.summary.totalErrors}\nWarnings: {result.summary.totalWarnings}\nSize: {result.summary.totalSize}\nDuration: {result.summary.totalTime}\n");
            foreach (var step in result.steps) foreach (var message in step.messages)
                if (message.type == LogType.Warning || message.type == LogType.Error) details.AppendLine(message.type + ": " + message.content);
            File.WriteAllText("QA/build-result.txt", details.ToString());
            if (result.summary.result != BuildResult.Succeeded) throw new System.Exception("Windows build failed");
        }
    }
}
