using UnityEngine;
using UnityEngine.EventSystems;

namespace SquishyDumpling.Layout
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class LayoutTestProp : MonoBehaviour, IPointerClickHandler
    {
        public ResponsiveWorldLayout layout;
        public int TapCount { get; private set; }
        public Vector2 LastContact { get; private set; }
        SpriteRenderer visual;
        float reactionRemaining;
        static readonly Color ReactColor = new Color(.5f, 1f, .8f);
        void Awake() { visual = GetComponent<SpriteRenderer>(); }
        public void OnPointerClick(PointerEventData data)
        {
            if (layout && layout.TryScreenToWorld(data.position, out var world) &&
                GetComponent<CircleCollider2D>().OverlapPoint(world))
            { LastContact = world; TapCount++; reactionRemaining = .35f; visual.color = ReactColor; }
        }
        void Update()
        {
            if (reactionRemaining <= 0) return;
            reactionRemaining -= Time.deltaTime;
            if (reactionRemaining <= 0) visual.color = Color.white;
        }
    }
}
