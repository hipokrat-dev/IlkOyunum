using UnityEditor;
using UnityEngine;
public static class MobilVarlikHazirligi
{
    public static void Profil()
    {
        var asset=AssetDatabase.LoadAssetAtPath<Object>("Assets/Settings/Mobile_RPAsset.asset");
        var so=new SerializedObject(asset);
        so.FindProperty("m_AdditionalLightsRenderingMode").intValue=2;
        so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=2;
        so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;
        so.FindProperty("m_ShadowDistance").floatValue=28;
        so.FindProperty("m_MainLightShadowmapResolution").intValue=1024;
        so.FindProperty("m_SoftShadowsSupported").boolValue=true;
        so.FindProperty("m_SoftShadowQuality").intValue=1;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
    public static void AndroidDerle()
    {
        Profil();
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources"})) {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)continue;
            bool ui=path.Contains("/Arayuz/");
            importer.isReadable=false;
            importer.mipmapEnabled=!ui;
            importer.anisoLevel=ui ? 1 : 4;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name="Android",overridden=true,maxTextureSize=ui ? 2048 : 1024,
                format=TextureImporterFormat.ASTC_6x6,compressionQuality=80
            });
            importer.SaveAndReimport();
        }
        Derleme.MalzemeSablonlariniOlustur();
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Resources/Malzemeler"})) {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            m.enableInstancing=true;EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
        Derleme.AndroidDerle();
    }
}
