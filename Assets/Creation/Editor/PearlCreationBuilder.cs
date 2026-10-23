using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SquishyDumpling.Creation;
using SquishyDumpling.Layout;
using Object=UnityEngine.Object;
public static partial class PearlCreationBuilder
{
    const string Root="Assets/Creation/",Pearl="Assets/GameContent/";
    static readonly Dictionary<string,Sprite> art=new Dictionary<string,Sprite>();
    [MenuItem("Squishy Dumpling/Refresh family menu and gameplay content")]
    public static void Build()=>BuildFamilies();
    static ParticleSystem CreateSparkles(Transform parent,string name)
    {
        var fxObject=new GameObject(name);fxObject.transform.SetParent(parent,false);var fx=fxObject.AddComponent<ParticleSystem>();fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=fx.main;main.loop=false;main.playOnAwake=false;main.duration=.8f;main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.05f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.7f,1.5f);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=64;
        var emission=fx.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,36)});
        var shape=fx.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=6.5f;shape.radiusThickness=.25f;
        var velocity=fx.velocityOverLifetime;velocity.enabled=true;velocity.x=new ParticleSystem.MinMaxCurve(-2,2);velocity.y=new ParticleSystem.MinMaxCurve(1,4);velocity.z=new ParticleSystem.MinMaxCurve(0,0);
        var colors=fx.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.7f,1),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var sheet=fx.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Sprites;sheet.AddSprite(A("sparkle_01-1"));
        string materialPath=Pearl+"Definitions/CompletionSparkle.mat";var particleMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!particleMaterial){particleMaterial=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(particleMaterial,materialPath);}particleMaterial.mainTexture=A("sparkle_01-1").texture;EditorUtility.SetDirty(particleMaterial);
        var fxRenderer=fxObject.GetComponent<ParticleSystemRenderer>();fxRenderer.sharedMaterial=particleMaterial;fxRenderer.sortingLayerName="WorldEffects";fxRenderer.sortingOrder=20;return fx;
    }
    [MenuItem("Squishy Dumpling/Update Pearl bowl pour presentation")]
    public static void UpdateBowlPourPresentation()
    {
        art.Clear();art["Dumpling_sparkle_01-1"]=AssetDatabase.LoadAssetAtPath<Sprite>(Pearl+"Art/Dumpling_sparkle_01-1.png");
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Gameplay.unity",OpenSceneMode.Single);
        ConfigureBowlPour(Object.FindObjectOfType<CreationFlowController>().bowl);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static void ConfigureBowlPour(BowlPresenter bowl)
    {
        // Contacts are authored on each sprite's visible upper surface, centred horizontally.
        bowl.contributionContactPoints=new[]{new Vector2(0,3.83f),new Vector2(0,3.68f),new Vector2(0,4.13f)};
        if(bowl.powderPuff){bowl.powderPuff.enabled=false;bowl.powderPuff=null;}
        if(bowl.pourCompletionFx)Object.DestroyImmediate(bowl.pourCompletionFx.gameObject);
        var fx=CreateSparkles(bowl.transform,"Ingredient completion sparkles");
        var main=fx.main;main.maxParticles=96;main.startLifetime=new ParticleSystem.MinMaxCurve(.5f,.9f);main.startSize=new ParticleSystem.MinMaxCurve(.85f,1.6f);
        var emission=fx.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,36),new ParticleSystem.Burst(.12f,18)});
        var shape=fx.shape;shape.radius=9.5f;shape.scale=new Vector3(1,.75f,1);shape.radiusThickness=.32f;
        var size=fx.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.2f));
        bowl.pourCompletionFx=fx;bowl.completionFxLayerCount=3;
    }
    static void AnchorVisibleBase(string name,float pivotY)
    {
        string path=Pearl+"Art/Dumpling_"+name+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,pivotY);
        importer.SetTextureSettings(settings);importer.SaveAndReimport();
        art["Dumpling_"+name]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void RebuildBowlArt(CreationFlowController flow,PearlRecipeDefinition data)
    {
        var bowl=flow.bowl;
        var oldLayers=bowl.contributionLayers;
        var renderers=bowl.visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach(var sr in renderers)sr.enabled=false;
        // Retain shared anchors/components but replace all visible geometry.
        var rear=Visual(bowl.visualRoot,"Client Bowl Back",A("bowl-back"),"GameplayBack",0,Vector3.zero);
        Visual(bowl.visualRoot,"Client Bowl Front",A("bowl-front"),"GameplayFront",10,new Vector3(0,(A("bowl-front").bounds.size.y-A("bowl-back").bounds.size.y)/2,0));
        Visual(bowl.visualRoot,"Client Bowl Shadow",A("bowl-shadow"),"GameplayBack",-2,new Vector3(0,-rear.sprite.bounds.extents.y,0));
        var group=bowl.GetComponent<UnityEngine.Rendering.SortingGroup>();if(group)Object.DestroyImmediate(group);
        bowl.contentArea.localPosition=new Vector3(0,-.7f,0);
        foreach(var mask in bowl.GetComponentsInChildren<SpriteMask>()){mask.sprite=A("bowl-back");mask.transform.localPosition=new Vector3(0,.7f,0);mask.transform.localScale=Vector3.one;mask.alphaCutoff=.001f;mask.isCustomRangeActive=true;mask.backSortingLayerID=mask.frontSortingLayerID=SortingLayer.NameToID("Gameplay");mask.backSortingOrder=-1;mask.frontSortingOrder=9;}
        string[] sprites={"CLEAR-GLUE-in-the-bowl","SQUISHY-POWDER-in-a-bowl","ACTIVATOR-in-the-bowl","color-preal1-in-the-bowl"};
        bowl.contributionLayers=new SpriteRenderer[4];
        for(int i=0;i<4;i++){var sr=Visual(bowl.contentArea,"Client contribution "+i,A(sprites[i]),"Gameplay",i,Vector3.zero);sr.maskInteraction=SpriteMaskInteraction.VisibleInsideMask;if(i<3)sr.transform.localPosition=new Vector3(0,-4.45f,0);sr.enabled=false;bowl.contributionLayers[i]=sr;}
        bowl.contents=bowl.contributionLayers[0];if(bowl.powderPuff){bowl.powderPuff.sprite=A("SQUISHY-POWDER-in-a-bowl");bowl.powderPuff.transform.localScale=Vector3.one;}
        flow.mixingSurface.unmixedLayers=bowl.contributionLayers;flow.mixingSurface.artStages=new SpriteRenderer[3];
        for(int i=0;i<3;i++){var sr=Visual(bowl.contentArea,"Pearl mixing state "+i,data.variants[0].mixing[i],"Gameplay",5+i,Vector3.zero);sr.maskInteraction=SpriteMaskInteraction.VisibleInsideMask;sr.enabled=false;flow.mixingSurface.artStages[i]=sr;}
        flow.mixingSurface.mixedLayer=flow.mixingSurface.artStages[2];
    }
    static GameObject IngredientPrefab(IngredientDefinition definition,GameObject original,string name,Sprite lid)
    {
        var root=Object.Instantiate(original);Unpack(root);root.name=name;
        var p=root.GetComponent<IngredientPresenter>();p.definition=definition;p.container.sprite=definition.containerSprite;p.container.transform.localScale=Vector3.one;p.container.transform.localPosition=Vector3.zero;
        p.content.sprite=definition.containerSprite;p.content.enabled=false;p.visualRoot.localScale=Vector3.one;p.stream.authoredWidth=definition.presentationType==IngredientPresentationType.PowderPour?.5f:.24f;
        float h=definition.containerSprite.bounds.size.y;p.containerMouth.localPosition=new Vector3(0,h/2-.12f,0);
        var actor=root.GetComponentInChildren<IngredientActor>();actor.dropContact.localPosition=new Vector3(0,-Mathf.Min(h*.35f,1.5f),0);actor.dragHalfExtents=new Vector2(definition.containerSprite.bounds.extents.x,h/2);
        var collider=actor.GetComponent<BoxCollider2D>();collider.offset=Vector2.zero;collider.size=new Vector2(Mathf.Max(2,definition.containerSprite.bounds.size.x),h);
        var cap=Visual(p.visualRoot,"Client removable lid",lid,"GameplayFront",21,new Vector3(0,h*.36f,0));
        cap.transform.localScale=Vector3.one;root.AddComponent<PearlContainerLid>().BindAuthoring(p,cap);
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Pearl+"Prefabs/"+name+".prefab");Object.DestroyImmediate(root);return asset;
    }
    static FormingStep FormingPrefab(FormingDefinition definition,CreationComposition composition)
    {
        var original=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/FirstDumplingForming.prefab");var root=Object.Instantiate(original);Unpack(root);root.name="Pearl Funnel Forming";
        var step=root.GetComponent<FormingStep>();step.definition=definition;
        for(int i=0;i<step.material.states.Length;i++){step.material.states[i].sprite=definition.shapeStates[i];step.material.states[i].transform.localScale=Vector3.one;}
        step.material.definition=definition;step.material.contactMark.sprite=A("flow-1");step.carriedMixture.sprite=definition.shapeStates[0];
        var presentation=root.AddComponent<FunnelFormingPresentation>();step.funnelPresentation=presentation;presentation.composition=composition;
        step.workspace.sprite=A("packet");step.workspace.transform.localScale=Vector3.one;
        presentation.pouch=step.workspace;presentation.funnel=Visual(root.transform,"Funnel",A("funnel"),"GameplayFront",11,Vector3.zero);
        presentation.fill=Visual(step.workspace.transform,"Pouch fill",definition.shapeStates[0],"GameplayBack",-2,new Vector3(0,-.3f,0));presentation.fill.maskInteraction=SpriteMaskInteraction.VisibleInsideMask;
        var mask=step.workspace.gameObject.AddComponent<SpriteMask>();mask.sprite=A("packet");mask.alphaCutoff=.001f;mask.isCustomRangeActive=true;mask.frontSortingLayerID=SortingLayer.NameToID("GameplayBack");mask.frontSortingOrder=0;mask.backSortingLayerID=SortingLayer.NameToID("GameplayBack");mask.backSortingOrder=-3;presentation.pouchMask=mask;
        presentation.upperStream=Visual(root.transform,"Bowl to funnel flow",A("flow-1"),"GameplayFront",14,Vector3.zero);
        presentation.lowerStream=Visual(root.transform,"Funnel to pouch flow",A("flow-1"),"GameplayFront",14,Vector3.zero);
        presentation.funnelFill=Visual(root.transform,"Funnel contents",definition.shapeStates[0],"GameplayFront",12,Vector3.zero);presentation.funnelFill.maskInteraction=SpriteMaskInteraction.VisibleInsideMask;
        var maskObject=new GameObject("Funnel interior mask");maskObject.transform.SetParent(presentation.funnel.transform,false);maskObject.transform.localPosition=Vector3.up*1.25f;var interiorMask=AssetDatabase.LoadAssetAtPath<Sprite>(Pearl+"Art/Utility/BowlInteriorMask.png");maskObject.transform.localScale=Vector3.one*(A("funnel").bounds.size.x*.745f/interiorMask.bounds.size.x);
        var funnelMask=maskObject.AddComponent<SpriteMask>();funnelMask.sprite=interiorMask;funnelMask.alphaCutoff=.01f;funnelMask.isCustomRangeActive=true;funnelMask.backSortingLayerID=funnelMask.frontSortingLayerID=SortingLayer.NameToID("GameplayFront");funnelMask.backSortingOrder=11;funnelMask.frontSortingOrder=13;presentation.funnelMask=funnelMask;
        presentation.completionFx=CreateSparkles(root.transform,"Forming completion sparkles");
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Pearl+"Prefabs/PearlForming.prefab");Object.DestroyImmediate(root);return asset.GetComponent<FormingStep>();
    }
    static MixingStep ToolPrefab()
    {
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/MixingTool.prefab"));Unpack(root);root.name="Pearl Spatula";var step=root.GetComponent<MixingStep>();
        foreach(var sr in step.visual.GetComponentsInChildren<SpriteRenderer>())Object.DestroyImmediate(sr.gameObject);
        var sprite=A("spatula");var tilt=Quaternion.Euler(0,0,-22);
        var art=Visual(step.visual,"Client spatula",sprite,"GameplayFront",20,tilt*Vector3.up*(sprite.bounds.size.y*.5f)-Vector3.up*.8f);art.transform.localRotation=tilt;
        // Cover the actual tilted artwork, including the enlarged handle and blade.
        Vector2 minimum=Vector2.one*float.PositiveInfinity,maximum=Vector2.one*float.NegativeInfinity;
        foreach(var corner in new[]{new Vector2(sprite.bounds.min.x,sprite.bounds.min.y),new Vector2(sprite.bounds.min.x,sprite.bounds.max.y),new Vector2(sprite.bounds.max.x,sprite.bounds.min.y),new Vector2(sprite.bounds.max.x,sprite.bounds.max.y)})
        {Vector2 point=root.transform.InverseTransformPoint(art.transform.TransformPoint(corner));minimum=Vector2.Min(minimum,point);maximum=Vector2.Max(maximum,point);}
        var collider=root.GetComponent<BoxCollider2D>();collider.offset=(minimum+maximum)*.5f;collider.size=maximum-minimum+Vector2.one*.6f;
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Pearl+"Prefabs/PearlSpatula.prefab");Object.DestroyImmediate(root);return asset.GetComponent<MixingStep>();
    }
    static FaceCustomizationStep FacesPrefab(FaceCustomizationDefinition definition,PearlRecipeDefinition data)
    {
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/FirstDumplingCustomization.prefab"));Unpack(root);root.name="Pearl Face Selection";var step=root.GetComponent<FaceCustomizationStep>();
        Object.DestroyImmediate(step.stamp.gameObject);step.stamp=null;
        step.choices.GetComponent<RectTransform>().sizeDelta=new Vector2(1400,300);step.choices.transform.localPosition=new Vector3(0,-16,0);
        step.choices.buttonTemplate.GetComponent<Image>().sprite=data.choiceFrame;
        var parent=step.choices.transform;var confirm=new GameObject("Confirm face",typeof(RectTransform),typeof(Image),typeof(Button));confirm.transform.SetParent(parent,false);
        var rect=(RectTransform)confirm.transform;rect.sizeDelta=new Vector2(230,230);rect.anchoredPosition=new Vector2(580,410);
        var image=confirm.GetComponent<Image>();image.sprite=data.confirmSprite;image.preserveAspect=true;step.confirm=confirm.GetComponent<Button>();step.confirm.targetGraphic=image;
        ConfigureFaceSelector(step);
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Pearl+"Prefabs/PearlFaceSelection.prefab");Object.DestroyImmediate(root);return asset.GetComponent<FaceCustomizationStep>();
    }
    static void BuildSelector(PearlCreationPresentation module,PearlRecipeDefinition data)
    {
        var root=new GameObject("Pearl dye choices",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(UnityEngine.UI.GraphicRaycaster));root.transform.SetParent(module.transform,false);root.transform.localPosition=new Vector3(0,-16,0);root.transform.localScale=Vector3.one*.01f;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingLayerName="UI";((RectTransform)root.transform).sizeDelta=new Vector2(1400,400);
        module.selectionCanvas=canvas;module.selectionGroup=root.GetComponent<CanvasGroup>();
        var b=new GameObject("Pearl choice template",typeof(RectTransform),typeof(Image),typeof(Button));b.transform.SetParent(root.transform,false);((RectTransform)b.transform).sizeDelta=new Vector2(280,320);var image=b.GetComponent<Image>();image.sprite=data.choiceFrame;
        module.choiceTemplate=b.GetComponent<Button>();module.choiceTemplate.targetGraphic=image;
        var preview=new GameObject("Preview",typeof(RectTransform),typeof(Image));preview.transform.SetParent(b.transform,false);((RectTransform)preview.transform).sizeDelta=new Vector2(170,230);((RectTransform)preview.transform).anchoredPosition=new Vector2(0,25);preview.GetComponent<Image>().preserveAspect=true;preview.GetComponent<Image>().raycastTarget=false;
        var label=new GameObject("Name",typeof(RectTransform),typeof(Text));label.transform.SetParent(b.transform,false);((RectTransform)label.transform).sizeDelta=new Vector2(270,60);((RectTransform)label.transform).anchoredPosition=new Vector2(0,-120);var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=34;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.3f,.1f,.4f);text.raycastTarget=false;b.SetActive(false);
    }
    static void BuildProgress(CreationFlowController flow)
    {
        var hud=Object.FindObjectOfType<CreationProofHud>();hud.enabled=false;hud.status.gameObject.SetActive(false);
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=flow.layout.GetComponent<Camera>();canvas.planeDistance=1;canvas.sortingLayerName="UI";
        var view=hud.gameObject.AddComponent<CreationProgressView>();view.flow=flow;view.safeRoot=hud.safeRoot;
        view.current=A("UI-progress-current");view.completed=A("UI-progress-completed");view.next=A("UI-progress-next");view.markers=new Image[6];
        var row=new GameObject("Creation milestones 1-6",typeof(RectTransform));row.transform.SetParent(hud.safeRoot,false);view.row=row;
        var rr=(RectTransform)row.transform;rr.anchorMin=rr.anchorMax=new Vector2(.5f,1);rr.anchoredPosition=new Vector2(0,-210);rr.sizeDelta=new Vector2(520,75);
        for(int i=0;i<6;i++)
        {
            if(i<5){var line=UiImage(row.transform,"Connector "+i,A("UI-progress-line"),new Vector2((i-2)*84,0),new Vector2(8,40));line.rectTransform.localRotation=Quaternion.Euler(0,0,90);}
            var marker=UiImage(row.transform,"Milestone "+(i+1),view.next,new Vector2((i-2.5f)*84,0),new Vector2(64,64));view.markers[i]=marker;
            var label=new GameObject("Number",typeof(RectTransform),typeof(Text));label.transform.SetParent(marker.transform,false);var rect=(RectTransform)label.transform;rect.sizeDelta=new Vector2(64,64);
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=42;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.text=(i+1).ToString();text.raycastTarget=false;
        }
        var restart=hud.GetComponentInChildren<Button>();var r=(RectTransform)restart.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(30,-30);r.sizeDelta=new Vector2(72,72);
        restart.GetComponent<Image>().sprite=A("UI-Button-base");restart.GetComponent<Image>().color=Color.white;restart.GetComponent<Image>().preserveAspect=true;
        restart.GetComponentInChildren<Text>().text="↻";restart.GetComponentInChildren<Text>().fontSize=50;
    }
    static Image UiImage(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.rectTransform.anchoredPosition=position;image.rectTransform.sizeDelta=size;return image;
    }
    static void Import(string name,float width,int maximum=2048,bool ui=false)
    {
        string src="Docs/ClientAssets/Originals/"+(ui?"UI/":"")+name+".png",path=Pearl+"Art/"+name+".png";
        if(File.Exists(path))
        {
            var cached=AssetDatabase.LoadAssetAtPath<Sprite>(path);var cachedImporter=(TextureImporter)AssetImporter.GetAtPath(path);
            if(cached && cachedImporter.maxTextureSize==maximum && Mathf.Abs(cached.bounds.size.x-width)<.001f){art[name]=cached;return;}
        }
        if(!File.Exists(path))File.Copy(src,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;if(!name.StartsWith("Dumpling_UI-panel"))importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.maxTextureSize=maximum;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);importer.spritePixelsPerUnit=sprite.rect.width/width;importer.SaveAndReimport();art[name]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Unpack(GameObject root)
    {var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(root);if(instance)PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);}
    static Sprite A(string suffix)=>art["Dumpling_"+suffix];
    static T LoadOrCreate<T>(string path)where T:ScriptableObject
    {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;}
    static T CloneAsset<T>(T original,string path)where T:Object
    {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=Object.Instantiate(original);AssetDatabase.CreateAsset(asset,path);}return asset;}
    static SpriteRenderer Visual(Transform parent,string name,Sprite sprite,string layer,int order,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingLayerName=layer;sr.sortingOrder=order;return sr;}
    static Vector2[] Outline(string suffix,Sprite sprite)
    {
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes("Docs/ClientAssets/Originals/Dumpling_"+suffix+".png"));var pixels=texture.GetPixels32();var result=new Vector2[64];Vector2 center=new Vector2(texture.width*.5f,texture.height*.45f);
        for(int i=0;i<64;i++){Vector2 direction=new Vector2(Mathf.Cos(i*Mathf.PI/32),Mathf.Sin(i*Mathf.PI/32)),last=center;for(float d=0;d<texture.width;d+=1){Vector2 p=center+direction*d;int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);if(x<0||y<0||x>=texture.width||y>=texture.height||pixels[y*texture.width+x].a<100)break;last=p;}result[i]=new Vector2((last.x/texture.width-.5f)*sprite.bounds.size.x,(last.y/texture.height-.5f)*sprite.bounds.size.y);}
        Object.DestroyImmediate(texture);return result;
    }
}
