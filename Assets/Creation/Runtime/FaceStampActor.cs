using UnityEngine;
using UnityEngine.EventSystems;
using SquishyDumpling.Layout;
namespace SquishyDumpling.Creation
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FaceStampActor : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IPointerUpHandler,IEndDragHandler,ICancelHandler
    {
        public SpriteRenderer artwork, facePreview;
        public Transform grabContact, stampContact;
        public Vector2 dragHalfExtents=new Vector2(2.5f,3);
        [System.NonSerialized] public FaceCustomizationStep step;
        public int? PointerOwner=>grab.PointerId;
        public Vector2 GrabOffset=>grab.Offset;
        readonly SinglePointerGrab grab=new SinglePointerGrab();
        public void Configure(FaceOption face)
        { facePreview.enabled=true; artwork.sprite=face.stampArtwork; facePreview.sprite=face.preview; }
        public void OnPointerDown(PointerEventData data)
        {
            if(!step || !step.CanDrag || grab.PointerId.HasValue || !step.layout.TryScreenToWorld(data.position,out var p) || !GetComponent<BoxCollider2D>().OverlapPoint(p)) return;
            grab.Begin(data.pointerId,p,transform.position); data.useDragThreshold=false; step.Grabbed();
        }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data)
        {
            if(!step || !step.CanDrag || !step.layout.TryScreenToWorld(data.position,out var p) || !grab.TryPosition(data.pointerId,p,out var position)) return;
            Rect bounds=CameraFit.CriticalBounds;
            position.x=Mathf.Clamp(position.x,bounds.xMin+dragHalfExtents.x,bounds.xMax-dragHalfExtents.x);
            position.y=Mathf.Clamp(position.y,bounds.yMin+dragHalfExtents.y,bounds.yMax-dragHalfExtents.y);
            transform.position=new Vector3(position.x,position.y,transform.position.z); step.CheckIntent(p);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if(grab.PointerId!=data.pointerId) return;
            if(IsCancelled(data.pointerId)) { CancelPointer(); return; }
            grab.Release(data.pointerId);
            if(step && step.CanDrag && step.layout.TryScreenToWorld(data.position,out var p)) step.ReleaseStamp(p);
            else if(step) step.ReturnStamp();
        }
        public void OnEndDrag(PointerEventData data) { if(grab.PointerId==data.pointerId) CancelPointer(); }
        public void OnCancel(BaseEventData data) { CancelPointer(); }
        public void ClearOwnership() { grab.Cancel(); }
        void CancelPointer() { bool owned=grab.PointerId.HasValue; grab.Cancel(); if(owned && step) step.ReturnStamp(); }
        static bool IsCancelled(int id)
        { for(int i=0;i<Input.touchCount;i++) { var t=Input.GetTouch(i); if(t.fingerId==id && t.phase==TouchPhase.Canceled) return true; } return false; }
        void Update() { if(grab.PointerId.HasValue && IsCancelled(grab.PointerId.Value)) CancelPointer(); }
        void OnDisable() { grab.Cancel(); if(step) step.ActorDisabled(); }
    }
}
