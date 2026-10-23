using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Explicit mobile policy; original images, PPU, pivots and sprite borders stay untouched.
public static class MobileTexturePolicy
{
    static readonly string[] Heroes=new[]{"Dumpling_4-rainbow","Dumpling_glitter-pink-heart","Dumpling_glitter-pink-big","Dumpling_glitter-gray","Dumpling_Sprinkles-smileys","Dumpling_2-silver","Dumpling_Sprinkles-lollipops","Dumpling_3-ink","Dumpling_color1","Dumpling_colo3","Dumpling_colo4","Dumpling_glitter-blue-big","Dumpling_colo2","Dumpling_1-gold","Dumpling_Sprinkles-gummy-bears","Dumpling_Sprinkles-stars"};
    public static bool Applies(string path)=>path.StartsWith("Assets/GameContent/Art/",StringComparison.Ordinal);
    public static int Limit(TextureImporter importer,string target)
    {
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        bool android=target=="Android";
        int minimum=android?128:256;
        // Narrow streams are enlarged during pouring and need more detail than the general baseline.
        string name=Path.GetFileNameWithoutExtension(importer.assetPath);
        if(name.EndsWith("-flow",StringComparison.Ordinal) || name=="Dumpling_flow-1" || name=="Dumpling_flow-2")
            minimum=android?512:1024;
        return Mathf.Max(minimum,PercentageLimit(width,height,android?.25f:.5f));
    }
    public static int PercentageLimit(int width,int height,float fraction)
    {
        // Use the largest supported power-of-two cap no greater than the requested source fraction.
        int desired=Mathf.Max(1,Mathf.FloorToInt(Mathf.Max(width,height)*fraction));
        int cap=32;while(cap<8192 && cap*2<=desired)cap*=2;
        return cap;
    }
    public static bool Detailed(string path)=>Heroes.Contains(Path.GetFileNameWithoutExtension(path)) || path.Contains("Dumpling_bowl-") || path.Contains("Dumpling_UI-Button") || path.Contains("Dumpling_face-");
    public static void Configure(TextureImporter importer)
    {
        if(!Applies(importer.assetPath))return;
        foreach(string target in new[]{"Android","iPhone"})
        {
            var settings=importer.GetPlatformTextureSettings(target);settings.name=target;settings.overridden=true;settings.maxTextureSize=Limit(importer,target);
            settings.format=target=="Android" ? (importer.DoesSourceTextureHaveAlpha()?TextureImporterFormat.ETC2_RGBA8:TextureImporterFormat.ETC2_RGB4)
                : (Detailed(importer.assetPath)?TextureImporterFormat.ASTC_4x4:TextureImporterFormat.ASTC_6x6);
            settings.compressionQuality=target=="Android"?50:100;settings.crunchedCompression=false;importer.SetPlatformTextureSettings(settings);
        }
    }
    [MenuItem("Squishy Dumpling/Textures/Apply mobile texture policy")]
    public static void ApplyAll()
    {
        foreach(string path in AssetDatabase.GetAllAssetPaths().Where(Applies))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)continue;
            Configure(importer);importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();Debug.Log("Mobile texture policy applied: Android ETC2; iOS ASTC; source-relative size limits (Android 25% minimum 128, iOS 50% minimum 256; pouring flows minimum 512/1024).");
    }
    public static void Validate()
    {
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        foreach(string path in AssetDatabase.GetDependencies(scenes,true))
        {
            if(path.StartsWith("Assets/Creation/Art/",StringComparison.Ordinal))throw new BuildFailedException("Removed placeholder artwork dependency: "+path);
            if(path=="Assets/ResponsiveLayout/Art/CanonicalBackground.png")throw new BuildFailedException("Stale CanonicalBackground texture dependency in production scenes.");
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer || !Applies(path))continue;
            foreach(string target in new[]{"Android","iPhone"})
            {
                var s=importer.GetPlatformTextureSettings(target);
                var expected=target=="Android" ? (importer.DoesSourceTextureHaveAlpha()?TextureImporterFormat.ETC2_RGBA8:TextureImporterFormat.ETC2_RGB4)
                    : (Detailed(path)?TextureImporterFormat.ASTC_4x4:TextureImporterFormat.ASTC_6x6);
                if(!s.overridden || s.maxTextureSize!=Limit(importer,target) || s.format!=expected || s.compressionQuality!=(target=="Android"?50:100))
                    throw new BuildFailedException("Unexpected mobile texture settings: "+path+" / "+target);
            }
        }
        Debug.Log("Mobile texture dependency/settings validation passed.");
    }
}
public sealed class MobileTextureImportPolicy : AssetPostprocessor
{
    void OnPreprocessTexture(){MobileTexturePolicy.Configure((TextureImporter)assetImporter);}
}
public sealed class MobileTextureBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder=>0;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform==BuildTarget.Android || report.summary.platform==BuildTarget.iOS)MobileTexturePolicy.Validate();
    }
}
