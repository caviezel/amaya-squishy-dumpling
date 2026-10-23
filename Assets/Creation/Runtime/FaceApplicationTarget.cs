using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Authored on the body's contact root: target, intent ellipse and output follow its transform.
    public sealed class FaceApplicationTarget : MonoBehaviour
    {
        public Vector2 intentRadii=new Vector2(3.4f,2.4f);
        public bool Contains(Vector2 world)
        { Vector2 p=transform.InverseTransformPoint(world); return intentRadii.x>0 && intentRadii.y>0 && new Vector2(p.x/intentRadii.x,p.y/intentRadii.y).sqrMagnitude<=1; }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=Color.cyan; var old=Gizmos.matrix; Gizmos.matrix=transform.localToWorldMatrix;
            Vector3 previous=new Vector3(intentRadii.x,0,0);
            for(int i=1;i<=64;i++) { float a=i*Mathf.PI*2/64; var p=new Vector3(Mathf.Cos(a)*intentRadii.x,Mathf.Sin(a)*intentRadii.y,0); Gizmos.DrawLine(previous,p); previous=p; }
            Gizmos.DrawLine(Vector3.left*.3f,Vector3.right*.3f); Gizmos.DrawLine(Vector3.down*.3f,Vector3.up*.3f); Gizmos.matrix=old;
        }
    }
}
