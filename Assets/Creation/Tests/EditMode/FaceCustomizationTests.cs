using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SquishyDumpling.Creation;
public class FaceCustomizationTests
{
    FaceCustomizationDefinition data;
    CreationSession session;
    [SetUp] public void Setup() { data=AssetDatabase.LoadAssetAtPath<FaceCustomizationDefinition>("Assets/Creation/Definitions/FirstDumplingCustomization.asset"); session=new CreationSession(1,true); Assert.IsNotNull(data); }
    void Formed()
    {
        int v=session.Version; Assert.IsTrue(session.Begin()); session.PresentationReady(CreationStep.PresentBowl,v,0); session.PresentationReady(CreationStep.PresentIngredient,v,0);
        session.BeginDrag(); session.Release(true); session.CommitIngredient(v,0); session.AdvanceAfterIngredient(v,0);
        session.BeginMixing(v); session.MixingReady(v); session.CompleteMixing(v); session.FinishMixing(v);
        session.BeginForming(v); session.FormingReady(v); session.CompleteForming(v); session.FinishForming(v);
        Assert.AreEqual(CreationStep.FormingComplete,session.Step);
    }
    void Ready() { Formed(); Assert.IsTrue(session.BeginCustomization(session.Version)); Assert.IsTrue(session.CustomizationReady(session.Version)); }
    [Test] public void CustomizationRequiresFormingCompleteAndFreshToken()
    {
        Assert.IsFalse(session.BeginCustomization(session.Version)); Formed(); Assert.IsFalse(session.BeginCustomization(session.Version-1));
        Assert.IsTrue(session.BeginCustomization(session.Version)); Assert.IsFalse(session.BeginCustomization(session.Version));
        Assert.IsFalse(session.CustomizationReady(session.Version-1)); Assert.IsTrue(session.CustomizationReady(session.Version));
    }
    [Test] public void ThreeCompleteDistinctOptionsComeFromAuthoredData()
    {
        Assert.IsTrue(data.IsValid); Assert.AreEqual(3,data.faces.Length);
        foreach(var f in data.faces) { Assert.AreSame(f,data.Find(f.id)); Assert.IsNotNull(f.preview); Assert.IsNotNull(f.appliedArtwork); Assert.IsNotNull(f.stampArtwork); }
        Assert.AreNotSame(data.faces[0].appliedArtwork,data.faces[1].appliedArtwork); Assert.AreNotSame(data.faces[1].appliedArtwork,data.faces[2].appliedArtwork);
    }
    [Test] public void SelectionCanChangeWithoutApplyingAnything()
    {
        Ready(); foreach(var face in data.faces) { Assert.IsTrue(session.SelectFace(face.id,data,session.Version)); Assert.AreEqual(face.id,session.SelectedFaceId); Assert.IsNull(session.AppliedFaceId); }
        Assert.AreEqual(0,session.FaceApplicationCount); Assert.AreEqual(CreationStep.Customization,session.Step);
    }
    [Test] public void SelectionRejectsUnknownWrongPhaseAndStaleToken()
    {
        Assert.IsFalse(session.SelectFace(data.faces[0].id,data,session.Version)); Ready();
        Assert.IsFalse(session.SelectFace("missing",data,session.Version)); Assert.IsFalse(session.SelectFace(null,data,session.Version));
        Assert.IsFalse(session.SelectFace(data.faces[0].id,data,session.Version-1)); Assert.IsNull(session.SelectedFaceId);
    }
    [Test] public void ArbitraryContentIdentityUsesSameSemanticPath()
    {
        var clone=Object.Instantiate(data); clone.faces[0].id="client-art-replacement-17";
        try { Ready(); Assert.IsTrue(session.SelectFace(clone.faces[0].id,clone,session.Version)); Assert.IsTrue(session.BeginStamping(session.Version)); Assert.IsTrue(session.ApplyFace(session.Version)); Assert.AreEqual("client-art-replacement-17",session.AppliedFaceId); }
        finally { Object.DestroyImmediate(clone); }
    }
    [Test] public void StampingNeedsSelectionAndLocksFurtherChanges()
    {
        Ready(); Assert.IsFalse(session.BeginStamping(session.Version)); session.SelectFace(data.faces[0].id,data,session.Version);
        Assert.IsTrue(session.BeginStamping(session.Version)); Assert.IsFalse(session.BeginStamping(session.Version));
        Assert.IsFalse(session.SelectFace(data.faces[1].id,data,session.Version)); Assert.AreEqual(data.faces[0].id,session.SelectedFaceId);
    }
    [Test] public void ApplicationAndCompletionCommitExactlyOnceRetainingIdentity()
    {
        Ready(); int v=session.Version; session.SelectFace(data.faces[2].id,data,v); session.BeginStamping(v);
        Assert.IsFalse(session.CompleteCustomization(v)); Assert.IsFalse(session.ApplyFace(v-1)); Assert.IsTrue(session.ApplyFace(v)); Assert.IsFalse(session.ApplyFace(v));
        Assert.IsTrue(session.CompleteCustomization(v)); Assert.IsFalse(session.CompleteCustomization(v)); Assert.IsFalse(session.SelectFace(data.faces[0].id,data,v));
        Assert.AreEqual(1,session.FaceApplicationCount); Assert.AreEqual(1,session.CustomizationCompletionCount);
        Assert.AreEqual(data.faces[2].id,session.AppliedFaceId); Assert.AreEqual(session.SelectedFaceId,session.AppliedFaceId); Assert.AreEqual(CreationStep.CustomizationComplete,session.Step);
    }
    [Test] public void ResetClearsCustomizationAndRejectsStaleCommits()
    {
        Ready(); int v=session.Version; session.SelectFace(data.faces[0].id,data,v); session.BeginStamping(v); session.ApplyFace(v); session.CompleteCustomization(v); session.Reset();
        Assert.IsNull(session.SelectedFaceId); Assert.IsNull(session.AppliedFaceId); Assert.AreEqual(0,session.FaceApplicationCount); Assert.AreEqual(0,session.CustomizationCompletionCount);
        Assert.IsFalse(session.ApplyFace(v)); Assert.IsFalse(session.CompleteCustomization(v)); Assert.AreEqual(CreationStep.Initialize,session.Step);
    }
    [Test] public void AuthoredIntentEllipseFollowsBodyTransform()
    {
        var go=new GameObject(); var target=go.AddComponent<FaceApplicationTarget>();
        try {
            Assert.IsTrue(target.Contains(new Vector2(2.5f,1))); Assert.IsFalse(target.Contains(new Vector2(8,0)));
            go.transform.position=new Vector3(6,9,0); go.transform.rotation=Quaternion.Euler(0,0,30);
            Assert.IsTrue(target.Contains(go.transform.TransformPoint(new Vector3(2.5f,1,0)))); Assert.IsFalse(target.Contains(Vector2.zero));
        } finally { Object.DestroyImmediate(go); }
    }
    [Test] public void InvalidOrDuplicateContentIsNotReady()
    {
        var clone=Object.Instantiate(data);
        try { clone.faces[1].id=clone.faces[0].id; Assert.IsFalse(clone.IsValid); clone.faces[1].id="unique"; clone.faces[1].preview=null; Assert.IsFalse(clone.IsValid); }
        finally { Object.DestroyImmediate(clone); }
    }
}
