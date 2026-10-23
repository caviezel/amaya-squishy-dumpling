using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Pure deterministic motion accumulation. No clock, visual state or tween is authoritative.
    public sealed class MixingProgress
    {
        public float Progress { get; private set; }
        public int CompletionCount { get; private set; }
        Vector2 previous;
        bool tracking;
        public void Begin(Vector2 point) { previous=point; tracking=true; }
        public void Release() { tracking=false; }
        public void Reset() { Progress=0; CompletionCount=0; tracking=false; }
        public bool Sample(Vector2 point,Vector2 center,Vector2 radii,MixingConfiguration config)
        {
            if(!tracking || Progress>=1) return false;
            Vector2 delta=point-previous, midpoint=(point+previous)*.5f-center;
            bool inside=Inside(point-center,radii) && Inside(previous-center,radii);
            float distance=delta.magnitude;
            if(!inside || (!config.acceptAnySwipe && distance>config.maximumDelta)) { previous=point; return false; }
            // Keep the last qualified baseline so gentle motion accumulates across frames.
            // Bounded jitter never crosses this distance; fast devices do not require faster stirring.
            if(distance<config.minimumMotion) return false;
            previous=point;
            if(config.acceptAnySwipe)
            {
                // Credit visible tool travel in any direction, with a per-sample cap for discontinuities.
                Progress=Mathf.Min(1,Progress+Mathf.Min(distance,config.maximumDelta)/Mathf.Max(1,config.requiredEffort));
                if(Progress>=1){CompletionCount++;return true;}
                return false;
            }
            if(midpoint.sqrMagnitude<.04f) return false;
            float tangent=Mathf.Abs(delta.x*midpoint.y-delta.y*midpoint.x)/(distance*midpoint.magnitude);
            if(tangent<config.minimumTangential) return false;
            Progress=Mathf.Min(1,Progress+distance*tangent/Mathf.Max(1,config.requiredEffort));
            if(Progress>=1) { CompletionCount++; return true; }
            return false;
        }
        static bool Inside(Vector2 point,Vector2 radii)
        { if(radii.x<=0 || radii.y<=0) return false; point=new Vector2(point.x/radii.x,point.y/radii.y); return point.sqrMagnitude<=1.0001f; }
    }
}
