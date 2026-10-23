using NUnit.Framework;
using UnityEditor;
using SquishyDumpling.Creation;

public class BaseSequenceSessionTests
{
    static CreationSession Start()
    {
        var session=new CreationSession(3,true); session.Begin();
        session.PresentationReady(CreationStep.PresentBowl,session.Version,0); return session;
    }
    static void Ready(CreationSession session)
    { Assert.IsTrue(session.PresentationReady(CreationStep.PresentIngredient,session.Version,session.CurrentIngredientIndex)); }
    static void Transfer(CreationSession session) { Ready(session); session.BeginDrag(); session.Release(true); }
    [Test] public void AuthoredRecipeHasOnlyThreeCorrectlyOrderedStepsAndReusableTypes()
    {
        var recipe=AssetDatabase.LoadAssetAtPath<CreationRecipe>("Assets/Creation/Definitions/BaseCreationRecipe.asset");
        Assert.IsNotNull(recipe); Assert.AreEqual(3,recipe.StepCount);
        Assert.AreEqual("clear-glue",recipe.GetIngredient(0).ingredientId);
        Assert.AreEqual("squishy-powder",recipe.GetIngredient(1).ingredientId);
        Assert.AreEqual("activator",recipe.GetIngredient(2).ingredientId);
        Assert.AreEqual(IngredientPresentationType.LiquidPour,recipe.GetIngredient(0).presentationType);
        Assert.AreEqual(IngredientPresentationType.PowderPour,recipe.GetIngredient(1).presentationType);
        Assert.AreEqual(IngredientPresentationType.LiquidPour,recipe.GetIngredient(2).presentationType);
        Assert.IsNull(recipe.GetIngredient(3));
        foreach(var definition in recipe.ingredients)
        {
            Assert.IsNotNull(definition.containerSprite); Assert.IsNotNull(definition.contentSprite);
            Assert.That(AssetDatabase.GetAssetPath(definition.containerSprite),Does.StartWith("Assets/GameContent/Art/"),"Template recipes must use client artwork");
        }
        Assert.IsNotNull(recipe.GetIngredient(1).powderSprite);
    }
    [Test] public void EveryStepCommitsAndAdvancesOnlyOnceAndFinalStateHasNoFourthIngredient()
    {
        var session=Start(); Assert.AreEqual(0,session.CurrentIngredientIndex);
        for(int i=0;i<3;i++)
        {
            Transfer(session); Assert.IsTrue(session.CommitIngredient(session.Version,i));
            Assert.IsFalse(session.CommitIngredient(session.Version,i)); Assert.AreEqual(i+1,session.CommittedIngredients);
            Assert.IsTrue(session.AdvanceAfterIngredient(session.Version,i));
            Assert.IsFalse(session.AdvanceAfterIngredient(session.Version,i)); Assert.AreEqual(i+1,session.CurrentIngredientIndex);
        }
        Assert.AreEqual(CreationStep.BaseIngredientsComplete,session.Step);
        Assert.IsFalse(session.HasIngredient); Assert.IsFalse(session.BeginDrag()); Assert.IsFalse(session.Release(true));
    }
    [Test] public void OldStepTokenCannotCommitOrUnlockCurrentStepInSameSession()
    {
        var session=Start(); Transfer(session); session.CommitIngredient(session.Version,0);
        session.AdvanceAfterIngredient(session.Version,0);
        Assert.IsFalse(session.PresentationReady(CreationStep.PresentIngredient,session.Version,0));
        Transfer(session); Assert.IsFalse(session.CommitIngredient(session.Version,0));
        Assert.IsFalse(session.CommitIngredient(session.Version)); // Ambiguous single-step API is disallowed for a recipe.
        Assert.IsTrue(session.CommitIngredient(session.Version,1));
    }
    [Test] public void ResetInvalidatesOldSessionTokensAndReturnsToFirstIngredient()
    {
        var session=Start(); Transfer(session); int version=session.Version;
        session.Reset(); Assert.AreEqual(0,session.CurrentIngredientIndex); Assert.AreEqual(0,session.CommittedIngredients);
        session.Begin(); session.PresentationReady(CreationStep.PresentBowl,session.Version,0); Transfer(session);
        Assert.IsFalse(session.CommitIngredient(version,0)); Assert.IsTrue(session.CommitIngredient(session.Version,0));
    }
    [Test] public void InvalidDropAndWrongStateCompletionNeverAdvanceRecipe()
    {
        var session=Start(); Assert.IsFalse(session.CommitIngredient(session.Version,0));
        Ready(session); session.BeginDrag(); session.Release(false);
        Assert.IsFalse(session.CommitIngredient(session.Version,0)); Assert.AreEqual(0,session.CurrentIngredientIndex);
        Assert.IsTrue(session.PresentationReady(CreationStep.ReturnIngredient,session.Version,0));
        Assert.IsTrue(session.BeginDrag()); Assert.IsTrue(session.Release(true));
    }
    [Test] public void ResetAfterFinalCompletionRetainsRecipeAndClearsAllProgress()
    {
        var session=Start();
        for(int i=0;i<3;i++) { Transfer(session); session.CommitIngredient(session.Version,i); session.AdvanceAfterIngredient(session.Version,i); }
        session.Reset(); Assert.AreEqual(3,session.IngredientCount); Assert.AreEqual(CreationStep.Initialize,session.Step);
        Assert.AreEqual(0,session.CurrentIngredientIndex); Assert.AreEqual(0,session.CommittedIngredients); Assert.IsTrue(session.HasIngredient);
    }
}
