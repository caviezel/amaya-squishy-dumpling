using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SquishyDumpling.Creation
{
    public sealed class FaceScrollHint : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IScrollHandler
    {
        public ScrollRect scroll;
        [Min(1)]public int firstItemOnAppear=8;
        float start,initialPosition,suspendedAt;bool running,suspended;
        public void SetSuspended(bool value)
        {
            if(suspended==value)return;
            if(value)suspendedAt=Time.unscaledTime;
            else start+=Time.unscaledTime-suspendedAt;
            suspended=value;if(value && scroll)scroll.StopMovement();
        }
        public bool Running=>running;
        public void Prepare(int itemCount)
        {
            Cancel();float travel=Mathf.Max(0,scroll.content.rect.width-scroll.viewport.rect.width);
            initialPosition=travel>0?Mathf.Clamp01(Mathf.Clamp(firstItemOnAppear-1,0,itemCount-1)*scroll.content.rect.width/Mathf.Max(1,itemCount)/travel):0;
            scroll.horizontalNormalizedPosition=initialPosition;
        }
        public void Play(){Cancel();scroll.horizontalNormalizedPosition=initialPosition;start=Time.unscaledTime;running=true;}
        public void Cancel(){running=false;if(scroll)scroll.StopMovement();}
        public void OnPointerDown(PointerEventData e)=>Cancel();
        public void OnBeginDrag(PointerEventData e)=>Cancel();
        public void OnScroll(PointerEventData e)=>Cancel();
        void Update()
        {
            if(!running || suspended)return;float t=Time.unscaledTime-start;if(t<.2f)return;
            float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.2f)/1.2f));
            scroll.horizontalNormalizedPosition=Mathf.Lerp(initialPosition,0,progress);
            if(progress>=1)running=false;
        }
        void OnDisable()=>Cancel();
    }
}
