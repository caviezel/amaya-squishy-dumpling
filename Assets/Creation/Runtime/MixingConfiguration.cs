using UnityEngine;
namespace SquishyDumpling.Creation
{
    [CreateAssetMenu(menuName="Squishy Dumpling/Mixing configuration")]
    public sealed class MixingConfiguration : ScriptableObject
    {
        [Header("Meaningful stirring (world units)")]
        [Tooltip("Accept any swipe direction while the tool contact stays in the bowl, including across the centre.")]
        public bool acceptAnySwipe;
        [Tooltip("Qualified tangential travel required; either direction works.")] [Min(1)] public float requiredEffort=38;
        [Min(.001f)] public float minimumMotion=.02f;
        [Tooltip("Reject discontinuities, rather than crediting teleports.")] [Min(.05f)] public float maximumDelta=.9f;
        [Range(0,1)] public float minimumTangential=.25f;
        [Header("Tool contact")]
        [Min(1)] public float followSpeed=14;
        [Range(0,1)] public float resistance=.35f;
        [Min(.05f)] public float maximumLag=.65f;
        [Min(1)] public float rotationResponse=12;
        [Header("Presentation")]
        [Min(.05f)] public float entranceDuration=.65f, releaseDuration=.25f, completionDuration=.65f, exitDuration=.45f;
        [Range(0,.2f)] public float wobbleStrength=.035f;
        [Range(0,15)] public float surfaceRotation=5;
        [Range(0,.1f)] public float milestoneStrength=.025f;
        [Header("Optional audio")]
        public AudioClip pickup, stirringLoop, milestone, complete;
    }
}
