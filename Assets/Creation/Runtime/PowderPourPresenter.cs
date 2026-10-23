using UnityEngine;

namespace SquishyDumpling.Creation
{
    // A fixed 24-sprite pool gives a soft, replaceable world-space transfer effect, not granular physics.
    public sealed class PowderPourPresenter : MonoBehaviour
    {
        public Transform containerMouth, bowlTarget;
        public IngredientPresenter owner;
        public Sprite grainSprite;
        public Color grainColor = new Color(1f,.92f,.73f);
        public float contactDelay = .45f;
        public float flightDuration = .42f;
        public float grainsPerSecond = 38f;
        public float Envelope { get; private set; }
        public int Arrivals { get; private set; }
        public int ActiveGrains { get; private set; }
        public bool IsEmitting => Envelope > .001f;
        public float MaximumFlightDuration => flightDuration * 1.1f;
        public Vector3 Origin => containerMouth.position;
        public Vector3 Target => bowlTarget.position;
        const int Capacity=24;
        sealed class Grain
        {
            public SpriteRenderer renderer;
            public Vector3 from, target;
            public float age, lifetime, scale;
            public bool active;
        }
        Grain[] grains;
        float spawnBudget;
        void Awake() { BuildPool(); }
        void BuildPool()
        {
            if(grains!=null) return;
            grains=new Grain[Capacity];
            for(int i=0;i<Capacity;i++)
            {
                var renderer=new GameObject("Powder grain "+i).AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(transform,false); renderer.sprite=grainSprite;
                renderer.sortingLayerName="Gameplay"; renderer.sortingOrder=4; renderer.enabled=false;
                grains[i]=new Grain{renderer=renderer};
            }
        }
        public void SetEnvelope(float value)
        { BuildPool(); Envelope=Mathf.Clamp01(value); }
        void LateUpdate()
        {
            if(!containerMouth || !bowlTarget) return;
            transform.position=containerMouth.position;
            float dt=Time.deltaTime;
            spawnBudget+=Envelope*grainsPerSecond*dt;
            // Bound emission work even after a long frame.
            int wanted=Mathf.Min(Capacity,Mathf.FloorToInt(spawnBudget)); spawnBudget-=Mathf.Floor(spawnBudget);
            for(int n=0;n<wanted;n++) Spawn();
            ActiveGrains=0;
            foreach(var grain in grains)
            {
                if(!grain.active) continue;
                grain.age+=dt; float t=Mathf.Clamp01(grain.age/grain.lifetime);
                if(t>=1) { grain.active=false; grain.renderer.enabled=false; Arrivals++; continue; }
                Vector3 control=Vector3.Lerp(grain.from,grain.target,.4f)+Vector3.down*.4f;
                grain.renderer.transform.position=(1-t)*(1-t)*grain.from+2*(1-t)*t*control+t*t*grain.target;
                grain.renderer.transform.localScale=Vector3.one*grain.scale*(1-.15f*t);
                grain.renderer.color=new Color(grainColor.r,grainColor.g,grainColor.b,Mathf.Min(1,t*8));
                ActiveGrains++;
            }
        }
        void Spawn()
        {
            foreach(var grain in grains)
            {
                if(grain.active) continue;
                grain.active=true; grain.age=0; grain.lifetime=flightDuration*Random.Range(.9f,1.1f);
                grain.from=containerMouth.position+containerMouth.right*Random.Range(-.12f,.12f);
                grain.target=bowlTarget.position+new Vector3(Random.Range(-.65f,.65f),Random.Range(-.12f,.12f),0);
                grain.scale=Random.Range(.065f,.12f);
                grain.renderer.sprite=grainSprite; grain.renderer.enabled=true; return;
            }
        }
        public void ResetVisuals()
        {
            Envelope=0; spawnBudget=0; Arrivals=0; ActiveGrains=0;
            if(grains!=null) foreach(var grain in grains) { grain.active=false; if(grain.renderer) grain.renderer.enabled=false; }
        }
        void OnDisable() { ResetVisuals(); if(owner && owner.flow) owner.flow.Interrupt(); }
    }
}
