using UnityEngine;

namespace SquishyDumpling.Creation
{
    public enum IngredientPresentationType { LiquidPour, PowderPour }

    [CreateAssetMenu(menuName = "Squishy Dumpling/Ingredient definition")]
    public sealed class IngredientDefinition : ScriptableObject
    {
        [Header("Content and production actor")]
        public GameObject actorPrefab;
        public string ingredientId = "clear-glue";
        public string displayName = "Clear Glue";
        public IngredientPresentationType presentationType = IngredientPresentationType.LiquidPour;
        [Min(0)] public int bowlLayer;
        [Min(.01f)] public float contributionAmount = 1f;
        public Color fillColor = Color.white;
        public Color transferColor = Color.white;
        public Sprite powderSprite;
        public Sprite containerSprite, contentSprite, streamSprite;
        public bool bakedContainer, authoredStream;
        public bool fadeAuthoredFlow;
        [Header("Optional centred pour presentation")]
        public bool fixedCenteredPour;
        public Vector2 fixedPourContact=new Vector2(0,-3.5f);
        public Sprite[] streamFrames=new Sprite[0];
        [Min(.02f)] public float streamFrameDuration=.12f;
        [Header("Optional individual dye drops")]
        public bool discreteDrops;
        [Range(1,6)] public int dropCount=3;
        [Min(.1f)] public float dropWidth=1.1f;
        [Header("Presentation timing (seconds)")]
        [Min(.01f)] public float anticipationDuration=.14f;
        [Min(.05f)] public float entranceDuration = .65f;
        [Min(.05f)] public float returnDuration = .38f;
        [Min(.05f)] public float positionDuration = .52f;
        [Min(.05f)] public float pourDuration = 1.25f;
        public float pourAngle = -108f;
        [Min(.05f)] public float exitDuration=.55f;
        [Header("Optional audio")]
        public AudioClip pickup, accepted, pourStart, pourLoop, impact, completed;
    }
}
