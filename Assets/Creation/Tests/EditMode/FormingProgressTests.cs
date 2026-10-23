using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using SquishyDumpling.Creation;
public class FormingProgressTests
{
    FormingDefinition config; FormingProgress progress;
    readonly Vector2 center=Vector2.zero, region=new Vector2(5.2f,3.5f);
    [SetUp] public void SetUp() { config=ScriptableObject.CreateInstance<FormingDefinition>(); progress=new FormingProgress(); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(config); }
    void Drag(Vector2 start,Vector2 end,int samples=30)
    { Assert.IsTrue(progress.Begin(start,center,region,config)); for(int i=1;i<=samples;i++) progress.Sample(Vector2.Lerp(start,end,i/(float)samples)); progress.Release(); }
    [Test] public void HoldingBoundedJitterAndTapsDoNotEarnProgress()
    {
        progress.Begin(Vector2.zero,center,region,config);
        for(int i=0;i<500;i++) progress.Sample(new Vector2(i%2==0 ? .04f : -.04f,0));
        Assert.AreEqual(0,progress.Progress); progress.Release();
        for(int i=0;i<100;i++) { progress.Begin(Vector2.zero,center,region,config); progress.Release(); }
        Assert.AreEqual(0,progress.Progress); Assert.AreEqual(0,progress.UsefulGestures);
        progress.Begin(Vector2.zero,center,region,config); progress.Sample(new Vector2(.3f,0)); float earned=progress.Progress;
        for(int i=0;i<200;i++) progress.Sample(new Vector2(.3f+(i%2==0 ? .005f : -.005f),0));
        Assert.AreEqual(earned,progress.Progress); // No stockpiled jitter reward after a real drag either.
    }
    [TestCase(-1,0)] [TestCase(1,0)] [TestCase(0,1)] [TestCase(0,-1)] [TestCase(1,1)] [TestCase(-1,1)]
    public void EveryNaturalDragDirectionEarnsDistance(int x,int y)
    { Drag(Vector2.zero,new Vector2(x,y).normalized*.8f); Assert.Greater(progress.Progress,.06f); Assert.Less(progress.Progress,1); }
    [Test] public void SameAreaRepeatedDragsCompleteExactlyOnceWithoutCoverageBudgets()
    {
        for(int i=0;i<20 && progress.Progress<1;i++) Drag(new Vector2(-1,0),new Vector2(-1.8f,0));
        Assert.AreEqual(1,progress.Progress); Assert.AreEqual(1,progress.CompletionCount);
        Assert.IsFalse(progress.Begin(Vector2.zero,center,region,config)); Assert.IsFalse(progress.Sample(Vector2.one)); Assert.AreEqual(1,progress.CompletionCount);
    }
    [Test] public void BackAndForthTravelWorksAndReleaseRetainsProgress()
    {
        progress.Begin(new Vector2(-1,0),center,region,config);
        for(int pass=0;pass<3;pass++) for(int i=1;i<=30;i++) progress.Sample(Vector2.Lerp(pass%2==0 ? Vector2.left : Vector2.right,pass%2==0 ? Vector2.right : Vector2.left,i/30f));
        Assert.Greater(progress.Progress,.5f); float earned=progress.Progress; progress.Release();
        for(int i=0;i<100;i++) progress.Sample(Vector2.zero); Assert.AreEqual(earned,progress.Progress);
        progress.Reset(); Assert.AreEqual(0,progress.Progress); Assert.AreEqual(0,progress.CompletionCount); Assert.AreEqual(0,progress.UsefulGestures);
    }
    [Test] public void OutsideStartOutsideTravelAndJumpsEarnNoProgress()
    {
        Assert.IsFalse(progress.Begin(new Vector2(10,10),center,region,config)); Assert.IsFalse(progress.Sample(Vector2.zero));
        progress.Begin(Vector2.zero,center,region,config); Assert.IsFalse(progress.Sample(new Vector2(4,0))); Assert.IsFalse(progress.Sample(new Vector2(.3f,0)));
        Assert.AreEqual(0,progress.Progress); progress.Release();
        progress.Begin(Vector2.zero,center,region,config); progress.Sample(new Vector2(0,4)); progress.Sample(new Vector2(0,.4f)); Assert.AreEqual(0,progress.Progress);
    }
    [Test] public void GentleMotionWorksAndSmallGestureCannotFinish()
    { Drag(Vector2.zero,new Vector2(.8f,.2f),4000); Assert.Greater(progress.Progress,.06f); Assert.Less(progress.Progress,.1f); }
    [Test] public void AuthoredNearEdgeContactIsForgivingAndEffortIsConfigurable()
    {
        Drag(new Vector2(4.9f,0),new Vector2(4.5f,0)); Assert.Greater(progress.Progress,0);
        progress.Reset(); config.requiredEffort=20; Drag(Vector2.zero,Vector2.right); float slow=progress.Progress;
        progress.Reset(); config.requiredEffort=10; Drag(Vector2.zero,Vector2.right); Assert.AreEqual(slow*2,progress.Progress,.001f);
    }
    [Test] public void ProductionMixingRequiresFortyPercentLessSameQualifiedMotion()
    {
        var tuned=AssetDatabase.LoadAssetAtPath<MixingConfiguration>("Assets/Creation/Definitions/ProductionMixing.asset");
        Assert.AreEqual(22.8f,tuned.requiredEffort,.0001f); var old=Object.Instantiate(tuned); old.requiredEffort=38;
        var a=new MixingProgress(); var b=new MixingProgress(); Vector2 radii=new Vector2(3.5f,1.15f);
        a.Begin(new Vector2(2.7f,0)); b.Begin(new Vector2(2.7f,0));
        for(int i=1;i<=50;i++) { var point=new Vector2(Mathf.Cos(i*.04f)*2.7f,Mathf.Sin(i*.04f)*.8f); a.Sample(point,Vector2.zero,radii,old); b.Sample(point,Vector2.zero,radii,tuned); }
        Assert.Greater(a.Progress,0); Assert.Less(b.Progress,1); Assert.AreEqual(a.Progress/.6f,b.Progress,.0001f); Object.DestroyImmediate(old);
    }
    [Test] public void FormingStateTransitionsRejectDuplicateAndStaleCallbacks()
    {
        var s=new CreationSession(1,true); s.Begin(); s.PresentationReady(CreationStep.PresentBowl,0,0); s.PresentationReady(CreationStep.PresentIngredient,0,0);
        s.BeginDrag(); s.Release(true); s.CommitIngredient(0,0); s.AdvanceAfterIngredient(0,0);
        s.BeginMixing(0); s.MixingReady(0); s.CompleteMixing(0); s.FinishMixing(0);
        Assert.IsTrue(s.BeginForming(0)); Assert.IsFalse(s.BeginForming(0)); Assert.IsTrue(s.FormingReady(0));
        Assert.IsTrue(s.CompleteForming(0)); Assert.IsFalse(s.CompleteForming(0)); Assert.IsTrue(s.FinishForming(0)); Assert.IsFalse(s.FinishForming(0));
        s.Reset(); Assert.IsFalse(s.FinishForming(0)); Assert.IsFalse(s.FormingReady(0));
    }
}
