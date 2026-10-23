using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SquishyDumpling.Creation;
using Object=UnityEngine.Object;
public static partial class PearlCreationBuilder
{
    const string LoadingPrefabPath="Assets/GameContent/Prefabs/LoadingScreen.prefab";
    static readonly Color Purple=new Color(.59f,.32f,.87f);
    [MenuItem("Squishy Dumpling/Install editable loading screen")]
    public static void InstallEditableLoadingScreen()
    {
        var prefab=EnsureLoadingScreenPrefab();
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Menu.unity",OpenSceneMode.Single);
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();menu.loadingScreenPrefab=prefab;EditorUtility.SetDirty(menu);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static GameplayLoadingTransition EnsureLoadingScreenPrefab()
    {
        var existing=AssetDatabase.LoadAssetAtPath<GameplayLoadingTransition>(LoadingPrefabPath);if(existing)return existing;
        var root=new GameObject("LoadingScreen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameplayLoadingTransition));
        var view=root.GetComponent<GameplayLoadingTransition>();
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32767;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var background=LoadingRect("Loading background",root.transform,Vector2.zero,Vector2.zero);background.anchorMin=Vector2.zero;background.anchorMax=Vector2.one;background.offsetMin=background.offsetMax=Vector2.zero;
            background.gameObject.AddComponent<Image>().color=Color.white;
            var content=LoadingRect("Loading content",root.transform,Vector2.zero,new Vector2(0,480));content.anchorMin=new Vector2(.12f,.5f);content.anchorMax=new Vector2(.88f,.5f);
            var track=LoadingRect("Progress track",content,Vector2.zero,new Vector2(0,22),true);
            var trackGraphic=track.gameObject.AddComponent<LoadingRoundedRect>();trackGraphic.color=new Color(.94f,.91f,.98f);trackGraphic.raycastTarget=false;
            view.fill=LoadingRect("Progress fill",track,Vector2.zero,Vector2.zero);view.fill.anchorMin=Vector2.zero;view.fill.anchorMax=Vector2.one;view.fill.offsetMin=view.fill.offsetMax=Vector2.zero;
            var fillGraphic=view.fill.gameObject.AddComponent<LoadingRoundedRect>();fillGraphic.color=Purple;fillGraphic.raycastTarget=false;

        var prefab=PrefabUtility.SaveAsPrefabAsset(root,LoadingPrefabPath);Object.DestroyImmediate(root);
        return prefab.GetComponent<GameplayLoadingTransition>();
    }
    static RectTransform LoadingRect(string name,Transform parent,Vector2 position,Vector2 size,bool stretch=false)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=new Vector2(stretch?0:.5f,.5f);rect.anchorMax=new Vector2(stretch?1:.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
}
