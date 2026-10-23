using UnityEngine;
using UnityEngine.UI;
using SquishyDumpling.Layout;

namespace SquishyDumpling.Creation
{
    // Genuine UI only; it never sizes or positions world actors.
    public sealed class CreationProofHud : MonoBehaviour
    {
        public CreationFlowController flow;
        public ResponsiveWorldLayout layout;
        public Text status;
        public RectTransform safeRoot;
        void LateUpdate()
        {
            Rect viewport=layout.WorldCamera.pixelRect, usable=layout.UsablePixels;
            if(viewport.width<=0 || viewport.height<=0) return;
            safeRoot.anchorMin=new Vector2((usable.xMin-viewport.xMin)/viewport.width,(usable.yMin-viewport.yMin)/viewport.height);
            safeRoot.anchorMax=new Vector2((usable.xMax-viewport.xMin)/viewport.width,(usable.yMax-viewport.yMin)/viewport.height);
            safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            string message=flow.pearl && flow.pearl.AwaitingChoice ? "Choose a Pearl colour" :
                flow.Session.Step==CreationStep.FinalSquishPlay ? "SQUISH IT!" :
                flow.Session.Step==CreationStep.CustomizationComplete ? "Meet your dumpling!" :
                flow.Session.Step==CreationStep.PresentCustomization ? "Choose your dumpling’s face…" :
                flow.Session.Step==CreationStep.Customization && flow.customizationDefinition.directSelection ? "Choose a face, then confirm" :
                flow.Session.Step==CreationStep.Customization ? (string.IsNullOrEmpty(flow.Session.SelectedFaceId) ? "Choose a face, then drag its stamp onto the dumpling" : "Drag the stamp onto your dumpling") :
                flow.Session.Step==CreationStep.FaceApplied && flow.customizationDefinition.directSelection ? "A little personality!" :
                flow.Session.Step==CreationStep.Stamping || flow.Session.Step==CreationStep.FaceApplied ? "Stamping a little personality…" :
                flow.Session.Step==CreationStep.FormingComplete ? "Your dumpling body is ready" :
                flow.Session.Step==CreationStep.Forming ? (flow.pearl ? "Stir the spatula to fill your dumpling pouch" : "Drag the dough to shape your dumpling") :
                flow.Session.Step==CreationStep.PresentForming ? (flow.pearl ? "Getting the dumpling pouch ready…" : "Moving the soft mixture onto the mat…") :
                flow.Session.Step==CreationStep.FinishForming ? "Settling your dumpling…" :
                flow.Session.Step==CreationStep.MixingComplete ? "Mixed base ready" :
                flow.Session.Step==CreationStep.Mixing ? "Stir around the bowl — "+Mathf.RoundToInt(flow.ActiveMixing.Progress.Progress*100)+"%" :
                flow.Session.Step==CreationStep.PresentMixingTool ? "Getting ready to stir…" :
                flow.Session.Step==CreationStep.FinishMixing ? "Settling the squishy mixture…" :
                flow.Session.Step==CreationStep.BaseIngredientsComplete ? "Base ingredients ready — unmixed" :
                flow.Session.Step==CreationStep.ProofComplete ? "Ingredient received — proof complete" :
                flow.CanInteract ? "Drag "+flow.CurrentDefinition.displayName+" into the bowl" : flow.Session.Step==CreationStep.ReturnIngredient ? "Try again — returning bottle" :
                flow.Session.Step==CreationStep.PourIngredient ? "Adding "+flow.CurrentDefinition.displayName+"…" : "Preparing…";
            status.text=message;
        }
    }
}
