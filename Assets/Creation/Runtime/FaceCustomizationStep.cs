using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using PrimeTween;
using SquishyDumpling.Layout;
namespace SquishyDumpling.Creation
{
    public sealed class FaceCustomizationStep : MonoBehaviour
    {
        public FaceStampActor stamp;
        public FaceChoiceView choices;
        public UnityEngine.UI.Button confirm;
        SpriteRenderer preview;
        [System.NonSerialized] public FaceCustomizationDefinition definition;
        [System.NonSerialized] public CreationFlowController flow;
        [System.NonSerialized] public ResponsiveWorldLayout layout;
        [System.NonSerialized] public Transform home, exit;
        [System.NonSerialized] public FormingMaterialPresenter body;
        [System.NonSerialized] public FaceApplicationTarget target;
        public UnityEvent onFaceSelected=new UnityEvent(), onGrabbed=new UnityEvent(), onValidTarget=new UnityEvent(), onAligned=new UnityEvent(), onImpact=new UnityEvent(), onTransfer=new UnityEvent(), onRebound=new UnityEvent(), onComplete=new UnityEvent();
        public AppliedFacePresenter AppliedFace { get; private set; }
        public bool Succeeded { get; private set; }
        public bool ActorDeactivationInProgress { get; private set; }
        public bool IsAnimating=>sequence.isAlive;
        public bool CanSelect=>flow && flow.CanCustomizeFor(this) && flow.Session.Step==CreationStep.Customization;
        public bool CanDrag=>CanSelect && !definition.directSelection && stamp && stamp.gameObject.activeInHierarchy && !sequence.isAlive && definition.Find(flow.Session.SelectedFaceId)!=null;
        public string Phase { get; private set; }="Unbound";
        Sequence sequence;
        int generation;
        bool near;
        Vector3 bodyScale; Quaternion bodyRotation;
        public IEnumerator Present()
        {
            Cancel(); int operation=generation; body.ClearContact(); bodyScale=body.contactRoot.localScale; bodyRotation=body.contactRoot.localRotation;
            if(flow.pearl)choices.transform.position=flow.pearl.composition.choicePosition;
            choices.GetComponent<Canvas>().worldCamera=layout.WorldCamera; choices.Bind(this); choices.group.alpha=0; if(stamp){stamp.step=null; stamp.gameObject.SetActive(false); stamp.step=this;} if(confirm){confirm.onClick.AddListener(ConfirmSelection);confirm.interactable=false;} Phase="Present";
            sequence=Sequence.Create().ChainDelay(definition.anticipation).Chain(Tween.Custom(choices.group,0,1,definition.entrance,(g,v)=>g.alpha=v));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        public void Ready()
        { Phase="Choose"; choices.BeginScrollHint(); choices.Refresh(); if(!string.IsNullOrEmpty(definition.defaultSelectedId)) SelectFace(definition.defaultSelectedId); }
        public bool SelectFace(string id)
        {
            if(!CanSelect || definition.Find(id)==null || !flow.Session.SelectFace(id,definition,flow.Session.Version)) return false;
            if(definition.directSelection)
            {
                StopMotion();var option=definition.Find(id);
                if(!preview){preview=new GameObject("Face selection preview").AddComponent<SpriteRenderer>();preview.transform.SetParent(target.transform,false);preview.sortingLayerName="Gameplay";preview.sortingOrder=14;}
                preview.sprite=option.appliedArtwork;preview.transform.localPosition=option.appliedLocalPosition;preview.transform.localRotation=Quaternion.Euler(0,0,option.appliedLocalRotation);
                preview.transform.localScale=option.appliedLocalScale*.94f;
                sequence=Sequence.Create().Chain(Tween.Scale(preview.transform,option.appliedLocalScale,.22f,Ease.OutBack));
                if(confirm)confirm.interactable=true;choices.Refresh();GameAudioPlayer.Current?.Play(GameSound.FaceSelect);onFaceSelected.Invoke();return true;
            }
            StopMotion(); int operation=generation;
            stamp.ClearOwnership(); stamp.Configure(definition.Find(id)); stamp.gameObject.SetActive(true); stamp.transform.rotation=home.rotation;
            stamp.transform.position=home.position+Vector3.down*3; near=false; Phase="StampEnter";
            sequence=Sequence.Create().Chain(Tween.Position(stamp.transform,home.position,definition.entrance,Ease.OutCubic));
            choices.Refresh(); onFaceSelected.Invoke(); return operation==generation;
        }
        public void ConfirmSelection()
        {
            if(!definition.directSelection || !CanSelect || definition.Find(flow.Session.SelectedFaceId)==null)return;
            int version=flow.Session.Version;if(!flow.Session.ConfirmSelectedFace(version))return;
            GameAudioPlayer.Current?.Play(GameSound.FaceConfirm);StopMotion();if(confirm)confirm.interactable=false;choices.Refresh();
            AppliedFace=preview.gameObject.AddComponent<AppliedFacePresenter>();AppliedFace.Apply(definition.Find(flow.Session.AppliedFaceId));preview=null;
            Succeeded=false;StartCoroutine(Confirm(version,generation));
        }
        IEnumerator Confirm(int version,int operation)
        {
            Phase="Confirm";onTransfer.Invoke();if(!Current(version,operation))yield break;
            sequence=Sequence.Create().Chain(Tween.Scale(body.contactRoot,new Vector3(1.035f,.97f,1),.1f,Ease.OutSine))
                .Chain(Tween.Scale(body.contactRoot,bodyScale,.32f,Ease.OutBack))
                .Group(Tween.Custom(choices.group,1,0,.28f,(g,v)=>g.alpha=v));
            while(sequence.isAlive)yield return null;if(!Current(version,operation))yield break;
            Succeeded=true;Phase="Complete";onComplete.Invoke();if(Current(version,operation))flow.RequestCustomizationCompletion(this,version);
        }
        void Update() { if(Phase=="StampEnter" && !sequence.isAlive && CanSelect) Phase="Choose"; }
        public void Grabbed() { int operation=generation; onGrabbed.Invoke(); if(operation!=generation) stamp.ClearOwnership(); }
        public void CheckIntent(Vector2 pointer)
        { bool valid=target.Contains(pointer) || target.Contains(stamp.stampContact.position); if(valid && !near) { near=true; onValidTarget.Invoke(); } else near=valid; }
        public void ReleaseStamp(Vector2 pointer)
        {
            if(!CanDrag) return;
            bool valid=target.Contains(pointer) || target.Contains(stamp.stampContact.position);
            if(!valid) { ReturnStamp(); return; }
            if(!flow.Session.BeginStamping(flow.Session.Version)) return;
            Succeeded=false; stamp.ClearOwnership(); choices.Refresh(); StartCoroutine(Apply(flow.Session.Version,generation));
        }
        public void ReturnStamp()
        {
            stamp.ClearOwnership(); if(!CanSelect) return; StopMotion(); near=false; Phase="Return";
            if(Vector3.SqrMagnitude(stamp.transform.position-home.position)<.000001f && Quaternion.Angle(stamp.transform.rotation,home.rotation)<.01f) { Phase="Choose"; return; }
            sequence=Vector3.SqrMagnitude(stamp.transform.position-home.position)<.000001f ? Sequence.Create().ChainDelay(definition.returnDuration) : Sequence.Create().Chain(Tween.Position(stamp.transform,home.position,definition.returnDuration,Ease.OutCubic));
            if(Quaternion.Angle(stamp.transform.rotation,home.rotation)>.01f) sequence=sequence.Group(Tween.Rotation(stamp.transform,home.rotation,definition.returnDuration,Ease.OutSine));
        }
        IEnumerator Apply(int version,int operation)
        {
            Phase="Align";
            Quaternion rotation=target.transform.rotation;
            // Align the authored contact, independent of placeholder sprite size or grab offset.
            Vector3 position=target.transform.position-rotation*Vector3.Scale(stamp.stampContact.localPosition,stamp.transform.localScale);
            // An accurate release can already align the contact. Retain the beat without a no-op tween.
            sequence=Vector3.SqrMagnitude(stamp.transform.position-position)<.000001f
                ? Sequence.Create().ChainDelay(definition.alignment)
                : Sequence.Create().Chain(Tween.Position(stamp.transform,position,definition.alignment,Ease.InOutSine));
            if(Quaternion.Angle(stamp.transform.rotation,rotation)>.01f) sequence=sequence.Group(Tween.Rotation(stamp.transform,rotation,definition.alignment,Ease.InOutSine));
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            onAligned.Invoke(); if(!Current(version,operation)) yield break;
            Phase="Anticipation";
            sequence=Sequence.Create().Chain(Tween.Position(stamp.transform,position+Vector3.up*definition.anticipationLift,definition.anticipation,Ease.OutSine));
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            Phase="Press";
            sequence=Sequence.Create().Chain(Tween.Position(stamp.transform,position+Vector3.down*definition.pressDepth,definition.press,Ease.InQuad))
                .Group(Tween.Scale(body.contactRoot,Vector3.Scale(bodyScale,new Vector3(definition.widthCompensation,definition.compression,1)),definition.press,Ease.InQuad));
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            Phase="Contact"; onImpact.Invoke(); if(!Current(version,operation)) yield break;
            sequence=Sequence.Create().ChainDelay(definition.contactHold);
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            if(!flow.Session.ApplyFace(version)) yield break;
            AppliedFace=new GameObject("Applied face").AddComponent<AppliedFacePresenter>(); AppliedFace.transform.SetParent(target.transform,false); AppliedFace.Apply(definition.Find(flow.Session.AppliedFaceId));
            stamp.facePreview.enabled=false; Phase="Transfer"; onTransfer.Invoke(); if(!Current(version,operation)) yield break;
            sequence=Sequence.Create().Chain(Tween.Position(stamp.transform,position+Vector3.up*definition.liftDistance,definition.lift,Ease.OutCubic))
                .Group(Tween.Scale(body.contactRoot,Vector3.Scale(bodyScale,new Vector3(1-definition.reboundStrength,1+definition.reboundStrength,1)),definition.rebound*.5f,Ease.OutCubic))
                .Chain(Tween.Scale(body.contactRoot,bodyScale,definition.rebound*.5f,Ease.OutBack));
            Phase="Rebound"; onRebound.Invoke(); if(!Current(version,operation)) yield break;
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            sequence=Sequence.Create().Chain(Tween.LocalRotation(body.contactRoot,Quaternion.Euler(0,0,definition.wobbleDegrees),definition.rebound*.5f,Ease.OutSine))
                .Chain(Tween.LocalRotation(body.contactRoot,bodyRotation,definition.rebound*.5f,Ease.OutBack));
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            Phase="Exit";
            sequence=Sequence.Create().Chain(Tween.Position(stamp.transform,exit.position,definition.exit,Ease.InCubic))
                .Group(Tween.Custom(choices.group,1,0,definition.exit,(g,v)=>g.alpha=v));
            while(sequence.isAlive) yield return null; if(!Current(version,operation)) yield break;
            stamp.step=null; stamp.gameObject.SetActive(false); Succeeded=true; Phase="Complete"; onComplete.Invoke();
            if(Current(version,operation)) flow.RequestCustomizationCompletion(this,version);
        }
        bool Current(int version,int operation)=>operation==generation && flow && flow.Session.Version==version && flow.ActiveCustomization==this;
        void StopMotion() { if(sequence.isAlive) sequence.Stop(); }
        public void Cancel()
        {
            generation++; Succeeded=false; choices.CancelScrollHint(); StopAllCoroutines(); StopMotion(); if(confirm)confirm.onClick.RemoveListener(ConfirmSelection);if(preview){Destroy(preview.gameObject);preview=null;} if(stamp) { stamp.ClearOwnership(); stamp.facePreview.enabled=true; }
            if(body && body.contactRoot) { body.ClearContact(); if(bodyScale!=Vector3.zero) body.contactRoot.localScale=bodyScale; body.contactRoot.localRotation=bodyRotation; }
        }
        public void ActorDisabled()
        {
            // Unity disallows recursively deactivating an ancestor inside a child's SetActive/OnDisable.
            ActorDeactivationInProgress=true; Interrupted(); ActorDeactivationInProgress=false;
        }
        public void Interrupted() { if(flow) flow.Interrupt(); }
        void OnDisable() { Cancel(); Interrupted(); }
    }
}
