using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Visual-only lid retirement follows the existing pickup/return ownership and never commits gameplay.
    public sealed class PearlContainerLid:MonoBehaviour
    {
        public IngredientPresenter presenter;public SpriteRenderer lid;
        Vector3 home;float amount;
        public void BindAuthoring(IngredientPresenter owner,SpriteRenderer visual){presenter=owner;lid=visual;}
        void Awake(){home=lid.transform.localPosition;}
        void Update()
        {
            bool opened=presenter.flow && (presenter.flow.Session.Step==CreationStep.DragIngredient || presenter.flow.Session.Step==CreationStep.PourIngredient);
            amount=Mathf.MoveTowards(amount,opened?1:0,Time.deltaTime*5);
            lid.transform.localPosition=home+new Vector3(.45f,1.4f,0)*amount;lid.transform.localRotation=Quaternion.Euler(0,0,-30*amount);
            var c=lid.color;c.a=1-amount;lid.color=c;lid.enabled=amount<.999f && presenter.container.enabled;
        }
    }
}
