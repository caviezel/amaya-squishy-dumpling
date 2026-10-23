using UnityEngine;
using UnityEngine.EventSystems;
namespace SquishyDumpling.Creation
{
    public sealed class FaceScrollTouch : MonoBehaviour,IPointerDownHandler
    {
        public FaceScrollHint hint;
        public void OnPointerDown(PointerEventData e){if(hint)hint.Cancel();}
    }
}
