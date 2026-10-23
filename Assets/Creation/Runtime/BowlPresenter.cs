using System.Collections;
using PrimeTween;
using UnityEngine;

namespace SquishyDumpling.Creation
{
    public sealed class BowlPresenter : MonoBehaviour
    {
        public Transform visualRoot, bowlCenter, pourTarget, contentArea, containerMouthAim;
        public SpriteRenderer contents;
        public SpriteRenderer[] shellRenderers=new SpriteRenderer[0];
        public void SetShellVisible(bool visible){foreach(var renderer in shellRenderers)if(renderer)renderer.enabled=visible;}
        // Empty in the approved single-step scene; the base sequence authors three persistent layers.
        public SpriteRenderer[] contributionLayers = new SpriteRenderer[0];
        public SpriteRenderer powderPuff;
        [Tooltip("Optional per-layer local contact points on the receiving artwork.")]
        public Vector2[] contributionContactPoints=new Vector2[0];
        public ParticleSystem pourCompletionFx;
        [Min(0)] public int completionFxLayerCount;
        public int CompletionFxPlayCount { get; private set; }
        public Vector3 PourContact => activeLayer<contributionContactPoints.Length
            ? layers[activeLayer].transform.TransformPoint(contributionContactPoints[activeLayer]) : pourTarget.position;
        public CreationFlowController flow;
        public bool Succeeded { get; private set; }
        public float ReceivedFraction { get; private set; }
        public float AccumulatedAmount
        { get { float amount=0; if(fractions!=null) for(int i=0;i<fractions.Length;i++) amount+=fractions[i]*amounts[i]; return amount; } }
        public int CompletedContributions { get; private set; }
        public bool IsAnimating => sequence.isAlive;
        Sequence sequence;
        Vector3 basePosition, baseScale;
        Quaternion baseRotation;
        SpriteRenderer[] layers;
        Vector3[] layerScales;
        float[] fractions, amounts;
        int activeLayer;
        IngredientPresentationType activeType;
        bool captured;
        int generation;
        void Awake() { CaptureBase(); }
        void CaptureBase()
        {
            if(captured) return;
            basePosition=visualRoot.localPosition; baseScale=visualRoot.localScale; baseRotation=visualRoot.localRotation;
            layers=contributionLayers.Length>0 ? contributionLayers : new[]{contents};
            layerScales=new Vector3[layers.Length]; fractions=new float[layers.Length]; amounts=new float[layers.Length];
            for(int i=0;i<layers.Length;i++) layerScales[i]=layers[i].transform.localScale;
            captured=true;
        }
        public void BeginContribution(IngredientDefinition definition)
        {
            CaptureBase(); activeLayer=Mathf.Clamp(definition.bowlLayer,0,layers.Length-1);
            activeType=definition.presentationType; ReceivedFraction=0;
            amounts[activeLayer]=definition.contributionAmount;
        }
        public IEnumerator Present()
        {
            Cancel(); int operation=generation;
            visualRoot.localPosition=basePosition+Vector3.down*9;
            sequence=Sequence.Create().Chain(Tween.LocalPosition(visualRoot,basePosition,.7f,Ease.OutCubic))
                .Group(Tween.Scale(visualRoot,baseScale*.985f,.55f,Ease.OutSine))
                .Chain(Tween.Scale(visualRoot,baseScale,.16f,Ease.InOutSine));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        // Visual accumulation is retained between steps. Only whole-session cancellation clears all layers.
        public void Receive(float fraction)
        {
            ReceivedFraction=Mathf.Clamp01(fraction); fractions[activeLayer]=ReceivedFraction;
            var layer=layers[activeLayer]; layer.enabled=ReceivedFraction>.001f;
            Color color=layer.color; color.a=Mathf.Lerp(.25f,.85f,ReceivedFraction); layer.color=color;
            layer.transform.localScale=Vector3.Scale(layerScales[activeLayer],
                new Vector3(Mathf.Lerp(.25f,1,ReceivedFraction),Mathf.Lerp(.2f,1,ReceivedFraction),1));
        }
        public IEnumerator PlayCompletionFx()
        {
            if(!pourCompletionFx || activeLayer>=completionFxLayerCount)yield break;
            int operation=generation;CompletionFxPlayCount++;
            pourCompletionFx.transform.position=bowlCenter.position+Vector3.up*.5f;
            GameAudioPlayer.Current?.Play(GameSound.StepComplete);pourCompletionFx.Play(true);
            while(operation==generation && pourCompletionFx.IsAlive(true))yield return null;
        }
        public void CompleteContribution() { CompletedContributions++; }
        public void Impact()
        {
            if(sequence.isAlive) sequence.Stop();
            sequence=Sequence.Create()
                .Chain(Tween.Scale(visualRoot,Vector3.Scale(baseScale,new Vector3(1.02f,.97f,1)),.12f,Ease.OutQuad))
                .Chain(Tween.Scale(visualRoot,baseScale,.27f,Ease.InOutSine));
            if(activeType==IngredientPresentationType.PowderPour && powderPuff)
            {
                powderPuff.enabled=true;
                sequence=sequence.Insert(0,Tween.Custom(powderPuff,0f,.55f,.1f,(s,a)=>SetAlpha(s,a),Ease.OutSine))
                    .Insert(.1f,Tween.Custom(powderPuff,.55f,0f,.29f,(s,a)=>SetAlpha(s,a),Ease.InSine))
                    .ChainCallback(this,b=>b.powderPuff.enabled=false);
            }
        }
        static void SetAlpha(SpriteRenderer sprite,float alpha)
        { Color color=sprite.color; color.a=alpha; sprite.color=color; }
        public IEnumerator Settle()
        {
            if(sequence.isAlive) sequence.Stop(); Succeeded=false; int operation=++generation;
            var layer=layers[activeLayer]; Vector3 target=layerScales[activeLayer];
            sequence=Sequence.Create().Chain(Tween.Scale(layer.transform,
                Vector3.Scale(target,new Vector3(1.035f,.965f,1)),.11f,Ease.OutSine))
                .Chain(Tween.Scale(layer.transform,target,.32f,Ease.InOutSine));
            while(sequence.isAlive) yield return null;
            Succeeded=operation==generation;
        }
        public void Cancel()
        {
            CaptureBase(); SetShellVisible(true); generation++;
            if(pourCompletionFx)pourCompletionFx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);CompletionFxPlayCount=0; if(sequence.isAlive) sequence.Stop(); Succeeded=false;
            visualRoot.localPosition=basePosition; visualRoot.localRotation=baseRotation; visualRoot.localScale=baseScale;
            for(int i=0;i<layers.Length;i++)
            { fractions[i]=amounts[i]=0; layers[i].enabled=false; layers[i].transform.localScale=layerScales[i]; }
            if(powderPuff) { powderPuff.enabled=false; SetAlpha(powderPuff,0); }
            ReceivedFraction=0; CompletedContributions=0; activeLayer=0;
        }
        void OnDisable() { Cancel(); if(flow) flow.Interrupt(); }
    }
}
