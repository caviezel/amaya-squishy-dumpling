using UnityEngine;
namespace SquishyDumpling.Creation
{
    public sealed class MixingArea : MonoBehaviour
    {
        [Tooltip("Canonical ellipse half-width/height; edit this region in Scene View.")] public Vector2 radii=new Vector2(3.5f,1.15f);
        public Vector2 Center => transform.position;
        public Vector2 Clamp(Vector2 point)
        { Vector2 p=point-Center; float length=new Vector2(p.x/radii.x,p.y/radii.y).magnitude; return Center+p/Mathf.Max(1,length); }
        public bool Contains(Vector2 point) => (point-Clamp(point)).sqrMagnitude<.00001f;
        void OnValidate() { radii=new Vector2(Mathf.Max(.1f,radii.x),Mathf.Max(.1f,radii.y)); }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(.3f,1,.75f); Vector3 previous=transform.position+Vector3.right*radii.x;
            for(int i=1;i<=64;i++) { float a=i*Mathf.PI*2/64; Vector3 next=transform.position+new Vector3(Mathf.Cos(a)*radii.x,Mathf.Sin(a)*radii.y,0); Gizmos.DrawLine(previous,next); previous=next; }
        }
    }
}
