using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SquishyDumpling.Layout;
namespace SquishyDumpling.Creation
{
    public sealed class DumplingFamilyMenu : MonoBehaviour
    {
        public const string MenuScene="Menu",CreationScene="Gameplay";
        public DumplingFamilyCatalog catalog;
        public ResponsiveWorldLayout layout;
        public SpriteRenderer background;
        public Button template;
        public GameplayLoadingTransition loadingScreenPrefab;
        public Button[] Buttons { get; private set; }
        [Min(0)] public float revealDelay=.35f;
        [Min(.1f)] public float revealStagger=.32f;
        [Min(.1f)] public float revealDuration=1.05f;
        Image[] lids, bodies, faces;
        bool leaving;
        float appeared;
        public bool PauseHints { get; set; }
        void Start()
        {
            appeared=Time.unscaledTime;
            Buttons=new Button[catalog.families.Length];
            lids=new Image[Buttons.Length];bodies=new Image[Buttons.Length];faces=new Image[Buttons.Length];
            for(int i=0;i<Buttons.Length;i++)
            {
                string id=catalog.families[i].id;var button=Instantiate(template,template.transform.parent);
                button.name="Family "+id;((RectTransform)button.transform).anchoredPosition=new Vector2(i%2==0?-630:630,i<2?750:-750);
                button.transform.Find("Body").GetComponent<Image>().sprite=catalog.families[i].presentation.variants[0].body;
                lids[i]=button.transform.Find("Basket lid").GetComponent<Image>();
                bodies[i]=button.transform.Find("Body").GetComponent<Image>();
                faces[i]=button.transform.Find("Face").GetComponent<Image>();
                button.interactable=false;AnimateReveal(i,0);
                button.onClick.AddListener(()=>Choose(id));button.gameObject.SetActive(true);Buttons[i]=button;
            }
        }
        public bool Choose(string id)
        {
            if(leaving || PauseHints || Buttons==null)return false;
            int index=System.Array.FindIndex(catalog.families,f=>f.id==id);
            if(index<0 || !Buttons[index].interactable)return false;
            if(!loadingScreenPrefab || !loadingScreenPrefab.IsConfigured)return false;
            DumplingFamilySelection.Select(id);
            if(!GameplayLoadingTransition.Begin(loadingScreenPrefab))return false;
            GameAudioPlayer.NavigationClick();leaving=true;return true;
        }
        public static void OpenMenu(){GameAudioPlayer.NavigationClick();SceneManager.LoadScene(MenuScene);}
        void LateUpdate()
        {
            if(!background || !layout || !layout.WorldCamera)return;
            layout.Refresh();
            var bounds=layout.VisibleWorldBounds;var size=background.sprite.bounds.size;
            background.transform.position=new Vector3(bounds.center.x,bounds.center.y,0);
            // Cover the actual viewport, including device-safe-area camera offsets.
            float cover=Mathf.Max(27/size.x,48/size.y);
            cover=Mathf.Max(cover,Mathf.Max(bounds.width/size.x,bounds.height/size.y));
            background.transform.localScale=Vector3.one*cover*1.015f;
            if(Buttons==null)return;
            var artBounds=background.bounds;
            for(int i=0;i<Buttons.Length;i++)
            {
                float t=Time.unscaledTime-appeared-revealDelay-i*revealStagger;
                float progress=Mathf.Clamp01(t/revealDuration);
                AnimateReveal(i,progress);
                Buttons[i].interactable=progress>=1 && !leaving;
                // Source-art stand centres, normalized from the full canvas.
                float x=artBounds.min.x+artBounds.size.x*(i%2==0?.333f:.71f);
                float y=artBounds.max.y-artBounds.size.y*(i<2?.421f:.619f);
                const float scale=.86f;
                x=Mathf.Clamp(x,-13.5f+5.5f*scale+.3f,13.5f-5.5f*scale-.3f);
                float cycle=Mathf.Repeat(Mathf.Max(0,t-revealDuration),5.8f);
                float bob=!PauseHints && t>=revealDuration && cycle<2.4f ? Mathf.Sin(cycle/2.4f*Mathf.PI*4)*.22f : 0;
                var rect=(RectTransform)Buttons[i].transform;
                rect.position=new Vector3(x,y+6.1f*scale+bob,0);
                rect.localScale=Vector3.one*scale;
            }
        }
        void AnimateReveal(int i,float progress)
        {
            float lift=1-Mathf.Pow(1-progress,3);
            var lid=lids[i];
            lid.rectTransform.anchoredPosition=new Vector2(-180*lift,-40+850*lift);
            lid.rectTransform.localRotation=Quaternion.Euler(0,0,18*lift);
            SetAlpha(lid,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.92f,progress)));
            float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.9f,progress));
            float scale=Mathf.Lerp(.38f,1,reveal);
            bodies[i].rectTransform.localScale=faces[i].rectTransform.localScale=Vector3.one*scale;
            bodies[i].rectTransform.anchoredPosition=new Vector2(0,Mathf.Lerp(-180,180,reveal));
            // Transform the face around the same centre as the body.
            faces[i].rectTransform.anchoredPosition=new Vector2(0,bodies[i].rectTransform.anchoredPosition.y-100*scale);
            float alpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.4f,progress));
            SetAlpha(bodies[i],alpha);SetAlpha(faces[i],alpha);
        }
        static void SetAlpha(Image image,float alpha)
        { var color=image.color;color.a=alpha;image.color=color; }
    }
}
