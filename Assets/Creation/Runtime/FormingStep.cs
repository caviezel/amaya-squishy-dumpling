using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SquishyDumpling.Layout;
using PrimeTween;
namespace SquishyDumpling.Creation
{
    public sealed class FormingStep : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler, IEndDragHandler, ICancelHandler
    {
        [Header("Prefab visuals")]
        public FormingMaterialPresenter material;
        public SpriteRenderer workspace, carriedMixture;
        public AudioSource audioSource;
        public FormingDefinition definition;
        [Header("Scene binding supplied by flow")]
        public CreationFlowController flow;
        public ResponsiveWorldLayout layout;
        public FormingRegion region;
        public Transform workspaceAnchor, bowlTransition, bowlExit;
        public BowlPresenter bowl;
        public BowlMixingPresentation mixedSurface;
        [Header("Optional feedback / later haptic hooks")]
        public UnityEvent onLanding=new UnityEvent(), onPress=new UnityEvent(), onGesture=new UnityEvent(), onRelease=new UnityEvent(), onMilestone=new UnityEvent(), onComplete=new UnityEvent();
        public FormingProgress Progress { get; private set; }=new FormingProgress();
        public int? PointerOwner => grab.PointerId;
        public bool Succeeded { get; private set; }
        public bool IsAnimating => sequence.isAlive;
        public bool CanInteract => flow && flow.CanFormFor(this);
        readonly SinglePointerGrab grab=new SinglePointerGrab();
        public FunnelFormingPresentation funnelPresentation;
        Sequence sequence;
        int generation, lastMilestone;
        Vector2 lastContact;
        public void BindProgress(FormingProgress progress) { Progress=progress; }
        public IEnumerator Present()
        {
            if(funnelPresentation){Cancel();int token=generation;yield return funnelPresentation.Present(this);Succeeded=token==generation;yield break;}
            Cancel(); int operation=generation; material.ApplyDefinition(definition); workspace.sprite=definition.workspaceSprite; material.Display(0); material.SetOpacity(0);
            workspace.transform.position=workspaceAnchor.position+Vector3.down*10;
            material.transform.position=mixedSurface.mixedLayer.transform.position;
            carriedMixture.sprite=mixedSurface.mixedLayer.sprite; carriedMixture.color=mixedSurface.mixedLayer.color;
            carriedMixture.transform.position=material.transform.position; carriedMixture.transform.localScale=mixedSurface.mixedLayer.transform.lossyScale;
            carriedMixture.enabled=true; mixedSurface.mixedLayer.enabled=false;
            sequence=Sequence.Create().Chain(Tween.Position(workspace.transform,workspaceAnchor.position,definition.workspaceEntrance,Ease.OutCubic))
                .Group(Tween.Position(bowl.transform,bowlTransition.position,definition.workspaceEntrance,Ease.InOutSine))
                .Group(Tween.Rotation(bowl.transform,bowlTransition.rotation,definition.workspaceEntrance,Ease.InOutSine))
                .Chain(Tween.Position(material.transform,region.transform.position,definition.materialTransition,Ease.InOutCubic))
                .Group(Tween.Custom(this,0f,1f,definition.materialTransition,(p,v)=>p.TransferBlend(v),Ease.InOutSine))
                .ChainCallback(this,p=>p.Feedback(p.onLanding,p.definition.landing))
                .Chain(Tween.Scale(material.contactRoot,Vector3.Scale(material.BaseScale,new Vector3(1.09f,.9f,1)),.1f,Ease.OutSine))
                .Chain(Tween.Scale(material.contactRoot,material.BaseScale,definition.landingSettle,Ease.InOutSine));
            Vector3 exit=bowlExit.position; exit.x=Mathf.Min(exit.x,layout.VisibleWorldBounds.xMin-7f);
            sequence=sequence.Group(Tween.Position(bowl.transform,exit,.55f,Ease.InOutCubic));
            while(sequence.isAlive) yield return null;
            if(operation!=generation) yield break;
            carriedMixture.enabled=false; material.SetOpacity(1); material.ClearContact(); Succeeded=true;
        }
        void TransferBlend(float value)
        { material.SetOpacity(value); var color=carriedMixture.color; color.a=(1-value)*.9f; carriedMixture.color=color; }
        public void OnPointerDown(PointerEventData data)
        {
            if(!CanInteract || grab.PointerId.HasValue || !layout.TryScreenToWorld(data.position,out var point)) return;
            if(funnelPresentation)
            {
                if(!funnelPresentation.CanPickup(point))return;
                funnelPresentation.BeginDrag();grab.Begin(data.pointerId,point,bowl.transform.position);data.useDragThreshold=false;return;
            }
            if(!region.Contains(point))return;
            if(!Progress.Begin(point,region.Center,region.radii,definition)) return;
            grab.Begin(data.pointerId,point,point); data.useDragThreshold=false;
            lastContact=point; material.SetContact(point); int operation=generation; Feedback(onPress,definition.press);
            if(operation!=generation) grab.Cancel();
        }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data)
        {
            if(!CanInteract || !layout.TryScreenToWorld(data.position,out var point) || !grab.TryPosition(data.pointerId,point,out var contact)) return;
            if(funnelPresentation){funnelPresentation.DragBowl(contact);return;}
            int gestures=Progress.UsefulGestures, operation=generation;
            bool completed=Progress.Sample(contact); material.SetContact(contact,contact-lastContact);material.Display(Progress.Progress); lastContact=contact;
            if(Progress.UsefulGestures>gestures) Feedback(onGesture,definition.gesture);
            if(operation!=generation || !CanInteract) return;
            int milestone=Mathf.FloorToInt(Progress.Progress*4);
            if(milestone>lastMilestone) { lastMilestone=milestone; Feedback(onMilestone,definition.milestone); }
            if(operation!=generation || !CanInteract) return;
            if(completed) { Release(false); flow.RequestFormingCompletion(this); }
        }
        void Update()
        {
            if(!CanInteract) return;
            if(grab.PointerId.HasValue)
                for(int i=0;i<Input.touchCount;i++) if(Input.GetTouch(i).fingerId==grab.PointerId && Input.GetTouch(i).phase==TouchPhase.Canceled) { Release(); break; }
            if(!funnelPresentation)material.Tick(Progress.Progress,Time.deltaTime);
        }
        public void SuspendInput() { if(grab.PointerId.HasValue)Release(false); }
        public void OnPointerUp(PointerEventData data)
        {
            if(grab.PointerId!=data.pointerId)return;
            for(int i=0;i<Input.touchCount;i++)if(Input.GetTouch(i).fingerId==data.pointerId && Input.GetTouch(i).phase==TouchPhase.Canceled){Release();return;}
            if(!funnelPresentation){Release();return;}
            grab.Cancel();
            if(CanInteract && layout.TryScreenToWorld(data.position,out var pointer) && funnelPresentation.TryAccept(pointer) && flow.RequestFunnelTransfer(this))return;
            funnelPresentation.ReturnBowl();
        }
        public void OnEndDrag(PointerEventData data) { if(grab.PointerId==data.pointerId) Release(); }
        public void OnCancel(BaseEventData data) { Release(); }
        void Release(bool feedback=true)
        { bool owned=grab.PointerId.HasValue; grab.Cancel(); Progress.Release(); if(material) material.ReleaseContact(); if(owned && funnelPresentation)funnelPresentation.ReturnBowl(); if(owned && feedback) Feedback(onRelease,definition.release); }
        public IEnumerator Finish()
        {
            if(funnelPresentation){Release(false);int token=generation;yield return funnelPresentation.Finish();Succeeded=token==generation;yield break;}
            Release(false); material.ClearContact(); material.Display(1); int operation=generation;
            Feedback(onComplete,definition.complete); if(operation!=generation) yield break;
            sequence=Sequence.Create().Chain(Tween.Scale(material.contactRoot,Vector3.Scale(material.BaseScale,new Vector3(.94f,1.06f,1)),definition.completionAnticipation,Ease.InQuad))
                .Chain(Tween.Scale(material.contactRoot,material.BaseScale,definition.completionSettle,Ease.OutBack));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        public void DetachResult(Transform persistentResultRoot)
        {
            material.ClearContact(); material.transform.SetParent(persistentResultRoot,true); workspace.transform.SetParent(persistentResultRoot,true);
            if(carriedMixture) Destroy(carriedMixture.gameObject);
            material=null; workspace=null;
        }
        void Feedback(UnityEvent hook,AudioClip clip)
        { int operation=generation; hook.Invoke(); if(operation==generation && audioSource && clip) audioSource.PlayOneShot(clip); }
        public void Cancel()
        {
            generation++; Succeeded=false; if(funnelPresentation)funnelPresentation.Cancel(); if(sequence.isAlive) sequence.Stop(); grab.Cancel(); Progress.Release();
            if(audioSource) audioSource.Stop(); if(carriedMixture) carriedMixture.enabled=false; if(material) material.ClearContact();
        }
        void OnDisable() { Cancel(); if(flow) flow.Interrupt(); }
    }
}
