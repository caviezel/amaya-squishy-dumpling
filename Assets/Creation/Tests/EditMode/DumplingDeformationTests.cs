using NUnit.Framework;
using UnityEngine;
using SquishyDumpling.Creation;
public class DumplingDeformationTests
{
    DumplingDeformationConfiguration config; DumplingSpringSimulation sim;
    [SetUp] public void Setup()
    {
        config=ScriptableObject.CreateInstance<DumplingDeformationConfiguration>();
        DumplingMeshUtility.Grid(config.meshBounds,17,13,out var rest,out var triangles);
        sim=new DumplingSpringSimulation(rest,triangles,config);
    }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(config); }
    void Tick(float seconds,float dt=1f/60) { for(int i=0;i<Mathf.CeilToInt(seconds/dt);i++) sim.Advance(dt); }
    int Nearest(Vector2 point)
    { int best=0;float distance=float.MaxValue;for(int i=0;i<sim.Rest.Length;i++) { float d=Vector2.Distance(sim.Rest[i],point);if(d<distance){distance=d;best=i;} }return best; }
    float Displacement(int i)=>(sim.Positions[i]-sim.Rest[i]).magnitude;
    void Safe()
    {
        Assert.LessOrEqual(sim.MaximumDisplacement,config.maxStretch+.0001f);
        foreach(var p in sim.Positions) Assert.IsTrue(DumplingSpringSimulation.Finite(p));
        for(int t=0;t<sim.Triangles.Length;t+=3)
        {
            int a=sim.Triangles[t],b=sim.Triangles[t+1],c=sim.Triangles[t+2];
            float rest=DumplingSpringSimulation.Cross(sim.Rest[b]-sim.Rest[a],sim.Rest[c]-sim.Rest[a]);
            float current=DumplingSpringSimulation.Cross(sim.Positions[b]-sim.Positions[a],sim.Positions[c]-sim.Positions[a]);
            Assert.Greater(current/rest,.05f,"Triangle inversion or collapse");
        }
    }
    [Test] public void ProductionFeelAllowsMuchLongerPullsAndRecoversWithoutInversion()
    {
        var production=UnityEditor.AssetDatabase.LoadAssetAtPath<DumplingDeformationConfiguration>("Assets/GameContent/Definitions/GoldDeformation.asset");
        DumplingMeshUtility.Grid(production.meshBounds,17,13,out var rest,out var triangles);
        var baseline=new DumplingSpringSimulation(rest,triangles,config);var stretched=new DumplingSpringSimulation(rest,triangles,production);
        Vector2 contact=new Vector2(-3,0);baseline.Begin(contact,contact);stretched.Begin(contact,contact);baseline.Move(new Vector2(-12,0));stretched.Move(new Vector2(-12,0));
        for(int frame=0;frame<90;frame++){baseline.Advance(1f/60);stretched.Advance(1f/60);}
        Assert.Greater(stretched.MaximumDisplacement,baseline.MaximumDisplacement*1.6f,"Production must stretch substantially farther than the old feel");
        Assert.Greater(stretched.MaximumDisplacement,3f);Assert.LessOrEqual(stretched.MaximumDisplacement,production.SafeMaxStretch+.001f);
        foreach(var position in stretched.Positions)Assert.IsTrue(DumplingSpringSimulation.Finite(position));
        for(int t=0;t<triangles.Length;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            Assert.Greater(DumplingSpringSimulation.Cross(stretched.Positions[b]-stretched.Positions[a],stretched.Positions[c]-stretched.Positions[a])/
                DumplingSpringSimulation.Cross(rest[b]-rest[a],rest[c]-rest[a]),.02f);
        }
        stretched.Release();for(int frame=0;frame<600;frame++)stretched.Advance(1f/60);
        Assert.Less(stretched.MaximumDisplacement,.01f,"Long pulls must recover to rest");
    }
    [Test] public void RestRemainsExactlyStable() { Tick(5);Assert.AreEqual(0,sim.MaximumDisplacement);CollectionAssert.AreEqual(sim.Rest,sim.Positions); }
    [Test] public void PressIsLocalAndDoesNotMoveFarSide()
    { Vector2 p=new Vector2(-2,0);sim.Begin(p,p);Tick(.4f);Assert.Greater(Displacement(Nearest(p)),.08f);Assert.Less(Displacement(Nearest(new Vector2(3,0))),.01f);Safe(); }
    [Test] public void InfluenceIsSmoothAndMonotonic()
    { Assert.AreEqual(1,DumplingSpringSimulation.Influence(0,2,1));float previous=1;for(float x=0;x<=2.1f;x+=.01f){float w=DumplingSpringSimulation.Influence(x,2,1);Assert.LessOrEqual(w,previous+.000001f);previous=w;}Assert.AreEqual(0,previous); }
    [Test] public void SustainedPullTracksChangingTargetWithFalloff()
    {
        Vector2 p=new Vector2(-2,0);sim.Begin(p,p);sim.Move(p+Vector2.left);Tick(.6f);float first=Displacement(Nearest(p));
        sim.Move(p+Vector2.left*4);Tick(1);Assert.Greater(Displacement(Nearest(p)),first+.1f);
        Assert.Greater(Displacement(Nearest(p)),Displacement(Nearest(new Vector2(2,0)))*3);Safe();
    }
    [Test] public void CouplingPropagatesOutsideDirectContact()
    {
        config.grabRadius=.8f;config.pressRadius=.7f;Vector2 p=sim.Rest[Nearest(new Vector2(-1,0))];sim.Begin(p,p);sim.Move(p+Vector2.left);Tick(.8f);
        Assert.Greater(Displacement(Nearest(p+Vector2.up)),.00001f);Safe();
    }
    [Test] public void ReleaseRestoresWithOvershootThenSettles()
    {
        Vector2 p=new Vector2(-2,0);int i=Nearest(p);sim.Begin(p,p);sim.Move(p+Vector2.left*3);Tick(.7f);sim.Release();
        bool overshot=false;for(int k=0;k<360;k++){sim.Advance(1f/120);if(sim.Positions[i].x>sim.Rest[i].x+.001f)overshot=true;}
        Assert.IsTrue(overshot);Assert.Less(sim.MaximumDisplacement,.01f);Tick(3);Assert.AreEqual(0,sim.MaximumDisplacement);Safe();
    }
    [Test] public void DampingReducesRecoveryEnergy()
    { sim.Begin(Vector2.zero,Vector2.zero);sim.Move(Vector2.right*2);Tick(.6f);sim.Release();Tick(.1f);float early=sim.MaximumSpeed;Tick(2);Assert.Less(sim.MaximumSpeed,early*.05f); }
    [TestCase(1f/30)] [TestCase(1f/60)] [TestCase(1f/120)]
    public void ExtremeInputsRemainFiniteBoundedAndRecover(float dt)
    { sim.Begin(new Vector2(2,0),new Vector2(2,0));for(int k=0;k<120;k++){sim.Move(new Vector2(k%2==0?1e12f:-1e12f,1e12f));sim.Advance(dt);Safe();}sim.Move(new Vector2(float.NaN,0));sim.Advance(float.PositiveInfinity);sim.Release();Tick(6,dt);Assert.Less(sim.MaximumDisplacement,.005f); }
    [Test] public void RepeatedRegrabDoesNotMutateRestOrDrift()
    { var saved=(Vector3[])sim.Rest.Clone();for(int k=0;k<50;k++){Vector2 p=new Vector2(k%2==0?-2:2,0);sim.Begin(p,p);sim.Move(p+p);Tick(.05f);sim.Release();Tick(.03f);}Safe();Tick(6);CollectionAssert.AreEqual(saved,sim.Rest);Assert.AreEqual(0,sim.MaximumDisplacement); }
    [Test] public void FrameRateVariationProducesEquivalentTrajectory()
    {
        var other=new DumplingSpringSimulation(sim.Rest,sim.Triangles,config);sim.Begin(Vector2.zero,Vector2.zero);other.Begin(Vector2.zero,Vector2.zero);sim.Move(Vector2.right*2);other.Move(Vector2.right*2);
        for(int k=0;k<60;k++)sim.Advance(1f/30);for(int k=0;k<240;k++)other.Advance(1f/120);
        for(int i=0;i<sim.Positions.Length;i++)Assert.Less(Vector3.Distance(sim.Positions[i],other.Positions[i]),.005f);
    }
    [Test] public void BarycentricCoordinatesResolveAndFollowVertices()
    {
        Vector2 p=new Vector2(.2f,-.3f);Assert.IsTrue(DumplingSurfaceCoordinate.Find(p,sim.Rest,sim.Triangles,out var attachment));
        Assert.Less(Vector2.Distance(p,attachment.Resolve(sim.Rest)),.0001f);
        var displaced=(Vector3[])sim.Rest.Clone();displaced[attachment.a]+=Vector3.right;
        Assert.Less(Vector3.Distance(attachment.Resolve(displaced),attachment.Resolve(sim.Rest)+Vector3.right*attachment.weights.x),.0001f);
    }
    [Test] public void SurfaceMappingTracksRecoveryAndStaysFiniteAtMaximum()
    {
        Vector2 p=new Vector2(0,-.3f);Assert.IsTrue(DumplingSurfaceCoordinate.Find(p,sim.Rest,sim.Triangles,out var attachment));
        sim.Begin(p,p);sim.Move(p+Vector2.one*100);Tick(1);Assert.Greater(Vector2.Distance(p,attachment.Resolve(sim.Positions)),.1f);Assert.IsTrue(DumplingSpringSimulation.Finite(attachment.Resolve(sim.Positions)));sim.Release();Tick(6);Assert.Less(Vector2.Distance(p,attachment.Resolve(sim.Positions)),.001f);
    }
    [Test] public void ResetRemovesOwnershipVelocityAndForces()
    { sim.Begin(Vector2.zero,Vector2.zero);sim.Move(Vector2.right*20);Tick(.3f);sim.Reset();Assert.IsFalse(sim.Held);Assert.AreEqual(0,sim.MaximumSpeed);CollectionAssert.AreEqual(sim.Rest,sim.Positions);Tick(1);Assert.AreEqual(0,sim.MaximumDisplacement); }
    [Test] public void FinalPlayRequiresCustomizationAndStartsExactlyOncePerSession()
    {
        var session=new CreationSession(1,true);int v=session.Version;
        Assert.IsFalse(session.BeginFinalPlay(v));session.Begin();session.PresentationReady(CreationStep.PresentBowl,v,0);session.PresentationReady(CreationStep.PresentIngredient,v,0);
        session.BeginDrag();session.Release(true);session.CommitIngredient(v,0);session.AdvanceAfterIngredient(v,0);session.BeginMixing(v);session.MixingReady(v);session.CompleteMixing(v);session.FinishMixing(v);
        session.BeginForming(v);session.FormingReady(v);session.CompleteForming(v);session.FinishForming(v);session.BeginCustomization(v);session.CustomizationReady(v);
        var definition=ScriptableObject.CreateInstance<FaceCustomizationDefinition>();definition.faces=new[]{new FaceOption{id="happy"}};
        Assert.IsTrue(session.SelectFace("happy",definition,v));session.BeginStamping(v);session.ApplyFace(v);Assert.IsFalse(session.BeginFinalPlay(v));session.CompleteCustomization(v);
        Assert.IsFalse(session.BeginFinalPlay(v-1));Assert.IsTrue(session.BeginFinalPlay(v));Assert.IsFalse(session.BeginFinalPlay(v));Assert.AreEqual(1,session.FinalPlayStartCount);Assert.AreEqual("happy",session.AppliedFaceId);
        session.Reset();Assert.AreEqual(0,session.FinalPlayStartCount);Assert.IsFalse(session.BeginFinalPlay(v));Object.DestroyImmediate(definition);
    }
    [Test] public void WarmSimulationHasNoManagedAllocationsAndReportsCpuCost()
    {
        sim.Begin(Vector2.zero,Vector2.zero);sim.Move(Vector2.right*3);Tick(.2f);
        var timer=new System.Diagnostics.Stopwatch();long before=System.GC.GetAllocatedBytesForCurrentThread();timer.Start();
        for(int i=0;i<1200;i++)sim.Advance(1f/60);
        timer.Stop();long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.AreEqual(0,allocated,"Steady simulation loop must not allocate");
        Debug.Log($"Task 8 CPU simulation: {sim.Rest.Length} vertices; {sim.Triangles.Length/3} triangles; 1200 advances at 1/60s: {timer.Elapsed.TotalMilliseconds:F3} ms; managed bytes: {allocated}. Editor CPU only, excludes renderer upload.");
    }
}
