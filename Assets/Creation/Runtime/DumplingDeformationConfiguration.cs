using UnityEngine;
namespace SquishyDumpling.Creation
{
    [CreateAssetMenu(menuName="Squishy Dumpling/Deformation")]
    public sealed class DumplingDeformationConfiguration : ScriptableObject
    {
        // Numerical safety ceilings, independent of the authored default feel.
        public const float AbsoluteMaxStrain=.98f;
        public const float AbsoluteMaxStretch=8;
        [Tooltip("Body mesh bounds in sprite-local world units. Rebuild final play after changing fitting.")]
        public Rect meshBounds=new Rect(-3.9f,-3.35f,7.8f,6.45f);
        [Tooltip("Horizontal vertex count. Existing production fitting stays at 17. Requires a new final-play object.")]
        [Range(10,20)] public int columns=17;
        [Tooltip("Vertical vertex count. Existing production fitting stays at 13. Requires a new final-play object.")]
        [Range(10,20)] public int rows=13;
        [Tooltip("Opaque body outline in rest/local space for mesh-aware contact acceptance. Independent of the tray artwork.")]
        public Vector2[] contactOutline;
        [Tooltip("Local radius of inward press compression. Larger values affect a broader region.")]
        [Min(.1f)] public float pressRadius=1.65f;
        [Tooltip("Local inward indentation depth target. Grab strength and material stiffness also influence achieved depth.")]
        [Min(0)] public float pressStrength=.48f;
        [Tooltip("Radius of the soft grab force. Increase for broad, exaggerated material movement.")]
        [Min(.1f)] public float grabRadius=2.8f;
        [Tooltip("Contact spring strength. Increase to make contacted material follow the pointer more strongly.")]
        [Min(0)] public float grabStrength=260;
        [Tooltip("Smooth influence shape. Lower values spread contact more broadly; higher values localize it.")]
        [Range(.1f,5)] public float influenceFalloff=1.25f;
        [Tooltip("Neighbor displacement coupling. Increase to spread a pull through more connected material.")]
        [Min(0)] public float neighborCoupling=45;
        [Tooltip("Restoring spring stiffness. Lower values allow more give; higher values pull harder toward rest.")]
        [Min(.1f)] public float springStiffness=65;
        [Tooltip("Velocity damping. Lower values give stronger/longer overshoot and wobble; zero is intentionally undamped.")]
        [Min(0)] public float damping=10;
        [Tooltip("Progressive resistance amount. Lower values allow stronger pulls before saturation. Value 2 preserves Task 8's curve exactly.")]
        [Range(.25f,8)] public float stretchResistance=2;
        [Tooltip("Maximum local displacement, live while playing. Hard safety ceiling is 8 units; the broad-phase contact envelope updates with this value.")]
        [Min(.1f)] public float maxStretch=2.6f;
        [Tooltip("Maximum displacement-gradient strain. Higher allows stronger local distortion. Absolute runtime ceiling .98 preserves positive orientation; broaden grab radius for larger smooth pulls.")]
        [Range(.05f,.98f)] public float maxStrain=.72f;
        [Tooltip("Rest spring multiplier after release. Together with damping, controls recovery speed and overshoot. No separate artificial wobble driver is used.")]
        [Min(.01f)] public float recoveryStrength=1;
        [Tooltip("Displacement AND speed threshold below which an unheld body settles exactly to rest.")]
        [Min(.0001f)] public float settleThreshold=.002f;
        [Tooltip("Fixed simulation substep in seconds. Smaller steps tolerate stronger springs but cost more CPU.")]
        [Range(.002f,.01f)] public float fixedStep=1f/240;
        [Tooltip("Maximum catch-up substeps per frame. Excess elapsed time is discarded to prevent a catch-up spiral.")]
        [Range(4,32)] public int maxSubsteps=24;
        [Tooltip("Maximum vertex speed in local units per second. Hard runtime ceiling 120.")]
        [Min(1)] public float velocityLimit=24;
        [Tooltip("Selected-object mesh edge gizmos only; off for normal presentation.")]
        public bool debugGizmos;
        public float SafeMaxStretch=>Safe(maxStretch,2.6f,.1f,AbsoluteMaxStretch);
        public float SafeMaxStrain=>Safe(maxStrain,.72f,.05f,AbsoluteMaxStrain);
        public float ResistanceExponent=>4/Safe(stretchResistance,2,.25f,8);
        public static float Safe(float value,float fallback,float minimum,float maximum)
        { return float.IsNaN(value)||float.IsInfinity(value) ? fallback : Mathf.Clamp(value,minimum,maximum); }
    }
}
