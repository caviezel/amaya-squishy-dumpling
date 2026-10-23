using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SquishyDumpling.Layout;
namespace SquishyDumpling.Creation
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider2D))]
    public sealed class DumplingDeformationController : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IPointerUpHandler,IEndDragHandler,ICancelHandler
    {
        public DumplingDeformationConfiguration configuration;
        public ResponsiveWorldLayout layout;
        public DumplingSpringSimulation Simulation { get; private set; }
        public int? PointerOwner=>grab.PointerId;
        public UnityEvent onPressed=new UnityEvent(),onGrabbed=new UnityEvent(),onReleased=new UnityEvent(),onSoftStrain=new UnityEvent(),onRebound=new UnityEvent(),onSettled=new UnityEvent();
        public UnityEvent<float> onIntensity=new UnityEvent<float>();
        readonly SinglePointerGrab grab=new SinglePointerGrab();
        BoxCollider2D contactCollider; float contactEnvelope=-1;
        Mesh mesh; Material material; DumplingSurfaceAttachment attachment;
        public ParticleSystem SwipeFx { get; private set; }
        Vector3 lastFxPosition;bool hasFxContact;
        bool applicationSuspended,appPaused,appFocused=true;
        public void SetApplicationSuspended(bool value){applicationSuspended=value;if(value)Suspend();}
        public void BindSwipeFx(ParticleSystem prefab)
        {
            if(SwipeFx)Destroy(SwipeFx.gameObject);
            SwipeFx=Instantiate(prefab,transform);SwipeFx.transform.localScale=Vector3.one;
            SwipeFx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);hasFxContact=false;
        }
        void TraceSwipe(Vector2 local)
        {
            if(!SwipeFx)return;
            if(!Hit(local,out _)){hasFxContact=false;return;}
            Vector3 world=transform.TransformPoint(local);
            float distance=hasFxContact?Vector3.Distance(lastFxPosition,world):.2f;
            if(distance<.08f)return;
            int count=Mathf.Clamp(Mathf.FloorToInt(distance/.08f),1,16);
            if(!SwipeFx.isPlaying)SwipeFx.Play(true);
            Vector3 from=hasFxContact?lastFxPosition:world;
            for(int i=1;i<=count;i++)
            {
                Vector3 point=Vector3.Lerp(from,world,(float)i/count);
                if(!Hit(transform.InverseTransformPoint(point),out _))continue;
                SwipeFx.transform.position=point;SwipeFx.Emit(1);
            }
            lastFxPosition=world;hasFxContact=true;
        }
        Vector2 releaseDirection; float previousProjection; bool recovering, strainSignaled, dragged;
        public void Initialize(SpriteRenderer source,DumplingDeformationConfiguration config,ResponsiveWorldLayout worldLayout,AppliedFacePresenter face)
        {
            configuration=config;layout=worldLayout;
            DumplingMeshUtility.Grid(config.meshBounds,Mathf.Clamp(config.columns,10,20),Mathf.Clamp(config.rows,10,20),out var rest,out var triangles);
            Simulation=new DumplingSpringSimulation(rest,triangles,config);
            mesh=DumplingMeshUtility.Create(source.sprite,rest,triangles);GetComponent<MeshFilter>().sharedMesh=mesh;
            material=DumplingMeshUtility.Material(source); var renderer=GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;
            contactCollider=GetComponent<BoxCollider2D>();
            contactCollider.offset=config.meshBounds.center; RefreshContactEnvelope();
            if(face && face.Visual)
            { var child=new GameObject("Deformation-aware Face");child.transform.SetParent(transform,false); attachment=child.AddComponent<DumplingSurfaceAttachment>();attachment.Bind(this,face.Visual); }
            source.enabled=false;
        }
        public bool Hit(Vector2 local,out Vector2 restContact)
        {
            restContact=default;
            if(Simulation==null || !DumplingSurfaceCoordinate.Find(local,Simulation.Positions,Simulation.Triangles,out var coordinate)) return false;
            restContact=coordinate.Resolve(Simulation.Rest);
            var polygon=configuration.contactOutline;
            if(polygon==null || polygon.Length<3) return configuration.meshBounds.Contains(restContact);
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                Vector2 a=polygon[i],b=polygon[j];
                if((a.y>restContact.y)!=(b.y>restContact.y) && restContact.x<(b.x-a.x)*(restContact.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return inside;
        }
        bool PointerLocal(PointerEventData data,out Vector2 local)
        {
            local=default;
            if(!layout || !layout.WorldCamera.pixelRect.Contains(data.position) || !layout.TryScreenToWorld(data.position,out var world)) return false;
            local=transform.InverseTransformPoint(world);return DumplingSpringSimulation.Finite(local);
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(applicationSuspended || !isActiveAndEnabled || PointerOwner.HasValue || !PointerLocal(data,out var local) || !Hit(local,out var rest)) return;
            lastFxPosition=transform.TransformPoint(local);hasFxContact=true;
            grab.Begin(data.pointerId,local,local); Simulation.Begin(rest,local);recovering=false;dragged=false;strainSignaled=false;data.useDragThreshold=false;GameAudioPlayer.Current?.Play(GameSound.Squish);onPressed.Invoke();
        }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data)
        {
            if(applicationSuspended || PointerOwner!=data.pointerId) return;
            if(!PointerLocal(data,out var local)) { Release(); return; }
            TraceSwipe(local);Simulation.Move(local);
            if(!dragged) { dragged=true;GameAudioPlayer.Current?.Play(GameSound.Stretch);onGrabbed.Invoke(); }
        }
        public void OnPointerUp(PointerEventData data) { if(PointerOwner==data.pointerId) Release(); }
        public void OnEndDrag(PointerEventData data) { if(PointerOwner==data.pointerId) Release(); }
        public void OnCancel(BaseEventData data) { Release(); }
        public void Release()
        {
            if(!PointerOwner.HasValue) return;
            hasFxContact=false;if(SwipeFx)SwipeFx.Stop(true,ParticleSystemStopBehavior.StopEmitting);
            grab.Cancel();Simulation.Release();recovering=true;
            Vector2 sum=Vector2.zero; for(int i=0;i<Simulation.Positions.Length;i++) sum+=(Vector2)(Simulation.Positions[i]-Simulation.Rest[i]);
            releaseDirection=sum.normalized;previousProjection=sum.magnitude;GameAudioPlayer.Current?.Play(GameSound.Release);onReleased.Invoke();
        }
        public void Suspend() { Release();hasFxContact=false;if(SwipeFx)SwipeFx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); if(Simulation!=null) Simulation.Reset(); Refresh();recovering=false; }
        void Update()
        {
            if(applicationSuspended || Simulation==null) return;
            if(PointerOwner.HasValue) for(int i=0;i<Input.touchCount;i++) { var touch=Input.GetTouch(i);if(touch.fingerId==PointerOwner.Value && touch.phase==TouchPhase.Canceled) { Release();break; } }
            RefreshContactEnvelope(); Simulation.Advance(Time.unscaledDeltaTime);Refresh();
            if(PointerOwner.HasValue)
            {
                onIntensity.Invoke(Simulation.MaximumDisplacement/configuration.SafeMaxStretch);
                if(!strainSignaled && Simulation.Strain>configuration.SafeMaxStrain*.9f) { strainSignaled=true;onSoftStrain.Invoke(); }
            }
            if(recovering)
            {
                Vector2 sum=Vector2.zero;for(int i=0;i<Simulation.Positions.Length;i++) sum+=(Vector2)(Simulation.Positions[i]-Simulation.Rest[i]);
                float projection=Vector2.Dot(sum,releaseDirection);
                if(previousProjection>0 && projection<=0) onRebound.Invoke(); previousProjection=projection;
                if(Simulation.MaximumDisplacement==0) { recovering=false;onSettled.Invoke(); }
            }
        }
        void RefreshContactEnvelope()
        {
            float stretch=configuration.SafeMaxStretch;
            if(contactEnvelope==stretch) return;
            contactEnvelope=stretch;contactCollider.size=configuration.meshBounds.size+Vector2.one*stretch*2;
        }
        void Refresh() { if(mesh && Simulation!=null) { mesh.vertices=Simulation.Positions;mesh.RecalculateBounds();if(attachment) attachment.Refresh(); } }
        void OnApplicationPause(bool value) { appPaused=value;SetApplicationSuspended(appPaused || !appFocused); }
        void OnApplicationFocus(bool value) { appFocused=value;SetApplicationSuspended(appPaused || !appFocused); }
        void OnDisable() { Suspend(); }
        void OnDestroy()
        {
            grab.Cancel();if(Simulation!=null)Simulation.Reset();
            onPressed.RemoveAllListeners();onGrabbed.RemoveAllListeners();onReleased.RemoveAllListeners();onIntensity.RemoveAllListeners();onSoftStrain.RemoveAllListeners();onRebound.RemoveAllListeners();onSettled.RemoveAllListeners();
            if(mesh) Destroy(mesh);if(material) Destroy(material);
        }
        void OnDrawGizmosSelected()
        {
            if(!configuration || !configuration.debugGizmos || Simulation==null) return;
            Gizmos.color=Color.cyan;
            for(int i=0;i<Simulation.Triangles.Length;i+=3) for(int k=0;k<3;k++) Gizmos.DrawLine(transform.TransformPoint(Simulation.Positions[Simulation.Triangles[i+k]]),transform.TransformPoint(Simulation.Positions[Simulation.Triangles[i+(k+1)%3]]));
        }
    }
}
