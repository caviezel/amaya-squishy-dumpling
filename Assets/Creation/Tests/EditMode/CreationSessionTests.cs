using NUnit.Framework;
using UnityEngine;
using SquishyDumpling.Creation;

public class CreationSessionTests
{
    static CreationSession Ready()
    {
        var s=new CreationSession(); s.Begin();
        s.PresentationReady(CreationStep.PresentBowl,s.Version);
        s.PresentationReady(CreationStep.PresentIngredient,s.Version); return s;
    }
    [Test] public void InitializationGatesActionsAndPresentationHasExplicitSteps()
    {
        var s=new CreationSession(); Assert.AreEqual(CreationStep.Initialize,s.Step);
        Assert.IsFalse(s.BeginDrag()); Assert.IsFalse(s.Release(true)); Assert.IsFalse(s.CommitIngredient(s.Version));
        Assert.IsTrue(s.Begin()); Assert.IsFalse(s.Begin());
        Assert.IsFalse(s.PresentationReady(CreationStep.PresentIngredient,s.Version));
        Assert.IsTrue(s.PresentationReady(CreationStep.PresentBowl,s.Version));
        Assert.IsFalse(s.BeginDrag());
        Assert.IsTrue(s.PresentationReady(CreationStep.PresentIngredient,s.Version)); Assert.IsTrue(s.CanPickUp);
    }
    [Test] public void InvalidReleaseReturnsWithoutCommitAndAllowsRetry()
    {
        var s=Ready(); Assert.IsTrue(s.BeginDrag()); Assert.IsTrue(s.Release(false));
        Assert.AreEqual(CreationStep.ReturnIngredient,s.Step); Assert.AreEqual(0,s.CommittedIngredients);
        Assert.IsFalse(s.BeginDrag()); Assert.IsFalse(s.CommitIngredient(s.Version));
        Assert.IsTrue(s.PresentationReady(CreationStep.ReturnIngredient,s.Version)); Assert.IsTrue(s.BeginDrag());
    }
    [Test] public void ValidReleaseLocksManipulationAndCommitsExactlyOnce()
    {
        var s=Ready(); s.BeginDrag(); Assert.IsTrue(s.Release(true));
        Assert.AreEqual(CreationStep.PourIngredient,s.Step); Assert.IsFalse(s.CanPickUp);
        Assert.IsFalse(s.BeginDrag()); Assert.IsFalse(s.Release(true));
        Assert.IsTrue(s.CommitIngredient(s.Version)); Assert.AreEqual(CreationStep.IngredientComplete,s.Step);
        Assert.IsFalse(s.CommitIngredient(s.Version)); Assert.IsTrue(s.Finish()); Assert.IsFalse(s.Finish());
        Assert.AreEqual(CreationStep.ProofComplete,s.Step); Assert.AreEqual(1,s.CommittedIngredients);
    }
    [Test] public void ResetInvalidatesStalePresentationAndCommitReports()
    {
        var s=Ready(); s.BeginDrag(); s.Release(true); int old=s.Version;
        s.Reset(); Assert.AreEqual(CreationStep.Initialize,s.Step); Assert.AreEqual(0,s.CommittedIngredients);
        s.Begin(); Assert.IsFalse(s.PresentationReady(CreationStep.PresentBowl,old));
        s.PresentationReady(CreationStep.PresentBowl,s.Version); s.PresentationReady(CreationStep.PresentIngredient,s.Version);
        s.BeginDrag(); s.Release(true); Assert.IsFalse(s.CommitIngredient(old)); Assert.IsTrue(s.CommitIngredient(s.Version));
    }
    [Test] public void OwnershipPreservesOriginalContactOffset()
    {
        var g=new SinglePointerGrab(); Assert.IsTrue(g.Begin(7,new Vector2(4,6),new Vector2(3,4)));
        Assert.AreEqual(new Vector2(-1,-2),g.Offset);
        Assert.IsTrue(g.TryPosition(7,new Vector2(10,12),out var position)); Assert.AreEqual(new Vector2(9,10),position);
    }
    [Test] public void AnotherPointerCannotStealMoveOrRelease()
    {
        var g=new SinglePointerGrab(); g.Begin(7,Vector2.zero,Vector2.one);
        Assert.IsFalse(g.Begin(8,Vector2.zero,Vector2.zero)); Assert.IsFalse(g.TryPosition(8,Vector2.one,out _));
        Assert.IsFalse(g.Release(8)); Assert.AreEqual(7,g.PointerId);
        Assert.IsTrue(g.Release(7)); Assert.IsNull(g.PointerId);
    }
    [Test] public void CancellationClearsOwnershipAndOffsetAndAllowsNewPointer()
    {
        var g=new SinglePointerGrab(); g.Begin(-1,Vector2.zero,Vector2.one); g.Cancel(); g.Cancel();
        Assert.IsNull(g.PointerId); Assert.AreEqual(Vector2.zero,g.Offset); Assert.IsTrue(g.Begin(5,Vector2.zero,Vector2.zero));
    }
}
