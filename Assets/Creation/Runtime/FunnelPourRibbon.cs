using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Dynamic world-space ribbon samples the selected mixture's real artwork.
    // No imported flow sprite stretching or per-frame material/mesh allocations.
    public sealed class FunnelPourRibbon : MonoBehaviour
    {
        const int Segments=24,DropCount=4,DropSides=8;
        readonly Vector3[] vertices=new Vector3[(Segments+1)*2+DropCount*(DropSides+1)];
        readonly Vector2[] uv=new Vector2[(Segments+1)*2+DropCount*(DropSides+1)];
        readonly Color[] colors=new Color[(Segments+1)*2+DropCount*(DropSides+1)];
        Mesh mesh;MeshRenderer view;Material material;Sprite artwork;ParticleSystem impact;
        float emission;
        public bool Visible=>view && view.enabled;
        public void Initialize(Sprite sprite,SpriteRenderer reference,ParticleSystem sparkleSource)
        {
            artwork=sprite;
            mesh=new Mesh{name="Funnel liquid ribbon"};mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;view=gameObject.AddComponent<MeshRenderer>();
            material=new Material(reference.sharedMaterial){name="Selected mixture ribbon"};material.mainTexture=sprite.texture;view.sharedMaterial=material;
            view.sortingLayerID=reference.sortingLayerID;view.sortingOrder=31;view.enabled=false;
            var triangles=new int[Segments*6+DropCount*DropSides*3];int n=0;
            for(int i=0;i<Segments;i++){int k=i*2;triangles[n++]=k;triangles[n++]=k+1;triangles[n++]=k+2;triangles[n++]=k+1;triangles[n++]=k+3;triangles[n++]=k+2;}
            for(int d=0;d<DropCount;d++)for(int j=0;j<DropSides;j++)
            {int k=(Segments+1)*2+d*(DropSides+1);triangles[n++]=k;triangles[n++]=k+1+j;triangles[n++]=k+1+(j+1)%DropSides;}
            mesh.vertices=vertices;mesh.triangles=triangles;
            if(!sparkleSource)return;
            var go=new GameObject("Pour impact sparkles");go.transform.SetParent(transform,false);impact=go.AddComponent<ParticleSystem>();impact.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=impact.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.2f,.4f);main.startSize=new ParticleSystem.MinMaxCurve(.16f,.34f);main.startSpeed=0;main.maxParticles=48;main.gravityModifier=.25f;
            var emit=impact.emission;emit.enabled=false;var shape=impact.shape;shape.enabled=false;
            var renderer=impact.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=sparkleSource.GetComponent<ParticleSystemRenderer>().sharedMaterial;renderer.sortingLayerID=reference.sortingLayerID;renderer.sortingOrder=32;
            var sourceSheet=sparkleSource.textureSheetAnimation;
            if(sourceSheet.enabled && sourceSheet.mode==ParticleSystemAnimationMode.Sprites && sourceSheet.spriteCount>0)
            {var sheet=impact.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Sprites;sheet.AddSprite(sourceSheet.GetSprite(0));}
            var fade=impact.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
        }
        Vector2 Sample(float x,float y)
        {var rect=artwork.textureRect;return new Vector2((rect.x+rect.width*x)/artwork.texture.width,(rect.y+rect.height*y)/artwork.texture.height);}
        public void Display(Vector3 start,Vector3 end,float width,float envelope,float clock)
        {
            view.enabled=envelope>.005f;if(!view.enabled){if(impact && impact.isPlaying)impact.Stop(true,ParticleSystemStopBehavior.StopEmitting);return;}
            Vector3 delta=end-start;Vector3 normal=new Vector3(-delta.y,delta.x,0).normalized;
            for(int i=0;i<=Segments;i++)
            {
                float t=i/(float)Segments;
                // Anchored ends, gently moving centre and narrower receiving end.
                Vector3 center=Vector3.Lerp(start,end,t)+normal*(Mathf.Sin(t*Mathf.PI)*Mathf.Sin(clock*11-t*8)*width*.09f);
                float radius=width*.5f*envelope*Mathf.Lerp(1,.65f,t)*(1+.07f*Mathf.Sin(t*14-clock*16));
                int k=i*2;vertices[k]=center-normal*radius;vertices[k+1]=center+normal*radius;
                float v=.36f+Mathf.Repeat(t*.24f-clock*.2f,.26f);
                uv[k]=Sample(.32f,v);uv[k+1]=Sample(.68f,v);colors[k]=colors[k+1]=Color.white;
            }
            for(int d=0;d<DropCount;d++)
            {
                float t=Mathf.Repeat(clock*1.9f+d/(float)DropCount,1);
                Vector3 center=Vector3.Lerp(start,end,t)+normal*((d%2==0?1:-1)*width*(.5f+Mathf.Sin(t*Mathf.PI)*.18f));
                int k=(Segments+1)*2+d*(DropSides+1);vertices[k]=center;uv[k]=Sample(.5f,.5f);colors[k]=new Color(1,1,1,envelope);
                float radius=width*.1f*envelope*Mathf.Sin(t*Mathf.PI);
                for(int j=0;j<DropSides;j++){float angle=j*Mathf.PI*2/DropSides;vertices[k+1+j]=center+normal*(Mathf.Cos(angle)*radius)+delta.normalized*(Mathf.Sin(angle)*radius*1.7f);uv[k+1+j]=Sample(.5f+Mathf.Cos(angle)*.06f,.5f+Mathf.Sin(angle)*.06f);colors[k+1+j]=colors[k];}
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.RecalculateBounds();
            if(impact && Time.deltaTime>0)
            {
                if(!impact.isPlaying)impact.Play();
                emission+=Time.deltaTime*24*envelope;int count=Mathf.FloorToInt(emission);emission-=count;
                for(int i=0;i<Mathf.Min(count,8);i++)impact.Emit(new ParticleSystem.EmitParams{position=end+new Vector3(Random.Range(-.22f,.22f),0,0),velocity=new Vector3(Random.Range(-.8f,.8f),Random.Range(.5f,1.3f),0)},1);
            }
        }
        public void Hide(bool clear=false)
        {if(view)view.enabled=false;emission=0;if(impact && clear)impact.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
