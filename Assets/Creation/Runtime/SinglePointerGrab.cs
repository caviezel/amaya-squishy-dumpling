using UnityEngine;

namespace SquishyDumpling.Creation
{
    // One pointer and its original contact offset. Unity conversion is owned by ResponsiveWorldLayout.
    public sealed class SinglePointerGrab
    {
        public int? PointerId { get; private set; }
        public Vector2 Offset { get; private set; }
        public bool Begin(int id, Vector2 pointerWorld, Vector2 objectWorld)
        {
            if (PointerId.HasValue) return false;
            PointerId = id; Offset = objectWorld - pointerWorld; return true;
        }
        public bool TryPosition(int id, Vector2 pointerWorld, out Vector2 position)
        { position = pointerWorld + Offset; return PointerId == id; }
        public bool Release(int id)
        { if (PointerId != id) return false; Cancel(); return true; }
        public void Cancel() { PointerId = null; Offset = Vector2.zero; }
    }
}
