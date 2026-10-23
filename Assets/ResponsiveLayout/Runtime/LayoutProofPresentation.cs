using UnityEngine;

namespace SquishyDumpling.Layout
{
    public sealed class LayoutProofPresentation : MonoBehaviour
    {
        public ResponsiveWorldLayout layout;
        public bool showGuides = true;
        SpriteRenderer backing;
        Texture2D backingTexture;
        Sprite backingSprite;
        Material guideMaterial;
        GameObject guides;
        void Start()
        {
            backingTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[256];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(224f/255f, 195f/255f, 152f/255f);
            backingTexture.SetPixels(pixels); backingTexture.Apply();
            backingSprite = Sprite.Create(backingTexture, new Rect(0,0,16,16), Vector2.one * .5f, CameraFit.PixelsPerUnit);
            backing = new GameObject("Decorative backing (uniform square)").AddComponent<SpriteRenderer>();
            backing.transform.SetParent(transform, false); backing.sprite = backingSprite;
            backing.sortingLayerName = "Background"; backing.sortingOrder = -10;
            guides = new GameObject("Development guides"); guides.transform.SetParent(transform, false);
            guideMaterial = new Material(Shader.Find("Sprites/Default"));
            Boundary("4096 x 5464 artwork", CameraFit.ArtworkBounds, new Color(1f,.25f,.15f));
            Boundary("2700 x 4800 strict critical region", CameraFit.CriticalBounds, new Color(.1f,.65f,.25f));
            Line("Canonical center", new[]{new Vector3(-.7f,0),new Vector3(.7f,0)}, Color.black);
            Line("Canonical center vertical", new[]{new Vector3(0,-.7f),new Vector3(0,.7f)}, Color.black);
        }
        void LateUpdate()
        {
            layout.Refresh();
            if (backing)
            {
                Rect visible = layout.VisibleWorldBounds;
                backing.transform.position = new Vector3(visible.center.x, visible.center.y, 0);
                float uniform = Mathf.Max(visible.width, visible.height) / backing.sprite.bounds.size.x * 1.02f;
                backing.transform.localScale = Vector3.one * uniform;
            }
            if (guides) guides.SetActive(showGuides);
        }
        void Boundary(string name, Rect r, Color color)
        {
            Line(name, new[]{new Vector3(r.xMin,r.yMin),new Vector3(r.xMin,r.yMax),
                new Vector3(r.xMax,r.yMax),new Vector3(r.xMax,r.yMin),new Vector3(r.xMin,r.yMin)}, color);
        }
        void Line(string name, Vector3[] points, Color color)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(guides.transform,false);
            line.sharedMaterial = guideMaterial; line.useWorldSpace = true; line.positionCount = points.Length;
            line.SetPositions(points); line.startWidth = line.endWidth = .08f; line.startColor = line.endColor = color;
            line.sortingLayerName = "WorldEffects"; line.sortingOrder = 10;
        }
        void OnDestroy()
        {
            if (backingSprite) Destroy(backingSprite);
            if (backingTexture) Destroy(backingTexture);
            if (guideMaterial) Destroy(guideMaterial);
        }
    }
}
