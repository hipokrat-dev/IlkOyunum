using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Dinamik sahnelerin grafik bağımlılıkları: hem menüden hem MCP/Build Profiles'tan derlemede denetlenir.
public class AndroidGrafikHazirligi : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    [MenuItem("Vücut Yolculuğu/Derleme/Android Grafik Uyumluluğunu Hazırla")]
    public static string Uygula()
    {
        Derleme.MalzemeSablonlariniOlustur();
        var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        GraphicsSettings.defaultRenderPipeline=mobile;
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
        var so=new SerializedObject(renderer);
        so.FindProperty("m_RenderingMode").intValue=(int)RenderingMode.Forward;
        var nativePass=so.FindProperty("m_UseNativeRenderPass");
        if(nativePass!=null)nativePass.boolValue=false;
        so.ApplyModifiedPropertiesWithoutUndo();
        // Efektler TakipKamera tarafından çalışma anında kurulur; sahne taraması bunları göremez.
        var strip=GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
        strip.stripUnusedPostProcessingVariants=false;
        var globals=AssetDatabase.LoadAssetAtPath<Object>("Assets/Settings/UniversalRenderPipelineGlobalSettings.asset");
        EditorUtility.SetDirty(globals);
        // Oyun Forward ve en fazla iki ek ışık kullanır; compute/Forward+ gerekmez.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.Android.bundleVersionCode=Mathf.Max(2,PlayerSettings.Android.bundleVersionCode);
        PlayerSettings.bundleVersion="1.0.1";
        AssetDatabase.SaveAssets();
        Dogrula();
        return "Android 1.0.1 (2): URP material reference, Forward/OpenGLES3, runtime post effects retained";
    }
    public void OnPreprocessBuild(BuildReport report){if(report.summary.platform==BuildTarget.Android)Dogrula();}
    public static void Dogrula()
    {
        var material=Resources.Load<Material>("Malzemeler/Sablon_00");
        if(material==null||material.shader==null||material.shader.name!="Universal Render Pipeline/Lit")
            throw new BuildFailedException("APK için URP malzeme şablonu eksik. Android Grafik Uyumluluğunu Hazırla komutunu çalıştırın.");
        var strip=GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
        if(strip==null||strip.stripUnusedPostProcessingVariants)
            throw new BuildFailedException("Dinamik kamera efektleri APK'dan ayıklanamaz. Android grafik hazırlığını çalıştırın.");
        var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if(mobile==null||GraphicsSettings.defaultRenderPipeline!=mobile)
            throw new BuildFailedException("Android URP profil bağlantısı eksik.");
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
        if(renderer==null||renderer.renderingMode!=RenderingMode.Forward)
            throw new BuildFailedException("Mobil renderer Forward olmalı.");
        for(int i=0;i<32;i++)if(Resources.Load<Material>("Malzemeler/Sablon_"+i.ToString("00"))==null)
            throw new BuildFailedException("Eksik dinamik malzeme çeşidi: "+i);
    }
}
