using UnityEngine;
using UnityEngine.Serialization;
namespace SquishyDumpling.Creation
{
    [CreateAssetMenu(menuName="Squishy Dumpling/Forming definition")]
    public sealed class FormingDefinition : ScriptableObject
    {
        [Header("First body content")]
        public FormingStep stepPrefab;
        public Sprite[] shapeStates;
        public Sprite workspaceSprite, contactSprite;
        [Header("Qualified drag (world units)")]
        [Tooltip("Total qualified drag distance required to shape the body.")]
        [Min(1)] public float requiredEffort=10;
        [Tooltip("Minimum displacement from the press point before a gesture earns progress.")]
        [Min(.05f)] public float minimumGestureTravel=.2f;
        [Min(.001f)] public float minimumMotion=.06f;
        [Min(.1f)] public float maximumDelta=.9f;
        [Header("Temporary contact")]
        [Range(0,.25f)] public float indentationStrength=.1f;
        [FormerlySerializedAs("inwardFollow")]
        [Range(0,.5f)] public float dragFollow=.22f;
        [Min(1)] public float contactDamping=18, releaseDamping=10;
        [Range(0,.06f)] public float wobbleStrength=.018f;
        [Tooltip("Approximate material half-extents used for contact placement and useful-motion qualification.")]
        public Vector2 initialContactExtents=new Vector2(4.7f,2.8f), finalContactExtents=new Vector2(3.5f,3.1f);
        [Min(0)] public float edgeForgiveness=.6f;
        [Header("Authored transition and settle (seconds)")]
        [Min(.05f)] public float workspaceEntrance=.5f, materialTransition=.75f, landingSettle=.3f;
        [Min(.05f)] public float completionAnticipation=.14f, completionSettle=.45f;
        [Header("Optional audio; events may connect later haptics")]
        public AudioClip landing, press, gesture, release, milestone, complete;
    }
}
