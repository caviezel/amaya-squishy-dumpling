using UnityEngine;
using UnityEngine.UI;
namespace SquishyDumpling.Creation
{
    // Genuine interface. Body, stamp and application remain SpriteRenderer gameplay.
    public sealed class FaceChoiceView : MonoBehaviour
    {
        public Button buttonTemplate;
        public RectTransform row;
        public CanvasGroup group;
        public ScrollRect scroll;
        public FaceScrollHint scrollHint;
        Button[] buttons;
        FaceCustomizationStep step;
        public void Bind(FaceCustomizationStep owner)
        {
            step=owner; var faces=step.definition.faces;
            if(buttons!=null)foreach(var old in buttons)if(old)Destroy(old.gameObject);
            float width=scroll?300:row.rect.width/faces.Length;
            if(scroll){row.sizeDelta=new Vector2(width*faces.Length,row.sizeDelta.y);row.anchoredPosition=Vector2.zero;scroll.horizontalNormalizedPosition=0;}
            buttons=new Button[faces.Length];
            for(int i=0;i<faces.Length;i++)
            {
                var face=faces[i]; var b=Instantiate(buttonTemplate,row); b.gameObject.name="Face choice "+face.id; b.gameObject.SetActive(true);
                if(scrollHint)b.gameObject.AddComponent<FaceScrollTouch>().hint=scrollHint;
                var rect=(RectTransform)b.transform;if(scroll)rect.anchorMin=rect.anchorMax=new Vector2(0,.5f); rect.anchoredPosition=new Vector2(scroll?(i+.5f)*width:(i-(faces.Length-1)*.5f)*width,0); rect.sizeDelta=new Vector2(width-24,scroll?360:250);
                b.transform.Find("Preview").GetComponent<Image>().sprite=face.preview;
                var label=b.GetComponentInChildren<Text>();if(label)label.gameObject.SetActive(false);
                string id=face.id; b.onClick.AddListener(()=>{if(scrollHint)scrollHint.Cancel();step.SelectFace(id);}); buttons[i]=b;
            }
            Refresh();if(scrollHint)scrollHint.Prepare(faces.Length);
        }
        public void BeginScrollHint(){if(scrollHint)scrollHint.Play();}
        public void CancelScrollHint(){if(scrollHint)scrollHint.Cancel();}
        public void Refresh()
        {
            if(buttons==null) return;
            for(int i=0;i<buttons.Length;i++)
            {
                buttons[i].interactable=step.CanSelect;
                buttons[i].GetComponent<Image>().color=step.flow.Session.SelectedFaceId==step.definition.faces[i].id ? new Color(1,.76f,.36f,1) : new Color(1,.96f,.86f,1);
            }
        }
    }
}
