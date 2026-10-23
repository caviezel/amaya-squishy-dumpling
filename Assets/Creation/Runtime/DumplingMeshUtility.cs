using UnityEngine;
namespace SquishyDumpling.Creation
{
    public static class DumplingMeshUtility
    {
        public static void Grid(Rect rect,int columns,int rows,out Vector3[] vertices,out int[] triangles)
        {
            vertices=new Vector3[columns*rows]; triangles=new int[(columns-1)*(rows-1)*6];
            for(int y=0;y<rows;y++) for(int x=0;x<columns;x++) vertices[y*columns+x]=new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,(float)x/(columns-1)),Mathf.Lerp(rect.yMin,rect.yMax,(float)y/(rows-1)),0);
            int k=0;
            for(int y=0;y<rows-1;y++) for(int x=0;x<columns-1;x++)
            { int a=y*columns+x,b=a+1,c=a+columns,d=c+1; triangles[k++]=a;triangles[k++]=c;triangles[k++]=b;triangles[k++]=b;triangles[k++]=c;triangles[k++]=d; }
        }
        public static Mesh Create(Sprite sprite,Vector3[] vertices,int[] triangles)
        {
            var uv=new Vector2[vertices.Length]; var colors=new Color[vertices.Length];
            Rect texture=sprite.textureRect; Vector2 pivot=sprite.pivot;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector2 pixels=(Vector2)vertices[i]*sprite.pixelsPerUnit+pivot;
                uv[i]=new Vector2((texture.x+pixels.x)/sprite.texture.width,(texture.y+pixels.y)/sprite.texture.height); colors[i]=Color.white;
            }
            var mesh=new Mesh{name="Runtime Dumpling Surface"}; mesh.MarkDynamic(); mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=uv;mesh.colors=colors;mesh.RecalculateBounds(); return mesh;
        }
        public static Material Material(SpriteRenderer source)
        { var material=new Material(source.sharedMaterial){name="Runtime Dumpling Material"}; material.mainTexture=source.sprite.texture; return material; }
    }
}
