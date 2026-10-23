using UnityEngine;

namespace SquishyDumpling.Layout
{
    [RequireComponent(typeof(Camera))]
    public sealed class ResponsiveWorldLayout : MonoBehaviour
    {
        public Camera WorldCamera { get; private set; }
        public Rect UsablePixels { get; private set; }
        public int Revision { get; private set; }
        public Rect VisibleWorldBounds => new Rect((Vector2)WorldCamera.transform.position -
            new Vector2(WorldCamera.orthographicSize * WorldCamera.aspect, WorldCamera.orthographicSize),
            new Vector2(2f * WorldCamera.orthographicSize * WorldCamera.aspect, 2f * WorldCamera.orthographicSize));
        Rect lastViewport, lastSafe;
        bool dirty = true;
        // Explicit override for automated tests/debugging. Production uses Screen.safeArea.
        Rect? safeOverride;

        void Awake() { WorldCamera = GetComponent<Camera>(); WorldCamera.orthographic = true; }
        void OnEnable() { dirty = true; }
        void LateUpdate() { Refresh(); }
        void OnApplicationFocus(bool focused) { if (focused) { dirty = true; Refresh(); } }
        void OnApplicationPause(bool paused) { if (!paused) { dirty = true; Refresh(); } }
        public void SetDeviceSafeAreaOverride(Rect? pixels) { safeOverride = pixels; dirty = true; Refresh(); }
        public void Refresh()
        {
            if (!WorldCamera) WorldCamera = GetComponent<Camera>();
            Rect viewport = WorldCamera.pixelRect;
            Rect safe = safeOverride ?? Screen.safeArea;
            if (!dirty && viewport == lastViewport && safe == lastSafe) return;
            if (!CameraFit.TryCalculate(viewport, safe, out var fit)) return; // Retry transient invalid viewport.
            WorldCamera.orthographic = true;
            WorldCamera.orthographicSize = fit.HalfHeight;
            WorldCamera.transform.position = new Vector3(fit.Center.x, fit.Center.y, -10f);
            UsablePixels = fit.UsablePixels;
            lastViewport = viewport; lastSafe = safe; dirty = false; Revision++;
        }
        public bool TryScreenToWorld(Vector2 screen, out Vector2 world)
        {
            Refresh();
            var plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = WorldCamera.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float distance)) { world = ray.GetPoint(distance); return true; }
            world = default; return false;
        }
    }
}
