using UnityEngine;
using PrimeTween;
using System.Collections;
namespace SquishyDumpling.Creation
{
    // A separate surface module keeps future creation steps out of BowlPresenter.
    public sealed class BowlMixingPresentation : MonoBehaviour
    {
        public SpriteRenderer[] unmixedLayers;
        public SpriteRenderer mixedLayer;
        public MixingConfiguration config;
        public SpriteRenderer[] artStages;
        public float DisplayedProgress { get; private set; }
        public float MotionResponse { get; private set; }
        public bool IsAnimating => beat.isAlive;
        Vector3 baseScale; Quaternion baseRotation; Sequence beat;
        float phase, impulse; int milestone;
        void Awake() { baseScale=mixedLayer.transform.localScale; baseRotation=mixedLayer.transform.localRotation; ResetVisuals(); }
        public void Display(float progress,float usefulSpeed)
        {
            DisplayedProgress=Mathf.Clamp01(progress); MotionResponse=Mathf.MoveTowards(MotionResponse,Mathf.Clamp01(usefulSpeed/8),Time.deltaTime*5);
            foreach(var layer in unmixedLayers) { Color c=layer.color; c.a=.85f*(1-DisplayedProgress); layer.color=c; if(progress>=1) layer.enabled=false; }
            mixedLayer.enabled=progress>0; Color color=mixedLayer.color; color.a=DisplayedProgress*.9f; mixedLayer.color=color;
            if(artStages!=null && artStages.Length==3)
            {
                float blend=Mathf.Clamp01(progress)*2;
                for(int i=0;i<3;i++){var c=artStages[i].color;c.a=Mathf.Clamp01(1-Mathf.Abs(blend-i))*(progress>0?1:0);artStages[i].color=c;artStages[i].enabled=c.a>.001f;}
            }
            phase+=Time.deltaTime*(3+MotionResponse*9);
            float wobble=(MotionResponse*(.5f+progress*.5f)*config.wobbleStrength+impulse)*Mathf.Sin(phase);
            mixedLayer.transform.localScale=Vector3.Scale(baseScale,new Vector3(1+wobble,1-wobble,1));
            mixedLayer.transform.localRotation=baseRotation*Quaternion.Euler(0,0,Mathf.Sin(phase*.5f)*config.surfaceRotation*MotionResponse);
            int reached=Mathf.FloorToInt(progress*4);
            if(reached>milestone) { milestone=reached; Accent(); }
        }
        void Accent()
        {
            if(beat.isAlive) beat.Stop();
            beat=Sequence.Create().Chain(Tween.Custom(this,0f,config.milestoneStrength,.09f,(p,v)=>p.impulse=v))
                .Chain(Tween.Custom(this,config.milestoneStrength,0f,.22f,(p,v)=>p.impulse=v));
        }
        public IEnumerator Finish()
        {
            if(beat.isAlive) beat.Stop(); impulse=0;
            Vector3 compressed=Vector3.Scale(baseScale,new Vector3(1.04f,.96f,1));
            // Stirring wobble can already be at this compression pose. Keep the beat without moving it.
            beat=Vector3.Distance(mixedLayer.transform.localScale,compressed)>.0001f
                ? Sequence.Create().Chain(Tween.Scale(mixedLayer.transform,compressed,.13f,Ease.OutSine))
                : Sequence.Create().ChainDelay(.13f);
            beat=beat.Chain(Tween.Scale(mixedLayer.transform,baseScale,config.completionDuration,Ease.InOutSine));
            // The scale settle already owns this beat; a resting surface needs no rotation tween.
            if(Quaternion.Angle(mixedLayer.transform.localRotation,baseRotation)>.001f)
                beat=beat.Group(Tween.LocalRotation(mixedLayer.transform,baseRotation,config.completionDuration,Ease.InOutSine));
            while(beat.isAlive) yield return null;
        }
        public void ResetVisuals()
        {
            if(beat.isAlive) beat.Stop(); milestone=0; impulse=phase=MotionResponse=DisplayedProgress=0;
            if(artStages!=null)foreach(var stage in artStages)if(stage)stage.enabled=false;
            if(mixedLayer) { mixedLayer.enabled=false; mixedLayer.transform.localScale=baseScale; mixedLayer.transform.localRotation=baseRotation; }
        }
        void OnDisable() { ResetVisuals(); }
    }
}
