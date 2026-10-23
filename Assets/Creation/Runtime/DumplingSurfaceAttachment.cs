using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Temporary combined-face skin: each face vertex is a barycentric surface attachment.
    // Final split eyes/mouth/decals can reuse the same coordinates without changing session identity.
    public sealed class DumplingSurfaceAttachment : MonoBehaviour
    {
        DumplingDeformationController owner;
        DumplingSurfaceCoordinate[] coordinates;
        Vector3[] vertices;
        Mesh mesh; Material material;
        public void Bind(DumplingDeformationController body,SpriteRenderer source)
        {
            owner=body;
            DumplingMeshUtility.Grid(source.sprite.bounds.ToRect(),9,7,out var faceVertices,out var triangles);
            vertices=new Vector3[faceVertices.Length];coordinates=new DumplingSurfaceCoordinate[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 local=owner.transform.InverseTransformPoint(source.transform.TransformPoint(faceVertices[i]));
                if(!DumplingSurfaceCoordinate.Find(local,owner.Simulation.Rest,owner.Simulation.Triangles,out coordinates[i])) throw new System.InvalidOperationException("Face extends outside deformable surface; refit attachment bounds.");
            }
            mesh=DumplingMeshUtility.Create(source.sprite,faceVertices,triangles);
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=gameObject.AddComponent<MeshRenderer>(); material=DumplingMeshUtility.Material(source);renderer.sharedMaterial=material;
            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder; source.enabled=false;
            Refresh();
        }
        public void Refresh()
        {
            if(!owner || mesh==null) return;
            for(int i=0;i<vertices.Length;i++) vertices[i]=transform.InverseTransformPoint(owner.transform.TransformPoint(coordinates[i].Resolve(owner.Simulation.Positions)));
            mesh.vertices=vertices; mesh.RecalculateBounds();
        }
        void OnDestroy() { if(mesh) Destroy(mesh); if(material) Destroy(material); }
    }
    static class DumplingBoundsExtensions
    { public static Rect ToRect(this Bounds bounds)=>new Rect(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y); }
}
