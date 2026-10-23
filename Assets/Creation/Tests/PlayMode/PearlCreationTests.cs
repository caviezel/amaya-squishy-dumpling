using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using SquishyDumpling.Creation;
using SquishyDumpling.Layout;
using Object=UnityEngine.Object;
public class PearlCreationTests
{
    CreationFlowController flow;ResponsiveWorldLayout layout;readonly List<string> diagnostics=new List<string>();
    void Log(string text,string stack,LogType type){if(type==LogType.Warning||type==LogType.Error||type==LogType.Exception)diagnostics.Add(text);}
    [UnitySetUp]public IEnumerator Setup()
    {
        diagnostics.Clear();Application.logMessageReceived+=Log;Time.timeScale=6;DumplingFamilySelection.Select("pearl");
        SceneManager.LoadScene("Gameplay");yield return null;
        flow=Object.FindObjectOfType<CreationFlowController>();layout=flow.layout;yield return Wait(()=>flow.CanInteract);
    }
    [UnityTearDown]public IEnumerator Teardown()
    {
        if(flow)flow.Interrupt();Time.timeScale=1;
        if(layout && layout.WorldCamera.targetTexture){var rt=layout.WorldCamera.targetTexture;layout.WorldCamera.targetTexture=null;rt.Release();Object.Destroy(rt);}
        SceneManager.LoadScene(DumplingFamilyMenu.MenuScene);yield return null;Application.logMessageReceived-=Log;Assert.IsEmpty(diagnostics,string.Join("\n",diagnostics));
    }
    IEnumerator Wait(Func<bool> condition,float timeout=20,Action capture=null)
    {float end=Time.realtimeSinceStartup+timeout,next=0;while(!condition() && Time.realtimeSinceStartup<end){if(Time.realtimeSinceStartup>=next){capture?.Invoke();next=Time.realtimeSinceStartup+.08f;}yield return null;}Assert.IsTrue(condition(),"Timeout: "+flow.Session.Step);}
    PointerEventData Pointer(int id,Vector2 world)=>new PointerEventData(EventSystem.current){pointerId=id,position=layout.WorldCamera.WorldToScreenPoint(world)};
    [UnityTest] public IEnumerator ClearGlueUsesContinuousClientLiquidArtwork()
    {
        CameraSetup(); Physics2D.SyncTransforms(); var actor=flow.ingredient;
        actor.OnPointerDown(Pointer(7,actor.transform.position));
        actor.OnDrag(Pointer(7,new Vector2(0,3))); actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
        var presenter=actor.GetComponentInParent<IngredientPresenter>();
        yield return Wait(()=>presenter.stream.Envelope>.99f && presenter.DepletedFraction>.4f);
        var stream=presenter.stream; stream.UpdateGeometry();
        CollectionAssert.Contains(new[]{"Dumpling_flow-1","Dumpling_flow-2"},stream.Renderer.sprite.name);
        Assert.AreEqual(SpriteDrawMode.Simple,stream.Renderer.drawMode);
        Assert.AreEqual(stream.transform.localScale.x,stream.transform.localScale.y,.0001f);
        Assert.AreEqual(Vector3.Distance(stream.containerMouth.position,stream.LandingPosition),
            stream.Renderer.sprite.bounds.size.y*stream.transform.lossyScale.y,.001f);
        Capture("clear-glue-client-stream");
        yield return Wait(()=>flow.CanInteract);
        Assert.AreEqual(1,flow.Session.CommittedIngredients);
        Assert.IsTrue(!stream || !stream.Renderer.enabled);
    }
    [UnityTest] public IEnumerator ClearGlueGrowsFromBowlFloorAndRestoresOnRestart()
    {
        CameraSetup();var liquid=flow.bowl.contributionLayers[0];
        flow.bowl.BeginContribution(flow.recipe.ingredients[0]);
        flow.bowl.Receive(.15f);float floor=liquid.transform.TransformPoint(Vector3.zero).y;
        flow.bowl.Receive(1);float filledFloor=liquid.transform.TransformPoint(Vector3.zero).y;
        Assert.AreEqual(floor,filledFloor,.02f,"Liquid must grow upward from its authored visible base");
        Assert.AreEqual(flow.bowl.transform.position.y-5.15f,filledFloor,.02f);
        Capture("clear-glue-grounded-in-bowl");
        flow.RestartProof();yield return Wait(()=>flow.CanInteract);
        Assert.IsFalse(liquid.enabled);Assert.AreEqual(-4.45f,liquid.transform.localPosition.y,.001f);
    }
    [UnityTest] public IEnumerator BaseIngredientsUseReferenceArtworkAndStayOnBowlFloor()
    {
        CameraSetup();
        for(int index=0;index<3;index++)
        {
            Physics2D.SyncTransforms();var actor=flow.ingredient;var presenter=actor.GetComponentInParent<IngredientPresenter>();
            var layer=flow.bowl.contributionLayers[index];
            Assert.AreEqual(-4.45f,layer.transform.localPosition.y,.001f);
            actor.OnPointerDown(Pointer(7,actor.transform.position));
            actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
            yield return Wait(()=>presenter.DepletedFraction>.4f);
            var stream=presenter.stream;stream.UpdateGeometry();
            Assert.AreEqual(SpriteDrawMode.Simple,stream.Renderer.drawMode);
            Assert.AreEqual(stream.transform.localScale.x,stream.transform.localScale.y,.0001f);
            if(index!=1)CollectionAssert.Contains(new[]{"Dumpling_flow-1","Dumpling_flow-2"},stream.Renderer.sprite.name);
            else Assert.AreEqual(index==1?"Dumpling_SQUISHY-POWDER-flow":"Dumpling_flow-2",stream.Renderer.sprite.name);
            Assert.AreEqual(135,presenter.definition.pourAngle);
            float floor=layer.transform.position.y;Assert.AreEqual(flow.bowl.transform.position.y-5.15f,floor,.02f);
            Assert.IsNull(flow.bowl.powderPuff,"Client artwork must not also emit the legacy fading powder sprite");
            Vector3 bottom=stream.transform.TransformPoint(new Vector3(0,stream.Renderer.sprite.bounds.min.y,0));
            Assert.Less(Vector3.Distance(bottom,stream.LandingPosition),.015f,"Stream bottom meets the authored landing point");
            if(index<3)
            {
                Time.timeScale=1;var landing=stream.LandingPosition;var seen=new HashSet<string>();
                for(int sample=0;sample<16;sample++)
                {
                    stream.UpdateGeometry();seen.Add(stream.Renderer.sprite.name);
                    Assert.Less(Vector3.Distance(landing,stream.LandingPosition),.0001f,"Landing must not rise with the bowl contents");
                    Assert.AreEqual(flow.bowl.transform.position.x,stream.LandingPosition.x,.001f);
                    Assert.AreEqual(stream.LandingPosition.x,stream.containerMouth.position.x,.001f,"Tilted mouth must be above the centred landing");
                    if(sample==0 || sample==15)Capture("fixed-step-"+(index+1)+"-"+sample);
                    yield return new WaitForSeconds(.02f);
                }
                if(index!=1)CollectionAssert.AreEquivalent(new[]{"Dumpling_flow-1","Dumpling_flow-2"},seen);
                Time.timeScale=6;
            }
            Capture("base-step-"+(index+1)+"-pour");
            yield return Wait(()=>flow.bowl.CompletionFxPlayCount==index+1);
            Assert.IsTrue(flow.bowl.pourCompletionFx.IsAlive(true));
            Time.timeScale=1;yield return new WaitForSeconds(.18f);
            var fxRenderer=flow.bowl.pourCompletionFx.GetComponent<ParticleSystemRenderer>();
            var withFx=FramePixels();fxRenderer.enabled=false;var withoutFx=FramePixels();fxRenderer.enabled=true;
            int sparklePixels=0;for(int px=0;px<withFx.Length;px++)if(withFx[px].r!=withoutFx[px].r || withFx[px].g!=withoutFx[px].g || withFx[px].b!=withoutFx[px].b)sparklePixels++;
            Assert.Greater(sparklePixels,120,"Completion sparkles must visibly render");
            Capture("base-step-"+(index+1)+"-sparkles");Time.timeScale=6;

            yield return Wait(()=>flow.CanInteract || flow.pearl.AwaitingChoice);
            Assert.AreEqual(index+1,flow.Session.CommittedIngredients);
            Assert.AreEqual(floor,layer.transform.position.y,.02f);
            Capture("base-step-"+(index+1)+"-settled");
        }
        flow.RestartProof();yield return Wait(()=>flow.CanInteract);
        for(int i=0;i<3;i++){Assert.IsFalse(flow.bowl.contributionLayers[i].enabled);Assert.AreEqual(-4.45f,flow.bowl.contributionLayers[i].transform.localPosition.y,.001f);}
    }
    [UnityTest] public IEnumerator RestartDuringIngredientSparklesClearsParticlesAndOwnership()
    {
        Physics2D.SyncTransforms();var actor=flow.ingredient;
        actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
        yield return Wait(()=>flow.bowl.CompletionFxPlayCount==1);
        Assert.IsTrue(flow.bowl.pourCompletionFx.IsAlive(true));flow.RestartProof();
        Assert.IsFalse(flow.bowl.pourCompletionFx.IsAlive(true));Assert.AreEqual(0,flow.bowl.CompletionFxPlayCount);
        yield return Wait(()=>flow.CanInteract);Assert.AreEqual(0,flow.Session.CommittedIngredients);Assert.IsNull(flow.ingredient.PointerOwner);
    }
    [UnityTest] public IEnumerator MenuFamiliesPopulateOneSelectorAndAllSixteenChoicesReachFinalPlay()
    {
        for(int familyIndex=0;familyIndex<4;familyIndex++)
        {
            if(familyIndex==0)SceneManager.LoadScene(DumplingFamilyMenu.MenuScene);
            else Object.FindObjectOfType<CreationProgressView>().GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;yield return null;Assert.AreEqual(DumplingFamilyMenu.MenuScene,SceneManager.GetActiveScene().name);
            var menu=Object.FindObjectOfType<DumplingFamilyMenu>();Assert.AreEqual(4,menu.Buttons.Length);
            layout=menu.layout;CameraSetup();yield return null;Capture("families/menu");
            var menuHits=new List<RaycastResult>();EventSystem.current.RaycastAll(Pointer(1,menu.Buttons[familyIndex].transform.position),menuHits);
            Assert.IsTrue(menuHits.Exists(h=>h.gameObject.GetComponentInParent<UnityEngine.UI.Button>()==menu.Buttons[familyIndex]),"Menu option must receive actual UI raycasts");
            var menuTexture=layout.WorldCamera.targetTexture;layout.WorldCamera.targetTexture=null;menuTexture.Release();Object.Destroy(menuTexture);
            string id=menu.catalog.families[familyIndex].id;menu.Buttons[familyIndex].onClick.Invoke();
            yield return null;flow=Object.FindObjectOfType<CreationFlowController>();layout=flow.layout;yield return Wait(()=>flow.CanInteract);
            Assert.AreEqual(id,flow.pearl.FamilyId);Assert.AreEqual(4,flow.pearl.ChoiceButtons.Count);
            CameraSetup();
            var backing=flow.pearl.selectionCanvas.transform.Find("Purple selector backing");Assert.IsNotNull(backing);
            Assert.AreEqual(3,backing.GetComponentsInChildren<UnityEngine.UI.Image>().Length);
            for(int variant=0;variant<4;variant++)
            {
                if(variant>0){flow.RestartProof();yield return Wait(()=>flow.CanInteract);}
                Assert.AreEqual(0,flow.pearl.ChoicePresentationCount);
                // Show the fourth-step UI before selecting via its actual button.
                while(flow.Session.CurrentIngredientIndex<3)
                {
                    Physics2D.SyncTransforms();var ingredient=flow.ingredient;
                    ingredient.OnPointerDown(Pointer(7,ingredient.transform.position));ingredient.OnDrag(Pointer(7,new Vector2(0,3)));ingredient.OnPointerUp(Pointer(7,new Vector2(0,3)));
                    yield return Wait(()=>flow.CanInteract || flow.pearl.AwaitingChoice);
                }
                yield return Wait(()=>flow.pearl.AwaitingChoice);Assert.AreEqual(1,flow.pearl.ChoicePresentationCount);
                Assert.AreEqual(4,Object.FindObjectOfType<CreationProgressView>().CurrentNumber);
                for(int slot=0;slot<4;slot++)Assert.AreEqual(flow.pearl.definition.variants[slot].ingredient.containerSprite,
                    flow.pearl.ChoiceButtons[slot].transform.Find("Preview").GetComponent<UnityEngine.UI.Image>().sprite);
                yield return Wait(()=>flow.pearl.selectionGroup.alpha>.99f);Capture("families/"+id+"-selector");
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(Pointer(1,flow.pearl.ChoiceButtons[variant].transform.position),hits);
                Assert.IsTrue(hits.Exists(h=>h.gameObject.GetComponentInParent<UnityEngine.UI.Button>()==flow.pearl.ChoiceButtons[variant]),"Selector slot must receive UI raycasts");
                flow.pearl.ChoiceButtons[variant].onClick.Invoke();Assert.IsFalse(flow.pearl.SelectVariant(variant));
                yield return Wait(()=>flow.CanInteract);
                var selected=flow.pearl.definition.variants[variant];Assert.AreEqual(selected.ingredient,flow.CurrentDefinition);
                yield return Mix(variant);Assert.AreEqual(selected.mixing[2],flow.mixingSurface.mixedLayer.sprite);
                yield return Form();Assert.AreEqual(selected.body,flow.FormedMaterial.states[flow.FormedMaterial.states.Length-1].sprite); 
                yield return FinishFace();Assert.AreEqual(selected.deformation,flow.deformationConfiguration);Assert.IsNotNull(flow.FinalDumpling);
                Assert.AreEqual(1,flow.pearl.ChoicePresentationCount);Assert.AreEqual(4,flow.pearl.ChoiceButtons.Count);
                Capture("families/"+id+"-final-"+variant);
            }
        }
        flow.RestartProof();yield return Wait(()=>flow.CanInteract);Assert.AreEqual(0,flow.pearl.ChoicePresentationCount);Assert.AreEqual("sprinkles",flow.pearl.FamilyId);
    }
    [UnityTest] public IEnumerator RestartWhileFamilySelectorIsOpenDoesNotLeaveStaleChoices()
    {
        while(flow.Session.CurrentIngredientIndex<3)
        {
            Physics2D.SyncTransforms();var ingredient=flow.ingredient;
            ingredient.OnPointerDown(Pointer(7,ingredient.transform.position));ingredient.OnDrag(Pointer(7,new Vector2(0,3)));ingredient.OnPointerUp(Pointer(7,new Vector2(0,3)));
            yield return Wait(()=>flow.CanInteract || flow.pearl.AwaitingChoice);
        }
        yield return Wait(()=>flow.pearl.AwaitingChoice);flow.RestartProof();
        Assert.IsFalse(flow.pearl.AwaitingChoice);Assert.AreEqual(0,flow.pearl.selectionGroup.alpha);Assert.AreEqual(4,flow.pearl.ChoiceButtons.Count);
        Assert.IsFalse(flow.pearl.SelectVariant(2));yield return Wait(()=>flow.CanInteract);Assert.AreEqual("pearl",flow.pearl.FamilyId);
    }
    IEnumerator ReachDye(int variant)
    {
        while(flow.Session.CurrentIngredientIndex<3)
        {
            Physics2D.SyncTransforms();var actor=flow.ingredient;
            actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
            yield return Wait(()=>flow.CanInteract || flow.pearl.AwaitingChoice);
        }
        yield return Wait(()=>flow.pearl.AwaitingChoice && flow.pearl.selectionGroup.alpha>.99f);
        Capture("drops/"+flow.pearl.FamilyId+"-panel");Assert.IsTrue(flow.pearl.SelectVariant(variant));yield return Wait(()=>flow.CanInteract);
    }
    [UnityTest] public IEnumerator AllEightDyesFallThreeTimesAtSmallScaleAndFillOnImpact()
    {
        foreach(string family in new[]{"pearl","pastel"})
        {
            DumplingFamilySelection.Select(family);SceneManager.LoadScene(DumplingFamilyMenu.CreationScene);yield return null;
            flow=Object.FindObjectOfType<CreationFlowController>();layout=flow.layout;yield return Wait(()=>flow.CanInteract);CameraSetup();
            for(int variant=0;variant<4;variant++)
            {
                if(variant>0){flow.RestartProof();yield return Wait(()=>flow.CanInteract);}
                yield return ReachDye(variant);Physics2D.SyncTransforms();var actor=flow.ingredient;var presenter=actor.GetComponentInParent<IngredientPresenter>();
                Assert.IsTrue(presenter.definition.discreteDrops);actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
                Time.timeScale=1;int last=0;bool[] seen=new bool[3];float end=Time.realtimeSinceStartup+10;
                while(presenter.DropImpactCount<3 && Time.realtimeSinceStartup<end)
                {
                    if(presenter.stream.Renderer.enabled)
                    {
                        Assert.LessOrEqual(presenter.stream.Renderer.bounds.size.x,1.101f);
                        Assert.AreEqual(presenter.stream.transform.localScale.x,presenter.stream.transform.localScale.y,.001f);
                        int drop=presenter.DropImpactCount;
                        if(drop<3 && !seen[drop] && presenter.stream.transform.position.y<presenter.containerMouth.position.y-1.4f)
                        {seen[drop]=true;Capture("drops/"+family+"-"+variant+"-drop-"+(drop+1));}
                    }
                    if(presenter.DropImpactCount!=last)
                    {last=presenter.DropImpactCount;Assert.AreEqual(last/3f,flow.bowl.ReceivedFraction,.001f);}
                    yield return null;
                }
                Assert.AreEqual(3,presenter.DropImpactCount);Assert.IsTrue(Array.TrueForAll(seen,v=>v));
                Assert.IsFalse(presenter.stream.Renderer.enabled);Time.timeScale=6;yield return Wait(()=>flow.Session.Step==CreationStep.Mixing);
                Assert.AreEqual(4,flow.Session.CommittedIngredients);
            }
        }
    }
    [UnityTest] public IEnumerator RestartDuringDyeFallClearsTheDroplet()
    {
        CameraSetup();yield return ReachDye(0);Physics2D.SyncTransforms();var actor=flow.ingredient;var presenter=actor.GetComponentInParent<IngredientPresenter>();
        actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
        yield return Wait(()=>presenter.stream.Renderer.enabled);var stream=presenter.stream;flow.RestartProof();yield return null;
        Assert.IsTrue(!stream || !stream.Renderer.enabled);yield return Wait(()=>flow.CanInteract);Assert.AreEqual(0,flow.Session.CommittedIngredients);
    }
    [UnityTest]public IEnumerator FunnelRibbonConnectsArtworkAndCleansUpWithoutVisualCapture()
    {
        yield return Mix();var step=flow.ActiveForming;var p=step.funnelPresentation;
        Physics2D.SyncTransforms();Vector2 drop=p.DropAssist.center;
        step.OnPointerDown(Pointer(17,flow.bowl.transform.position));step.OnDrag(Pointer(17,drop));step.OnPointerUp(Pointer(17,drop));
        yield return Wait(()=>p.DisplayedFill>.15f && p.DisplayedFill<.8f);
        var ribbons=Object.FindObjectsOfType<FunnelPourRibbon>();Assert.AreEqual(2,ribbons.Length);
        Assert.IsFalse(p.upperStream.enabled);Assert.IsFalse(p.lowerStream.enabled);
        foreach(var ribbon in ribbons)
        {
            Assert.IsTrue(ribbon.Visible);
            var mesh=ribbon.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
            Vector3 start=(vertices[0]+vertices[1])*.5f;
            Vector3 end=(vertices[48]+vertices[49])*.5f;
            Vector3 expectedStart=ribbon.name.StartsWith("Bowl")?flow.bowl.transform.TransformPoint((Vector3)p.composition.bowlPourMouthLocal):p.funnel.transform.position+Vector3.down*2;
            Vector3 expectedEnd=ribbon.name.StartsWith("Bowl")?p.funnel.transform.position+Vector3.up*1.1f:p.pouch.transform.position+Vector3.up*2.6f;
            Assert.Less(Vector3.Distance(start,expectedStart),.001f);Assert.Less(Vector3.Distance(end,expectedEnd),.001f);
            if(ribbon.name.StartsWith("Bowl"))
            {
                Assert.Less(Mathf.Abs(start.x-end.x),.02f,"Visible pouring lip must sit directly above the funnel stream");
                Assert.Greater(p.composition.bowlPourMouthLocal.y,1,"Contact must be on the rim, not the lower bowl wall");
                Assert.Greater(start.y-end.y,1,"Stream must visibly bridge the lip and funnel");
            }
            Assert.AreSame(p.fill.sprite.texture,ribbon.GetComponent<MeshRenderer>().sharedMaterial.mainTexture);
            Assert.Greater(Vector3.Distance(vertices[0],vertices[1]),.1f);
            var particles=ribbon.GetComponentInChildren<ParticleSystem>();Assert.IsNotNull(particles);Assert.IsTrue(particles.isPlaying);
        }
        yield return Wait(()=>flow.Session.Step==CreationStep.Customization);
        foreach(var ribbon in Object.FindObjectsOfType<FunnelPourRibbon>())Assert.IsFalse(ribbon.Visible);
        flow.RestartProof();yield return Wait(()=>flow.CanInteract);yield return null;
        Assert.AreEqual(0,Object.FindObjectsOfType<FunnelPourRibbon>().Length,"Restart must remove owned ribbon meshes and particles");
    }
    [UnityTest] public IEnumerator FormingSeatingAndSixteenFaceScrollBehaveWithoutVisualCapture()
    {
        yield return Mix();var p=flow.ActiveForming.funnelPresentation;var c=p.composition;
        var group=flow.bowl.visualRoot.GetComponent<UnityEngine.Rendering.SortingGroup>();Assert.IsNotNull(group);Assert.IsTrue(group.enabled);
        Assert.AreEqual("GameplayFront",group.sortingLayerName);Assert.Greater(group.sortingOrder,p.funnel.sortingOrder);
        Assert.Less(Mathf.Abs(p.funnel.bounds.min.y-p.pouch.bounds.max.y),.35f,"Funnel outlet must sit in the pouch neck");
        Vector3 mouth=(Vector3)c.bowlPourPosition+Quaternion.Euler(0,0,c.bowlPourAngle)*(Vector3)c.bowlPourMouthLocal;
        Assert.Less(Mathf.Abs(mouth.x-c.funnelPosition.x),.02f,"Authored pouring edge must align with the funnel centre");
        yield return Form();Assert.IsFalse(group.enabled,"Temporary bowl foreground sorting must restore after forming");var step=flow.ActiveCustomization;var choices=step.choices;
        var check=(RectTransform)step.confirm.transform;Assert.Greater(check.anchoredPosition.y-check.rect.height/2,240,"Check must clear the purple panel");
        Assert.AreEqual(16,step.definition.faces.Length);Assert.AreEqual(16,choices.row.GetComponentsInChildren<UnityEngine.UI.Button>().Length);
        Assert.IsTrue(choices.scroll.horizontal);Assert.IsFalse(choices.scroll.vertical);Assert.Greater(choices.row.rect.width,choices.scroll.viewport.rect.width);
        Assert.IsNotNull(choices.transform.Find("Purple face backing"));Assert.IsTrue(choices.scrollHint.Running);
        float initialScroll=choices.scroll.horizontalNormalizedPosition;Assert.Greater(initialScroll,.5f);
        yield return new WaitForSecondsRealtime(.65f);Assert.Less(choices.scroll.horizontalNormalizedPosition,initialScroll);Assert.Greater(choices.scroll.horizontalNormalizedPosition,0);
        ExecuteEvents.Execute(choices.row.GetComponentsInChildren<UnityEngine.UI.Button>()[0].gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerDownHandler);
        Assert.IsFalse(choices.scrollHint.Running);choices.scroll.horizontalNormalizedPosition=1;yield return null;Assert.AreEqual(1,choices.scroll.horizontalNormalizedPosition,.001f);
        choices.scrollHint.Play();yield return new WaitForSecondsRealtime(1.5f);Assert.IsFalse(choices.scrollHint.Running);Assert.AreEqual(0,choices.scroll.horizontalNormalizedPosition,.001f);
        for(int i=1;i<=16;i++){Assert.IsTrue(step.SelectFace("face-"+i));Assert.IsNull(flow.Session.AppliedFaceId);}
        step.ConfirmSelection();step.ConfirmSelection();yield return Wait(()=>flow.Session.Step==CreationStep.FinalSquishPlay);
        Assert.AreEqual("face-16",flow.Session.AppliedFaceId);Assert.AreEqual(1,flow.Session.FaceApplicationCount);
        var body=flow.FinalDumpling;Assert.IsNotNull(body.SwipeFx);Assert.AreEqual(0,body.SwipeFx.particleCount);
        body.OnPointerDown(Pointer(81,body.transform.TransformPoint(new Vector2(-2,0))));Assert.AreEqual(81,body.PointerOwner);
        body.OnDrag(Pointer(81,body.transform.TransformPoint(new Vector2(-1.5f,0))));int sparks=body.SwipeFx.particleCount;Assert.Greater(sparks,0);
        body.OnDrag(Pointer(81,body.transform.TransformPoint(new Vector2(-1.5f,0))));Assert.AreEqual(sparks,body.SwipeFx.particleCount,"Stationary input must not emit");
        body.OnDrag(Pointer(81,body.transform.TransformPoint(new Vector2(6,0))));Assert.AreEqual(sparks,body.SwipeFx.particleCount,"Off-body swipes must not emit");
        body.OnPointerUp(Pointer(81,body.transform.TransformPoint(new Vector2(6,0))));Assert.IsFalse(body.SwipeFx.isEmitting);
        body.Suspend();Assert.AreEqual(0,body.SwipeFx.particleCount);
        var hint=choices.scrollHint;flow.RestartProof();yield return Wait(()=>flow.CanInteract);Assert.IsNull(flow.ActiveCustomization);Assert.IsTrue(!hint || !hint.Running);
        yield return Mix();yield return Form();Assert.AreEqual(16,flow.ActiveCustomization.choices.row.GetComponentsInChildren<UnityEngine.UI.Button>().Length);
        Assert.AreEqual(7f/12,flow.ActiveCustomization.choices.scroll.horizontalNormalizedPosition,.001f);
    }
    [UnityTest] public IEnumerator MenuRoutesAllFamiliesToRenamedGameplayWithoutLegacyScenes()
    {
        Assert.IsFalse(Application.CanStreamedLevelBeLoaded("PearlCreationGameplay"));
        foreach(string id in new[]{"pearl","glitter","pastel","sprinkles"})
        {
            SceneManager.LoadScene(DumplingFamilyMenu.MenuScene);yield return null;yield return null;
            var menu=Object.FindObjectOfType<DumplingFamilyMenu>();Assert.IsTrue(menu.Choose(id));yield return null;
            flow=Object.FindObjectOfType<CreationFlowController>();layout=flow.layout;yield return Wait(()=>flow.CanInteract);
            Assert.AreEqual("Gameplay",SceneManager.GetActiveScene().name);Assert.AreEqual(id,flow.pearl.FamilyId);
            Object.FindObjectOfType<CreationProgressView>().GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            Assert.AreEqual(DumplingFamilyMenu.MenuScene,SceneManager.GetActiveScene().name);
        }
        flow=null;
    }
    IEnumerator PauseAndResumeWithoutReset(string context)
    {
        int version=flow.Session.Version;float originalScale=Time.timeScale;
        flow.SendMessage("OnApplicationFocus",false);flow.SendMessage("OnApplicationPause",true);
        Assert.IsTrue(flow.IsSuspended);Assert.AreEqual(0,Time.timeScale);yield return null;
        var phase=flow.Session.Step;int committed=flow.Session.CommittedIngredients;
        float mixed=flow.MixingProgress.Progress,formed=flow.FormingProgress.Progress;
        float poured=flow.presenter?flow.presenter.DepletedFraction:0;
        var scroll=flow.ActiveCustomization?flow.ActiveCustomization.choices.scroll:null;
        float scrollPosition=scroll?scroll.horizontalNormalizedPosition:0;
        yield return new WaitForSecondsRealtime(.08f);
        Assert.AreEqual(version,flow.Session.Version,context);Assert.AreEqual(phase,flow.Session.Step,context);
        Assert.AreEqual(committed,flow.Session.CommittedIngredients,context);Assert.AreEqual(mixed,flow.MixingProgress.Progress,.0001f,context);
        Assert.AreEqual(formed,flow.FormingProgress.Progress,.0001f,context);
        if(flow.presenter)Assert.AreEqual(poured,flow.presenter.DepletedFraction,.0001f,context);
        if(scroll)Assert.AreEqual(scrollPosition,scroll.horizontalNormalizedPosition,.0001f,"Scroll hint must freeze while suspended");
        flow.SendMessage("OnApplicationPause",false);Assert.IsTrue(flow.IsSuspended);Assert.AreEqual(0,Time.timeScale);
        flow.SendMessage("OnApplicationFocus",true);Assert.IsFalse(flow.IsSuspended);Assert.AreEqual(originalScale,Time.timeScale);
    }
    [UnityTest]public IEnumerator OrdinaryInterruptionsPreserveCreationThroughFinalPlay()
    {
        Physics2D.SyncTransforms();var actor=flow.ingredient;actor.OnPointerDown(Pointer(7,actor.transform.position));
        actor.OnDrag(Pointer(7,new Vector2(7,8)));Assert.AreEqual(7,actor.PointerOwner);
        yield return PauseAndResumeWithoutReset("ingredient drag");Assert.IsNull(actor.PointerOwner);
        yield return Wait(()=>flow.CanInteract);Assert.AreEqual(0,flow.Session.CommittedIngredients);
        actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
        yield return Wait(()=>flow.presenter.DepletedFraction>.2f);
        yield return PauseAndResumeWithoutReset("accepted ingredient pour");
        yield return Wait(()=>flow.CanInteract);Assert.AreEqual(1,flow.Session.CommittedIngredients);
        yield return Ingredients();var mix=flow.ActiveMixing;Physics2D.SyncTransforms();mix.OnPointerDown(Pointer(11,mix.transform.position));
        for(int i=0;i<25;i++){mix.OnDrag(Pointer(11,flow.mixingArea.Center+Vector2.right*Mathf.Sin(i*.3f)*3));yield return null;}
        Assert.Greater(mix.Progress.Progress,0);yield return PauseAndResumeWithoutReset("held mixing");Assert.IsNull(mix.PointerOwner);
        yield return Mix();var forming=flow.ActiveForming;Physics2D.SyncTransforms();
        forming.OnPointerDown(Pointer(17,flow.bowl.transform.position));forming.OnDrag(Pointer(17,flow.bowl.transform.position+Vector3.right));
        yield return PauseAndResumeWithoutReset("held bowl");Assert.IsNull(forming.PointerOwner);Assert.IsFalse(forming.funnelPresentation.Accepted);
        yield return Wait(()=>!forming.funnelPresentation.Returning);Physics2D.SyncTransforms();
        Vector2 drop=forming.funnelPresentation.DropAssist.center;forming.OnPointerDown(Pointer(17,flow.bowl.transform.position));forming.OnDrag(Pointer(17,drop));forming.OnPointerUp(Pointer(17,drop));
        yield return Wait(()=>forming.Progress.Progress>.2f);yield return PauseAndResumeWithoutReset("automatic funnel transfer");
        yield return Wait(()=>flow.Session.Step==CreationStep.Customization);Assert.AreEqual(1,flow.FormingProgress.CompletionCount);
        var customization=flow.ActiveCustomization;Assert.IsTrue(customization.SelectFace("face-8"));
        yield return PauseAndResumeWithoutReset("face preview");Assert.AreEqual("face-8",flow.Session.SelectedFaceId);Assert.IsNull(flow.Session.AppliedFaceId);
        yield return FinishFace(7);var body=flow.FinalDumpling;body.OnPointerDown(Pointer(81,body.transform.TransformPoint(new Vector2(-2,0))));
        body.OnDrag(Pointer(81,body.transform.TransformPoint(new Vector2(-1,0))));yield return PauseAndResumeWithoutReset("final squish");
        Assert.IsNull(body.PointerOwner);Assert.AreSame(body,flow.FinalDumpling);Assert.AreEqual("face-8",flow.Session.AppliedFaceId);
        Assert.AreEqual(1,flow.Session.FinalPlayStartCount);Assert.AreEqual(1,flow.Session.FaceApplicationCount);
    }
    [UnityTest]public IEnumerator LeavingSuspendedGameplayRestoresThePreviousTimeScale()
    {
        float original=Time.timeScale;flow.SendMessage("OnApplicationPause",true);Assert.AreEqual(0,Time.timeScale);
        DumplingFamilyMenu.OpenMenu();yield return null;Assert.AreEqual(original,Time.timeScale);flow=null;
    }
    [UnityTest]public IEnumerator StraightSwipesWithToolInsideBowlCompleteMixingWithoutCircles()
    {
        yield return Ingredients();var step=flow.ActiveMixing;Assert.IsTrue(flow.mixingConfiguration.acceptAnySwipe);
        Physics2D.SyncTransforms();step.OnPointerDown(Pointer(11,step.transform.position));Assert.AreEqual(11,step.PointerOwner);
        for(int i=0;i<900 && flow.Session.Step==CreationStep.Mixing && step.Progress.Progress<1;i++)
        {
            // Pointer can travel past the bowl rim; the tool remains inside and swipes horizontally.
            float x=Mathf.Sin(i*.13f)*flow.mixingArea.radii.x*1.4f;
            step.OnDrag(Pointer(11,flow.mixingArea.Center+Vector2.right*x));yield return null;
        }
        Assert.IsNotNull(step.StirringSparkles);Assert.IsTrue(step.StirringSparkles.isPlaying);Assert.Greater(step.StirringSparkles.particleCount,0,"Useful stirring must actually emit particles");
        Assert.AreEqual(1,step.Progress.Progress);Assert.AreEqual(CreationStep.Mixing,flow.Session.Step);
        flow.RequestMixingCompletion(step);Assert.AreEqual(CreationStep.Mixing,flow.Session.Step);
        yield return PauseAndResumeWithoutReset("completed held mix");Assert.IsNull(step.PointerOwner);
        Assert.AreEqual(CreationStep.Mixing,flow.Session.Step);Assert.AreEqual(1,step.Progress.Progress);
        Physics2D.SyncTransforms();step.OnPointerDown(Pointer(11,step.transform.position));Assert.AreEqual(11,step.PointerOwner);
        Vector3 completedPosition=step.transform.position;
        for(int i=0;i<30;i++){step.OnDrag(Pointer(11,flow.mixingArea.Center+Vector2.left*flow.mixingArea.radii.x));yield return null;}
        Assert.Greater(Vector3.Distance(completedPosition,step.transform.position),.1f,"Completed tool must remain draggable");
        Assert.AreEqual(1,step.Progress.CompletionCount);Assert.AreEqual(11,step.PointerOwner);
        step.OnPointerUp(Pointer(12,step.transform.position));Assert.AreEqual(CreationStep.Mixing,flow.Session.Step);
        step.OnPointerUp(Pointer(11,step.transform.position));step.OnPointerUp(Pointer(11,step.transform.position));
        Assert.IsFalse(step.StirringSparkles.isEmitting,"Release stops stirring sparkle emission");
        yield return Wait(()=>flow.Session.Step==CreationStep.Forming);Assert.AreEqual(1,flow.MixingProgress.CompletionCount);Assert.IsNull(step.PointerOwner);
    }
    IEnumerator Ingredients(int variant=0,Action capture=null)
    {
        while(flow.Session.HasIngredient)
        {
            if(flow.Session.CurrentIngredientIndex==3 && (flow.pearl.AwaitingChoice || flow.pearl.ChoicePresentationCount==0)){yield return Wait(()=>flow.pearl.AwaitingChoice,20,capture);Assert.IsTrue(flow.pearl.SelectVariant(variant));yield return Wait(()=>flow.CanInteract,20,capture);}
            Physics2D.SyncTransforms();var actor=flow.ingredient;
            if(flow.Session.CurrentIngredientIndex==0)
            {
                actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnPointerUp(Pointer(7,actor.transform.position));
                Assert.AreEqual(CreationStep.ReturnIngredient,flow.Session.Step,"A tap at the shelf is not a bowl drop");yield return Wait(()=>flow.CanInteract,20,capture);
            }
            actor.OnPointerDown(Pointer(7,actor.transform.position));Assert.AreEqual(7,actor.PointerOwner);
            actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));
            yield return Wait(()=>flow.CanInteract||flow.pearl.AwaitingChoice||flow.Session.Step==CreationStep.Mixing,20,capture);
        }
        yield return Wait(()=>flow.Session.Step==CreationStep.Mixing,20,capture);Assert.AreEqual(4,flow.Session.CommittedIngredients);
    }
    IEnumerator Mix(int variant=0,Action capture=null)
    {
        yield return Ingredients(variant,capture);var step=flow.ActiveMixing;Physics2D.SyncTransforms();step.OnPointerDown(Pointer(11,step.transform.position));Assert.AreEqual(11,step.PointerOwner);
        for(int i=0;i<2200 && flow.Session.Step==CreationStep.Mixing && step.Progress.Progress<1;i++)
        {float a=Mathf.PI+i*.13f;step.OnDrag(Pointer(11,flow.mixingArea.Center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),flow.mixingArea.radii*.7f)));if(i%4==0)capture?.Invoke();yield return null;}
        step.OnPointerUp(Pointer(11,step.transform.position));
        yield return Wait(()=>flow.Session.Step==CreationStep.Forming,20,capture);Assert.AreEqual(22.8f,flow.mixingConfiguration.requiredEffort);
    }
    IEnumerator Form(Action capture=null)
    {
        var step=flow.ActiveForming;Vector2 drop=step.funnelPresentation.DropAssist.center;Assert.IsNotNull(step.funnelPresentation);Physics2D.SyncTransforms();
        Assert.IsFalse(Array.Exists(step.GetComponentsInChildren<SpriteRenderer>(),r=>r.name.Contains("spatula")),"Step 5 has no spatula");
        step.OnPointerDown(Pointer(17,flow.bowl.transform.position));Assert.AreEqual(17,step.PointerOwner);
        Vector2 start=flow.bowl.transform.position;
        for(int i=1;i<=18;i++){step.OnDrag(Pointer(17,Vector2.Lerp(start,drop,i/18f)));if(i%3==0)capture?.Invoke();yield return null;}
        Assert.AreEqual(0,step.Progress.Progress,"Dragging alone must not transfer material");
        step.OnPointerUp(Pointer(17,drop));Assert.IsNull(step.PointerOwner);Assert.IsTrue(step.funnelPresentation.Accepted);
        step.OnPointerUp(Pointer(17,drop));Assert.AreEqual(CreationStep.FinishForming,flow.Session.Step);
        yield return Wait(()=>flow.Session.Step==CreationStep.Customization,20,capture);
        Assert.IsTrue(flow.customizationDefinition.directSelection);Assert.IsNull(flow.ActiveCustomization.stamp);Assert.IsFalse(flow.FormingWorkspace.gameObject.activeInHierarchy);
    }
    IEnumerator FinishFace(int index=0,Action capture=null)
    {
        var step=flow.ActiveCustomization;Assert.IsTrue(step.SelectFace(flow.customizationDefinition.faces[index].id));
        Assert.IsNull(flow.Session.AppliedFaceId);Assert.AreEqual(0,flow.Session.FaceApplicationCount);capture?.Invoke();
        step.ConfirmSelection();step.ConfirmSelection();Assert.AreEqual(1,flow.Session.FaceApplicationCount);
        yield return Wait(()=>flow.Session.Step==CreationStep.FinalSquishPlay,20,capture);
        Assert.IsNull(flow.ActiveStep);Assert.AreEqual(1,flow.Session.FinalPlayStartCount);Assert.IsNotNull(flow.FinalDumpling);
        Assert.AreEqual(flow.customizationDefinition.faces[index].id,flow.Session.AppliedFaceId);
    }
    void Capture(string name,int width=540,int height=1200)
    {
        var camera=layout.WorldCamera;if(!camera.targetTexture)return;camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        string folder=Path.Combine(Path.GetTempPath(),"squishy-pearl-evidence");Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(folder,name+".png")));File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;Object.Destroy(texture);
    }
    void CameraSetup(){layout.WorldCamera.targetTexture=new RenderTexture(540,1200,16);layout.WorldCamera.pixelRect=new Rect(0,0,540,1200);layout.SetDeviceSafeAreaOverride(new Rect(18,48,510,1056));}
    Color32[] FramePixels()
    {
        Canvas.ForceUpdateCanvases();var camera=layout.WorldCamera;camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
        var texture=new Texture2D(540,1200,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,540,1200),0,0);texture.Apply();var pixels=texture.GetPixels32();
        RenderTexture.active=previous;Object.Destroy(texture);return pixels;
    }
    [UnityTest]public IEnumerator ClientCompositionShowsMixedResultAndSixMilestonesResetCorrectly()
    {
        CameraSetup();yield return null;var progress=Object.FindObjectOfType<CreationProgressView>();Assert.IsNotNull(progress);Assert.AreEqual(6,progress.markers.Length);Assert.AreEqual(1,progress.CurrentNumber);
        Assert.IsFalse(Object.FindObjectOfType<CreationProofHud>().enabled);Assert.IsFalse(Object.FindObjectOfType<CreationProofHud>().status.gameObject.activeInHierarchy);
        Assert.AreEqual(-6.5f,flow.bowl.transform.position.y);Assert.AreEqual(flow.pearl.composition.containerWidth,flow.presenter.container.sprite.bounds.size.x,.01f);
        var withProgress=FramePixels();progress.row.SetActive(false);var withoutProgress=FramePixels();progress.row.SetActive(true);
        int uiPixels=0;for(int i=0;i<withProgress.Length;i++)if(Math.Abs(withProgress[i].r-withoutProgress[i].r)+Math.Abs(withProgress[i].g-withoutProgress[i].g)+Math.Abs(withProgress[i].b-withoutProgress[i].b)>30)uiPixels++;
        Assert.Greater(uiPixels,150,"Progress artwork must render above the background");
        Capture("client-01-ready");yield return Ingredients();yield return null;Assert.AreEqual(4,progress.CurrentNumber);Capture("client-02-base-and-dye");
        var mixingArt=flow.ActiveMixing.visual.GetComponentInChildren<SpriteRenderer>();
        Assert.AreEqual(3.8f,mixingArt.sprite.bounds.size.x,.01f);
        Assert.That(mixingArt.sprite.bounds.size.y/flow.pearl.composition.bowlWidth,Is.InRange(.75f,.82f),"Reference-sized spatula relative to the bowl");
        Physics2D.SyncTransforms();foreach(float y in new[]{-.4f,0,.4f})Assert.IsTrue(flow.ActiveMixing.GetComponent<Collider2D>().OverlapPoint(mixingArt.transform.TransformPoint(new Vector2(0,mixingArt.sprite.bounds.size.y*y))),"Blade and handle must both be grabbable");
        var toolSprite=mixingArt.sprite;Capture("client-step-4-spatula");
        yield return Mix();yield return null;Assert.AreEqual(5,progress.CurrentNumber);
        Assert.IsFalse(Array.Exists(flow.ActiveForming.GetComponentsInChildren<SpriteRenderer>(),r=>r.name.Contains("spatula")));
        var mixed=flow.mixingSurface.mixedLayer;Assert.IsTrue(mixed.enabled);var withMixture=FramePixels();mixed.enabled=false;var withoutMixture=FramePixels();mixed.enabled=true;
        int changed=0;for(int i=0;i<withMixture.Length;i++)if(Math.Abs(withMixture[i].r-withoutMixture[i].r)+Math.Abs(withMixture[i].g-withoutMixture[i].g)+Math.Abs(withMixture[i].b-withoutMixture[i].b)>30)changed++;
        Assert.Greater(changed,600,"Completed mixture must actually render through the bowl mask, not merely have an enabled renderer");Capture("client-03-visible-mixed-result");
        yield return Form();yield return null;Assert.AreEqual(6,progress.CurrentNumber);Capture("client-04-face");yield return FinishFace();yield return null;Assert.IsFalse(progress.row.activeSelf);
        flow.RestartProof();yield return Wait(()=>flow.CanInteract);yield return null;Assert.AreEqual(1,progress.CurrentNumber);Assert.IsTrue(progress.row.activeSelf);
        foreach(var shell in flow.pearl.bowlShell)Assert.IsTrue(shell.enabled);Capture("client-05-restart");
    }
    [UnityTest]public IEnumerator BowlDropRejectsTapAndDistantReleaseThenFillsAndWaitsForFx()
    {
        CameraSetup();yield return null;yield return Mix();var step=flow.ActiveForming;var presentation=step.funnelPresentation;Vector2 drop=presentation.DropAssist.center;Physics2D.SyncTransforms();
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(Pointer(17,(Vector2)flow.bowl.transform.position+Vector2.left*6),hits);
        Assert.IsTrue(hits.Exists(h=>ExecuteEvents.GetEventHandler<IPointerDownHandler>(h.gameObject)==step.gameObject),"Visible bowl must receive real event-system raycasts");
        var funnelHome=presentation.funnel.transform.position;var pouchHome=presentation.pouch.transform.position;
        step.OnPointerDown(Pointer(17,flow.bowl.transform.position));step.OnPointerUp(Pointer(17,flow.bowl.transform.position));Assert.IsFalse(presentation.Accepted);Assert.AreEqual(0,step.Progress.Progress);
        step.OnPointerDown(Pointer(17,flow.bowl.transform.position));step.OnDrag(Pointer(17,new Vector2(0,12)));step.OnPointerUp(Pointer(17,new Vector2(0,12)));
        Assert.IsFalse(presentation.Accepted);yield return Wait(()=>!presentation.Returning);Assert.AreEqual(presentation.composition.formingBowlPosition,(Vector2)flow.bowl.transform.position);
        step.OnPointerDown(Pointer(17,flow.bowl.transform.position));step.OnDrag(Pointer(17,drop));
        Assert.AreEqual(funnelHome,presentation.funnel.transform.position);Assert.AreEqual(pouchHome,presentation.pouch.transform.position);Assert.AreEqual(0,presentation.DisplayedFill);
        step.OnPointerUp(Pointer(17,drop));Assert.IsTrue(presentation.Accepted);Assert.IsNull(step.PointerOwner);Assert.IsFalse(step.CanInteract);
        step.OnPointerUp(Pointer(17,drop));Assert.IsFalse(flow.RequestFunnelTransfer(step));
        yield return Wait(()=>presentation.FunnelLevel>.85f);Assert.IsTrue(presentation.funnelFill.enabled);Assert.Less(presentation.DisplayedFill,1);
        var withFunnel=FramePixels();presentation.funnelFill.enabled=false;var withoutFunnel=FramePixels();presentation.funnelFill.enabled=true;
        Vector2 bottom=layout.WorldCamera.WorldToScreenPoint(presentation.funnel.bounds.min),top=layout.WorldCamera.WorldToScreenPoint(presentation.funnel.bounds.max);
        int funnelPixels=0;for(int y=Mathf.Max(0,Mathf.CeilToInt(bottom.y));y<Mathf.Min(1200,top.y);y++)for(int x=Mathf.Max(0,Mathf.CeilToInt(bottom.x));x<Mathf.Min(540,top.x);x++)
        {int i=y*540+x;if(Math.Abs(withFunnel[i].r-withoutFunnel[i].r)+Math.Abs(withFunnel[i].g-withoutFunnel[i].g)+Math.Abs(withFunnel[i].b-withoutFunnel[i].b)>30)funnelPixels++;}
        Assert.Greater(funnelPixels,100,"Funnel fill must visibly render inside its solid interior mask");Capture("step5-funnel-filled");
        yield return Wait(()=>presentation.DisplayedFill>.3f);Assert.Less(presentation.DisplayedFill,1);Assert.AreEqual(CreationStep.FinishForming,flow.Session.Step);
        yield return Wait(()=>presentation.FxStarted);Assert.AreEqual(1,step.Progress.CompletionCount);Assert.AreEqual(1,presentation.DisplayedFill);Assert.AreEqual(CreationStep.FinishForming,flow.Session.Step);Assert.IsFalse(presentation.FxCompleted);
        Time.timeScale=1;yield return new WaitForSeconds(.18f);
        var renderer=presentation.completionFx.GetComponent<ParticleSystemRenderer>();var withFx=FramePixels();renderer.enabled=false;var withoutFx=FramePixels();renderer.enabled=true;
        int fxPixels=0;for(int i=0;i<withFx.Length;i++)if(Math.Abs(withFx[i].r-withoutFx[i].r)+Math.Abs(withFx[i].g-withoutFx[i].g)+Math.Abs(withFx[i].b-withoutFx[i].b)>30)fxPixels++;
        Assert.Greater(fxPixels,60,"Completion particles must be visible, not only alive");Capture("step5-completion-fx");Time.timeScale=6;
        yield return Wait(()=>flow.Session.Step==CreationStep.Customization);Assert.AreEqual(1,flow.FormingProgress.CompletionCount);Assert.AreEqual(4,flow.Session.CurrentIngredientIndex);
    }
    [UnityTest]public IEnumerator RestartDuringBowlReturnAutomaticPourAndFxCleansPresentation()
    {
        for(int phase=0;phase<3;phase++)
        {
            yield return Mix();var step=flow.ActiveForming;var presentation=step.funnelPresentation;Vector2 drop=presentation.DropAssist.center;Physics2D.SyncTransforms();
            step.OnPointerDown(Pointer(17,flow.bowl.transform.position));step.OnDrag(Pointer(17,(phase==0?new Vector2(0,12):drop)));step.OnPointerUp(Pointer(17,(phase==0?new Vector2(0,12):drop)));
            if(phase==1)yield return Wait(()=>presentation.DisplayedFill>.15f);
            if(phase==2)yield return Wait(()=>presentation.FxStarted);
            var fx=presentation.completionFx;flow.RestartProof();yield return null;yield return null;yield return Wait(()=>flow.CanInteract);
            Assert.IsTrue(!fx || !fx.IsAlive(true));Assert.IsNull(flow.ActiveForming);Assert.AreEqual(0,flow.FormingProgress.Progress);Assert.AreEqual(0,flow.formingResultRoot.childCount);Assert.AreEqual(0,flow.Session.CurrentIngredientIndex);
            foreach(var shell in flow.bowl.shellRenderers)Assert.IsTrue(shell.enabled);
        }
    }
    [UnityTest]public IEnumerator AllPearlChoicesReachMatchingBodyAndDirectFaceWithoutStamp()
    {
        for(int index=0;index<4;index++)
        {
            yield return Mix(index);yield return Form();yield return FinishFace(index);
            var variant=flow.pearl.definition.variants[index];Assert.AreSame(variant.body,flow.FormedMaterial.states[3].sprite);Assert.AreSame(variant.deformation,flow.FinalDumpling.configuration);
            Assert.AreEqual(221,flow.FinalDumpling.Simulation.Positions.Length);Assert.IsNotNull(flow.FinalDumpling.GetComponentInChildren<DumplingSurfaceAttachment>());
            flow.RestartProof();yield return Wait(()=>flow.CanInteract);Assert.IsNull(flow.Session.AppliedFaceId);
        }
    }
    [UnityTest]public IEnumerator RestartDuringChoiceTransferPreviewAndFinalPullCleansActors()
    {
        for(int phase=0;phase<4;phase++)
        {
            if(phase==0){while(flow.Session.CurrentIngredientIndex<3){var actor=flow.ingredient;Physics2D.SyncTransforms();actor.OnPointerDown(Pointer(7,actor.transform.position));actor.OnDrag(Pointer(7,new Vector2(0,3)));actor.OnPointerUp(Pointer(7,new Vector2(0,3)));yield return Wait(()=>flow.CanInteract||flow.pearl.AwaitingChoice);}yield return Wait(()=>flow.pearl.AwaitingChoice);}
            else {yield return Mix();if(phase>=2){yield return Form();Assert.IsTrue(flow.ActiveCustomization.SelectFace(flow.customizationDefinition.faces[0].id));if(phase==3){yield return FinishFace();var body=flow.FinalDumpling;body.OnPointerDown(Pointer(51,body.transform.TransformPoint(new Vector2(-2,0))));body.OnDrag(Pointer(51,new Vector2(-8,1)));yield return null;}}}
            flow.RestartProof();yield return null;yield return null;yield return Wait(()=>flow.CanInteract);
            Assert.IsNull(flow.FinalDumpling);Assert.IsNull(flow.ActiveForming);Assert.IsNull(flow.ActiveCustomization);Assert.IsFalse(flow.pearl.AwaitingChoice);Assert.AreEqual(0,flow.formingResultRoot.childCount);Assert.AreEqual(0,flow.Session.CurrentIngredientIndex);
        }
    }
    [UnityTest]public IEnumerator TwelvePortraitProfilesKeepCanonicalPearlGeometryAndUniformBackground()
    {
        yield return Mix();yield return Form();yield return FinishFace();var camera=layout.WorldCamera;camera.targetTexture=new RenderTexture(1200,2800,16);
        foreach(var size in new[]{new Vector2(900,1600),new Vector2(900,1950),new Vector2(900,2000),new Vector2(1200,1600),new Vector2(1170,2532),new Vector2(1125,2436)})for(int inset=0;inset<2;inset++)
        {
            camera.pixelRect=new Rect(Vector2.zero,size);Rect safe=inset==0?camera.pixelRect:new Rect(30,80,size.x-50,size.y-240);if(inset==1&&size.x==1170)safe=new Rect(0,102,1170,2289);if(inset==1&&size.x==1125)safe=new Rect(0,102,1125,2202);
            layout.SetDeviceSafeAreaOverride(safe);yield return null;yield return null;
            foreach(var vertex in flow.FinalDumpling.Simulation.Positions){Vector2 world=flow.FinalDumpling.transform.TransformPoint(vertex);Assert.IsTrue(CameraFit.CriticalBounds.Contains(world));Assert.IsTrue(layout.UsablePixels.Contains(camera.WorldToScreenPoint(world)));}
            var scale=flow.pearl.background.transform.localScale;Assert.AreEqual(scale.x,scale.y);Assert.IsTrue(flow.pearl.background.bounds.Contains(camera.ViewportToWorldPoint(new Vector3(0,0,-camera.transform.position.z))));
            Assert.AreEqual(1,flow.Session.FinalPlayStartCount);
            var body=flow.FinalDumpling;body.OnPointerDown(Pointer(51,body.transform.TransformPoint(new Vector2(-2,0))));Assert.AreEqual(51,body.PointerOwner);
            body.OnDrag(Pointer(51,body.transform.TransformPoint(new Vector2(-6,0))));
            float end=Time.realtimeSinceStartup+.15f;while(Time.realtimeSinceStartup<end)yield return null;
            foreach(var vertex in body.Simulation.Positions)Assert.IsTrue(CameraFit.CriticalBounds.Contains(body.transform.TransformPoint(vertex)));
            body.OnPointerUp(Pointer(51,Vector2.zero));body.Suspend();
            Rect visible=layout.VisibleWorldBounds;
            foreach(var sprite in flow.bowl.GetComponentsInChildren<SpriteRenderer>())if(sprite.enabled)
                Assert.IsFalse(sprite.bounds.max.x>visible.xMin && sprite.bounds.min.x<visible.xMax && sprite.bounds.max.y>visible.yMin && sprite.bounds.min.y<visible.yMax,"Bowl must retire outside even wide/inset viewports");
            Capture($"profiles/{size.x}x{size.y}-{inset}",(int)size.x,(int)size.y);
        }
    }
    [UnityTest]public IEnumerator NormalTimeRealArtWalkthrough()
    {
        Time.timeScale=1;CameraSetup();yield return null;int frame=0;string phase="Ingredients";
        string folder=Path.Combine(Path.GetTempPath(),"squishy-pearl-evidence","walkthrough");if(Directory.Exists(folder))Directory.Delete(folder,true);
        Action capture=()=>Capture($"walkthrough/{frame++:D4}-{phase}-{flow.Session.Step}");
        Capture("01-ready");yield return Mix(0,capture);Capture("02-transfer-ready");phase="Forming";yield return Form(capture);Capture("03-face-choice");
        foreach(int index in new[]{0,1,2,3}){flow.ActiveCustomization.SelectFace(flow.customizationDefinition.faces[index].id);yield return new WaitForSeconds(.25f);Capture("face-"+index);}
        phase="Confirm";yield return FinishFace(0,capture);yield return null;Capture("04-final");var body=flow.FinalDumpling;
        foreach(Vector2 direction in new[]{Vector2.left,Vector2.right,Vector2.up,new Vector2(-1,1).normalized})
        {
            body.OnPointerDown(Pointer(51,body.transform.TransformPoint(direction*2)));Assert.AreEqual(51,body.PointerOwner);
            phase="Pull";for(int i=0;i<40;i++){body.OnDrag(Pointer(51,body.transform.TransformPoint(direction*(2+i/15f))));capture();yield return null;}
            body.OnPointerUp(Pointer(51,Vector2.zero));phase="Recover";yield return Wait(()=>body.Simulation.MaximumDisplacement<.005f,5,capture);
        }
        Capture("05-settled");flow.RestartProof();phase="Restart";yield return Wait(()=>flow.CanInteract,20,capture);Capture("06-restart");
    }
}
