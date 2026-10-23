using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SquishyDumpling.Creation;
using SquishyDumpling.Layout;
using Object=UnityEngine.Object;
public static partial class PearlCreationBuilder
{
    [MenuItem("Squishy Dumpling/Build family menu and shared selector")]
    public static void BuildFamilies()
    {
        art.Clear();
        foreach(string path in Directory.GetFiles(Pearl+"Art","*.png"))art[Path.GetFileNameWithoutExtension(path)]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        foreach(string name in new[]{"UI-panel1","UI-panel2","UI-panel3"})Import("Dumpling_"+name,2.5f,2048,true);
        Import("Dumpling_Menu-BG",40.96f,4096);Import("Dumpling_basket-back",11);Import("Dumpling_basket-front",11);
        var catalog=LoadOrCreate<DumplingFamilyCatalog>(Pearl+"Definitions/DumplingFamilies.asset");
        var pearl=AssetDatabase.LoadAssetAtPath<PearlRecipeDefinition>(Pearl+"Definitions/PearlPresentation.asset");
        catalog.families=new[]{new DumplingFamily{id="pearl",displayName="Pearl",presentation=pearl},
            MakeFamily("glitter","Glitter",new[]{"glitter-blue-big","glitter-pink-big","glitter-grey","glitter-pink-heart"},new[]{"Blue glitter","Pink glitter","Silver glitter","Pink hearts"},new[]{"glitter-blue-big","glitter-pink-big","glitter-gray","glitter-pink-heart"},true),
            MakeFamily("pastel","Pastel",new[]{"color1","color2","color3","color4"},new[]{"Mint","Lavender","Yellow","Pink"},new[]{"color1","colo2","colo3","colo4"},false),
            MakeFamily("sprinkles","Sprinkles",new[]{"Sprinkles-stars","Sprinkles-smileys","Sprinkles-lollipops","Sprinkles-gummybears"},new[]{"Stars","Smileys","Candy","Gummy bears"},new[]{"Sprinkles-stars","Sprinkles-smileys","Sprinkles-lollipops","Sprinkles-gummy-bears"},true)};
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Gameplay.unity",OpenSceneMode.Single);
        var flow=Object.FindObjectOfType<CreationFlowController>();var module=flow.pearl;module.families=catalog;
        Object.DestroyImmediate(module.selectionCanvas.gameObject);BuildSelector(module,pearl);DecorateSelector(module);
        // The production navigation button now returns to family selection; restart remains available on the controller.
        var hud=Object.FindObjectOfType<CreationProgressView>();var home=hud.GetComponentsInChildren<Button>(true).First();
        home.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(home.onClick,DumplingFamilyMenu.OpenMenu);
        Import("Dumpling_UI-Button-home",2.5f,2048,true);
        home.GetComponent<Image>().sprite=A("UI-Button-base");home.GetComponent<Image>().preserveAspect=true;
        var text=home.GetComponentInChildren<Text>();if(text)Object.DestroyImmediate(text.gameObject);
        var iconTransform=home.transform.Find("Home icon") as RectTransform;
        if(!iconTransform)iconTransform=new GameObject("Home icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<RectTransform>();
        iconTransform.SetParent(home.transform,false);iconTransform.anchorMin=Vector2.zero;iconTransform.anchorMax=Vector2.one;
        iconTransform.anchoredPosition=Vector2.zero;iconTransform.sizeDelta=Vector2.zero;
        var icon=iconTransform.GetComponent<Image>();icon.sprite=A("UI-Button-home");icon.preserveAspect=true;icon.raycastTarget=false;
        ConfigureSceneAudio();EditorUtility.SetDirty(module);EditorSceneManager.SaveScene(scene);
        BuildFamilyMenu(catalog);
        string menu=Pearl+"Scenes/Menu.unity",creation=Pearl+"Scenes/Gameplay.unity";
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(menu,true),new EditorBuildSettingsScene(creation,true)};
        AssetDatabase.SaveAssets();Debug.Log("Four family definitions, 16 ingredient/body options, shared purple selector and production family menu authored.");
    }
    static DumplingFamily MakeFamily(string id,string displayName,string[] prefixes,string[] labels,string[] bodies,bool powder)
    {
        var source=AssetDatabase.LoadAssetAtPath<PearlRecipeDefinition>(Pearl+"Definitions/PearlPresentation.asset");
        var data=LoadOrCreate<PearlRecipeDefinition>(Pearl+"Definitions/"+displayName+"Presentation.asset");
        data.makingBackground=source.makingBackground;data.playBackground=source.playBackground;data.choiceFrame=source.choiceFrame;data.selectedFrame=source.selectedFrame;data.confirmSprite=source.confirmSprite;
        data.variants=new PearlVariant[4];
        var composition=AssetDatabase.LoadAssetAtPath<CreationComposition>(Pearl+"Definitions/CreationComposition.asset");
        for(int i=0;i<4;i++)
        {
            string prefix=prefixes[i],key=displayName+i;float width=powder?5.4f:composition.dyeContainerWidth;
            Import("Dumpling_"+prefix+"-base",width);Import("Dumpling_"+prefix+"-lid",powder?width:width*.656f);Import("Dumpling_"+prefix+"-flow",powder?.7f:.35f);
            string bowlName=prefix=="Sprinkles-stars"?"iSprinkles-stars-n-the-bowl":prefix+"-in-the-bowl";
            Import("Dumpling_"+bowlName,composition.bowlWidth*.53f);
            var stages=new Sprite[3];
            for(int k=0;k<3;k++){string suffix=prefix=="Sprinkles-stars"?"-mix":"-mix-";Import("Dumpling_"+prefix+suffix+(k+1),composition.bowlWidth*.8f);stages[k]=A(prefix+suffix+(k+1));}
            Import("Dumpling_"+bodies[i],10);
            var ingredient=CloneAsset(source.variants[0].ingredient,Pearl+"Definitions/"+key+"Ingredient.asset");
            ingredient.ingredientId=id+"-"+i;ingredient.displayName=labels[i];ingredient.bowlLayer=3;ingredient.presentationType=powder?IngredientPresentationType.PowderPour:IngredientPresentationType.LiquidPour;
            ingredient.containerSprite=ingredient.contentSprite=A(prefix+"-base");ingredient.streamSprite=ingredient.powderSprite=A(prefix+"-flow");ingredient.streamFrames=new Sprite[0];
            ingredient.fadeAuthoredFlow=id=="sprinkles";
            ingredient.discreteDrops=!powder;ingredient.authoredStream=ingredient.bakedContainer=true;ingredient.fixedCenteredPour=true;ingredient.fixedPourContact=new Vector2(0,-3.5f);ingredient.pourAngle=135;
            var baseRecipe=AssetDatabase.LoadAssetAtPath<CreationRecipe>(Root+"Definitions/ProductionRecipe.asset");
            ingredient.actorPrefab=IngredientPrefab(ingredient,baseRecipe.ingredients[powder?1:0].actorPrefab,key,A(prefix+"-lid"));EditorUtility.SetDirty(ingredient);
            var deform=CloneAsset(source.variants[0].deformation,Pearl+"Definitions/"+key+"Deformation.asset");var body=A(bodies[i]);
            deform.meshBounds=new Rect(body.bounds.min.x,body.bounds.min.y,body.bounds.size.x,body.bounds.size.y);deform.contactOutline=Outline(bodies[i],body);TuneFinalStretch(deform);EditorUtility.SetDirty(deform);
            data.variants[i]=new PearlVariant{id=id+"-"+i,displayName=labels[i],ingredient=ingredient,inBowl=A(bowlName),mixing=stages,body=body,deformation=deform};
        }
        EditorUtility.SetDirty(data);return new DumplingFamily{id=id,displayName=displayName,presentation=data};
    }
    static void DecorateSelector(PearlCreationPresentation module)
    {
        var root=module.selectionCanvas.transform;root.name="Family ingredient choices";
        var rect=(RectTransform)root;rect.sizeDelta=new Vector2(1400,500);
        // Client exports the rounded backing in three pieces; only its plain middle strip stretches.
        var backing=new GameObject("Purple selector backing",typeof(RectTransform));backing.transform.SetParent(root,false);backing.transform.SetAsFirstSibling();
        // Match the visible vertical extents rather than each export's differently padded canvas.
        var l=PanelPiece("UI-panel1",6,764);var m=PanelPiece("UI-panel2",11,768);var r=PanelPiece("UI-panel3",6,763);
        float height=480,left=height*l.rect.width/l.rect.height,right=height*r.rect.width/r.rect.height;
        UiImage(backing.transform,"Left cap",l,new Vector2(-700+left/2,0),new Vector2(left,height));
        var middle=UiImage(backing.transform,"Middle",m,new Vector2((left-right)/2,0),new Vector2(1400-left-right+2,height));middle.preserveAspect=false;
        UiImage(backing.transform,"Right cap",r,new Vector2(700-right/2,0),new Vector2(right,height));
        var button=module.choiceTemplate;((RectTransform)button.transform).sizeDelta=new Vector2(280,380);
        var preview=(RectTransform)button.transform.Find("Preview");preview.sizeDelta=new Vector2(220,320);preview.anchoredPosition=Vector2.zero;
        var label=button.transform.Find("Name");if(label)Object.DestroyImmediate(label.gameObject);
        root.position=module.composition.choicePosition;root.localScale=Vector3.one*.013f;
    }
    static Sprite PanelPiece(string name,int top,int bottom)
    {
        string path=Pearl+"Art/Dumpling_"+name+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        string spriteName=name+" aligned";
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
        var existing=sprites.FirstOrDefault(s=>s.name==spriteName);if(existing)return existing;
        importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritesheet=new[]{new SpriteMetaData{name=spriteName,rect=new Rect(0,height-bottom,width,bottom-top),alignment=9,pivot=new Vector2(.5f,.5f)}};
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s=>s.name==spriteName);
    }
    [MenuItem("Squishy Dumpling/Update dye drops and panel edges")]
    public static void UpdateDyesAndPanel()
    {
        art.Clear();foreach(string name in new[]{"UI-panel1","UI-panel2","UI-panel3"})art["Dumpling_"+name]=AssetDatabase.LoadAssetAtPath<Sprite>(Pearl+"Art/Dumpling_"+name+".png");
        var catalog=AssetDatabase.LoadAssetAtPath<DumplingFamilyCatalog>(Pearl+"Definitions/DumplingFamilies.asset");
        foreach(var family in catalog.families)if(family.id=="pearl" || family.id=="pastel")foreach(var variant in family.presentation.variants)
        {
            var d=variant.ingredient;d.discreteDrops=true;d.dropCount=3;d.dropWidth=1.1f;d.fixedCenteredPour=true;d.fixedPourContact=new Vector2(0,-3.5f);d.pourAngle=135f;EditorUtility.SetDirty(d);
        }
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Gameplay.unity",OpenSceneMode.Single);
        var module=Object.FindObjectOfType<PearlCreationPresentation>();var old=module.selectionCanvas.transform.Find("Purple selector backing");if(old)Object.DestroyImmediate(old.gameObject);
        DecorateSelector(module);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    [MenuItem("Squishy Dumpling/Update forming alignment and sixteen faces")]
    public static void UpdateFormingAndFaces()
    {
        art.Clear();foreach(string name in new[]{"UI-panel1","UI-panel2","UI-panel3"})art["Dumpling_"+name]=AssetDatabase.LoadAssetAtPath<Sprite>(Pearl+"Art/Dumpling_"+name+".png");
        var composition=AssetDatabase.LoadAssetAtPath<CreationComposition>(Pearl+"Definitions/CreationComposition.asset");
        composition.pouchPosition=new Vector2(0,-8.78f);composition.bowlPourPosition=new Vector2(-6.43467f,5.7f);composition.bowlPourMouthLocal=new Vector2(7.4f,1.7f);EditorUtility.SetDirty(composition);
        var faces=AssetDatabase.LoadAssetAtPath<FaceCustomizationDefinition>(Pearl+"Definitions/PearlFaces.asset");faces.faces=new FaceOption[16];
        for(int i=0;i<16;i++)
        {
            Import("Dumpling_face-"+(i+1),3.8f);var sprite=A("face-"+(i+1));
            faces.faces[i]=new FaceOption{id="face-"+(i+1),displayName="Face "+(i+1),preview=sprite,appliedArtwork=sprite,appliedLocalScale=Vector3.one,appliedLocalPosition=new Vector3(0,-.25f,0)};
        }
        var root=Object.Instantiate(faces.stepPrefab.gameObject);Unpack(root);ConfigureFaceSelector(root.GetComponent<FaceCustomizationStep>());
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Pearl+"Prefabs/PearlFaceSelection.prefab");faces.stepPrefab=prefab.GetComponent<FaceCustomizationStep>();Object.DestroyImmediate(root);
        EditorUtility.SetDirty(faces);AssetDatabase.SaveAssets();
    }
    static void ConfigureFaceSelector(FaceCustomizationStep step)
    {
        var choices=step.choices;var root=choices.transform;
        // Keep the full check button above the purple backing, including its hit area.
        if(step.confirm){var check=(RectTransform)step.confirm.transform;check.anchoredPosition=new Vector2(580,410);check.sizeDelta=new Vector2(200,200);}
        choices.buttonTemplate.transform.SetParent(root,false);
        var canvasRect=(RectTransform)root;canvasRect.pivot=new Vector2(.5f,.5f);canvasRect.anchorMin=canvasRect.anchorMax=new Vector2(.5f,.5f);canvasRect.sizeDelta=new Vector2(1400,500);
        root.localPosition=new Vector3(0,-17.5f,0);root.localScale=Vector3.one*.013f;
        var oldViewport=root.Find("Face scroll viewport");if(oldViewport)Object.DestroyImmediate(oldViewport.gameObject);
        var oldBacking=root.Find("Purple face backing");if(oldBacking)Object.DestroyImmediate(oldBacking.gameObject);
        var backing=new GameObject("Purple face backing",typeof(RectTransform));backing.transform.SetParent(root,false);backing.transform.SetAsFirstSibling();
        var l=PanelPiece("UI-panel1",6,764);var m=PanelPiece("UI-panel2",11,768);var r=PanelPiece("UI-panel3",6,763);
        float height=480,left=height*l.rect.width/l.rect.height,right=height*r.rect.width/r.rect.height;
        UiImage(backing.transform,"Left cap",l,new Vector2(-700+left/2,0),new Vector2(left,height));
        var middle=UiImage(backing.transform,"Middle",m,new Vector2((left-right)/2,0),new Vector2(1400-left-right+2,height));middle.preserveAspect=false;
        UiImage(backing.transform,"Right cap",r,new Vector2(700-right/2,0),new Vector2(right,height));
        var viewport=new GameObject("Face scroll viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect),typeof(FaceScrollHint));viewport.transform.SetParent(root,false);
        var rect=(RectTransform)viewport.transform;rect.sizeDelta=new Vector2(1200,400);viewport.GetComponent<Image>().color=Color.clear;
        choices.row=new GameObject("Face scroll content",typeof(RectTransform)).GetComponent<RectTransform>();choices.row.SetParent(viewport.transform,false);choices.row.anchorMin=choices.row.anchorMax=new Vector2(0,.5f);choices.row.pivot=new Vector2(0,.5f);choices.row.sizeDelta=new Vector2(4800,400);choices.row.anchoredPosition=Vector2.zero;
        var scroll=viewport.GetComponent<ScrollRect>();scroll.viewport=rect;scroll.content=choices.row;scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
        choices.scroll=scroll;choices.scrollHint=viewport.GetComponent<FaceScrollHint>();choices.scrollHint.scroll=scroll;
        var preview=(RectTransform)choices.buttonTemplate.transform.Find("Preview");preview.sizeDelta=new Vector2(230,280);preview.anchoredPosition=Vector2.zero;
        var label=choices.buttonTemplate.transform.Find("Name");if(label)Object.DestroyImmediate(label.gameObject);
    }
    [MenuItem("Squishy Dumpling/Update face check and exaggerated stretch")]
    public static void UpdateCheckAndStretch()
    {
        var faces=AssetDatabase.LoadAssetAtPath<FaceCustomizationDefinition>(Pearl+"Definitions/PearlFaces.asset");
        var root=PrefabUtility.LoadPrefabContents(Pearl+"Prefabs/PearlFaceSelection.prefab");var step=root.GetComponent<FaceCustomizationStep>();
        var check=(RectTransform)step.confirm.transform;check.anchoredPosition=new Vector2(580,410);check.sizeDelta=new Vector2(200,200);
        PrefabUtility.SaveAsPrefabAsset(root,Pearl+"Prefabs/PearlFaceSelection.prefab");PrefabUtility.UnloadPrefabContents(root);
        var catalog=AssetDatabase.LoadAssetAtPath<DumplingFamilyCatalog>(Pearl+"Definitions/DumplingFamilies.asset");
        foreach(var family in catalog.families)foreach(var variant in family.presentation.variants){TuneFinalStretch(variant.deformation);EditorUtility.SetDirty(variant.deformation);}
        AssetDatabase.SaveAssets();
    }
    static void TuneFinalStretch(DumplingDeformationConfiguration d)
    {
        d.grabRadius=8f;d.grabStrength=380;d.influenceFalloff=1f;d.neighborCoupling=90;d.springStiffness=30;
        d.damping=8;d.stretchResistance=.8f;d.maxStretch=7;d.maxStrain=.94f;d.recoveryStrength=1.4f;d.velocityLimit=40;
    }
    [MenuItem("Squishy Dumpling/Refresh menu presentation")]
    public static void RefreshMenuPresentation()
    {
        art.Clear();
        foreach(string path in Directory.GetFiles(Pearl+"Art","*.png"))art[Path.GetFileNameWithoutExtension(path)]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        BuildFamilyMenu(AssetDatabase.LoadAssetAtPath<DumplingFamilyCatalog>(Pearl+"Definitions/DumplingFamilies.asset"));
    }
    [MenuItem("Squishy Dumpling/Install menu basket lids")]
    public static void InstallMenuBasketLids()
    {
        Import("Dumpling_basket-lid",11);
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Menu.unity");
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();
        var parent=menu.template.transform;
        if(!parent.Find("Basket lid"))
            UiImage(parent,"Basket lid",A("basket-lid"),new Vector2(0,-40),new Vector2(1100,650));
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
    static void BuildFamilyMenu(DumplingFamilyCatalog catalog)
    {
        Import("Dumpling_basket-lid",11);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("World Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=24;camera.transform.position=new Vector3(0,0,-10);camera.nearClipPlane=.1f;camera.farClipPlane=100;
        var layout=camera.gameObject.AddComponent<ResponsiveWorldLayout>();
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        var module=new GameObject("Dumpling family menu").AddComponent<DumplingFamilyMenu>();module.catalog=catalog;module.layout=layout;module.loadingScreenPrefab=EnsureLoadingScreenPrefab();
        module.background=Visual(module.transform,"Menu background",A("Menu-BG"),"Background",0,Vector3.zero);
        var root=new GameObject("Family selection",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));root.transform.localScale=Vector3.one*.01f;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.sortingLayerName="UI";((RectTransform)root.transform).sizeDelta=new Vector2(2700,4800);
        var button=new GameObject("Family template",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(root.transform,false);((RectTransform)button.transform).sizeDelta=new Vector2(1100,1300);
        var image=button.GetComponent<Image>();image.color=Color.clear;module.template=button.GetComponent<Button>();module.template.targetGraphic=image;
        UiImage(button.transform,"Basket back",A("basket-back"),new Vector2(0,-130),new Vector2(1100,650));
        UiImage(button.transform,"Body",catalog.families[0].presentation.variants[0].body,new Vector2(0,180),new Vector2(1000,1100));
        UiImage(button.transform,"Basket front",A("basket-front"),new Vector2(0,-320),new Vector2(1100,650));
        UiImage(button.transform,"Face",A("face-3"),new Vector2(0,80),new Vector2(370,320));
        UiImage(button.transform,"Basket lid",A("basket-lid"),new Vector2(0,-40),new Vector2(1100,650));
        button.SetActive(false);ConfigureSceneAudio();EditorSceneManager.SaveScene(scene,Pearl+"Scenes/Menu.unity");
    }
}
