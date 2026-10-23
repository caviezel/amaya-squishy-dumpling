using NUnit.Framework;
using UnityEngine;
using SquishyDumpling.Creation;
public class MixingProgressTests
{
    MixingConfiguration config; MixingProgress progress;
    readonly Vector2 center=new Vector2(0,-4.7f), radii=new Vector2(3.5f,1.15f);
    [SetUp] public void SetUp() { config=ScriptableObject.CreateInstance<MixingConfiguration>(); progress=new MixingProgress(); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(config); }
    Vector2 Point(float angle,float irregular=0) => center+new Vector2(Mathf.Cos(angle)*(2.7f+irregular),Mathf.Sin(angle)*(.8f+irregular*.1f));
    [Test] public void StillJitterTapsAndWaitingDoNotEarnProgress()
    {
        Assert.AreEqual(0,progress.Progress); var point=Point(0); progress.Begin(point);
        for(int i=0;i<200;i++) { progress.Sample(point,center,radii,config); progress.Sample(point+Vector2.up*.001f,center,radii,config); }
        for(int i=0;i<50;i++) { progress.Release(); progress.Begin(Point(i*.1f)); }
        Assert.AreEqual(0,progress.Progress); Assert.AreEqual(0,progress.CompletionCount);
    }
    [TestCase(1)] [TestCase(-1)] public void ImperfectStirringInEitherDirectionCompletesExactlyOnce(int direction)
    {
        progress.Begin(Point(0));
        for(int i=1;i<=2200;i++) progress.Sample(Point(direction*i*.035f,Mathf.Sin(i*.1f)*.15f),center,radii,config);
        Assert.AreEqual(1,progress.Progress); Assert.AreEqual(1,progress.CompletionCount);
        for(int i=0;i<200;i++) Assert.IsFalse(progress.Sample(Point(i*.1f),center,radii,config));
        Assert.AreEqual(1,progress.CompletionCount); progress.Reset(); Assert.AreEqual(0,progress.Progress);
    }
    [Test] public void OutsideAndTeleportMotionIsRejected()
    {
        progress.Begin(center+new Vector2(10,10));
        for(int i=0;i<100;i++) progress.Sample(center+new Vector2(10+i*.1f,10),center,radii,config);
        progress.Begin(Point(0)); Assert.IsFalse(progress.Sample(Point(Mathf.PI),center,radii,config));
        Assert.AreEqual(0,progress.Progress);
    }
    [Test] public void EffortIsTunableAndMotionIsNotATimer()
    {
        progress.Begin(Point(0)); for(int i=1;i<=80;i++) progress.Sample(Point(i*.04f),center,radii,config);
        Assert.Greater(progress.Progress,0); Assert.Less(progress.Progress,1);
        float earned=progress.Progress; progress.Release(); for(int i=0;i<100;i++) progress.Sample(Point(i*.04f),center,radii,config);
        Assert.AreEqual(earned,progress.Progress);
        var easier=new MixingProgress(); config.requiredEffort/=2; easier.Begin(Point(0));
        for(int i=1;i<=80;i++) easier.Sample(Point(i*.04f),center,radii,config);
        Assert.AreEqual(earned*2,easier.Progress,.0001f);
    }
    [Test] public void GentleHighFrequencyStirringAccumulatesWithoutRewardingBoundedJitter()
    {
        progress.Begin(Point(0));
        for(int i=1;i<=8000;i++) progress.Sample(Point(i*.001f),center,radii,config);
        Assert.Greater(progress.Progress,.1f);
        Assert.Less(progress.Progress,1);
    }
    [Test]public void ForgivingSwipesCrossTheCentreAndCompleteOnceWithoutCircles()
    {
        config.acceptAnySwipe=true;progress.Begin(center);
        for(int i=0;i<2000;i++)progress.Sample(center+Vector2.right*(Mathf.Sin(i*.15f)*2.5f),center,radii,config);
        Assert.AreEqual(1,progress.Progress);Assert.AreEqual(1,progress.CompletionCount);
        Assert.IsFalse(progress.Sample(center,center,radii,config));Assert.AreEqual(1,progress.CompletionCount);
    }
    [Test]public void ForgivingModeStillRejectsIdleJitterOutsideAndReleasedMotion()
    {
        config.acceptAnySwipe=true;progress.Begin(center);
        for(int i=0;i<100;i++){progress.Sample(center,center,radii,config);progress.Sample(center+Vector2.up*.001f,center,radii,config);}
        Assert.AreEqual(0,progress.Progress);progress.Sample(center+Vector2.up*10,center,radii,config);Assert.AreEqual(0,progress.Progress);
        progress.Begin(center);progress.Sample(center+Vector2.right*2,center,radii,config);
        Assert.AreEqual(config.maximumDelta/config.requiredEffort,progress.Progress,.0001f);
        float earned=progress.Progress;progress.Release();progress.Sample(center,center,radii,config);Assert.AreEqual(earned,progress.Progress);
    }
    [Test] public void MixingStateTransitionsRejectDuplicateAndStaleCompletion()
    {
        var session=new CreationSession(1,true); session.Begin(); session.PresentationReady(CreationStep.PresentBowl,0,0);
        session.PresentationReady(CreationStep.PresentIngredient,0,0); session.BeginDrag(); session.Release(true);
        session.CommitIngredient(0,0); session.AdvanceAfterIngredient(0,0);
        Assert.IsTrue(session.BeginMixing(0)); Assert.IsFalse(session.BeginMixing(0)); Assert.IsTrue(session.MixingReady(0));
        Assert.IsTrue(session.CompleteMixing(0)); Assert.IsFalse(session.CompleteMixing(0)); Assert.IsTrue(session.FinishMixing(0)); Assert.IsFalse(session.FinishMixing(0));
        session.Reset(); Assert.IsFalse(session.FinishMixing(0)); Assert.AreEqual(CreationStep.Initialize,session.Step);
    }
}
