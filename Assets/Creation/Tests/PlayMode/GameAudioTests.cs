using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using SquishyDumpling.Creation;
using Object=UnityEngine.Object;
public class GameAudioTests
{
    bool hadMusic,hadSfx;int previousMusic,previousSfx;float listenerVolume;
    AudioClip clip;GameAudioLibrary runtime;
    [UnitySetUp]public IEnumerator Setup()
    {
        hadMusic=PlayerPrefs.HasKey(GameAudioPlayer.MusicKey);hadSfx=PlayerPrefs.HasKey(GameAudioPlayer.SfxKey);
        previousMusic=PlayerPrefs.GetInt(GameAudioPlayer.MusicKey);previousSfx=PlayerPrefs.GetInt(GameAudioPlayer.SfxKey);
        PlayerPrefs.SetInt(GameAudioPlayer.MusicKey,1);PlayerPrefs.SetInt(GameAudioPlayer.SfxKey,1);
        listenerVolume=AudioListener.volume;AudioListener.volume=0;
        SceneManager.LoadScene(DumplingFamilyMenu.MenuScene);yield return null;yield return null;
        clip=AudioClip.Create("Silent audio lifecycle test",44100,1,44100,false);
        runtime=ScriptableObject.CreateInstance<GameAudioLibrary>();runtime.menuBgm=runtime.gameplayBgm=runtime.liquidPourLoop=runtime.ingredientAppear=runtime.finalReveal=clip;
        GameAudioPlayer.Current.library=runtime;GameAudioPlayer.Current.RefreshMusic();
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        if(GameAudioPlayer.Current)GameAudioPlayer.Current.StopGameplaySounds();
        SceneManager.LoadScene(DumplingFamilyMenu.MenuScene);yield return null;
        if(runtime)Object.Destroy(runtime);if(clip)Object.Destroy(clip);
        if(hadMusic)PlayerPrefs.SetInt(GameAudioPlayer.MusicKey,previousMusic);else PlayerPrefs.DeleteKey(GameAudioPlayer.MusicKey);
        if(hadSfx)PlayerPrefs.SetInt(GameAudioPlayer.SfxKey,previousSfx);else PlayerPrefs.DeleteKey(GameAudioPlayer.SfxKey);
        PlayerPrefs.Save();GameAudioPlayer.Current.ApplySettings();AudioListener.volume=listenerVolume;Time.timeScale=1;
    }
    [UnityTest]public IEnumerator BasketLidsRevealInOrderAndGateSelection()
    {
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();
        Assert.AreEqual(4,menu.Buttons.Length);
        foreach(var button in menu.Buttons)
        {
            Assert.IsFalse(button.interactable);
            Assert.Less(button.transform.Find("Body").localScale.x,.5f);
            Assert.Greater(button.transform.Find("Basket lid").GetComponent<UnityEngine.UI.Image>().color.a,.95f);
        }
        Assert.IsFalse(menu.Choose("pearl"),"Closed baskets must reject navigation, including direct calls");
        int ready=0;float deadline=Time.realtimeSinceStartup+6;
        while(ready<4 && Time.realtimeSinceStartup<deadline)
        {
            ready=0;bool foundClosed=false;
            for(int i=0;i<4;i++)
            {
                var button=menu.Buttons[i];
                if(!button.interactable){foundClosed=true;continue;}
                Assert.IsFalse(foundClosed,"Baskets must become tappable in reading order");ready++;
                Assert.AreEqual(0,button.transform.Find("Basket lid").GetComponent<UnityEngine.UI.Image>().color.a,.001f);
                Assert.AreEqual(Vector3.one,button.transform.Find("Body").localScale);
                Assert.AreEqual(Vector3.one,button.transform.Find("Face").localScale);
            }
            yield return null;
        }
        Assert.AreEqual(4,ready,"Every revealed basket must become tappable");
        menu.PauseHints=true;Assert.IsFalse(menu.Choose("pearl"),"Settings must block direct navigation too");
        menu.PauseHints=false;
        SceneManager.LoadScene(DumplingFamilyMenu.MenuScene);yield return null;yield return null;
        menu=Object.FindObjectOfType<DumplingFamilyMenu>();
        foreach(var button in menu.Buttons)Assert.IsFalse(button.interactable,"Re-entering Menu must reset the reveals");
    }
    [UnityTest]public IEnumerator SettingsAreIndependentPersistentAndScenePlayersCleanUp()
    {
        var player=GameAudioPlayer.Current;var settings=Object.FindObjectOfType<AudioSettingsPopup>();Assert.IsFalse(settings.popup.activeSelf);
        settings.settingsButton.onClick.Invoke();Assert.IsTrue(settings.popup.activeSelf);yield return null;Canvas.ForceUpdateCanvases();
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();var camera=menu.layout.WorldCamera;
        var hits=new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(menu.Buttons[0].transform.position)},hits);
        Assert.IsNotEmpty(hits);Assert.IsFalse(hits[0].gameObject.transform.IsChildOf(menu.Buttons[0].transform),"Modal settings must block the family choices");
        settings.musicButton.onClick.Invoke();Assert.IsFalse(GameAudioPlayer.MusicEnabled);Assert.IsTrue(GameAudioPlayer.SfxEnabled);Assert.IsTrue(player.Music.mute);Assert.IsFalse(player.Effects.mute);
        Assert.AreSame(settings.musicOff,settings.musicButton.image.sprite);
        player.SetLoop(GameSound.LiquidLoop,true,settings);Assert.AreSame(clip,player.Loop.clip);
        settings.sfxButton.onClick.Invoke();Assert.IsFalse(GameAudioPlayer.SfxEnabled);Assert.IsNull(player.Loop.clip);Assert.IsTrue(player.Effects.mute);
        settings.musicButton.onClick.Invoke();Assert.IsTrue(GameAudioPlayer.MusicEnabled);Assert.IsFalse(GameAudioPlayer.SfxEnabled);
        settings.closeButton.onClick.Invoke();Assert.IsFalse(settings.popup.activeSelf);
        Assert.IsNotNull(menu.loadingScreenPrefab);Assert.IsTrue(menu.loadingScreenPrefab.IsConfigured);
        // A designer-edited prefab must be cloned as authored, including an inactive template root.
        var authored=Object.Instantiate(menu.loadingScreenPrefab);authored.gameObject.SetActive(false);
        var authoredColor=new Color(.98f,.96f,1,1);
        authored.transform.Find("Loading background").GetComponent<UnityEngine.UI.Image>().color=authoredColor;
        menu.loadingScreenPrefab=authored;
        while(!menu.Buttons[0].interactable)yield return null;
        Assert.IsTrue(menu.Choose("pearl"));Assert.IsFalse(menu.Choose("glitter"),"Repeated taps must not start another load");
        Assert.IsTrue(GameplayLoadingTransition.IsLoading);var transition=Object.FindObjectOfType<GameplayLoadingTransition>();Assert.IsNotNull(transition);
        Assert.AreEqual(authoredColor,transition.transform.Find("Loading background").GetComponent<UnityEngine.UI.Image>().color);
        Assert.IsNull(transition.percentage);Assert.IsNull(transition.status);Assert.AreEqual(0,transition.ProgressPercent);
        Assert.AreEqual(DumplingFamilyMenu.MenuScene,SceneManager.GetActiveScene().name,"Overlay appears before scene loading");
        float deadline=Time.realtimeSinceStartup+30;
        int previous=0;
        while(GameplayLoadingTransition.IsLoading && Time.realtimeSinceStartup<deadline)
        {
            if(transition){Assert.GreaterOrEqual(transition.ProgressPercent,previous);Assert.LessOrEqual(transition.ProgressPercent,100);previous=transition.ProgressPercent;}
            yield return null;
        }
        Assert.IsFalse(GameplayLoadingTransition.IsLoading,"Loading overlay must retire after activation");
        Assert.AreEqual(DumplingFamilyMenu.CreationScene,SceneManager.GetActiveScene().name);
        Assert.IsNull(Object.FindObjectOfType<GameplayLoadingTransition>());
        Assert.IsFalse(player);Assert.AreEqual(1,Object.FindObjectsOfType<GameAudioPlayer>().Length);Assert.AreEqual(1,Object.FindObjectsOfType<AudioListener>().Length);
        player=GameAudioPlayer.Current;Assert.IsFalse(player.Music.mute);Assert.IsTrue(player.Effects.mute);Assert.AreEqual(3,player.GetComponents<AudioSource>().Length);
        DumplingFamilyMenu.OpenMenu();yield return null;Assert.IsFalse(player);
        settings=Object.FindObjectOfType<AudioSettingsPopup>();settings.Open();Assert.AreSame(settings.musicOn,settings.musicButton.image.sprite);Assert.AreSame(settings.sfxOff,settings.sfxButton.image.sprite);
    }
    [UnityTest]public IEnumerator AudioWaitsForBothFocusAndPauseToResume()
    {
        var player=GameAudioPlayer.Current;player.Music.Play();player.SetLoop(GameSound.LiquidLoop,true,player);
        yield return null;Assert.IsTrue(player.Music.isPlaying);Assert.IsTrue(player.Loop.isPlaying);
        player.SendMessage("OnApplicationFocus",false);player.SendMessage("OnApplicationPause",true);
        Assert.IsFalse(player.Music.isPlaying);Assert.IsTrue(player.Music.mute);Assert.IsFalse(player.Loop.isPlaying);
        player.SendMessage("OnApplicationPause",false);Assert.IsTrue(player.Music.mute);Assert.IsFalse(player.Music.isPlaying);
        player.SendMessage("OnApplicationFocus",true);yield return null;
        Assert.IsTrue(player.Music.isPlaying);Assert.IsTrue(player.Loop.isPlaying);Assert.IsFalse(player.Music.mute);
        player.SetMusicEnabled(false);player.SendMessage("OnApplicationPause",true);player.SendMessage("OnApplicationFocus",false);
        player.SendMessage("OnApplicationFocus",true);Assert.IsTrue(player.Music.mute);
        player.SendMessage("OnApplicationPause",false);Assert.IsTrue(player.Music.mute);Assert.IsTrue(GameAudioPlayer.SfxEnabled);
        player.StopLoop(player);
    }
    [UnityTest]public IEnumerator MenuGroupsStaySafeAndSettingsPauseHints()
    {
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();yield return new WaitForSecondsRealtime(1);
        Assert.AreEqual(4,menu.Buttons.Length);
        foreach(var button in menu.Buttons)
        {
            var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
            foreach(var corner in corners){Assert.That(corner.x,Is.InRange(-13.5f,13.5f));Assert.That(corner.y,Is.InRange(-24f,24f));}
        }
        var settings=Object.FindObjectOfType<AudioSettingsPopup>();settings.Open();Assert.IsTrue(menu.PauseHints);
        settings.Close();Assert.IsFalse(menu.PauseHints);
        Assert.IsNotNull(settings.settingsButton.transform.Find("Settings icon"));
        Assert.AreEqual(.5f,settings.backdrop.image.color.a,.001f);
        Assert.IsNull(settings.backdrop.image.sprite,"Overlay alpha must not depend on an opaque source texture");
        Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced,settings.popup.transform.Find("Panel").GetComponent<UnityEngine.UI.Image>().type);
    }
    [UnityTest]public IEnumerator MenuCoversOffsetViewportAndSettingsFollowSafeCorner()
    {
        var menu=Object.FindObjectOfType<DumplingFamilyMenu>();var settings=Object.FindObjectOfType<AudioSettingsPopup>();
        var layout=menu.layout;
        try
        {
            foreach(var safe in new[]{new Rect(0,0,Screen.width,Screen.height),new Rect(Screen.width*.03f,Screen.height*.04f,Screen.width*.94f,Screen.height*.84f)})
            {
                layout.SetDeviceSafeAreaOverride(safe);yield return null;yield return null;
                var viewport=layout.VisibleWorldBounds;var artwork=menu.background.bounds;
                Assert.LessOrEqual(artwork.min.x,viewport.xMin);Assert.GreaterOrEqual(artwork.max.x,viewport.xMax);
                Assert.LessOrEqual(artwork.min.y,viewport.yMin);Assert.GreaterOrEqual(artwork.max.y,viewport.yMax);
                Assert.AreEqual(menu.background.transform.localScale.x,menu.background.transform.localScale.y);
                Assert.IsTrue(layout.TryScreenToWorld(new Vector2(layout.UsablePixels.xMin,layout.UsablePixels.yMax),out var corner));
                var corners=new Vector3[4];((RectTransform)settings.settingsButton.transform).GetWorldCorners(corners);
                Assert.AreEqual(corner.x+.6f,corners[0].x,.01f);Assert.AreEqual(corner.y-.6f,corners[1].y,.01f);
            }
        }
        finally{layout.SetDeviceSafeAreaOverride(null);}
    }
    [UnityTest]public IEnumerator RestartStopsGameplayLoopsWithoutClearingBgm()
    {
        SceneManager.LoadScene(DumplingFamilyMenu.CreationScene);yield return null;
        Assert.AreSame(clip,runtime.Get(GameSound.IngredientAppear));Assert.AreSame(clip,runtime.Get(GameSound.FinalReveal));
        var player=GameAudioPlayer.Current;player.library=runtime;player.RefreshMusic();var flow=Object.FindObjectOfType<CreationFlowController>();
        player.SetLoop(GameSound.LiquidLoop,true,flow);Assert.AreSame(clip,player.Loop.clip);Assert.AreSame(clip,player.Music.clip);
        flow.RestartProof();Assert.IsNull(player.Loop.clip);Assert.AreSame(clip,player.Music.clip);
        player.SetSfxEnabled(false);player.SetLoop(GameSound.LiquidLoop,true,flow);Assert.IsNull(player.Loop.clip);Assert.IsTrue(player.Effects.mute);
        yield return null;
    }
}
