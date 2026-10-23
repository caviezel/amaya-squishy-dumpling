using System;
using UnityEngine;
namespace SquishyDumpling.Creation
{
    [Serializable] public sealed class FaceOption
    {
        public string id, displayName;
        public Sprite preview, stampArtwork, appliedArtwork;
        public Vector3 appliedLocalPosition;
        public Vector3 appliedLocalScale=Vector3.one*2.4f;
        public float appliedLocalRotation;
    }
    [CreateAssetMenu(menuName="Squishy Dumpling/Face customization")]
    public sealed class FaceCustomizationDefinition : ScriptableObject
    {
        public FaceCustomizationStep stepPrefab;
        public bool directSelection;
        public FaceOption[] faces=new FaceOption[0];
        [Tooltip("Empty means the player chooses first.")] public string defaultSelectedId;
        [Header("Presentation seconds")]
        [Min(.01f)] public float entrance=.35f, returnDuration=.35f, alignment=.28f, anticipation=.12f, press=.16f, contactHold=.18f, lift=.25f, rebound=.35f, exit=.35f;
        [Header("Authored temporary stamp response")]
        [Min(0)] public float anticipationLift=.35f, pressDepth=.35f, liftDistance=2.5f;
        [Range(.5f,1)] public float compression=.8f;
        [Range(1,1.5f)] public float widthCompensation=1.12f;
        [Range(0,.2f)] public float reboundStrength=.07f;
        [Range(0,12)] public float wobbleDegrees=3;
        public FaceOption Find(string id)
        { if(string.IsNullOrEmpty(id) || faces==null) return null; foreach(var f in faces) if(f!=null && f.id==id) return f; return null; }
        public bool IsValid
        {
            get {
                if(!stepPrefab || faces==null || faces.Length==0) return false;
                for(int i=0;i<faces.Length;i++)
                {
                    var f=faces[i]; if(f==null || string.IsNullOrEmpty(f.id) || !f.preview || (!directSelection && !f.stampArtwork) || !f.appliedArtwork) return false;
                    for(int j=0;j<i;j++) if(faces[j].id==f.id) return false;
                }
                return string.IsNullOrEmpty(defaultSelectedId) || Find(defaultSelectedId)!=null;
            }
        }
    }
}
