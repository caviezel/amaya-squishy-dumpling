using System;
using UnityEngine;

namespace SquishyDumpling.Layout
{
    // Pixel input is confined to the layout boundary; actors consume world coordinates.
    public static class CameraFit
    {
        public const float PixelsPerUnit = 100f;
        public static readonly Vector2 ArtworkSize = new Vector2(40.96f, 54.64f);
        public static readonly Vector2 CriticalSize = new Vector2(27f, 48f);
        public static Rect CriticalBounds => new Rect(-CriticalSize * .5f, CriticalSize);
        public static Rect ArtworkBounds => new Rect(-ArtworkSize * .5f, ArtworkSize);

        public readonly struct Result
        {
            public readonly float HalfHeight;
            public readonly Vector2 Center;
            public readonly Rect UsablePixels;
            public Result(float halfHeight, Vector2 center, Rect usablePixels)
            { HalfHeight = halfHeight; Center = center; UsablePixels = usablePixels; }
        }

        public static bool TryCalculate(Rect viewport, Rect deviceSafeArea, out Result result)
        {
            result = default;
            if (!Finite(viewport) || !Finite(deviceSafeArea) || viewport.width <= 0 || viewport.height <= 0)
                return false;
            float left = Mathf.Max(viewport.xMin, deviceSafeArea.xMin);
            float right = Mathf.Min(viewport.xMax, deviceSafeArea.xMax);
            float bottom = Mathf.Max(viewport.yMin, deviceSafeArea.yMin);
            float top = Mathf.Min(viewport.yMax, deviceSafeArea.yMax);
            if (right <= left || top <= bottom) return false;
            var usable = Rect.MinMaxRect(left, bottom, right, top);
            float aspect = viewport.width / viewport.height;
            float fw = usable.width / viewport.width;
            float fh = usable.height / viewport.height;
            float halfHeight = Mathf.Max(CriticalSize.y / (2f * fh), CriticalSize.x / (2f * aspect * fw));
            Vector2 normalizedCenter = (usable.center - viewport.position);
            normalizedCenter = new Vector2(normalizedCenter.x / viewport.width, normalizedCenter.y / viewport.height);
            Vector2 center = new Vector2(-(2f * normalizedCenter.x - 1f) * halfHeight * aspect,
                                        -(2f * normalizedCenter.y - 1f) * halfHeight);
            result = new Result(halfHeight, center, usable);
            return true;
        }

        static bool Finite(Rect r) => Finite(r.x) && Finite(r.y) && Finite(r.width) && Finite(r.height);
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
