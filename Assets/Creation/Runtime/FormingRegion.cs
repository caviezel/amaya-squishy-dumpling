using UnityEngine;
namespace SquishyDumpling.Creation
{
    public sealed class FormingRegion : MonoBehaviour
    {
        [Tooltip("Broad canonical interaction envelope, including a forgiving edge margin.")]
        public Vector2 radii=new Vector2(5.2f,3.5f);
        public Vector2 Center => transform.position;
        public bool Contains(Vector2 point)
        { point-=Center; return new Vector2(point.x/radii.x,point.y/radii.y).sqrMagnitude<=1; }
        void OnValidate() { radii=new Vector2(Mathf.Max(.1f,radii.x),Mathf.Max(.1f,radii.y)); }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(1,.65f,.3f); Vector3 previous=transform.position+Vector3.right*radii.x;
            for(int i=1;i<=64;i++) { float a=i*Mathf.PI*2/64; Vector3 next=transform.position+new Vector3(Mathf.Cos(a)*radii.x,Mathf.Sin(a)*radii.y,0); Gizmos.DrawLine(previous,next); previous=next; }
        }
    }
}
