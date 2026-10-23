using UnityEngine;

namespace SquishyDumpling.Creation
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BowlDropZone : MonoBehaviour
    {
        public bool Contains(Vector2 world) => isActiveAndEnabled && GetComponent<BoxCollider2D>().enabled &&
            GetComponent<BoxCollider2D>().OverlapPoint(world);
        void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider2D>();
            Gizmos.color = new Color(.15f,.85f,.4f,.4f);
            Gizmos.matrix = transform.localToWorldMatrix; Gizmos.DrawWireCube(box.offset,box.size);
        }
    }
}
