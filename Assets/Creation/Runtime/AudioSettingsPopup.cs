using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
namespace SquishyDumpling.Creation
{
    public sealed class AudioSettingsPopup : MonoBehaviour
    {
        public GameObject popup;
        public Button settingsButton,closeButton,musicButton,sfxButton,backdrop;
        public Sprite musicOn,musicOff,sfxOn,sfxOff;
        Tween entrance;
        SquishyDumpling.Layout.ResponsiveWorldLayout layout;
        void LateUpdate()
        {
            if(!layout)
            {
                var canvas=GetComponentInParent<Canvas>();
                if(canvas && canvas.worldCamera)layout=canvas.worldCamera.GetComponent<SquishyDumpling.Layout.ResponsiveWorldLayout>();
            }
            if(!layout || !settingsButton)return;
            layout.Refresh();var safe=layout.UsablePixels;
            if(!layout.TryScreenToWorld(new Vector2(safe.xMin,safe.yMax),out var corner))return;
            var rect=(RectTransform)settingsButton.transform;
            Vector2 half=Vector2.Scale(rect.rect.size,rect.lossyScale)*.5f;
            rect.position=new Vector3(corner.x+half.x+.6f,corner.y-half.y-.6f,rect.position.z);
        }
        void Awake()
        {
            settingsButton.onClick.AddListener(Open);closeButton.onClick.AddListener(Close);backdrop.onClick.AddListener(Close);
            musicButton.onClick.AddListener(ToggleMusic);sfxButton.onClick.AddListener(ToggleSfx);popup.SetActive(false);Refresh();
        }
        public void Open()
        {
            GameAudioPlayer.Current?.Play(GameSound.Button);transform.SetAsLastSibling();Refresh();popup.SetActive(true);var menu=FindObjectOfType<DumplingFamilyMenu>();if(menu)menu.PauseHints=true;if(entrance.isAlive)entrance.Stop();
            popup.transform.localScale=Vector3.one*.94f;entrance=Tween.Scale(popup.transform,Vector3.one,.18f,Ease.OutBack);
        }
        public void Close(){GameAudioPlayer.Current?.Play(GameSound.Button);if(entrance.isAlive)entrance.Stop();popup.SetActive(false);var menu=FindObjectOfType<DumplingFamilyMenu>();if(menu)menu.PauseHints=false;}
        public void ToggleMusic(){if(GameAudioPlayer.Current)GameAudioPlayer.Current.SetMusicEnabled(!GameAudioPlayer.MusicEnabled);GameAudioPlayer.Current?.Play(GameSound.Button);Refresh();}
        public void ToggleSfx(){if(GameAudioPlayer.Current)GameAudioPlayer.Current.SetSfxEnabled(!GameAudioPlayer.SfxEnabled);GameAudioPlayer.Current?.Play(GameSound.Button);Refresh();}
        public void Refresh(){musicButton.image.sprite=GameAudioPlayer.MusicEnabled?musicOn:musicOff;sfxButton.image.sprite=GameAudioPlayer.SfxEnabled?sfxOn:sfxOff;}
        void OnDisable(){if(entrance.isAlive)entrance.Stop();if(popup)popup.SetActive(false);}
    }
}
