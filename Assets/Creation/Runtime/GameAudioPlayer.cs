using UnityEngine;
namespace SquishyDumpling.Creation
{
    [DefaultExecutionOrder(-1000),DisallowMultipleComponent]
    public sealed class GameAudioPlayer : MonoBehaviour
    {
        public const string MusicKey="SquishyDumpling.Audio.Music",SfxKey="SquishyDumpling.Audio.Sfx";
        public static GameAudioPlayer Current { get; private set; }
        public static bool MusicEnabled=>PlayerPrefs.GetInt(MusicKey,1)!=0;
        public static bool SfxEnabled=>PlayerPrefs.GetInt(SfxKey,1)!=0;
        public GameAudioLibrary library;
        public AudioSource Music { get; private set; }
        public AudioSource Effects { get; private set; }
        public AudioSource Loop { get; private set; }
        Object loopOwner;static bool pendingClick;bool playNavigationClick;
        bool appPaused,appFocused=true,audioSuspended;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Current=null;pendingClick=false;}
        void Awake()
        {
            Current=this;Music=Source(true);Effects=Source(false);Loop=Source(true);ApplySettings();RefreshMusic();
            playNavigationClick=pendingClick;pendingClick=false;
        }
        AudioSource Source(bool looping)
        {var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.loop=looping;return source;}
        public void RefreshMusic()
        {
            if(!library || !Music)return;
            var clip=gameObject.scene.name==DumplingFamilyMenu.MenuScene?library.menuBgm:library.gameplayBgm;
            if(Music.clip==clip)return;Music.Stop();Music.clip=clip;if(clip)Music.Play();
        }
        public void Play(GameSound sound,AudioClip clipOverride=null)
        {
            if(!SfxEnabled || !Effects)return;var clip=clipOverride?clipOverride:library?library.Get(sound):null;
            if(clip)Effects.PlayOneShot(clip,library?library.sfxVolume:1);
        }
        public void SetLoop(GameSound sound,bool playing,Object owner,AudioClip clipOverride=null)
        {
            if(!playing){StopLoop(owner);return;}
            if(!SfxEnabled || !Loop)return;var clip=clipOverride?clipOverride:library?library.Get(sound):null;
            if(!clip){StopLoop(owner);return;}
            if(loopOwner==owner && Loop.clip==clip && Loop.isPlaying)return;
            Loop.Stop();loopOwner=owner;Loop.clip=clip;Loop.volume=library?library.loopVolume:.5f;Loop.Play();
        }
        public void StopLoop(Object owner){if(loopOwner==owner && Loop){Loop.Stop();Loop.clip=null;loopOwner=null;}}
        public void StopGameplaySounds(){if(Effects)Effects.Stop();if(Loop){Loop.Stop();Loop.clip=null;}loopOwner=null;}
        public void SetMusicEnabled(bool value){PlayerPrefs.SetInt(MusicKey,value?1:0);PlayerPrefs.Save();ApplySettings();}
        public void SetSfxEnabled(bool value){PlayerPrefs.SetInt(SfxKey,value?1:0);PlayerPrefs.Save();if(!value)StopGameplaySounds();ApplySettings();}
        public void ApplySettings()
        {
            if(Music){Music.mute=!MusicEnabled || audioSuspended;Music.volume=library?library.musicVolume:.45f;}
            if(Effects)Effects.mute=!SfxEnabled || audioSuspended;if(Loop)Loop.mute=!SfxEnabled || audioSuspended;
        }
        public static void NavigationClick()=>pendingClick=true;
        void LateUpdate(){if(playNavigationClick){playNavigationClick=false;Play(GameSound.Button);}}
        void Update(){if(Loop && Loop.isPlaying && !loopOwner){Loop.Stop();Loop.clip=null;}}
        void ApplyApplicationSuspension()
        {
            bool value=appPaused || !appFocused;if(value==audioSuspended)return;audioSuspended=value;
            if(!Music)return;ApplySettings();
            if(value){Music.Pause();Effects.Pause();Loop.Pause();}
            else{Music.UnPause();Effects.UnPause();Loop.UnPause();}
        }
        void OnApplicationPause(bool paused){appPaused=paused;ApplyApplicationSuspension();}
        void OnApplicationFocus(bool focused){appFocused=focused;ApplyApplicationSuspension();}
        void OnDestroy(){StopGameplaySounds();if(Current==this)Current=null;}
    }
}
