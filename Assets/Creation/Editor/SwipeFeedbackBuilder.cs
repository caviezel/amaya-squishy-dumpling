using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SquishyDumpling.Creation;
using Object=UnityEngine.Object;
public static partial class PearlCreationBuilder
{
    [MenuItem("Squishy Dumpling/Install stirring sparkles")]
    public static void InstallStirringFeedback()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<ParticleSystem>(Pearl+"Prefabs/DumplingSwipeSparkles.prefab");
        var effect=Object.Instantiate(prefab);effect.name="Bowl stirring sparkles";
        var main=effect.main;main.startSize=new ParticleSystem.MinMaxCurve(.9f,1.7f);main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(.4f,1.2f);
        var shape=effect.shape;shape.radius=.85f;
        var velocity=effect.velocityOverLifetime;velocity.enabled=true;velocity.x=new ParticleSystem.MinMaxCurve(-.4f,.4f);velocity.y=new ParticleSystem.MinMaxCurve(.8f,1.8f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);
        var colors=effect.colorOverLifetime;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.8f,1),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.4f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var saved=PrefabUtility.SaveAsPrefabAsset(effect.gameObject,Pearl+"Prefabs/BowlStirringSparkles.prefab");Object.DestroyImmediate(effect.gameObject);
        prefab=saved.GetComponent<ParticleSystem>();
        var path=Pearl+"Prefabs/PearlSpatula.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        root.GetComponent<MixingStep>().stirringSparklePrefab=prefab;
        PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();
    }
    [MenuItem("Squishy Dumpling/Install dumpling swipe sparkles")]
    public static void InstallSwipeFeedback()
    {
        art.Clear();art["Dumpling_sparkle_01-1"]=AssetDatabase.LoadAssetAtPath<Sprite>(Pearl+"Art/Dumpling_sparkle_01-1.png");
        var fx=CreateSparkles(null,"Dumpling swipe sparkles");
        var main=fx.main;main.loop=true;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.5f);main.startSize=new ParticleSystem.MinMaxCurve(.4f,.95f);main.startSpeed=new ParticleSystem.MinMaxCurve(.2f,.8f);main.maxParticles=256;main.scalingMode=ParticleSystemScalingMode.Shape;
        var emission=fx.emission;emission.rateOverTime=0;emission.rateOverDistance=0;emission.SetBursts(new ParticleSystem.Burst[0]);
        var shape=fx.shape;shape.radius=.14f;shape.radiusThickness=1;
        var velocity=fx.velocityOverLifetime;velocity.enabled=false;
        var size=fx.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
        fx.GetComponent<ParticleSystemRenderer>().sortingOrder=40;
        var prefab=PrefabUtility.SaveAsPrefabAsset(fx.gameObject,Pearl+"Prefabs/DumplingSwipeSparkles.prefab");Object.DestroyImmediate(fx.gameObject);
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Gameplay.unity",OpenSceneMode.Single);
        var presentation=Object.FindObjectOfType<PearlCreationPresentation>();presentation.swipeFxPrefab=prefab.GetComponent<ParticleSystem>();EditorUtility.SetDirty(presentation);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
}
