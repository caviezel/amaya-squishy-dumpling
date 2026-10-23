using UnityEngine;
using UnityEngine.EventSystems;
using SquishyDumpling.Layout;

namespace SquishyDumpling.Creation
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class IngredientActor : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler, IEndDragHandler, ICancelHandler
    {
        public ResponsiveWorldLayout layout;
        public CreationFlowController flow;
        public IngredientPresenter presenter;
        public Transform dropContact; // Authored lower-body intent point, independent of pickup visual offsets.
        // Authored maximum pickup envelope, not computed from device pixels or temporary art bounds.
        public Vector2 dragHalfExtents = new Vector2(2.4f,4.2f);
        public int? PointerOwner => grab.PointerId;
        public Vector2 GrabOffset => grab.Offset;
        readonly SinglePointerGrab grab=new SinglePointerGrab();
        public void OnPointerDown(PointerEventData data)
        {
            if(!isActiveAndEnabled || grab.PointerId.HasValue || !flow.CanInteractFor(this) ||
                !layout.TryScreenToWorld(data.position,out var world) ||
                !GetComponent<BoxCollider2D>().OverlapPoint(world)) return;
            if(!flow.TryBeginDrag()) return;
            grab.Begin(data.pointerId,world,transform.position); data.useDragThreshold=false;
            presenter.PlayPickup();
        }
        public void OnBeginDrag(PointerEventData data) { } // Ownership starts on down, including no-motion release.
        public void OnDrag(PointerEventData data)
        {
            if(flow.IsSuspended || flow.ingredient!=this || flow.Session.Step!=CreationStep.DragIngredient ||
                !layout.TryScreenToWorld(data.position,out var world) || !grab.TryPosition(data.pointerId,world,out var position)) return;
            Rect critical=CameraFit.CriticalBounds;
            position.x=Mathf.Clamp(position.x,critical.xMin+dragHalfExtents.x,critical.xMax-dragHalfExtents.x);
            position.y=Mathf.Clamp(position.y,critical.yMin+dragHalfExtents.y,critical.yMax-dragHalfExtents.y);
            transform.position=new Vector3(position.x,position.y,transform.position.z);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if(flow.ingredient!=this) { grab.Cancel(); return; }
            if(grab.PointerId==data.pointerId && IsCancelledTouch(data.pointerId)) { CancelPointer(); return; }
            if(!grab.Release(data.pointerId)) return;
            if(layout.TryScreenToWorld(data.position,out var world)) flow.ReleaseIngredient(world);
            else flow.CancelDrag();
        }
        public void OnEndDrag(PointerEventData data)
        { if(grab.PointerId==data.pointerId) CancelPointer(); } // Fallback when release delivery was interrupted.
        public void OnCancel(BaseEventData data) { CancelPointer(); }
        public void ClearOwnership() { grab.Cancel(); }
        public void CancelPointer() { bool owned=grab.PointerId.HasValue; grab.Cancel(); if(owned && flow && flow.ingredient==this) flow.CancelDrag(); }
        static bool IsCancelledTouch(int pointerId)
        {
            for(int i=0;i<Input.touchCount;i++)
            { var touch=Input.GetTouch(i); if(touch.fingerId==pointerId && touch.phase==TouchPhase.Canceled) return true; }
            return false;
        }
        void Update() { if(grab.PointerId.HasValue && IsCancelledTouch(grab.PointerId.Value)) CancelPointer(); }
        void OnDisable() { grab.Cancel(); if(flow) flow.Interrupt(); }
        void OnApplicationFocus(bool focused) { if(!focused) CancelPointer(); }
        void OnApplicationPause(bool paused) { if(paused) CancelPointer(); }
    }
}
