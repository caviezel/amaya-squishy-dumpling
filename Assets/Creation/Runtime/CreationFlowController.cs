using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SquishyDumpling.Creation
{
    public sealed class CreationFlowController : MonoBehaviour
    {
        [Header("Persistent session")]
        public BowlPresenter bowl;
        [Header("Production lifecycle (off in preserved proof fixtures)")]
        public bool dynamicSteps;
        public Transform activeStepRoot, ingredientSpawn, ingredientInteraction, ingredientExit;
        public SquishyDumpling.Layout.ResponsiveWorldLayout layout;
        [Header("Mixing composition and tuning")]
        public MixingStep mixingPrefab;
        public MixingConfiguration mixingConfiguration;
        public MixingArea mixingArea;
        public BowlMixingPresentation mixingSurface;
        public Transform mixingSpawn, mixingRest, mixingExit;
        public GameObject ActiveStep { get; private set; }
        public MixingStep ActiveMixing { get; private set; }
        public MixingProgress MixingProgress { get; }=new MixingProgress();
        public UnityEvent onMixingComplete=new UnityEvent();
        [Header("Forming extension (optional for phase-specific fixtures)")]
        public FormingDefinition formingDefinition;
        public FormingRegion formingRegion;
        public Transform formingWorkspace, bowlFormingTransition, bowlExit, formingResultRoot;
        public FormingStep ActiveForming { get; private set; }
        public FormingProgress FormingProgress { get; }=new FormingProgress();
        public FormingMaterialPresenter FormedMaterial { get; private set; }
        public SpriteRenderer FormingWorkspace { get; private set; }
        public UnityEvent onFormingComplete=new UnityEvent();
        public bool CanFormFor(FormingStep step) => step && step==ActiveForming && isActiveAndEnabled && !paused && focused && Ready && Session.Step==CreationStep.Forming;
        [Header("Face customization extension (optional for phase-specific fixtures)")]
        public FaceCustomizationDefinition customizationDefinition;
        public Transform stampHome, stampExit;
        public FaceCustomizationStep ActiveCustomization { get; private set; }
        public UnityEvent onCustomizationComplete=new UnityEvent();
        public bool CanCustomizeFor(FaceCustomizationStep step)=>step && step==ActiveCustomization && isActiveAndEnabled && !paused && focused && Ready;
        [Header("Final antistress play (optional for phase-specific fixtures)")]
        public DumplingDeformationConfiguration deformationConfiguration;
        public DumplingDeformationController FinalDumpling { get; private set; }
        public UnityEvent onFinalPlayStarted=new UnityEvent();
        public PearlCreationPresentation pearl;
        Vector3 bowlStartPosition; Quaternion bowlStartRotation;
        void Awake() { if(bowl) { bowlStartPosition=bowl.transform.position; bowlStartRotation=bowl.transform.rotation; } }
        public bool CanMixFor(MixingStep step) => dynamicSteps && step==ActiveMixing && isActiveAndEnabled && !paused && focused && Ready && Session.Step==CreationStep.Mixing;
        public BowlDropZone dropZone;
        // Null recipe retains the approved single-ingredient proof for regression testing.
        public CreationRecipe recipe;
        public IngredientActor[] availableIngredients = new IngredientActor[0];
        public IngredientActor ingredient;
        public IngredientPresenter presenter;
        public UnityEvent onBaseIngredientsComplete = new UnityEvent();
        public CreationSession Session { get; } = new CreationSession();
        public IngredientDefinition CurrentDefinition => recipe ? recipe.GetIngredient(Session.CurrentIngredientIndex) : presenter ? presenter.definition : null;
        public bool CanInteract => isActiveAndEnabled && !paused && focused && Ready && Session.CanPickUp;
        public bool CanInteractFor(IngredientActor actor) => actor==ingredient && CanInteract;
        public bool Ready
        {
            get
            {
                if(!bowl || !bowl.isActiveAndEnabled || !dropZone || !dropZone.isActiveAndEnabled) return false;
                if(dynamicSteps)
                {
                    if(!recipe || recipe.StepCount==0 || !activeStepRoot || !activeStepRoot.gameObject.activeInHierarchy || !layout || !ingredientSpawn || !ingredientInteraction || !ingredientExit || !mixingPrefab || !mixingConfiguration || !mixingArea || !mixingArea.isActiveAndEnabled || !mixingSurface || !mixingSurface.isActiveAndEnabled || !mixingSpawn || !mixingRest || !mixingExit) return false;
                    foreach(var definition in recipe.ingredients) if(!definition || !definition.actorPrefab || !definition.actorPrefab.GetComponent<IngredientPresenter>() || !definition.actorPrefab.GetComponentInChildren<IngredientActor>()) return false;
                    if(formingDefinition && (!formingDefinition.stepPrefab || !formingRegion || !formingRegion.isActiveAndEnabled || !formingWorkspace || !bowlFormingTransition || !bowlExit || !formingResultRoot)) return false;
                    if(formingDefinition)
                    {
                        if(formingDefinition.shapeStates==null || formingDefinition.shapeStates.Length<2 || !formingDefinition.workspaceSprite || !formingDefinition.contactSprite) return false;
                        foreach(var sprite in formingDefinition.shapeStates) if(!sprite) return false;
                    }
                    if(customizationDefinition && (!formingDefinition || !customizationDefinition.IsValid || !stampHome || !stampExit)) return false;
                    if(ActiveCustomization) return ActiveCustomization.isActiveAndEnabled;
                    if(ActiveForming) return ActiveForming.isActiveAndEnabled;
                    if(ActiveMixing) return ActiveMixing.isActiveAndEnabled;
                    return !ActiveStep || ActorReady(ingredient);
                }
                if(!recipe) return ActorReady(ingredient);
                if(recipe.StepCount==0) return false;
                foreach(var definition in recipe.ingredients) if(!definition || !ActorReady(FindActor(definition))) return false;
                return true;
            }
        }
        bool cleaning, paused, focused=true, ownsTimePause;
        float resumeTimeScale=1;
        public bool IsSuspended=>paused || !focused;
        void ApplyApplicationSuspension()
        {
            if(IsSuspended && !ownsTimePause)
            {
                resumeTimeScale=Time.timeScale;ownsTimePause=true;Time.timeScale=0;
                // Cancel player ownership only. Accepted pours/transitions retain their state and tween.
                if(ingredient && ingredient.PointerOwner.HasValue)ingredient.CancelPointer();
                if(ActiveMixing)ActiveMixing.SuspendInput();
                if(ActiveForming)ActiveForming.SuspendInput();
                if(ActiveCustomization && ActiveCustomization.stamp)ActiveCustomization.stamp.ClearOwnership();
            }
            else if(!IsSuspended)RestoreTimeScale();
            if(ActiveCustomization && ActiveCustomization.choices.scrollHint)ActiveCustomization.choices.scrollHint.SetSuspended(IsSuspended);
            if(FinalDumpling)FinalDumpling.SetApplicationSuspended(IsSuspended);
            if(IsSuspended && UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.currentInputModule)
                UnityEngine.EventSystems.EventSystem.current.currentInputModule.DeactivateModule();
        }
        void RestoreTimeScale()
        {
            if(!ownsTimePause)return;ownsTimePause=false;Time.timeScale=resumeTimeScale;
        }
        bool ActorReady(IngredientActor actor)
        {
            if(!actor || !actor.isActiveAndEnabled || !actor.presenter || !actor.presenter.isActiveAndEnabled) return false;
            var p=actor.presenter;
            return p.definition && actor.layout && p.bowl && p.interactionAnchor && p.exitAnchor && p.stream && p.containerMouth &&
                (p.definition.presentationType!=IngredientPresentationType.PowderPour || p.definition.authoredStream || (p.powder && p.powder.isActiveAndEnabled));
        }
        IngredientActor FindActor(IngredientDefinition definition)
        {
            foreach(var actor in availableIngredients)
                if(actor && actor.presenter && actor.presenter.definition==definition) return actor;
            return null;
        }
        void Start() { RestartProof(); }
        void Update()
        {
            if(IsSuspended)return;
            if(!paused && focused && Ready && Session.Step==CreationStep.Initialize) StartCoroutine(Begin());
            else if(!Ready && Session.Step!=CreationStep.Initialize) Interrupt();
        }
        public void RestartProof() { Interrupt(); if(isActiveAndEnabled && !paused && focused && Ready) StartCoroutine(Begin()); }
        public void Interrupt()
        {
            if(cleaning) return; cleaning=true;GameAudioPlayer.Current?.StopGameplaySounds();
            StopAllCoroutines(); if(pearl)pearl.ResetPresentation(); FormingProgress.Reset(); MixingProgress.Reset();
            if(FinalDumpling) FinalDumpling.Suspend(); FinalDumpling=null;
            FormedMaterial=null; FormingWorkspace=null;
            if(formingResultRoot) for(int i=formingResultRoot.childCount-1;i>=0;i--) { var result=formingResultRoot.GetChild(i).gameObject; result.SetActive(false); Destroy(result); } Session.Reset(recipe ? Mathf.Max(1,recipe.StepCount) : 1,recipe!=null);
            if(dynamicSteps) CleanupActiveStep();
            else if(recipe)
            {
                if(presenter) presenter.Cancel();
                foreach(var actor in availableIngredients)
                {
                    if(!actor) continue;
                    actor.ClearOwnership();
                    if(actor.presenter) { actor.presenter.Cancel(); actor.presenter.SetAvailable(false); }
                }
                ingredient=recipe.StepCount>0 ? FindActor(recipe.GetIngredient(0)) : null;
                presenter=ingredient ? ingredient.presenter : null;
            }
            else
            {
                if(ingredient) ingredient.ClearOwnership();
                if(presenter) presenter.Cancel();
            }
            if(bowl) { bowl.Cancel(); if(dynamicSteps) { bowl.transform.position=bowlStartPosition; bowl.transform.rotation=bowlStartRotation; } } if(mixingSurface) mixingSurface.ResetVisuals(); cleaning=false;
        }
        IEnumerator Begin()
        {
            if(!Session.Begin()) yield break;
            int version=Session.Version;
            yield return bowl.Present();
            if(!bowl.Succeeded || !Session.PresentationReady(CreationStep.PresentBowl,version,0)) { Recover(version); yield break; }
            yield return PresentCurrent(version,0);
        }
        IEnumerator PresentCurrent(int version,int index)
        {
            if(!Session.Matches(version,index)) yield break;
            if(dynamicSteps)
            {
                if(pearl && index==3){yield return pearl.ChooseVariant(version);if(!Session.Matches(version,index))yield break;}
                var definition=recipe.GetIngredient(index);
                ActiveStep=Instantiate(definition.actorPrefab,activeStepRoot);
                ingredient=ActiveStep.GetComponentInChildren<IngredientActor>(); presenter=ingredient.presenter;
                ingredient.flow=this; ingredient.layout=layout;
                presenter.flow=this; presenter.definition=definition; presenter.bowl=bowl; presenter.layout=layout;
                presenter.interactionAnchor=ingredientInteraction; presenter.spawnAnchor=ingredientSpawn; presenter.exitAnchor=ingredientExit;
                presenter.stream.bowlTarget=bowl.pourTarget;
                if(presenter.powder) presenter.powder.bowlTarget=bowl.pourTarget;
                presenter.ApplyDefinition();
            }
            else if(recipe) { ingredient=FindActor(recipe.GetIngredient(index)); presenter=ingredient.presenter; }
            bowl.BeginContribution(presenter.definition);
            presenter.SetAvailable(true);
            yield return presenter.Present();
            if(!presenter.Succeeded || !Session.PresentationReady(CreationStep.PresentIngredient,version,index)) Recover(version);
        }
        public bool TryBeginDrag() => CanInteract && Session.BeginDrag();
        public void ReleaseIngredient(Vector2 pointerWorld)
        {
            bool valid=Ready && (dropZone.Contains(pointerWorld) ||
                (ingredient.dropContact && dropZone.Contains(ingredient.dropContact.position)));
            if(!Session.Release(valid)) return;
            ingredient.ClearOwnership();
            if(valid) StartCoroutine(Pour(Session.Version,Session.CurrentIngredientIndex));
            else StartCoroutine(Return(Session.Version,Session.CurrentIngredientIndex));
        }
        public void CancelDrag()
        {
            if(!Session.Release(false)) return;
            ingredient.ClearOwnership(); StartCoroutine(Return(Session.Version,Session.CurrentIngredientIndex));
        }
        IEnumerator Return(int version,int index)
        {
            yield return presenter.ReturnToIdle();
            if(!presenter.Succeeded || !Session.PresentationReady(CreationStep.ReturnIngredient,version,index)) Recover(version);
        }
        IEnumerator Pour(int version,int index)
        {
            var completedPresenter=presenter;
            yield return completedPresenter.PlayPour();
            if(!completedPresenter.Succeeded || !Session.CommitIngredient(version,index)) { Recover(version); yield break; }
            bowl.CompleteContribution();
            if(!recipe) { Session.Finish(); yield break; }
            completedPresenter.SetAvailable(false);
            if(index+1<recipe.StepCount)
            {
                yield return completedPresenter.StageNextIngredient();
                if(!completedPresenter.Succeeded || !Session.Matches(version,index)) { Recover(version); yield break; }
            }
            if(dynamicSteps) { CleanupActiveStep(); yield return null; if(!Session.Matches(version,index)) yield break; }
            if(!Session.AdvanceAfterIngredient(version,index)) { Recover(version); yield break; }
            if(Session.HasIngredient) yield return PresentCurrent(version,Session.CurrentIngredientIndex);
            else
            {
                onBaseIngredientsComplete.Invoke();
                if(dynamicSteps && Session.Version==version) { yield return null; yield return BeginMixing(version); }
            }
        }
        // Only the flow owns lifetime. Detach interruption hooks before normal retirement.
        void CleanupActiveStep()
        {
            var root=ActiveStep; ActiveStep=null;
            bool deferDeactivation=ActiveCustomization && ActiveCustomization.ActorDeactivationInProgress;
            if(ActiveCustomization) { ActiveCustomization.flow=null; ActiveCustomization.Cancel(); }
            ActiveCustomization=null;
            if(ActiveForming) { ActiveForming.flow=null; ActiveForming.Cancel(); }
            ActiveForming=null;
            if(ActiveMixing) { ActiveMixing.flow=null; ActiveMixing.Cancel(); }
            ActiveMixing=null;
            if(ingredient) { ingredient.flow=null; ingredient.ClearOwnership(); }
            if(presenter) { presenter.flow=null; presenter.Cancel(); presenter.SetAvailable(false); }
            ingredient=null; presenter=null;
            if(root) { if(!deferDeactivation && root.activeSelf) root.SetActive(false); Destroy(root); }
        }
        IEnumerator BeginMixing(int version)
        {
            if(!Session.BeginMixing(version)) yield break;
            ActiveMixing=Instantiate(mixingPrefab,activeStepRoot); ActiveStep=ActiveMixing.gameObject;
            var step=ActiveMixing; step.BindProgress(MixingProgress); step.flow=this; step.layout=layout; step.area=mixingArea; step.surface=mixingSurface;
            step.config=mixingConfiguration; mixingSurface.config=mixingConfiguration; step.spawn=mixingSpawn; step.rest=mixingRest; step.exit=mixingExit;
            yield return step.Present();
            if(Session.Version!=version) yield break;
            if(!step.Succeeded || !Session.MixingReady(version)) Recover(version);
        }
        public void RequestMixingCompletion(MixingStep step)
        {
            if(!step || step!=ActiveMixing || step.PointerOwner.HasValue || step.Progress.Progress<1 || !Session.CompleteMixing(Session.Version)) return;
            StartCoroutine(FinishMixing(Session.Version,step));
        }
        IEnumerator FinishMixing(int version,MixingStep step)
        {
            yield return step.Finish();
            if(Session.Version!=version) yield break;
            if(!step.Succeeded) { Recover(version); yield break; }
            CleanupActiveStep(); yield return null;
            if(!Session.FinishMixing(version)) yield break;
            onMixingComplete.Invoke();
            if(formingDefinition && Session.Version==version) { yield return null; yield return BeginForming(version); }
        }
        IEnumerator BeginForming(int version)
        {
            if(!Session.BeginForming(version)) yield break;
            ActiveForming=Instantiate(formingDefinition.stepPrefab,activeStepRoot); ActiveStep=ActiveForming.gameObject;
            var step=ActiveForming; step.transform.position=formingRegion.Center; step.GetComponent<BoxCollider2D>().size=formingRegion.radii*2; step.BindProgress(FormingProgress); step.definition=formingDefinition; step.flow=this; step.layout=layout;
            step.region=formingRegion; step.workspaceAnchor=formingWorkspace; step.bowlTransition=bowlFormingTransition; step.bowlExit=bowlExit;
            step.bowl=bowl; step.mixedSurface=mixingSurface;
            yield return step.Present();
            if(Session.Version!=version) yield break;
            if(!step.Succeeded || !Session.FormingReady(version)) Recover(version);
        }
        public void RequestFormingCompletion(FormingStep step)
        {
            if(!step || step!=ActiveForming || step.Progress.Progress<1 || !Session.CompleteForming(Session.Version)) return;
            StartCoroutine(FinishForming(Session.Version,step));
        }
        public bool RequestFunnelTransfer(FormingStep step)
        {
            if(!CanFormFor(step) || !step.funnelPresentation || !step.funnelPresentation.Accepted || !step.Progress.StartAutomaticTransfer())return false;
            if(!Session.CompleteForming(Session.Version))return false;
            StartCoroutine(FinishForming(Session.Version,step));return true;
        }
        IEnumerator FinishForming(int version,FormingStep step)
        {
            yield return step.Finish();
            if(Session.Version!=version) yield break;
            if(!step.Succeeded) { Recover(version); yield break; }
            FormedMaterial=step.material; FormingWorkspace=step.workspace; step.DetachResult(formingResultRoot);
            CleanupActiveStep(); yield return null;
            if(!Session.FinishForming(version)) yield break;
            onFormingComplete.Invoke();
            if(customizationDefinition && Session.Version==version) { yield return null; yield return BeginCustomization(version); }
        }
        IEnumerator BeginCustomization(int version)
        {
            if(!Session.BeginCustomization(version)) yield break;
            var target=FormedMaterial.GetComponentInChildren<FaceApplicationTarget>();
            if(!target) { Recover(version); yield break; }
            ActiveCustomization=Instantiate(customizationDefinition.stepPrefab,activeStepRoot); ActiveStep=ActiveCustomization.gameObject;
            var step=ActiveCustomization; step.flow=this; step.definition=customizationDefinition; step.layout=layout;
            step.body=FormedMaterial; step.target=target; step.home=stampHome; step.exit=stampExit;
            yield return step.Present();
            if(Session.Version!=version) yield break;
            if(!step.Succeeded || !Session.CustomizationReady(version)) { Recover(version); yield break; }
            step.Ready();
        }
        public void RequestCustomizationCompletion(FaceCustomizationStep step,int version)
        {
            if(!step || step!=ActiveCustomization || !step.Succeeded || !Session.CompleteCustomization(version)) return;
            CleanupActiveStep(); onCustomizationComplete.Invoke();
            if(deformationConfiguration && Session.Version==version) StartCoroutine(BeginFinalPlay(version));
        }
        IEnumerator BeginFinalPlay(int version)
        {
            // Let retirement finish; callbacks may restart while this semantic boundary is visible.
            yield return null;
            if(Session.Version!=version || Session.Step!=CreationStep.CustomizationComplete || !FormedMaterial) yield break;
            var states=FormedMaterial.states; var sprite=states[states.Length-1];
            var root=new GameObject("Final Dumpling / Deformable Surface"); root.transform.SetParent(sprite.transform,false);
            FinalDumpling=root.AddComponent<DumplingDeformationController>();
            FinalDumpling.Initialize(sprite,deformationConfiguration,layout,FormedMaterial.GetComponentInChildren<AppliedFacePresenter>());
            if(pearl && pearl.swipeFxPrefab)FinalDumpling.BindSwipeFx(pearl.swipeFxPrefab);
            if(pearl){FinalDumpling.enabled=false;yield return pearl.RevealFinal(FormedMaterial);if(Session.Version!=version)yield break;FinalDumpling.enabled=true;}
            if(Session.BeginFinalPlay(version))
            {
                // The forming workspace is a separate retained stage prop; never hide its result-root sibling.
                // Retire it atomically at this semantic boundary so FinalSquishPlay is tray-free from its first frame.
                if(FormingWorkspace) FormingWorkspace.gameObject.SetActive(false);
                onFinalPlayStarted.Invoke();
            }
        }
        void Recover(int version) { if(Session.Version==version) Interrupt(); }
        void OnDisable() { RestoreTimeScale();Interrupt(); }
        void OnApplicationPause(bool value) { paused=value;ApplyApplicationSuspension(); }
        void OnApplicationFocus(bool value) { focused=value;ApplyApplicationSuspension(); }
    }
}
