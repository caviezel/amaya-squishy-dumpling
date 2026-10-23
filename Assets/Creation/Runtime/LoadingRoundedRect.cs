using UnityEngine;
using UnityEngine.UI;
namespace SquishyDumpling.Creation
{
    // Resolution-independent UI geometry; no loading-screen textures to import or retain.
    public sealed class LoadingRoundedRect : MaskableGraphic
    {
        public float radius=12;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=rectTransform.rect;if(rect.width<=0 || rect.height<=0)return;
            float r=Mathf.Min(radius,Mathf.Min(rect.width,rect.height)*.5f);
            mesh.AddVert(rect.center,color,Vector2.zero);
            const int steps=10;int index=1;
            for(int corner=0;corner<4;corner++)
            {
                Vector2 center=new Vector2(corner<2?rect.xMax-r:rect.xMin+r,corner==0||corner==3?rect.yMin+r:rect.yMax-r);
                for(int i=0;i<=steps;i++)
                {
                    float a=(-90+corner*90+i*90f/steps)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,color,Vector2.zero);
                    if(index>1)mesh.AddTriangle(0,index-1,index);index++;
                }
            }
            mesh.AddTriangle(0,index-1,1);
        }
    }
}
