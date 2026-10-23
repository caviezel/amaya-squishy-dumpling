using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Shared canonical staging. Families supply artwork/recipes, not new layout code.
    [CreateAssetMenu(menuName="Squishy Dumpling/Creation composition")]
    public sealed class CreationComposition : ScriptableObject
    {
        [Header("Canonical artwork widths") ]
        public float bowlWidth=17,containerWidth=4.8f,powderContainerWidth=6,dyeContainerWidth=3.6f,spatulaWidth=3.8f;
        [Header("Stage positions") ]
        public Vector2 bowlPosition=new Vector2(0,-6.5f);
        public Vector2 ingredientPosition=new Vector2(0,10);
        public Vector2 mixingOffset=new Vector2(0,3.5f);
        public Vector2 mixingRadii=new Vector2(5.3f,2.5f);
        public Vector2 formingBowlPosition=new Vector2(0,7);
        public Vector2 funnelPosition=new Vector2(0,-3.8f);
        public Vector2 pouchPosition=new Vector2(0,-8.78f);
        public Vector2 bodyPosition=new Vector2(0,-6);
        public Vector2 choicePosition=new Vector2(0,-17.5f);
        public float customizationScale=1.35f;
        public float mixedResultHold=.8f;
        [Header("Bowl drop and automatic transfer") ]
        public Vector2 funnelDropOffset=new Vector2(0,2),funnelDropSize=new Vector2(12,6);
        public Vector2 bowlPourPosition=new Vector2(-6.43467f,5.7f);
        [Tooltip("Contact inside the visible right rim; includes a small overlap so the ribbon joins the glass lip.")]
        public Vector2 bowlPourMouthLocal=new Vector2(7.4f,1.7f);
        public float bowlPourAngle=-45,transferDuration=2.8f;
    }
}
