using System.Collections.Generic;
using UnityEngine;

namespace SquishyDumpling.Creation
{
    [CreateAssetMenu(menuName = "Squishy Dumpling/Creation recipe")]
    public sealed class CreationRecipe : ScriptableObject
    {
        public List<IngredientDefinition> ingredients = new List<IngredientDefinition>();
        public int StepCount => ingredients.Count;
        public IngredientDefinition GetIngredient(int index) => index >= 0 && index < ingredients.Count ? ingredients[index] : null;
    }
}
