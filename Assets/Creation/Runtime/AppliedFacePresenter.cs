using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Replaceable rendering adapter. Session identity is authoritative, never sprite visibility.
    public sealed class AppliedFacePresenter : MonoBehaviour
    {
        public string FaceId { get; private set; }
        public SpriteRenderer Visual { get; private set; }
        public void Apply(FaceOption option)
        {
            FaceId=option.id; transform.localPosition=option.appliedLocalPosition;
            transform.localRotation=Quaternion.Euler(0,0,option.appliedLocalRotation); transform.localScale=option.appliedLocalScale;
            Visual=GetComponent<SpriteRenderer>();if(!Visual)Visual=gameObject.AddComponent<SpriteRenderer>(); Visual.sprite=option.appliedArtwork;
            Visual.sortingLayerName="Gameplay"; Visual.sortingOrder=14;
        }
    }
}
