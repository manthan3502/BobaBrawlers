using UnityEngine;
using UnityEngine.UI;
namespace BobaBrawlers
{
    public sealed class ArcMeter : MaskableGraphic
    {
        [Range(0, 1)] public float amount = 1;
        public float thickness = 12;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); int count = Mathf.Max(1, Mathf.CeilToInt(64 * amount));
            float radius = Mathf.Min(rectTransform.rect.width * .5f, rectTransform.rect.height) - thickness;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(175, 5, i / 64f) * Mathf.Deg2Rad, b = Mathf.Lerp(175, 5, Mathf.Min((i + 1) / 64f, amount)) * Mathf.Deg2Rad;
                Vector2 pa = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), pb = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int o = vh.currentVertCount;
                vh.AddVert(pa * radius, color, Vector2.zero); vh.AddVert(pa * (radius - thickness), color, Vector2.zero);
                vh.AddVert(pb * (radius - thickness), color, Vector2.zero); vh.AddVert(pb * radius, color, Vector2.zero);
                vh.AddTriangle(o, o + 1, o + 2); vh.AddTriangle(o, o + 2, o + 3);
            }
        }
        public void Set(float value, Color tint)
        { if (Mathf.Abs(value - amount) > .002f || color != tint) { amount = value; color = tint; SetVerticesDirty(); } }
    }
}
