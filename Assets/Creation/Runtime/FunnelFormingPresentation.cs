using System.Collections;
using UnityEngine;
using PrimeTween;
using UnityEngine.Rendering;
using SquishyDumpling.Layout;
namespace SquishyDumpling.Creation
{
    // Shared bowl drag -> accepted release -> automatic transfer -> reveal/FX presentation.
    public sealed class FunnelFormingPresentation : MonoBehaviour
    {
        public SpriteRenderer funnel,pouch,fill,funnelFill,upperStream,lowerStream;
        public SpriteMask pouchMask,funnelMask;
        public ParticleSystem completionFx;
        public CreationComposition composition;
        FormingStep step;Sequence motion;Vector3 mixtureScale,dragStart;bool dragging,seated;
        Vector3 pouchOffset,pouchScale;
        FunnelPourRibbon bowlRibbon,pouchRibbon;
        SortingGroup bowlGroup;bool previousGroupEnabled;string previousGroupLayer;int previousGroupOrder;
        void BringBowlForward()
        {
            bowlGroup=step.bowl.visualRoot.GetComponent<SortingGroup>();
            if(!bowlGroup){bowlGroup=step.bowl.visualRoot.gameObject.AddComponent<SortingGroup>();bowlGroup.enabled=false;}
            previousGroupEnabled=bowlGroup.enabled;previousGroupLayer=bowlGroup.sortingLayerName;previousGroupOrder=bowlGroup.sortingOrder;
            bowlGroup.sortingLayerName="GameplayFront";bowlGroup.sortingOrder=30;bowlGroup.enabled=true;
        }
        void RestoreBowlSorting()
        {
            if(!bowlGroup)return;bowlGroup.enabled=previousGroupEnabled;bowlGroup.sortingLayerName=previousGroupLayer;bowlGroup.sortingOrder=previousGroupOrder;bowlGroup=null;
        }
        public bool Accepted { get; private set; }
        public bool Returning=>motion.isAlive && !Accepted;
        public bool FxStarted { get; private set; }
        public bool FxCompleted { get; private set; }
        public float DisplayedFill { get; private set; }
        public float FunnelLevel { get; private set; }
        public Rect DropAssist=>new Rect(composition.funnelPosition+composition.funnelDropOffset-composition.funnelDropSize*.5f,composition.funnelDropSize);
        Bounds BowlBounds
        {
            get { var b=step.bowl.shellRenderers[0].bounds;foreach(var shell in step.bowl.shellRenderers)if(shell && shell.enabled)b.Encapsulate(shell.bounds);return b; }
        }
        public IEnumerator Present(FormingStep owner)
        {
            step=owner;BringBowlForward();Accepted=dragging=FxStarted=FxCompleted=false;
            step.material.ApplyDefinition(step.definition);step.material.ClearContact();step.material.Display(1);step.material.SetOpacity(0);
            step.material.transform.position=composition.bodyPosition;step.material.transform.localScale=Vector3.one*composition.customizationScale;
            step.carriedMixture.enabled=false;fill.sprite=funnelFill.sprite=step.definition.shapeStates[0];
            fill.enabled=funnelFill.enabled=upperStream.enabled=lowerStream.enabled=false;DisplayedFill=FunnelLevel=0;
            if(!bowlRibbon)
            {
                bowlRibbon=CreateRibbon("Bowl pouring ribbon",upperStream);
                pouchRibbon=CreateRibbon("Pouch filling ribbon",lowerStream);
            }
            bowlRibbon.Hide(true);pouchRibbon.Hide(true);
            mixtureScale=step.mixedSurface.mixedLayer.transform.localScale;
            // Match the finished dumpling's width while keeping the pouch neck under the funnel.
            float previousScale=pouch.transform.localScale.x;
            var finishedState=step.material.states[step.material.states.Length-1];
            float finishedWidth=finishedState.transform.TransformVector(Vector3.right*finishedState.sprite.bounds.size.x).magnitude;
            float pouchWidth=pouch.sprite.bounds.size.x;
            pouchScale=Vector3.one*(finishedWidth/pouchWidth);
            pouch.transform.localScale=pouchScale;
            pouchOffset=composition.pouchPosition-composition.funnelPosition;
            pouchOffset.y-=pouch.sprite.bounds.max.y*(pouchScale.y-previousScale);
            seated=true;
            funnel.transform.position=new Vector3(-14,0,0);pouch.transform.position=funnel.transform.position+pouchOffset;
            funnel.enabled=pouch.enabled=false;
            yield return new WaitForSeconds(composition.mixedResultHold);
            funnel.enabled=pouch.enabled=true;
            motion=Sequence.Create().Chain(Tween.Position(step.bowl.transform,(Vector3)composition.formingBowlPosition,.65f,Ease.InOutCubic))
                .Group(Tween.Position(funnel.transform,(Vector3)composition.funnelPosition,.6f,Ease.OutCubic));
            while(motion.isAlive)yield return null;
            UpdateHitArea();
        }
        public bool CanPickup(Vector2 pointer)=>step && !Accepted && !motion.isAlive && step.GetComponent<Collider2D>().OverlapPoint(pointer);
        public void BeginDrag(){dragging=true;dragStart=step.bowl.transform.position;}
        public void DragBowl(Vector2 position)
        {
            if(!dragging || Accepted)return;
            var bounds=BowlBounds;Vector2 centerOffset=bounds.center-step.bowl.transform.position;
            Rect safe=CameraFit.CriticalBounds;
            position.x=Mathf.Clamp(position.x,safe.xMin+bounds.extents.x-centerOffset.x,safe.xMax-bounds.extents.x-centerOffset.x);
            position.y=Mathf.Clamp(position.y,safe.yMin+bounds.extents.y-centerOffset.y,safe.yMax-bounds.extents.y-centerOffset.y);
            step.bowl.transform.position=position;UpdateHitArea();
        }
        public bool TryAccept(Vector2 pointer)
        {
            if(!dragging || Accepted)return false;
            dragging=false;
            if(Vector3.Distance(dragStart,step.bowl.transform.position)<1 || !(DropAssist.Contains(pointer)||DropAssist.Contains(step.bowl.transform.position)))return false;
            Accepted=true;return true;
        }
        public void ReturnBowl()
        {
            dragging=false;if(Accepted || !step)return;
            if(motion.isAlive)motion.Stop();
            if(Vector3.SqrMagnitude(step.bowl.transform.position-(Vector3)composition.formingBowlPosition)<.000001f)return;
            motion=Sequence.Create().Chain(Tween.Position(step.bowl.transform,(Vector3)composition.formingBowlPosition,.35f,Ease.OutCubic));
        }
        void LateUpdate(){if(seated && pouch && funnel)pouch.transform.position=funnel.transform.position+pouchOffset;if(step)UpdateHitArea();}
        void UpdateHitArea()
        {
            var bounds=BowlBounds;var box=step.GetComponent<BoxCollider2D>();box.offset=step.transform.InverseTransformPoint(bounds.center);box.size=(Vector2)bounds.size+Vector2.one*.4f;
        }
        public IEnumerator Finish()
        {
            // Pointer ownership and session state have already transferred to this sequence.
            motion=Vector3.SqrMagnitude(step.bowl.transform.position-(Vector3)composition.bowlPourPosition)>.000001f
                ?Sequence.Create().Chain(Tween.Position(step.bowl.transform,(Vector3)composition.bowlPourPosition,.5f,Ease.InOutCubic)):Sequence.Create().ChainDelay(.5f);
            var rotation=Quaternion.Euler(0,0,composition.bowlPourAngle);
            if(Quaternion.Angle(step.bowl.transform.rotation,rotation)>.001f)motion=motion.Group(Tween.Rotation(step.bowl.transform,rotation,.5f,Ease.InOutSine));
            motion=motion.ChainCallback(this,p=>{GameAudioPlayer.Current?.SetLoop(GameSound.FunnelLoop,true,p);}).Chain(Tween.Custom(this,0,1,composition.transferDuration,(p,v)=>p.Transfer(v),Ease.Linear));
            while(motion.isAlive)yield return null;
            GameAudioPlayer.Current?.StopLoop(this);Transfer(1);bowlRibbon.Hide();pouchRibbon.Hide();upperStream.enabled=lowerStream.enabled=funnelFill.enabled=false;step.mixedSurface.mixedLayer.enabled=false;
            seated=false;
            Rect visible=step.layout.VisibleWorldBounds;float rightExtent=composition.bowlWidth*.5f;
            foreach(var shell in step.bowl.shellRenderers)if(shell && shell.enabled)rightExtent=Mathf.Max(rightExtent,shell.bounds.max.x-step.bowl.transform.position.x);
            motion=Sequence.Create().Chain(Tween.Position(step.bowl.transform,new Vector3(visible.xMin-rightExtent-1,composition.bowlPourPosition.y,0),.5f,Ease.InOutCubic))
                .Group(Tween.Position(funnel.transform,new Vector3(visible.xMax+funnel.bounds.extents.x+1,2,0),.5f,Ease.InCubic));
            while(motion.isAlive)yield return null;
            step.bowl.SetShellVisible(false);
            motion=Vector3.SqrMagnitude(pouch.transform.position-(Vector3)composition.bodyPosition)>.000001f
                ?Sequence.Create().Chain(Tween.Position(pouch.transform,(Vector3)composition.bodyPosition,.45f,Ease.InOutCubic)):Sequence.Create().ChainDelay(.2f);
            motion=motion.Chain(Tween.Scale(pouch.transform,pouchScale*1.06f,.13f,Ease.OutSine))
                .Chain(Tween.Custom(this,0,1,.4f,(p,v)=>p.Reveal(v),Ease.InOutSine))
                .Group(Tween.Scale(step.material.contactRoot,new Vector3(1.06f,.94f,1),.18f,Ease.OutSine))
                .Chain(Tween.Scale(step.material.contactRoot,Vector3.one,.4f,Ease.OutBack));
            while(motion.isAlive)yield return null;
            pouch.gameObject.SetActive(false);step.material.SetOpacity(1);step.material.ClearContact();
            GameAudioPlayer.Current?.Play(GameSound.StepComplete);FxStarted=true;completionFx.transform.position=step.material.transform.position;completionFx.Play(true);
            while(completionFx.IsAlive(true))yield return null;
            FxCompleted=true;
        }
        void Transfer(float value)
        {
            float poured=Mathf.SmoothStep(0,1,Mathf.Clamp01(value/.72f));
            DisplayedFill=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,1,value));
            FunnelLevel=Mathf.Min(Mathf.SmoothStep(0,1,value/.25f),Mathf.SmoothStep(0,1,(1-value)/.3f));
            step.Progress.SetAutomaticTransferProgress(value);
            step.mixedSurface.mixedLayer.transform.localScale=mixtureScale*Mathf.Lerp(1,.015f,poured);
            fill.enabled=DisplayedFill>.001f;fill.transform.localScale=Vector3.one*Mathf.Lerp(.02f,.40f,Mathf.Sqrt(DisplayedFill));fill.transform.localPosition=new Vector3(0,-.9f-(1-DisplayedFill)*1.2f,0);
            funnelFill.transform.position=funnelMask.transform.position-Vector3.up*(1-FunnelLevel)*.5f;funnelFill.transform.localScale=Vector3.one*Mathf.Lerp(.15f,.4f,Mathf.Sqrt(FunnelLevel));funnelFill.enabled=FunnelLevel>.001f;funnelFill.color=new Color(1,1,1,FunnelLevel);
            // The old tiny flow artwork is no longer used for the forming transfer.
            upperStream.enabled=lowerStream.enabled=false;
            float upperEnvelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.01f,.09f,value))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,.74f,value)));
            float lowerEnvelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.28f,value))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.87f,1,value)));
            Vector3 mouth=step.bowl.transform.TransformPoint((Vector3)composition.bowlPourMouthLocal);
            bowlRibbon.Display(mouth,funnel.transform.position+Vector3.up*1.1f,.82f,upperEnvelope,Time.time);
            pouchRibbon.Display(funnel.transform.position+Vector3.down*2,pouch.transform.TransformPoint(Vector3.up*2.6f),.4f,lowerEnvelope,Time.time);
        }
        FunnelPourRibbon CreateRibbon(string name,SpriteRenderer reference)
        {
            var root=new GameObject(name);
            // World-space vertices remain independent of bowl rotation and pouch scale.
            var ribbon=root.AddComponent<FunnelPourRibbon>();ribbon.Initialize(fill.sprite,reference,completionFx);return ribbon;
        }
        void Reveal(float value)
        {step.material.SetOpacity(value);var color=pouch.color;color.a=1-value;pouch.color=color;color=fill.color;color.a=1-value;fill.color=color;}
        public void Cancel()
        {
            GameAudioPlayer.Current?.StopLoop(this);if(motion.isAlive)motion.Stop();RestoreBowlSorting();dragging=seated=false;upperStream.enabled=lowerStream.enabled=false;
            if(bowlRibbon)bowlRibbon.Hide(true);if(pouchRibbon)pouchRibbon.Hide(true);
            if(completionFx)completionFx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDisable(){Cancel();}
        void OnDestroy(){if(bowlRibbon)Destroy(bowlRibbon.gameObject);if(pouchRibbon)Destroy(pouchRibbon.gameObject);}
    }
}
