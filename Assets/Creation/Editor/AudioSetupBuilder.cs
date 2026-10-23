using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SquishyDumpling.Creation;
using Object=UnityEngine.Object;
public static partial class PearlCreationBuilder
{
    const string AudioLibraryPath="Assets/Audio/GameAudioLibrary.asset";
    [MenuItem("Squishy Dumpling/Install shared audio and menu settings")]
    public static void InstallAudio()
    {
        EditorSceneManager.OpenScene(Pearl+"Scenes/Menu.unity",OpenSceneMode.Single);ConfigureSceneAudio();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.OpenScene(Pearl+"Scenes/Gameplay.unity",OpenSceneMode.Single);ConfigureSceneAudio();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
    [MenuItem("Squishy Dumpling/Refresh settings presentation")]
    public static void RefreshSettingsPresentation()
    {
        art.Clear();
        EditorSceneManager.OpenScene(Pearl+"Scenes/Menu.unity",OpenSceneMode.Single);
        BuildAudioSettings(Object.FindObjectOfType<DumplingFamilyMenu>());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
    static void ConfigureSceneAudio()
    {
        Directory.CreateDirectory("Assets/Audio/SFX");Directory.CreateDirectory("Assets/Audio/BGM");AssetDatabase.Refresh();
        var library=LoadOrCreate<GameAudioLibrary>(AudioLibraryPath);var player=Object.FindObjectOfType<GameAudioPlayer>();
        if(!player)player=new GameObject("Scene Audio").AddComponent<GameAudioPlayer>();player.library=library;EditorUtility.SetDirty(player);
        if(!Object.FindObjectOfType<AudioListener>())Camera.main.gameObject.AddComponent<AudioListener>();
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();if(menu)BuildAudioSettings(menu);
    }
    static void BuildAudioSettings(DumplingFamilyMenu menu)
    {
        foreach(string name in new[]{"UI-Button-base","UI-Button-settings","UI-Button-exit","UI-Button-music-on","UI-Button-music-off","UI-Button-sounds-on","UI-Button-sounds-off","UI-Pop-Up-setings","UI-Pop-Up-bg-shadow-30%"})Import("Dumpling_"+name,2.5f,2048,true);
        var parent=menu.template.GetComponentInParent<Canvas>().transform;var old=parent.Find("Audio settings");if(old)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Audio settings",typeof(RectTransform),typeof(AudioSettingsPopup));root.transform.SetParent(parent,false);var controller=root.GetComponent<AudioSettingsPopup>();
        controller.settingsButton=AudioButton(root.transform,"Settings",A("UI-Button-base"),new Vector2(-1150,2220),new Vector2(300,300));
        UiImage(controller.settingsButton.transform,"Settings icon",A("UI-Button-settings"),Vector2.zero,new Vector2(200,200));
        var popup=new GameObject("Settings popup",typeof(RectTransform));popup.transform.SetParent(root.transform,false);controller.popup=popup;
        controller.backdrop=AudioButton(popup.transform,"Modal backdrop",null,Vector2.zero,new Vector2(10000,15000));controller.backdrop.image.preserveAspect=false;
        controller.backdrop.image.color=new Color(.12f,.05f,.2f,.5f);controller.backdrop.transition=Selectable.Transition.None;
        var panelSprite=A("UI-Pop-Up-setings");
        var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(panelSprite));importer.spriteBorder=new Vector4(150,150,150,150);importer.SaveAndReimport();
        var panel=UiImage(popup.transform,"Panel",AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(panelSprite)),new Vector2(0,1000),new Vector2(1400,820));panel.raycastTarget=true;panel.type=Image.Type.Sliced;panel.preserveAspect=false;panel.pixelsPerUnitMultiplier=100/panel.sprite.pixelsPerUnit;
        UiImage(popup.transform,"BGM button base",A("UI-Button-base"),new Vector2(-290,1000),new Vector2(440,440));
        UiImage(popup.transform,"SFX button base",A("UI-Button-base"),new Vector2(290,1000),new Vector2(440,440));
        controller.musicButton=AudioButton(popup.transform,"Toggle BGM",A("UI-Button-music-on"),new Vector2(-290,1000),new Vector2(280,280));
        controller.sfxButton=AudioButton(popup.transform,"Toggle SFX",A("UI-Button-sounds-on"),new Vector2(290,1000),new Vector2(280,280));
        controller.closeButton=AudioButton(popup.transform,"Close settings",A("UI-Button-exit"),new Vector2(650,1380),new Vector2(300,300));
        controller.musicOn=A("UI-Button-music-on");controller.musicOff=A("UI-Button-music-off");controller.sfxOn=A("UI-Button-sounds-on");controller.sfxOff=A("UI-Button-sounds-off");popup.SetActive(false);
    }
    static Button AudioButton(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size)
    {var image=UiImage(parent,name,sprite,position,size);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;return button;}
}
