using System.Collections;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using SquishyDumpling.Layout;

namespace SquishyDumpling.Creation
{
    public sealed class IngredientPresenter : MonoBehaviour
    {
        public IngredientDefinition definition;
        public Transform motionRoot, visualRoot, containerMouth, interactionAnchor, exitAnchor;
        public SpriteRenderer container, content;
        public LiquidStreamPresenter stream;
        public PowderPourPresenter powder;
        bool UsesParticles=>definition.presentationType==IngredientPresentationType.PowderPour && !definition.authoredStream;
        public float TransferEnvelope => UsesParticles ? powder.Envelope : stream.Envelope;
        public BowlPresenter bowl;
        public CreationFlowController flow;
        public ResponsiveWorldLayout layout;
        public Vector3 ExitPosition { get; private set; }
        public AudioSource oneShots, loop;
        public UnityEvent onPickup = new UnityEvent(), onAccepted = new UnityEvent(), onPourStart = new UnityEvent(),
            onImpact = new UnityEvent(), onCompleted = new UnityEvent();
        public bool Succeeded { get; private set; }
        public bool IsAnimating => sequence.isAlive;
        public float DepletedFraction { get; private set; }
        public int DropImpactCount { get; private set; }
        public Vector3 AuthoredScale { get; private set; }
        public Quaternion AuthoredRotation { get; private set; }
        Vector3 MouthAim => definition.fixedCenteredPour
            ? new Vector3(bowl.transform.TransformPoint(definition.fixedPourContact).x,bowl.containerMouthAim.position.y,bowl.containerMouthAim.position.z)
            : bowl.containerMouthAim.position;
        public Vector3 PourRootPosition => MouthAim -
            (AuthoredRotation * Quaternion.Euler(0,0,definition.pourAngle)) * Vector3.Scale(authoredWorldScale, mouthOffset);
        Sequence sequence;
        Vector3 visualPosition, visualScale, contentPosition, mouthOffset, authoredWorldScale;
        Quaternion visualRotation;
        Vector2 contentSize;
        bool captured;
        int generation;

