using System;

namespace SquishyDumpling.Creation
{
    public enum CreationStep
    {
        Initialize, PresentBowl, PresentIngredient, AwaitIngredientInteraction,
        DragIngredient, ReturnIngredient, PourIngredient, IngredientComplete, ProofComplete, BaseIngredientsComplete, PresentMixingTool, Mixing, FinishMixing, MixingComplete, PresentForming, Forming, FinishForming, FormingComplete, PresentCustomization, Customization, Stamping, FaceApplied, CustomizationComplete, FinalSquishPlay
    }

    // Semantic authority only. A (version, ingredient index) pair identifies every asynchronous operation.
    public sealed class CreationSession
    {
        public CreationStep Step { get; private set; } = CreationStep.Initialize;
        public int Version { get; private set; }
        public int CommittedIngredients { get; private set; }
        public int CurrentIngredientIndex { get; private set; }
        public int IngredientCount { get; private set; }
        public bool HasIngredient => CurrentIngredientIndex < IngredientCount;
        public bool CanPickUp => HasIngredient && Step == CreationStep.AwaitIngredientInteraction;
        public string SelectedFaceId { get; private set; }
        public string AppliedFaceId { get; private set; }
        public int FaceApplicationCount { get; private set; }
        public int CustomizationCompletionCount { get; private set; }
        public int FinalPlayStartCount { get; private set; }
        bool completeBaseIngredients;
        public CreationSession(int ingredientCount = 1, bool completeBaseIngredients = false)
        { Configure(ingredientCount, completeBaseIngredients); }
        void Configure(int count, bool completeBase)
        {
            if(count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            IngredientCount=count; completeBaseIngredients=completeBase;
        }
        public void Reset(int ingredientCount, bool completeBase)
        { Configure(ingredientCount,completeBase); Reset(); }
        public void Reset()
        { Version++; FinalPlayStartCount=0; SelectedFaceId=AppliedFaceId=null; FaceApplicationCount=CustomizationCompletionCount=0; CommittedIngredients=0; CurrentIngredientIndex=0; Step=CreationStep.Initialize; }
        public bool Begin() => Move(CreationStep.Initialize, CreationStep.PresentBowl);
        public bool Matches(int version, int index) => Version==version && CurrentIngredientIndex==index;
        public bool PresentationReady(CreationStep expected, int version) => IngredientCount==1 && PresentationReady(expected,version,0);
        public bool PresentationReady(CreationStep expected, int version, int index)
        {
            if(!Matches(version,index)) return false;
            switch(expected)
            {
                case CreationStep.PresentBowl: return Move(expected, CreationStep.PresentIngredient);
                case CreationStep.PresentIngredient:
                case CreationStep.ReturnIngredient: return Move(expected, CreationStep.AwaitIngredientInteraction);
                default: return false;
            }
        }
        public bool BeginDrag() => CanPickUp && Move(CreationStep.AwaitIngredientInteraction, CreationStep.DragIngredient);
        public bool Release(bool valid) => Move(CreationStep.DragIngredient,
            valid ? CreationStep.PourIngredient : CreationStep.ReturnIngredient);
        // Retained for the single-ingredient regression fixture; multi-step callers must supply a step token.
        public bool CommitIngredient(int version) => IngredientCount==1 && CommitIngredient(version,0);
        public bool CommitIngredient(int version,int index)
        {
            if(!Matches(version,index) || !HasIngredient || Step!=CreationStep.PourIngredient ||
                CommittedIngredients!=CurrentIngredientIndex) return false;
            CommittedIngredients++; Step=CreationStep.IngredientComplete; return true;
        }
        public bool AdvanceAfterIngredient(int version,int index)
        {
            if(!Matches(version,index) || Step!=CreationStep.IngredientComplete || CommittedIngredients!=index+1) return false;
            CurrentIngredientIndex=CommittedIngredients;
            Step=HasIngredient ? CreationStep.PresentIngredient :
                completeBaseIngredients ? CreationStep.BaseIngredientsComplete : CreationStep.ProofComplete;
            return true;
        }
        public bool BeginMixing(int version) => Version==version && Move(CreationStep.BaseIngredientsComplete,CreationStep.PresentMixingTool);
        public bool MixingReady(int version) => Version==version && Move(CreationStep.PresentMixingTool,CreationStep.Mixing);
        public bool CompleteMixing(int version) => Version==version && Move(CreationStep.Mixing,CreationStep.FinishMixing);
        public bool FinishMixing(int version) => Version==version && Move(CreationStep.FinishMixing,CreationStep.MixingComplete);
        public bool BeginForming(int version) => Version==version && Move(CreationStep.MixingComplete,CreationStep.PresentForming);
        public bool FormingReady(int version) => Version==version && Move(CreationStep.PresentForming,CreationStep.Forming);
        public bool CompleteForming(int version) => Version==version && Move(CreationStep.Forming,CreationStep.FinishForming);
        public bool FinishForming(int version) => Version==version && Move(CreationStep.FinishForming,CreationStep.FormingComplete);
        public bool BeginCustomization(int version) => Version==version && Move(CreationStep.FormingComplete,CreationStep.PresentCustomization);
        public bool CustomizationReady(int version) => Version==version && Move(CreationStep.PresentCustomization,CreationStep.Customization);
        public bool SelectFace(string id,FaceCustomizationDefinition definition,int version)
        {
            if(Version!=version || Step!=CreationStep.Customization || !definition || definition.Find(id)==null) return false;
            SelectedFaceId=id; return true;
        }
        public bool BeginStamping(int version) => Version==version && !string.IsNullOrEmpty(SelectedFaceId) && Move(CreationStep.Customization,CreationStep.Stamping);
        public bool ApplyFace(int version)
        {
            if(Version!=version || !Move(CreationStep.Stamping,CreationStep.FaceApplied)) return false;
            AppliedFaceId=SelectedFaceId; FaceApplicationCount++; return true;
        }
        public bool CompleteCustomization(int version)
        {
            if(Version!=version || !Move(CreationStep.FaceApplied,CreationStep.CustomizationComplete)) return false;
            CustomizationCompletionCount++; return true;
        }
        public bool ConfirmSelectedFace(int version)
        {
            if(Version!=version || string.IsNullOrEmpty(SelectedFaceId) || !Move(CreationStep.Customization,CreationStep.FaceApplied))return false;
            AppliedFaceId=SelectedFaceId;FaceApplicationCount++;return true;
        }
        public bool BeginFinalPlay(int version)
        { if(Version!=version || !Move(CreationStep.CustomizationComplete,CreationStep.FinalSquishPlay)) return false; FinalPlayStartCount++; return true; }
        public bool Finish() => IngredientCount==1 && AdvanceAfterIngredient(Version,0);
        bool Move(CreationStep expected, CreationStep next)
        { if(Step!=expected) return false; Step=next; return true; }
    }
}
