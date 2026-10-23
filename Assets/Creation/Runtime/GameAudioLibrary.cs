using UnityEngine;
namespace SquishyDumpling.Creation
{
    public enum GameSound { Button,Pickup,Accepted,PourStart,IngredientImpact,IngredientComplete,DyeDrop,MixingMilestone,StepComplete,FaceSelect,FaceConfirm,Squish,Stretch,Release,LiquidLoop,PowderLoop,StirLoop,FunnelLoop,IngredientAppear,FinalReveal }
    [CreateAssetMenu(menuName="Squishy Dumpling/Shared audio library")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [Header("Background music")]
        public AudioClip menuBgm,gameplayBgm;
        [Header("Interface and ingredients")]
        public AudioClip buttonClick,pickup,accepted,pourStart,ingredientImpact,ingredientComplete,dyeDrop;
        [Header("Creation and final play")]
        public AudioClip mixingMilestone,stepComplete,faceSelect,faceConfirm,squish,stretch,release;
        [Header("Presentation accents")]
        public AudioClip ingredientAppear,finalReveal;
        [Header("Looping effects")]
        public AudioClip liquidPourLoop,powderPourLoop,stirringLoop,funnelPourLoop;
        [Header("Mix levels")]
        [Range(0,1)]public float musicVolume=.45f,sfxVolume=.8f,loopVolume=.5f;
        public AudioClip Get(GameSound sound)
        {
            switch(sound)
            {
                case GameSound.Button:return buttonClick;case GameSound.Pickup:return pickup;case GameSound.Accepted:return accepted;
                case GameSound.PourStart:return pourStart;case GameSound.IngredientImpact:return ingredientImpact;case GameSound.IngredientComplete:return ingredientComplete;
                case GameSound.DyeDrop:return dyeDrop;case GameSound.MixingMilestone:return mixingMilestone;case GameSound.StepComplete:return stepComplete;
                case GameSound.FaceSelect:return faceSelect;case GameSound.FaceConfirm:return faceConfirm;case GameSound.Squish:return squish;
                case GameSound.Stretch:return stretch;case GameSound.Release:return release;case GameSound.LiquidLoop:return liquidPourLoop;
                case GameSound.PowderLoop:return powderPourLoop;case GameSound.StirLoop:return stirringLoop;case GameSound.FunnelLoop:return funnelPourLoop;
                case GameSound.IngredientAppear:return ingredientAppear;case GameSound.FinalReveal:return finalReveal;
                default:return null;
            }
        }
    }
}
