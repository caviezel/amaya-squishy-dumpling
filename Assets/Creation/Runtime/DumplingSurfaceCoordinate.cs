using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Reusable rest-space attachment, independent of face identity or renderer representation.
    public struct DumplingSurfaceCoordinate
    {
        public int a,b,c;
        public Vector3 weights;
        public Vector3 Resolve(Vector3[] vertices)=>vertices[a]*weights.x+vertices[b]*weights.y+vertices[c]*weights.z;
        public static bool Find(Vector2 point,Vector3[] vertices,int[] triangles,out DumplingSurfaceCoordinate coordinate)
        {
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                Vector2 x=vertices[b]-vertices[a],y=vertices[c]-vertices[a],p=point-(Vector2)vertices[a];
                float determinant=DumplingSpringSimulation.Cross(x,y); if(Mathf.Abs(determinant)<.000001f) continue;
                float v=DumplingSpringSimulation.Cross(p,y)/determinant,w=DumplingSpringSimulation.Cross(x,p)/determinant;
                if(v>=-.0001f && w>=-.0001f && v+w<=1.0001f)
                { coordinate=new DumplingSurfaceCoordinate{a=a,b=b,c=c,weights=new Vector3(1-v-w,v,w)};return true; }
            }
            coordinate=default; return false;
        }
    }
}
