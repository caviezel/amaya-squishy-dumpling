using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static partial class PearlCreationBuilder
{
    [MenuItem("Squishy Dumpling/Repair mobile selection panel sprites")]
    public static void RepairMobileSelectionPanels()
    {
        art.Clear();
        var old=new Sprite[3];var replacement=new Sprite[3];
        int[] tops={6,11,6},bottoms={764,768,763};
        for(int i=0;i<3;i++)
        {
            string name="UI-panel"+(i+1);old[i]=AssetDatabase.LoadAssetAtPath<Sprite>(Pearl+"Definitions/"+name+"-aligned.asset");
            replacement[i]=PanelPiece(name,tops[i],bottoms[i]);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Pearl+"Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
            foreach(var image in root.GetComponentsInChildren<Image>(true))for(int i=0;i<3;i++)if(old[i] && image.sprite==old[i]){image.sprite=replacement[i];changed=true;}
            if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        var scene=EditorSceneManager.OpenScene(Pearl+"Scenes/Gameplay.unity",OpenSceneMode.Single);
        foreach(var root in scene.GetRootGameObjects())foreach(var image in root.GetComponentsInChildren<Image>(true))
            for(int i=0;i<3;i++)if(old[i] && image.sprite==old[i])image.sprite=replacement[i];
        EditorSceneManager.SaveScene(scene);
        for(int i=0;i<3;i++)AssetDatabase.DeleteAsset(Pearl+"Definitions/UI-panel"+(i+1)+"-aligned.asset");
        AssetDatabase.SaveAssets();
    }
}
