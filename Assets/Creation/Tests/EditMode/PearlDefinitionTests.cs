using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using SquishyDumpling.Creation;
public class PearlDefinitionTests
{
    [Test]public void LongMusicStreamsAtModerateQualityInBackground()
    {
        foreach(string path in new[]{"Assets/Audio/BGM/menu.mp3","Assets/Audio/BGM/game.mp3"})
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            Assert.AreEqual(AudioClipLoadType.Streaming,settings.loadType);Assert.AreEqual(AudioCompressionFormat.Vorbis,settings.compressionFormat);
            Assert.AreEqual(.5f,settings.quality,.001f);Assert.IsTrue(importer.loadInBackground);Assert.IsFalse(settings.preloadAudioData);
        }
    }
    [Test]public void SelectionPanelCropsAreImporterSpritesForMobileDownscaling()
    {
        for(int i=1;i<=3;i++)
        {
            string path="Assets/GameContent/Art/Dumpling_UI-panel"+i+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(SpriteImportMode.Multiple,importer.spriteImportMode);
            Assert.AreEqual(1,importer.spritesheet.Length);
            Assert.Greater(importer.spritesheet[0].rect.height,700,"Crop coordinates stay in source pixels");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameContent/Definitions/UI-panel"+i+"-aligned.asset"),"Raw texture-backed Sprite assets must not return");
        }
    }
    [Test]public void ProductionMobileTexturesHaveExplicitCompressedBudgetsAndNoPlaceholderBackground()
    {
        Assert.IsFalse(AssetDatabase.IsValidFolder("Assets/Creation/Art"),"Placeholder artwork folder must stay removed");
        var paths=AssetDatabase.GetDependencies(new[]{"Assets/GameContent/Scenes/Menu.unity","Assets/GameContent/Scenes/Gameplay.unity"},true);
        Assert.IsFalse(System.Array.Exists(paths,p=>p=="Assets/ResponsiveLayout/Art/CanonicalBackground.png"));
        int checkedTextures=0;
        foreach(var path in paths)
        {
            if(!path.StartsWith("Assets/"))continue;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)continue;checkedTextures++;
            Assert.IsFalse(importer.isReadable,path);
            foreach(string target in new[]{"Android","iPhone"})
            {
                var settings=importer.GetPlatformTextureSettings(target);
                Assert.IsTrue(settings.overridden,path+" "+target);
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                float fraction=target=="Android"?.25f:.5f;
                int desired=Mathf.FloorToInt(Mathf.Max(width,height)*fraction);
                Assert.LessOrEqual(settings.maxTextureSize,Mathf.Max(32,desired),path+" "+target);
                Assert.IsTrue(settings.maxTextureSize==32 || settings.maxTextureSize*2>desired,"Largest supported cap below target: "+path);
                Assert.AreEqual(target=="Android"?50:100,settings.compressionQuality,path+" "+target);
                Assert.IsTrue(settings.format==TextureImporterFormat.ETC2_RGB4 || settings.format==TextureImporterFormat.ETC2_RGBA8 || settings.format==TextureImporterFormat.ASTC_4x4 || settings.format==TextureImporterFormat.ASTC_6x6,path+" "+target);
            }
        }
        Assert.Greater(checkedTextures,100,"Ensure the complete family catalog was inspected");
    }
    [Test]public void SprinklesFlowsFadeWithoutChangingOtherFamilies()
    {
        var data=AssetDatabase.LoadAssetAtPath<PearlRecipeDefinition>("Assets/GameContent/Definitions/SprinklesPresentation.asset");
        foreach(var variant in data.variants)Assert.IsTrue(variant.ingredient.fadeAuthoredFlow);
        var go=new GameObject("Flow fade");var mouth=new GameObject("Mouth");var target=new GameObject("Target");
        try
        {
            mouth.transform.position=Vector3.up*4;
            var stream=go.AddComponent<LiquidStreamPresenter>();stream.containerMouth=mouth.transform;stream.bowlTarget=target.transform;
            stream.fadeWithPourProgress=true;stream.SetEnvelope(1);stream.SetPourProgress(0);Assert.AreEqual(1,stream.Renderer.color.a,.001f);
            stream.SetPourProgress(.6f);Assert.That(stream.Renderer.color.a,Is.InRange(.1f,.8f));
            stream.SetPourProgress(1);Assert.AreEqual(0,stream.Renderer.color.a,.001f);
            stream.ResetVisuals();stream.SetEnvelope(1);Assert.AreEqual(1,stream.Renderer.color.a,.001f);
            stream.fadeWithPourProgress=false;stream.SetPourProgress(1);Assert.AreEqual(1,stream.Renderer.color.a,.001f);
        }
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(mouth);Object.DestroyImmediate(target);}
    }
    [Test]public void SharedProgressMapsCreationPhasesToSixClientMilestones()
    {
        var go=new GameObject("Progress mapping");var view=go.AddComponent<CreationProgressView>();
        try
        {
            for(int i=0;i<4;i++)Assert.AreEqual(i+1,view.Milestone(CreationStep.AwaitIngredientInteraction,i));
            Assert.AreEqual(4,view.Milestone(CreationStep.Mixing,4));Assert.AreEqual(5,view.Milestone(CreationStep.Forming,4));Assert.AreEqual(6,view.Milestone(CreationStep.Customization,4));
            view.ingredientMilestones=new[]{1,2,3,4,4};Assert.AreEqual(4,view.Milestone(CreationStep.PourIngredient,4),"An additional family ingredient can share the configurable fourth milestone");
        }
        finally{Object.DestroyImmediate(go);}
    }
    [Test]public void AutomaticTransferProgressCompletesOnceAndResetClearsIt()
    {
        var progress=new FormingProgress();Assert.IsTrue(progress.StartAutomaticTransfer());Assert.IsFalse(progress.StartAutomaticTransfer());
        progress.SetAutomaticTransferProgress(.4f);progress.SetAutomaticTransferProgress(.2f);Assert.AreEqual(.4f,progress.Progress);
        progress.SetAutomaticTransferProgress(1);progress.SetAutomaticTransferProgress(1);Assert.AreEqual(1,progress.CompletionCount);progress.Reset();Assert.AreEqual(0,progress.Progress);Assert.IsTrue(progress.StartAutomaticTransfer());
    }
    [Test]public void PearlArtDefinitionsHaveRegisteredMeshesAndFourCompleteChoices()
    {
        var data=AssetDatabase.LoadAssetAtPath<PearlRecipeDefinition>("Assets/GameContent/Definitions/PearlPresentation.asset");Assert.IsNotNull(data);Assert.AreEqual(4,data.variants.Length);
        foreach(var variant in data.variants)
        {
            Assert.IsNotNull(variant.ingredient.actorPrefab);Assert.AreEqual(3,variant.mixing.Length);Assert.AreEqual(10,variant.body.bounds.size.x,.001f);
            Assert.AreEqual(variant.body.bounds.size.x,variant.deformation.meshBounds.width,.001f);Assert.AreEqual(variant.body.bounds.size.y,variant.deformation.meshBounds.height,.001f);
            Assert.AreEqual(17,variant.deformation.columns);Assert.AreEqual(13,variant.deformation.rows);Assert.AreEqual(64,variant.deformation.contactOutline.Length);
            Assert.IsTrue(variant.ingredient.bakedContainer);Assert.IsTrue(variant.ingredient.authoredStream);
        }
        var recipe=AssetDatabase.LoadAssetAtPath<CreationRecipe>("Assets/GameContent/Definitions/PearlRecipe.asset");Assert.AreEqual(4,recipe.StepCount);
    }
    [Test]public void DirectFaceOptionsFitBodyAndNeedNoStampArtwork()
    {
        var faces=AssetDatabase.LoadAssetAtPath<FaceCustomizationDefinition>("Assets/GameContent/Definitions/PearlFaces.asset");Assert.IsTrue(faces.directSelection);Assert.IsTrue(faces.IsValid);Assert.IsNull(faces.stepPrefab.stamp);
        var data=AssetDatabase.LoadAssetAtPath<PearlRecipeDefinition>("Assets/GameContent/Definitions/PearlPresentation.asset");
        foreach(var face in faces.faces)foreach(var variant in data.variants)
        {
            var bounds=face.appliedArtwork.bounds;
            foreach(Vector2 corner in new[]{(Vector2)bounds.min,(Vector2)bounds.max,new Vector2(bounds.min.x,bounds.max.y),new Vector2(bounds.max.x,bounds.min.y)})
            {Vector2 p=Vector2.Scale(corner,face.appliedLocalScale)+(Vector2)face.appliedLocalPosition+new Vector2(0,-.3f);Assert.IsTrue(variant.deformation.meshBounds.Contains(p));}
        }
    }
    [Test]public void FourFamilyCatalogContainsSixteenCompleteIndependentVariants()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<DumplingFamilyCatalog>("Assets/GameContent/Definitions/DumplingFamilies.asset");Assert.IsNotNull(catalog);
        Assert.AreEqual(4,catalog.families.Length);var ids=new System.Collections.Generic.HashSet<string>();
        foreach(var family in catalog.families)
        {
            Assert.AreSame(family,catalog.Find(family.id));Assert.AreEqual(4,family.presentation.variants.Length);
            foreach(var variant in family.presentation.variants)
            {
                Assert.IsTrue(ids.Add(variant.ingredient.ingredientId));Assert.IsNotNull(variant.inBowl);Assert.AreEqual(3,variant.mixing.Length);
                foreach(var stage in variant.mixing)Assert.IsNotNull(stage);
                Assert.AreEqual(10,variant.body.bounds.size.x,.001f);Assert.AreEqual(variant.body.bounds.size.y,variant.deformation.meshBounds.height,.001f);
                Assert.AreEqual(64,variant.deformation.contactOutline.Length);
                var presenter=variant.ingredient.actorPrefab.GetComponent<IngredientPresenter>();Assert.IsNotNull(presenter);
                Assert.AreEqual(1,presenter.GetComponents<PearlContainerLid>().Length,"Each family actor must own exactly one lid presenter");
                Assert.IsTrue(variant.ingredient.authoredStream);
            }
        }
        Assert.AreEqual(16,ids.Count);Assert.IsNull(catalog.Find("unknown"));
    }
    [Test]public void ProductionBuildContainsOnlyMenuAndGameplayAndPreservesGameplayGuid()
    {
        var scenes=EditorBuildSettings.scenes;Assert.AreEqual(2,scenes.Length);
        Assert.AreEqual("Assets/GameContent/Scenes/Menu.unity",scenes[0].path);Assert.IsTrue(scenes[0].enabled);
        Assert.AreEqual("Assets/GameContent/Scenes/Gameplay.unity",scenes[1].path);Assert.IsTrue(scenes[1].enabled);
        Assert.AreEqual("de997903530a7412785dfa4bc979557d",AssetDatabase.AssetPathToGUID(scenes[1].path));
        Assert.AreEqual(2,AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}).Length);
    }
    CreationSession Ready(FaceCustomizationDefinition definition)
    {
        var s=new CreationSession(1,true);int v=s.Version;s.Begin();s.PresentationReady(CreationStep.PresentBowl,v,0);s.PresentationReady(CreationStep.PresentIngredient,v,0);s.BeginDrag();s.Release(true);s.CommitIngredient(v,0);s.AdvanceAfterIngredient(v,0);s.BeginMixing(v);s.MixingReady(v);s.CompleteMixing(v);s.FinishMixing(v);s.BeginForming(v);s.FormingReady(v);s.CompleteForming(v);s.FinishForming(v);s.BeginCustomization(v);s.CustomizationReady(v);return s;
    }
    [Test]public void SelectionPreviewsDoNotApplyAndConfirmationCommitsExactlyOnce()
    {
        var d=AssetDatabase.LoadAssetAtPath<FaceCustomizationDefinition>("Assets/GameContent/Definitions/PearlFaces.asset");var s=Ready(d);int v=s.Version;
        Assert.IsFalse(s.ConfirmSelectedFace(v));foreach(var f in d.faces){Assert.IsTrue(s.SelectFace(f.id,d,v));Assert.AreEqual(0,s.FaceApplicationCount);Assert.IsNull(s.AppliedFaceId);}
        Assert.IsFalse(s.ConfirmSelectedFace(v-1));Assert.IsTrue(s.ConfirmSelectedFace(v));Assert.AreEqual(CreationStep.FaceApplied,s.Step);Assert.IsFalse(s.ConfirmSelectedFace(v));Assert.AreEqual(1,s.FaceApplicationCount);Assert.AreEqual(s.SelectedFaceId,s.AppliedFaceId);
        s.Reset();Assert.IsFalse(s.ConfirmSelectedFace(v));Assert.IsNull(s.AppliedFaceId);
    }
}
