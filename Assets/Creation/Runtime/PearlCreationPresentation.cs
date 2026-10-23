using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
namespace SquishyDumpling.Creation
{
    // Optional content adapter. Session and all actual ingredient/mix/deformation mechanics stay shared.
    public sealed class PearlCreationPresentation : MonoBehaviour
    {
        public CreationFlowController flow;
        public PearlRecipeDefinition definition;
        public DumplingFamilyCatalog families;
        public string FamilyId { get; private set; }="pearl";
        public int ChoicePresentationCount { get; private set; }
        public IReadOnlyList<Button> ChoiceButtons=>buttons;
        public CreationComposition composition;
        public ParticleSystem swipeFxPrefab;
        public SpriteRenderer background,overlay;
        public Canvas selectionCanvas;
        public CanvasGroup selectionGroup;
        public Button choiceTemplate;
        public SpriteRenderer[] mixingStages;
        public SpriteRenderer[] bowlShell=>flow.bowl.shellRenderers;
        public void SetBowlVisible(bool visible)=>flow.bowl.SetShellVisible(visible);
        [Header("Authored final reveal (uniform, independent of screen size)")]
        public float finalScale=1.6f;
        public Vector3 finalPosition=new Vector3(0,1,0);
        public bool AwaitingChoice { get; private set; }
        public int SelectedIndex { get; private set; }
        CreationRecipe runtimeRecipe;FormingDefinition runtimeForming;Sequence transition;
        readonly List<Button> buttons=new List<Button>();
        void Awake()
        {
            if(families)
            {
                var family=families.Find(DumplingFamilySelection.SelectedId) ?? families.Find("pearl");
                definition=family.presentation;FamilyId=family.id;
            }
            runtimeRecipe=Instantiate(flow.recipe);runtimeRecipe.ingredients=new List<IngredientDefinition>(flow.recipe.ingredients);flow.recipe=runtimeRecipe;
            runtimeForming=Instantiate(flow.formingDefinition);runtimeForming.shapeStates=(Sprite[])flow.formingDefinition.shapeStates.Clone();flow.formingDefinition=runtimeForming;
            flow.pearl=this;selectionCanvas.worldCamera=flow.layout.WorldCamera;
            selectionCanvas.transform.position=composition.choicePosition;
            for(int i=0;i<definition.variants.Length;i++)
            {
                int index=i;var button=Instantiate(choiceTemplate,choiceTemplate.transform.parent);button.gameObject.SetActive(true);
                button.name=FamilyId+" choice "+definition.variants[i].id;
                ((RectTransform)button.transform).anchoredPosition=new Vector2((i-1.5f)*300,0);
                button.transform.Find("Preview").GetComponent<Image>().sprite=definition.variants[i].ingredient.containerSprite;
                var label=button.GetComponentInChildren<Text>();if(label)label.text=definition.variants[i].displayName;
                button.onClick.AddListener(()=>SelectVariant(index));buttons.Add(button);
            }
            ResetPresentation();
        }
        public void ResetPresentation()
        {
            if(transition.isAlive)transition.Stop();AwaitingChoice=false;SelectedIndex=0;ChoicePresentationCount=0;
            if(selectionGroup){selectionGroup.alpha=0;selectionGroup.interactable=false;selectionGroup.blocksRaycasts=false;}
            if(background)background.sprite=definition.makingBackground;if(overlay){overlay.enabled=false;overlay.color=Color.clear;}
            SetBowlVisible(true);ApplyVariant(0);
        }
        void ApplyVariant(int index)
        {
            var variant=definition.variants[index];SelectedIndex=index;
            runtimeRecipe.ingredients[3]=variant.ingredient;
            flow.bowl.contributionLayers[3].sprite=variant.inBowl;
            for(int i=0;i<mixingStages.Length;i++)mixingStages[i].sprite=variant.mixing[i];
            runtimeForming.shapeStates[0]=runtimeForming.shapeStates[1]=runtimeForming.shapeStates[2]=variant.mixing[2];runtimeForming.shapeStates[3]=variant.body;
            flow.deformationConfiguration=variant.deformation;
        }
        public IEnumerator ChooseVariant(int version)
        {
            ChoicePresentationCount++;AwaitingChoice=true;selectionGroup.interactable=selectionGroup.blocksRaycasts=true;
            foreach(var button in buttons)button.interactable=true;
            transition=Sequence.Create().Chain(Tween.Custom(selectionGroup,0,1,.3f,(g,v)=>g.alpha=v,Ease.OutSine));
            while(AwaitingChoice && flow.Session.Version==version)yield return null;
            if(flow.Session.Version!=version)yield break;
            selectionGroup.interactable=selectionGroup.blocksRaycasts=false;
            if(transition.isAlive)transition.Stop();
            float from=selectionGroup.alpha;
            if(from>.0001f){transition=Sequence.Create().Chain(Tween.Custom(selectionGroup,from,0,.2f,(g,v)=>g.alpha=v));while(transition.isAlive)yield return null;}
        }
        public bool SelectVariant(int index)
        {
            if(flow.IsSuspended || !AwaitingChoice || index<0 || index>=definition.variants.Length)return false;
            GameAudioPlayer.Current?.Play(GameSound.Button);ApplyVariant(index);AwaitingChoice=false;foreach(var button in buttons)button.interactable=false;return true;
        }
        public IEnumerator RevealFinal(FormingMaterialPresenter body)
        {
            GameAudioPlayer.Current?.Play(GameSound.FinalReveal);
            overlay.sprite=definition.playBackground;overlay.color=new Color(1,1,1,0);overlay.enabled=true;
            transition=Sequence.Create().Chain(Tween.Custom(overlay,0,1,.55f,(s,v)=>s.color=new Color(1,1,1,v),Ease.InOutSine))
                .Group(Tween.Scale(body.transform,Vector3.one*finalScale,.7f,Ease.OutCubic))
                .Group(Tween.Position(body.transform,finalPosition,.7f,Ease.InOutSine));
            while(transition.isAlive)yield return null;
            background.sprite=definition.playBackground;overlay.enabled=false;
        }
        void LateUpdate()
        {
            if(!flow || !flow.layout)return;
            Rect bounds=flow.layout.VisibleWorldBounds;
            Fit(background,bounds);Fit(overlay,bounds);
        }
        static void Fit(SpriteRenderer renderer,Rect bounds)
        {
            if(!renderer || !renderer.sprite)return;
            Vector2 size=renderer.sprite.bounds.size;
            float scale=Mathf.Max(bounds.width/size.x,bounds.height/size.y)*1.015f;
            renderer.transform.position=new Vector3(bounds.center.x,bounds.center.y,renderer.transform.position.z);
            renderer.transform.localScale=Vector3.one*scale; // Uniform cover of decorative-only artwork; gameplay fit remains canonical.
        }
        void OnDisable(){if(transition.isAlive)transition.Stop();AwaitingChoice=false;}
        void OnDestroy(){if(runtimeRecipe)Destroy(runtimeRecipe);if(runtimeForming)Destroy(runtimeForming);}
    }
}
