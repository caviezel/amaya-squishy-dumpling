using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using SquishyDumpling.Creation;
public class DeformationTuningTests
{
    DumplingDeformationConfiguration config;
    Vector3[] rest;int[] triangles;
    [SetUp] public void Setup()
    { config=ScriptableObject.CreateInstance<DumplingDeformationConfiguration>();DumplingMeshUtility.Grid(config.meshBounds,17,13,out rest,out triangles); }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(config); }
    DumplingSpringSimulation Pull(float distance=3)
    { var sim=new DumplingSpringSimulation(rest,triangles,config);sim.Begin(Vector2.zero,Vector2.zero);sim.Move(Vector2.right*distance);for(int k=0;k<90;k++)sim.Advance(1f/60);return sim; }
    float At(DumplingSpringSimulation sim,Vector2 p)
    { int best=0;float min=float.MaxValue;for(int i=0;i<rest.Length;i++){float d=Vector2.Distance(p,rest[i]);if(d<min){best=i;min=d;}}return Vector3.Distance(sim.Positions[best],rest[best]); }
    [Test] public void InspectorHasEveryMeaningfulSerializedSettingAndTooltips()
    {
        var asset=AssetDatabase.LoadAssetAtPath<DumplingDeformationConfiguration>("Assets/Creation/Definitions/FirstDumplingDeformation.asset");Assert.IsNotNull(asset);
        var inspector=Editor.CreateEditor(asset);
        try { Assert.AreEqual("DumplingDeformationConfigurationEditor",inspector.GetType().Name,"The dedicated feel Inspector must be selected for this asset"); }
        finally { Object.DestroyImmediate(inspector); }
        var serialized=new SerializedObject(asset);
        foreach(string field in new[]{"meshBounds","columns","rows","contactOutline","pressRadius","pressStrength","grabRadius","grabStrength","influenceFalloff","neighborCoupling","springStiffness","damping","stretchResistance","maxStretch","maxStrain","recoveryStrength","settleThreshold","fixedStep","maxSubsteps","velocityLimit","debugGizmos"})
        {
            var property=serialized.FindProperty(field);Assert.IsNotNull(property,field);Assert.IsNotEmpty(property.tooltip,field);
        }
        Assert.AreEqual(17,asset.columns);Assert.AreEqual(13,asset.rows);
    }
    [Test] public void DefaultResistanceCalibrationRetainsTask8Curve()
    { Assert.AreEqual(2,config.ResistanceExponent);Assert.AreEqual(.72f,config.SafeMaxStrain);Assert.AreEqual(2.6f,config.SafeMaxStretch); }
    [Test] public void HigherStrainAllowsGreaterSafeDistortion()
    { config.maxStrain=.15f;float low=Pull(8).MaximumDisplacement;config.maxStrain=.95f;var high=Pull(8);Assert.Greater(high.MaximumDisplacement,low*1.5f);Safe(high); }
    [Test] public void HigherGrabStrengthProducesStrongerResponse()
    { config.pressStrength=0;config.maxStrain=.95f;config.grabStrength=10;float low=Pull(.5f).MaximumDisplacement;config.grabStrength=800;Assert.Greater(Pull(.5f).MaximumDisplacement,low*2); }
    [Test] public void WiderRadiusAffectsMoreMaterial()
    { config.pressStrength=0;config.grabRadius=.7f;float narrow=At(Pull(.8f),new Vector2(2,0));config.grabRadius=4;Assert.Greater(At(Pull(.8f),new Vector2(2,0)),narrow+.03f); }
    [Test] public void LowerResistanceAllowsStrongerPullWithoutChangingDefaultCalibration()
    { config.grabRadius=8;config.springStiffness=5;config.maxStretch=4;config.maxStrain=.95f;config.stretchResistance=6;float strongResistance=Pull(5).MaximumDisplacement;config.stretchResistance=.5f;Assert.Greater(Pull(5).MaximumDisplacement,strongResistance*1.2f); }
    [Test] public void LargerMaximumStretchActuallyIncreasesExcursion()
    { config.grabRadius=8;config.springStiffness=5;config.maxStrain=.95f;config.maxStretch=.6f;float small=Pull(8).MaximumDisplacement;config.maxStretch=5;Assert.Greater(Pull(8).MaximumDisplacement,small*2); }
    [Test] public void FeelSettingsAreConsumedLiveByAnExistingSimulation()
    { config.grabStrength=5;config.pressStrength=0;var sim=Pull(.7f);float before=sim.MaximumDisplacement;config.grabStrength=1000;for(int k=0;k<90;k++)sim.Advance(1f/60);Assert.Greater(sim.MaximumDisplacement,before*2); }
    [Test] public void ExaggeratedSettingsAndNonFiniteParametersRemainBounded()
    {
        config.maxStrain=100;config.maxStretch=1000;config.grabRadius=8;config.grabStrength=20000;config.neighborCoupling=10000;config.damping=0;config.springStiffness=.1f;config.stretchResistance=.25f;
        var sim=Pull(10000);Safe(sim);Assert.LessOrEqual(sim.MaximumDisplacement,DumplingDeformationConfiguration.AbsoluteMaxStretch+.0001f);
        config.grabStrength=float.PositiveInfinity;config.maxStrain=float.NaN;config.maxStretch=float.PositiveInfinity;config.damping=float.NaN;config.fixedStep=float.NaN;
        for(int k=0;k<30;k++)sim.Advance(1f/30);Safe(sim);
    }
    void Safe(DumplingSpringSimulation sim)
    {
        foreach(var v in sim.Positions)Assert.IsTrue(DumplingSpringSimulation.Finite(v));
        for(int t=0;t<triangles.Length;t+=3)
        { int a=triangles[t],b=triangles[t+1],c=triangles[t+2];float r=DumplingSpringSimulation.Cross(rest[b]-rest[a],rest[c]-rest[a]);float d=DumplingSpringSimulation.Cross(sim.Positions[b]-sim.Positions[a],sim.Positions[c]-sim.Positions[a]);Assert.Greater(d/r,0); }
    }
}
