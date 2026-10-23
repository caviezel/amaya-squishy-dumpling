using UnityEngine;
namespace SquishyDumpling.Creation
{
    [System.Serializable] public sealed class PearlVariant
    {
        public string id,displayName;
        public IngredientDefinition ingredient;
        public Sprite inBowl,body;
        public Sprite[] mixing;
        public DumplingDeformationConfiguration deformation;
    }
    [CreateAssetMenu(menuName="Squishy Dumpling/Pearl presentation")]
    public sealed class PearlRecipeDefinition : ScriptableObject
    {
        public PearlVariant[] variants;
        public Sprite makingBackground,playBackground,choiceFrame,selectedFrame,confirmSprite;
    }
}
