using UnityEngine;
namespace SquishyDumpling.Creation
{
    public sealed class FormingMaterialPresenter : MonoBehaviour
    {
        public Transform contactRoot;
        public SpriteRenderer[] states;
        public SpriteRenderer contactMark;
        public FormingDefinition definition;
        public float DisplayedProgress { get; private set; }
        public float ContactAmount { get; private set; }
        public float PresentationOpacity => opacity;
        public Vector3 BaseScale { get; private set; }
        float targetContact, opacity=1, phase;
        Vector2 contact, direction, dragDirection;
        void Awake() { BaseScale=contactRoot.localScale; }
        public void ApplyDefinition(FormingDefinition data)
        {
            definition=data;
            for(int i=0;i<states.Length;i++) states[i].sprite=data.shapeStates[Mathf.Min(i,data.shapeStates.Length-1)];
            contactMark.sprite=data.contactSprite;
        }
        public void SetContact(Vector2 world,Vector2 movement=default)
        {
            dragDirection=Vector2.ClampMagnitude(movement/Mathf.Max(.05f,definition.minimumGestureTravel),1);
            contact=world-(Vector2)transform.position; direction=contact.normalized; targetContact=1;
            ContactAmount=Mathf.Max(ContactAmount,.35f); ApplyContact(); // Immediate spatial feedback on press.
        }
        public void ReleaseContact() { targetContact=0; }
        public void SetOpacity(float value) { opacity=Mathf.Clamp01(value); Display(DisplayedProgress); }
        public void Display(float progress)
        {
            DisplayedProgress=Mathf.Clamp01(progress); float scaled=DisplayedProgress*(states.Length-1);
            for(int i=0;i<states.Length;i++)
            { Color color=states[i].color; color.a=Mathf.Clamp01(1-Mathf.Abs(i-scaled))*opacity; states[i].color=color; states[i].enabled=color.a>.0001f; }
        }
        public void Tick(float progress,float dt)
        {
            Display(progress); float damping=targetContact>ContactAmount ? definition.contactDamping : definition.releaseDamping;
            ContactAmount=Mathf.Lerp(ContactAmount,targetContact,1-Mathf.Exp(-damping*dt));
            if(targetContact==0 && ContactAmount<.001f) ContactAmount=0;
            phase+=dt*8; ApplyContact();
        }
        void ApplyContact()
        {
            float amount=ContactAmount;
            float x=Mathf.Abs(direction.x), y=Mathf.Abs(direction.y);
            float wobble=amount*definition.wobbleStrength*Mathf.Sin(phase);
            contactRoot.localScale=Vector3.Scale(BaseScale,new Vector3(1-amount*definition.indentationStrength*x+wobble,1-amount*definition.indentationStrength*y-wobble,1));
            contactRoot.localPosition=new Vector3(dragDirection.x,dragDirection.y,0)*definition.dragFollow*amount;
            Vector2 extents=Vector2.Lerp(definition.initialContactExtents,definition.finalContactExtents,DisplayedProgress);
            Vector2 mark=contact; float length=new Vector2(mark.x/extents.x,mark.y/extents.y).magnitude;
            mark/=Mathf.Max(1,length); contactMark.transform.localPosition=new Vector3(mark.x,mark.y,-.01f);
            contactMark.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            Color color=contactMark.color; color.a=amount*.48f; contactMark.color=color; contactMark.enabled=amount>.001f && opacity>.99f;
        }
        public void ClearContact()
        { dragDirection=Vector2.zero; targetContact=ContactAmount=0; contactRoot.localPosition=Vector3.zero; contactRoot.localScale=BaseScale; contactMark.enabled=false; }
    }
}
