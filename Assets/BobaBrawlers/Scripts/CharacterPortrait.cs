using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace BobaBrawlers
{
    // Live isolated renders of the same prefab/rig used by the playable fighter.
    public sealed class CharacterPortrait : MonoBehaviour
    {
        public Fighter Model { get; private set; }
        RenderTexture texture;
        GameObject stage;
        Camera portraitCamera;
        public void Initialize(Fighter prefab, int index)
        {
            stage = new GameObject("Live character portrait " + index);
            Vector3 origin = new Vector3(300 + index * 8, 0, 0);
            stage.transform.position = origin;
            Model = Instantiate(prefab, origin, Quaternion.identity, stage.transform);
            Model.name = GamePreferences.SkinNames[index];
            Model.Body.isKinematic = true; Model.GetComponent<Collider>().enabled = false;
            Model.GetComponent<PlayerController>().enabled = false;
            var look = Model.GetComponent<FighterPresentation>(); look.preview = true; look.ApplySkin(index);
            foreach (var t in Model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            var camera = new GameObject("Portrait camera", typeof(Camera)).GetComponent<Camera>(); portraitCamera = camera; camera.enabled = false;
            camera.transform.SetParent(stage.transform, false); camera.transform.localPosition = new Vector3(.12f, .8f, -4);
            camera.transform.LookAt(origin + Vector3.up * .25f);
            camera.orthographic = true; camera.orthographicSize = 1.15f; camera.nearClipPlane = .1f; camera.farClipPlane = 8;
            camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .12f, .23f, 1);
            texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { name = "Live portrait " + index, antiAliasing = 2 };
            camera.targetTexture = texture;
            GetComponent<RawImage>().texture = texture;
            for (int i = 0; i < 2; i++)
            {
                var light = new GameObject("Portrait softbox", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(stage.transform, false); light.transform.localPosition = new Vector3(i == 0 ? -1.5f : 1.5f, 2, -2);
                light.type = LightType.Point; light.range = 6; light.intensity = i == 0 ? 4 : 2;
                light.color = i == 0 ? Color.white : new Color(.6f, .8f, 1); light.cullingMask = 1 << 30;
            }
        }
        public void Render()
        {
            if (portraitCamera && stage.activeSelf)
                portraitCamera.SubmitRenderRequest(new RenderPipeline.StandardRequest { destination = texture });
        }
        public bool HasRenderedModel()
        {
            Render(); var previous = RenderTexture.active; RenderTexture.active = texture;
            var pixels = new Texture2D(512, 512, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); pixels.Apply();
            Color corner = pixels.GetPixel(5, 5); int different = 0;
            for (int y = 60; y < 460; y += 8) for (int x = 60; x < 460; x += 8)
            { Color c = pixels.GetPixel(x, y); if (Mathf.Abs(c.r - corner.r) + Mathf.Abs(c.g - corner.g) + Mathf.Abs(c.b - corner.b) > .16f) different++; }
            Destroy(pixels); RenderTexture.active = previous; return different > 100;
        }
        void LateUpdate() { Render(); }
        void OnDisable() { if (stage) stage.SetActive(false); }
        void OnDestroy()
        {
            if (stage) Destroy(stage);
            if (texture) { texture.Release(); Destroy(texture); }
        }
    }
}
