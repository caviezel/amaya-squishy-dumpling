using System;
using UnityEngine;
namespace SquishyDumpling.Creation
{
    [Serializable] public sealed class DumplingFamily
    {
        public string id,displayName;
        public PearlRecipeDefinition presentation;
    }
    [CreateAssetMenu(menuName="Squishy Dumpling/Family catalog")]
    public sealed class DumplingFamilyCatalog : ScriptableObject
    {
        public DumplingFamily[] families=new DumplingFamily[0];
        public DumplingFamily Find(string id)
        {foreach(var family in families)if(family.id==id)return family;return null;}
    }
    public static class DumplingFamilySelection
    {
        public static string SelectedId { get; private set; }="pearl";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()=>SelectedId="pearl";
        public static void Select(string id)=>SelectedId=id;
    }
}
