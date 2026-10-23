using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Qualified drag distance, independent of direction, time, art pixels or coverage.
    public sealed class FormingProgress
    {
        public float Progress { get; private set; }
        public int CompletionCount { get; private set; }
        public int UsefulGestures { get; private set; }
        public bool Tracking { get; private set; }
        Vector2 previous, start, center, contactRadii;
        float credited;
        bool qualified, valid, automatic;
        public bool StartAutomaticTransfer()
        { if(automatic || Tracking || CompletionCount!=0)return false;automatic=true;return true; }
        public void SetAutomaticTransferProgress(float fraction)
        { if(!automatic)return;Progress=Mathf.Max(Progress,Mathf.Clamp01(fraction));if(Progress>=1 && CompletionCount==0)CompletionCount=1; }
        FormingDefinition config;
        public bool Begin(Vector2 point,Vector2 origin,Vector2 halfExtents,FormingDefinition definition)
        {
            if(Tracking || Progress>=1 || !Inside(point-origin,halfExtents)) return false;
            Vector2 body=Vector2.Lerp(definition.initialContactExtents,definition.finalContactExtents,Progress);
            var near=new Vector2(Mathf.Min(halfExtents.x,body.x+definition.edgeForgiveness),Mathf.Min(halfExtents.y,body.y+definition.edgeForgiveness));
            if(!Inside(point-origin,near)) return false;
            config=definition; center=origin; contactRadii=near; previous=start=point;
            qualified=false; valid=true; Tracking=true; return true;
        }
        public bool Sample(Vector2 point)
        {
            if(!Tracking || !valid || Progress>=1) return false;
            float distance=Vector2.Distance(point,previous);
            if(!Inside(point-center,contactRadii) || distance>config.maximumDelta) { valid=false; return false; }
            if(distance<config.minimumMotion) return false; // Gentle motion accumulates; bounded jitter does not.
            previous=point;
            float contribution=distance;
            if(!qualified)
            {
                contribution=Vector2.Distance(point,start);
                if(contribution<config.minimumGestureTravel) return false;
                qualified=true; UsefulGestures++;
            }
            credited+=contribution;
            Progress=Mathf.Clamp01(credited/Mathf.Max(1,config.requiredEffort));
            if(Progress>=1) { CompletionCount++; return true; }
            return false;
        }
        public void Release() { Tracking=false; valid=false; }
        public void Reset() { automatic=false;Release(); credited=0; Progress=0; CompletionCount=0; UsefulGestures=0; }
        static bool Inside(Vector2 point,Vector2 extents)
        { if(extents.x<=0 || extents.y<=0) return false; point=new Vector2(point.x/extents.x,point.y/extents.y); return point.sqrMagnitude<=1; }
    }
}
