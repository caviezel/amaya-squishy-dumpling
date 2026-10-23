using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using SquishyDumpling.Layout;
using PrimeTween;
namespace SquishyDumpling.Creation
{
    public sealed class MixingStep : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler, IEndDragHandler, ICancelHandler
    {
        [Header("Composition supplied by the production flow")]
        public CreationFlowController flow;
        public ResponsiveWorldLayout layout;
        public MixingArea area;
        public Transform spawn, rest, exit, visual;
        public BowlMixingPresentation surface;
        public MixingConfiguration config;
        public AudioSource audioSource;
        public ParticleSystem stirringSparklePrefab;
        public ParticleSystem StirringSparkles { get; private set; }
        void ShowStirringSparkles(float speed)
        {
            if(speed<=.1f){StopSparkles(false);return;}
            if(!StirringSparkles && stirringSparklePrefab)
            {
                StirringSparkles=Instantiate(stirringSparklePrefab,transform);
                StirringSparkles.transform.localPosition=Vector3.zero;StirringSparkles.transform.localScale=Vector3.one;
                StirringSparkles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if(!StirringSparkles)return;
            if(!StirringSparkles.isPlaying)StirringSparkles.Play();
            // Always emit on a useful movement frame, including short or intermittent swipes.
            StirringSparkles.Emit(Mathf.Clamp(Mathf.CeilToInt(Time.deltaTime*75),1,12));
        }
        void StopSparkles(bool clear)
        {
            if(StirringSparkles)StirringSparkles.Stop(true,clear?ParticleSystemStopBehavior.StopEmittingAndClear:ParticleSystemStopBehavior.StopEmitting);
        }
        public MixingProgress Progress { get; private set; }=new MixingProgress();
        public void BindProgress(MixingProgress progress) { Progress=progress; }
        public int? PointerOwner => grab.PointerId;
        public bool Succeeded { get; private set; }
        public bool IsAnimating => sequence.isAlive;
        public bool CanInteract => flow && flow.CanMixFor(this);
        readonly SinglePointerGrab grab=new SinglePointerGrab();
        Sequence sequence;
        Vector2 target, lastInput;
        Vector3 visualScale;
        bool eligible;
        int generation, seenMilestone;
        void Awake() { visualScale=visual.localScale; }
        public IEnumerator Present()
        {
            Cancel(); int operation=generation; transform.position=spawn.position;
            sequence=Sequence.Create().Chain(Tween.Position(transform,rest.position,config.entranceDuration,Ease.OutCubic));
            while(sequence.isAlive) yield return null;
            target=transform.position; Succeeded=operation==generation;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(!CanInteract || grab.PointerId.HasValue || !layout.TryScreenToWorld(data.position,out var point) || !GetComponent<Collider2D>().OverlapPoint(point)) return;
            if(sequence.isAlive) sequence.Stop();
            grab.Begin(data.pointerId,point,transform.position); data.useDragThreshold=false;
            target=lastInput=transform.position; eligible=area.Contains(target); Progress.Begin(transform.position);
            Play(config.pickup,GameSound.Pickup);
            sequence=Sequence.Create().Chain(Tween.Scale(visual,visualScale*1.04f,.1f,Ease.OutSine)).Chain(Tween.Scale(visual,visualScale,.15f,Ease.InOutSine));
        }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data)
        {
            if(!CanInteract || !layout.TryScreenToWorld(data.position,out var point) || !grab.TryPosition(data.pointerId,point,out var intended)) return;
            // In forgiving mode the authored blade/contact remains clamped inside the bowl.
            // The finger may be over the handle or outside the bowl; only actual tool travel earns progress.
            bool accepted=config.acceptAnySwipe || (area.Contains(intended) && Vector2.Distance(intended,lastInput)<=config.maximumDelta);
            if(!accepted || !eligible) Progress.Begin(transform.position);
            eligible=accepted; lastInput=intended; target=area.Clamp(intended);
        }
        void Update()
        {
            if(!CanInteract) return;
            if(grab.PointerId.HasValue)
            {
                for(int i=0;i<Input.touchCount;i++) if(Input.GetTouch(i).fingerId==grab.PointerId && Input.GetTouch(i).phase==TouchPhase.Canceled) { Release(); return; }
                Vector2 before=transform.position;
                float speed=config.followSpeed/(1+config.resistance*Progress.Progress);
                Vector2 position=Vector2.Lerp(before,target,1-Mathf.Exp(-speed*Time.deltaTime));
                Vector2 lag=target-position;
                if(lag.magnitude>config.maximumLag) position=target-lag.normalized*config.maximumLag;
                position=area.Clamp(position); transform.position=new Vector3(position.x,position.y,transform.position.z);
                Vector2 movement=position-before;
                if(eligible) Progress.Sample(position,area.Center,area.radii,config);
                float usefulSpeed=eligible && movement.magnitude>=config.minimumMotion ? movement.magnitude/Mathf.Max(.001f,Time.deltaTime) : 0;
                float tilt=Mathf.Clamp(-movement.x/Mathf.Max(.001f,Time.deltaTime)*2,-14,14);
                visual.localRotation=Quaternion.Slerp(visual.localRotation,Quaternion.Euler(0,0,tilt),1-Mathf.Exp(-config.rotationResponse*Time.deltaTime));
                surface.Display(Progress.Progress,usefulSpeed); SetLoop(usefulSpeed>.1f); ShowStirringSparkles(usefulSpeed);
                int milestone=Mathf.FloorToInt(Progress.Progress*4);
                if(milestone>seenMilestone) { seenMilestone=milestone; Play(config.milestone,GameSound.MixingMilestone); }
            }
            else surface.Display(Progress.Progress,0);
        }
        public void SuspendInput()
        {
            if(!grab.PointerId.HasValue)return;
            Release(false);visual.localScale=visualScale;visual.localRotation=Quaternion.identity;
        }
        public void OnPointerUp(PointerEventData data) { if(grab.PointerId==data.pointerId) Release(); }
        public void OnEndDrag(PointerEventData data) { if(grab.PointerId==data.pointerId) Release(); }
        public void OnCancel(BaseEventData data) { Release(); }
        void Release(bool settle=true)
        {
            grab.Cancel(); Progress.Release(); eligible=false; SetLoop(false); StopSparkles(false);
            if(sequence.isAlive) sequence.Stop();
            if(settle && CanInteract && Progress.Progress>=1) { flow.RequestMixingCompletion(this); return; }
            if(settle && CanInteract)
            {
                bool rotate=Quaternion.Angle(visual.localRotation,Quaternion.identity)>.001f;
                bool scale=Vector3.Distance(visual.localScale,visualScale)>.0001f;
                if(rotate || scale)
                {
                    sequence=Sequence.Create();
                    if(rotate) sequence=sequence.Chain(Tween.LocalRotation(visual,Quaternion.identity,config.releaseDuration,Ease.OutSine));
                    if(scale) sequence=sequence.Group(Tween.Scale(visual,visualScale,config.releaseDuration,Ease.OutSine));
                }
            }
        }
        public IEnumerator Finish()
        {
            Release(false); int operation=generation; Play(config.complete,GameSound.StepComplete);
            yield return surface.Finish(); if(operation!=generation) yield break;
            Vector3 exitPosition=exit.position;
            // Extend the authored retirement path into bleed space, as ingredient exits already do.
            // Required gameplay geometry and sprite scale remain canonical.
            float rightExtent=3;
            foreach(var renderer in visual.GetComponentsInChildren<SpriteRenderer>())if(renderer.enabled)
                rightExtent=Mathf.Max(rightExtent,renderer.bounds.max.x-transform.position.x);
            exitPosition.x=Mathf.Min(exitPosition.x,layout.VisibleWorldBounds.xMin-rightExtent-1);
            sequence=Sequence.Create().Chain(Tween.Position(transform,exitPosition,config.exitDuration,Ease.InOutCubic));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        void SetLoop(bool playing)
        {
            if(GameAudioPlayer.Current){GameAudioPlayer.Current.SetLoop(GameSound.StirLoop,playing,this,config.stirringLoop);return;}
            if(!audioSource || !config.stirringLoop) return;
            if(playing && !audioSource.isPlaying) { audioSource.clip=config.stirringLoop; audioSource.loop=true; audioSource.Play(); }
            else if(!playing && audioSource.isPlaying) audioSource.Stop();
        }
        void Play(AudioClip clip,GameSound cue) { if(GameAudioPlayer.Current)GameAudioPlayer.Current.Play(cue,clip);else if(audioSource && clip)audioSource.PlayOneShot(clip); }
        public void Cancel()
        {
            StopSparkles(true);
            generation++; Succeeded=false; if(sequence.isAlive) sequence.Stop(); grab.Cancel(); Progress.Release(); eligible=false;
            GameAudioPlayer.Current?.StopLoop(this);if(audioSource) audioSource.Stop(); visual.localScale=visualScale; visual.localRotation=Quaternion.identity;
        }
        void OnDisable() { Cancel(); if(flow) flow.Interrupt(); }
    }
}
