using UnityEngine;

namespace SquishyDumpling.Creation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LiquidStreamPresenter : MonoBehaviour
    {
        public Transform containerMouth, bowlTarget;
        public BowlPresenter receivingBowl;
        public bool useFixedContact,discreteDrop;
        public Vector3 fixedContactPoint;
        public Sprite[] frames=new Sprite[0];
        [Min(.02f)]public float frameDuration=.12f;
        float animationStart;
        public Vector3 LandingPosition => useFixedContact ? fixedContactPoint : receivingBowl ? receivingBowl.PourContact : bowlTarget.position;
        [Min(.01f)] public float authoredWidth = .24f;
        [Tooltip("Display the complete client stream once, preserving its aspect ratio.")]
        public bool preserveArtworkAspect;
        public float Envelope { get; private set; }
        public bool fadeWithPourProgress;
        float pourOpacity=1;
        public void SetPourProgress(float progress)
        {
            pourOpacity=fadeWithPourProgress ? 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,1,progress)) : 1;
            UpdateGeometry();
        }
        public SpriteRenderer Renderer => GetComponent<SpriteRenderer>();
        public void SetEnvelope(float value)
        {
            if(Envelope<=0 && value>0)animationStart=Time.time;
            Envelope = Mathf.Clamp01(value); Renderer.enabled = Envelope > .001f; UpdateGeometry();
        }
        void LateUpdate() { if (Envelope > 0) UpdateGeometry(); }
        public void UpdateGeometry()
        {
            if (discreteDrop || !containerMouth || !bowlTarget) return;
            if(Envelope>0 && frames!=null && frames.Length>0)
                Renderer.sprite=frames[Mathf.FloorToInt((Time.time-animationStart)/Mathf.Max(.02f,frameDuration))%frames.Length];
            Vector3 from = containerMouth.position, to = LandingPosition;
            Vector2 delta = from - to; // Local +Y is the authored mouth end of the stream.
            transform.position = (from + to) * .5f;
            transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg - 90);
            if (preserveArtworkAspect && Renderer.sprite)
            {
                // One continuous stream: tiling repeats highlights/droplets and destroys the authored silhouette.
                Renderer.drawMode = SpriteDrawMode.Simple;
                float scale = delta.magnitude / Renderer.sprite.bounds.size.y;
                transform.localScale = Vector3.one * scale;
                Color tint = Renderer.color; tint.a = Envelope*pourOpacity; Renderer.color = tint;
                return;
            }
            // Legacy procedural stream geometry, independent of device resolution.
            Renderer.drawMode = SpriteDrawMode.Tiled;
            Renderer.size = new Vector2(authoredWidth * Mathf.Max(.001f, Envelope),delta.magnitude);
            Color color = Renderer.color; color.a = Envelope*pourOpacity; Renderer.color = color;
        }
        public void ShowDrop(Vector3 position,float width)
        {
            Envelope=1;Renderer.enabled=true;Renderer.drawMode=SpriteDrawMode.Simple;
            transform.position=position;transform.rotation=Quaternion.identity;
            transform.localScale=Vector3.one*(width/Renderer.sprite.bounds.size.x);
            Color color=Renderer.color;color.a=1;Renderer.color=color;
        }
        public void ResetVisuals() { pourOpacity=1; SetEnvelope(0); }
        void OnDisable() { ResetVisuals(); }
    }
}
