using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Android (tablet) paketi oluşturma ve derlenmiş oyunun doğru görünmesi için gereken hazırlıklar.
//   Unity -batchmode -quit -projectPath . -executeMethod Derleme.AndroidDerle
static class Derleme
{
    const string SablonKlasoru = "Assets/Resources/Malzemeler";
    public const string ApkYolu = "Derlemeler/VucutYolculugu.apk";

    // Oyun malzemelerini çalışma anında kodla kurar. Derlenmiş oyunda bir gölgelendirici çeşidi ancak
    // onu kullanan bir malzeme projede varsa pakete girer; bu şablonlar gereken bütün çeşitleri
    // (saydam, normal haritalı, ışıldayan, ortam gölgeli) pakete dahil ettirir.
    [MenuItem("Vücut Yolculuğu/Derleme/Malzeme Şablonlarını Oluştur")]
    public static void MalzemeSablonlariniOlustur()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Directory.CreateDirectory(SablonKlasoru);
        var beyaz = Texture2D.whiteTexture;
        for (int i = 0; i < 32; i++)
        {
            bool saydam = (i & 1) != 0, normal = (i & 2) != 0, isilti = (i & 4) != 0, golge = (i & 8) != 0, islaklik = (i & 16) != 0;
            string yol = $"{SablonKlasoru}/Sablon_{i:00}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m == null)
            {
                m = new Material(lit);
                AssetDatabase.CreateAsset(m, yol);
            }
            m.shader = lit;
            m.shaderKeywords = new string[0];
            if (saydam)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if (normal)
            {
                m.SetTexture("_BumpMap", Texture2D.normalTexture);
                m.EnableKeyword("_NORMALMAP");
            }
            if (isilti)
            {
                m.SetColor("_EmissionColor", Color.white);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (golge)
            {
                m.SetTexture("_OcclusionMap", beyaz);
                m.EnableKeyword("_OCCLUSIONMAP");
            }
            if (islaklik)
            {
                m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/AgizV2/dil_mask.png"));
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Vücut Yolculuğu/Derleme/Android Ayarlarını Uygula")]
    public static void AndroidAyarla()
    {
        MalzemeSablonlariniOlustur();
        BolumSahneleri.SahneleriOlustur();

        PlayerSettings.companyName = "VucutYolculugu";
        PlayerSettings.productName = "Vücut Yolculuğu";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.vucutyolculugu.oyun");

        // Yatay ekran, tüm ekranı kaplar (çentik bölgesi dahil); arayüz güvenli alana kendisi yerleşir.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.Android.resizeableActivity = true;

        // Yeni tabletlerin tamamı 64 bit
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Sis sahnelerde kodla açılıyor; derlemede sis çeşitleri ayıklanmasın.
        var grafik = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        grafik.FindProperty("m_FogStripping").intValue = 1;       // özel
        grafik.FindProperty("m_FogKeepLinear").boolValue = false;
        grafik.FindProperty("m_FogKeepExp").boolValue = false;
        grafik.FindProperty("m_FogKeepExp2").boolValue = true;
        grafik.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log("Android ayarları uygulandı.");
    }

    [MenuItem("Vücut Yolculuğu/Derleme/Android Paketi (APK) Oluştur")]
    public static void AndroidDerle()
    {
        AndroidAyarla();
        var sahneler = new List<string>();
        foreach (var s in EditorBuildSettings.scenes) if (s.enabled) sahneler.Add(s.path);
        Directory.CreateDirectory(Path.GetDirectoryName(ApkYolu));

        var rapor = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = sahneler.ToArray(),
            locationPathName = ApkYolu,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        var ozet = rapor.summary;
        Debug.Log($"ANDROID_DERLEME sonuc={ozet.result} hata={ozet.totalErrors} boyut={ozet.totalSize / (1024 * 1024)}MB sure={ozet.totalTime.TotalSeconds:0}sn");
        if (Application.isBatchMode) EditorApplication.Exit(ozet.result == BuildResult.Succeeded ? 0 : 1);
    }
}
