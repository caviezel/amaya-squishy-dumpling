using UnityEngine;
using UnityEngine.UI;
namespace SquishyDumpling.Creation
{
    // Genuine UI shared by every family; semantic creation states drive six milestones.
    public sealed class CreationProgressView : MonoBehaviour
    {
        public CreationFlowController flow;
        public Image[] markers;
        public Sprite current,completed,next;
        public RectTransform safeRoot;
        public GameObject row;
        public int[] ingredientMilestones={1,2,3,4};
        public int CurrentNumber { get; private set; }
        public int Milestone(CreationStep step,int ingredient)
        {
            if(step>=CreationStep.PresentCustomization)return 6;
            if(step>=CreationStep.PresentForming)return 5;
            if(step>=CreationStep.BaseIngredientsComplete)return 4;
            return ingredientMilestones[Mathf.Clamp(ingredient,0,ingredientMilestones.Length-1)];
        }
        void LateUpdate()
        {
            var viewport=flow.layout.WorldCamera.pixelRect;var safe=flow.layout.UsablePixels;
            if(viewport.width<=0||viewport.height<=0)return;
            safeRoot.anchorMin=new Vector2((safe.xMin-viewport.xMin)/viewport.width,(safe.yMin-viewport.yMin)/viewport.height);
            safeRoot.anchorMax=new Vector2((safe.xMax-viewport.xMin)/viewport.width,(safe.yMax-viewport.yMin)/viewport.height);
            safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            CurrentNumber=Milestone(flow.Session.Step,flow.Session.CurrentIngredientIndex);
            row.SetActive(flow.Session.Step!=CreationStep.FinalSquishPlay);
            for(int i=0;i<markers.Length;i++)
            {
                markers[i].sprite=i+1==CurrentNumber?current:i+1<CurrentNumber?completed:next;
                markers[i].transform.localScale=Vector3.one*(i+1==CurrentNumber?1.18f:1);
            }
        }
    }
}