        void Awake() { Capture(); ApplyDefinition(); }
        void Capture()
        {
            if(captured) return;
            AuthoredScale=motionRoot.localScale; AuthoredRotation=motionRoot.rotation;
            authoredWorldScale=motionRoot.lossyScale; mouthOffset=motionRoot.InverseTransformPoint(containerMouth.position);
            visualPosition=visualRoot.localPosition; visualScale=visualRoot.localScale; visualRotation=visualRoot.localRotation;
            contentPosition=content.transform.localPosition; contentSize=content.size; captured=true;
        }
        public void ApplyDefinition()
        {
            container.sprite=definition.containerSprite; content.sprite=definition.contentSprite;
            stream.discreteDrop=definition.discreteDrops;
            stream.fadeWithPourProgress=definition.fadeAuthoredFlow;
            stream.frames=definition.streamFrames;stream.frameDuration=definition.streamFrameDuration;
            stream.Renderer.sprite=definition.streamSprite; stream.Renderer.color=definition.transferColor;
            stream.preserveArtworkAspect=definition.authoredStream;
            content.color=definition.fillColor;
            if(powder) { powder.grainSprite=definition.powderSprite; powder.grainColor=definition.transferColor; }
        }
        void ResetVisualTransform()
        {
            if(!visualRoot) return;
            visualRoot.localPosition=visualPosition; visualRoot.localRotation=visualRotation; visualRoot.localScale=visualScale;
        }
        void StopOwned()
        {
            generation++; DropImpactCount=0; if(sequence.isAlive) sequence.Stop(); Succeeded=false;
            stream.ResetVisuals(); if(powder) powder.ResetVisuals(); GameAudioPlayer.Current?.StopLoop(this);if(loop) loop.Stop(); if(oneShots) oneShots.Stop();
        }
        public void Cancel()
        {
            Capture(); StopOwned(); ResetVisualTransform();
            if(container) container.enabled=true;
            if(motionRoot) { if(interactionAnchor) motionRoot.position=interactionAnchor.position; motionRoot.rotation=AuthoredRotation; motionRoot.localScale=AuthoredScale; }
            SetDepletion(0);
        }
        public void SetAvailable(bool available)
        {
            if(container) container.enabled=available;
            if(content) content.enabled=!definition.bakedContainer && available && DepletedFraction<.999f;
            var actor=motionRoot ? motionRoot.GetComponent<IngredientActor>() : null;
            if(actor) { actor.ClearOwnership(); actor.GetComponent<Collider2D>().enabled=available; }
            if(!available) { stream.ResetVisuals(); if(powder) powder.ResetVisuals(); }
        }
        public IEnumerator StageNextIngredient()
        {
            StopOwned(); int operation=generation;
            sequence=Sequence.Create().ChainDelay(.18f);
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        void SetTransferEnvelope(float value)
        {
            if(UsesParticles) powder.SetEnvelope(value);
            else stream.SetEnvelope(value);
        }
        public Transform spawnAnchor;
        public IEnumerator Present()
        {
            Cancel(); int operation=generation;
            motionRoot.position=spawnAnchor ? spawnAnchor.position : interactionAnchor.position+Vector3.left*10;
            GameAudioPlayer.Current?.Play(GameSound.IngredientAppear);
            sequence=Sequence.Create().Chain(Tween.Position(motionRoot,interactionAnchor.position,definition.entranceDuration,Ease.OutCubic))
                .Group(Tween.LocalRotation(visualRoot,visualRotation*Quaternion.Euler(0,0,3),definition.entranceDuration,Ease.OutSine))
                .Chain(Tween.LocalRotation(visualRoot,visualRotation,.16f,Ease.InOutSine));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        public void PlayPickup()
        {
            StopOwned(); ResetVisualTransform(); int operation=generation;
            onPickup.Invoke(); if(operation!=generation || !isActiveAndEnabled) return; Play(definition.pickup,GameSound.Pickup);
            sequence=Sequence.Create().Chain(Tween.Scale(visualRoot,visualScale*1.055f,.11f,Ease.OutQuad))
                .Group(Tween.LocalPosition(visualRoot,visualPosition+Vector3.up*.2f,.11f,Ease.OutQuad))
                .Group(Tween.LocalRotation(visualRoot,visualRotation*Quaternion.Euler(0,0,-4),.13f,Ease.OutSine));
            // Position of motionRoot remains exclusively owned by IngredientActor during this sequence.
        }
        public IEnumerator ReturnToIdle()
        {
            StopOwned(); int operation=generation;
            sequence=Sequence.Create();
            if(Vector3.Distance(motionRoot.position,interactionAnchor.position)>.0001f)
                sequence=sequence.Chain(Tween.Position(motionRoot,interactionAnchor.position,definition.returnDuration,Ease.OutCubic));
            else sequence=sequence.Chain(Tween.Delay(definition.returnDuration));
            // An immediate cancelled pickup may still be at rest: do not schedule no-op transform tweens.
            if(Quaternion.Angle(motionRoot.rotation,AuthoredRotation)>.001f)
                sequence=sequence.Group(Tween.Rotation(motionRoot,AuthoredRotation,definition.returnDuration,Ease.InOutSine));
            if(Vector3.Distance(visualRoot.localScale,visualScale)>.0001f)
                sequence=sequence.Group(Tween.Scale(visualRoot,visualScale,.2f,Ease.OutSine));
            if(Vector3.Distance(visualRoot.localPosition,visualPosition)>.0001f)
                sequence=sequence.Group(Tween.LocalPosition(visualRoot,visualPosition,.2f,Ease.OutSine));
            if(Quaternion.Angle(visualRoot.localRotation,visualRotation)>.001f)
                sequence=sequence.Group(Tween.LocalRotation(visualRoot,visualRotation,.2f,Ease.OutSine));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        public IEnumerator PlayPour()
        {
            StopOwned(); ResetVisualTransform(); int operation=generation;
            onAccepted.Invoke(); if(operation!=generation || !isActiveAndEnabled) yield break; Play(definition.accepted,GameSound.Accepted);
            stream.receivingBowl=definition.authoredStream ? bowl : null;
            stream.useFixedContact=definition.fixedCenteredPour;
            stream.fixedContactPoint=bowl.transform.TransformPoint(definition.fixedPourContact);
            Vector3 pose=PourRootPosition;
            Quaternion tilt=AuthoredRotation*Quaternion.Euler(0,0,definition.pourAngle);
            Vector3 initial=motionRoot.position;
            ExitPosition=exitAnchor.position;
            if(layout) ExitPosition=new Vector3(Mathf.Min(ExitPosition.x,layout.VisibleWorldBounds.xMin-4f),ExitPosition.y,ExitPosition.z);
            sequence=Sequence.Create()
                // Capture/anticipation then controlled positioning and tilt.
                .Chain(Tween.Scale(visualRoot,visualScale*.97f,.09f,Ease.OutQuad))
                .Chain(Tween.Position(motionRoot,initial+Vector3.up*.5f,definition.anticipationDuration,Ease.InQuad))
                .Group(Tween.Scale(visualRoot,visualScale,definition.anticipationDuration,Ease.OutSine));
            // Compare against the preceding tween's endpoint, not the current release position.
            // Accurate releases can already reach the pour pose through anticipation alone.
            sequence=Vector3.Distance(initial+Vector3.up*.5f,pose)>.0001f
                ? sequence.Chain(Tween.Position(motionRoot,pose,definition.positionDuration,Ease.InOutCubic))
                : sequence.ChainDelay(definition.positionDuration);
            sequence=sequence.Group(Tween.Rotation(motionRoot,tilt,definition.positionDuration,Ease.InOutSine))
                .ChainCallback(this,p=>p.StartPourBeat());
            if(definition.discreteDrops)
            {
                int count=Mathf.Max(1,definition.dropCount);float beat=definition.pourDuration/count;
                for(int i=0;i<count;i++)
                {
                    sequence=sequence.Chain(Tween.Custom(this,0f,1f,beat*.28f,(p,v)=>p.FormDrop(v),Ease.OutSine))
                        .Chain(Tween.Custom(this,0f,1f,beat*.62f,(p,v)=>p.FallDrop(v),Ease.InQuad))
                        .ChainCallback(this,p=>p.ImpactDrop()).ChainDelay(beat*.1f);
                }
            }
            else
            {
                sequence=sequence.Chain(Tween.Custom(this,0f,1f,.13f,(p,v)=>p.SetTransferEnvelope(v),Ease.OutSine));
                if(UsesParticles) sequence=sequence.ChainDelay(powder.contactDelay);
                sequence=sequence.ChainCallback(this,p=>p.ContactBeat())
                    .Chain(Tween.Custom(this,0f,1f,definition.pourDuration,(p,v)=>p.ReceiveProgress(v),Ease.InOutSine))
                    .Chain(Tween.Custom(this,1f,0f,.2f,(p,v)=>p.SetTransferEnvelope(v),Ease.InSine));
                if(UsesParticles) sequence=sequence.ChainDelay(powder.MaximumFlightDuration);
            }
            sequence=sequence.ChainCallback(this,p=>p.StopPourBeat())
                .Chain(Tween.Rotation(motionRoot,tilt*Quaternion.Euler(0,0,-5),.13f,Ease.OutQuad))
                .Chain(Tween.Rotation(motionRoot,AuthoredRotation,.32f,Ease.InOutSine))
                .Chain(Tween.Position(motionRoot,ExitPosition,definition.exitDuration,Ease.InOutCubic));
            while(sequence.isAlive) yield return null;
            if(operation!=generation) yield break;
            stream.ResetVisuals(); if(powder) powder.ResetVisuals(); if(loop) loop.Stop(); container.enabled=false;
            yield return bowl.Settle();
            if(operation!=generation || !bowl.Succeeded) yield break;
            yield return bowl.PlayCompletionFx();
            if(operation!=generation || !bowl.Succeeded) yield break;
            onCompleted.Invoke(); if(operation!=generation || !isActiveAndEnabled) yield break; Play(definition.completed,GameSound.IngredientComplete); Succeeded=true;
        }
        Vector3 DropStart => containerMouth.position-Vector3.up*(definition.dropWidth*.5f*stream.Renderer.sprite.bounds.size.y/stream.Renderer.sprite.bounds.size.x);
        void FormDrop(float value)=>stream.ShowDrop(DropStart,definition.dropWidth*Mathf.Lerp(.3f,1,value));
        void FallDrop(float value)=>stream.ShowDrop(Vector3.Lerp(DropStart,stream.LandingPosition,value),definition.dropWidth);
        void ImpactDrop()
        {
            stream.ResetVisuals();DropImpactCount++;ReceiveProgress((float)DropImpactCount/Mathf.Max(1,definition.dropCount));ContactBeat();
        }
        void StartPourBeat()
        {
            int operation=generation; onPourStart.Invoke();
            if(operation!=generation || !isActiveAndEnabled) return; Play(definition.pourStart,GameSound.PourStart);
            if(GameAudioPlayer.Current){if(!definition.discreteDrops)GameAudioPlayer.Current.SetLoop(definition.presentationType==IngredientPresentationType.PowderPour?GameSound.PowderLoop:GameSound.LiquidLoop,true,this,definition.pourLoop);}
            else if(loop && definition.pourLoop) { loop.clip=definition.pourLoop; loop.loop=true; loop.Play(); }
        }
        void ContactBeat()
        { int operation=generation; onImpact.Invoke(); if(operation!=generation || !isActiveAndEnabled) return; Play(definition.impact,definition.discreteDrops?GameSound.DyeDrop:GameSound.IngredientImpact); bowl.Impact(); }
        void ReceiveProgress(float value) { SetDepletion(value); bowl.Receive(value); stream.SetPourProgress(value); }
        void StopPourBeat()
        {
            GameAudioPlayer.Current?.StopLoop(this);if(loop) loop.Stop(); bowl.Impact();
        }
        void SetDepletion(float value)
        {
            DepletedFraction=Mathf.Clamp01(value);
            if(!content) return;
            if(definition.bakedContainer){content.enabled=false;return;}
            float height=contentSize.y*(1-DepletedFraction);
            content.drawMode=SpriteDrawMode.Tiled; content.size=new Vector2(contentSize.x,Mathf.Max(.001f,height));
            content.transform.localPosition=contentPosition-Vector3.up*(contentSize.y-height)*.5f;
            content.enabled=DepletedFraction<.999f;
        }
        void Play(AudioClip clip,GameSound cue) { if(GameAudioPlayer.Current)GameAudioPlayer.Current.Play(cue,clip);else if(oneShots && clip)oneShots.PlayOneShot(clip); }
        void OnDisable() { Cancel(); if(flow) flow.Interrupt(); }
    }
}
