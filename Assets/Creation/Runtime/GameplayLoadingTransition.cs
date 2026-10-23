using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace SquishyDumpling.Creation
{
    // Only this short-lived UI survives activation; gameplay and audio remain scene-owned.
    public sealed class GameplayLoadingTransition : MonoBehaviour
    {
        public static bool IsLoading { get; private set; }
        public int ProgressPercent { get; private set; }
        [Header("Prefab UI references")]
        public Text percentage,status;
        public RectTransform fill;
        [Header("Loading copy")]
        public string loadingText="Loading…";
        public string finishingText="Finishing up…";
        public bool IsConfigured=>fill && GetComponent<Canvas>();
        bool ownsLoading;
        public static bool Begin(GameplayLoadingTransition prefab)
        {
            if(IsLoading || !prefab || !prefab.IsConfigured)return false;
            IsLoading=true;
            var instance=Instantiate(prefab);instance.name="Loading gameplay";instance.ownsLoading=true;instance.gameObject.SetActive(true);
            DontDestroyOnLoad(instance.gameObject);instance.StartCoroutine(instance.Load());return true;
        }
        void SetProgress(int value,bool finishing=false)
        {
            ProgressPercent=Mathf.Max(ProgressPercent,Mathf.Clamp(value,0,100));
            if(percentage)percentage.text=ProgressPercent+"%";
            if(status)status.text=finishing?finishingText:loadingText;
            fill.anchorMax=new Vector2(ProgressPercent/100f,1);fill.gameObject.SetActive(ProgressPercent>0);
        }
        IEnumerator Load()
        {
            SetProgress(0);Canvas.ForceUpdateCanvases();
            // Let the overlay render before requesting any expensive scene work.
            yield return null;yield return null;
            var operation=SceneManager.LoadSceneAsync(DumplingFamilyMenu.CreationScene,LoadSceneMode.Single);
            operation.allowSceneActivation=false;
            // Unity reports 0..0.9 before activation; reserve the last percentage point for activation.
            while(operation.progress<.9f){SetProgress(Mathf.FloorToInt(operation.progress/.9f*99));yield return null;}
            SetProgress(99,true);operation.allowSceneActivation=true;
            while(!operation.isDone)yield return null;
            SetProgress(100,true);yield return null;Destroy(gameObject);
        }
        void OnDestroy(){if(ownsLoading)IsLoading=false;}
    }
}
