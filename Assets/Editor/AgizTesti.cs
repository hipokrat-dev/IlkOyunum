using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ağız bölümünün otomatik duman testi. Komut satırından çalıştırılır:
//   Unity -batchmode -projectPath . -executeMethod AgizTesti.Calistir
// Bölümü oynatır, tüm mikropları temizler, kartı kapatır, bölüm sonuna ulaşıldığını doğrular
// ve her aşamada VY_CIKTI klasörüne (yoksa Temp/AgizTesti) ekran görüntüsü kaydeder.
[InitializeOnLoad]
static class AgizTesti
{
    const string Anahtar = "VY.AgizTesti";
    static bool bagli;
    static int asama, hata;
    static double sonraki, baslangic;

    static AgizTesti()
    {
        if (SessionState.GetBool(Anahtar, false)) Baglan();
    }

    public static void Calistir()
    {
        SessionState.SetBool(Anahtar, true);
        EditorSceneManager.OpenScene(BolumSahneleri.AgizYolu);
        Baglan();
        EditorApplication.EnterPlaymode();
    }

    static void Baglan()
    {
        if (bagli) return;
        bagli = true;
        baslangic = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += (mesaj, iz, tur) =>
        {
            if (tur == LogType.Error || tur == LogType.Exception || tur == LogType.Assert) hata++;
        };
        EditorApplication.update += Guncelle;
    }

    static void Guncelle()
    {
        double t = EditorApplication.timeSinceStartup;
        if (t - baslangic > 180) Bitir(2, "zaman aşımı");
        if (!EditorApplication.isPlaying || AgizBolumu.Ornek == null || t < sonraki) return;
        var bolum = AgizBolumu.Ornek;

        switch (asama)
        {
            case 0: sonraki = t + 2.5; asama++; break;
            case 1: Cek("1_baslangic"); bolum.DalgaGonder(); sonraki = t + 1.6; asama++; break;
            case 2: Cek("2_dalga"); sonraki = t + 0.3; asama++; break;
            case 3:
                Mikrop siradaki = null;
                foreach (var m in bolum.Mikroplar)
                {
                    if (m != null && !m.Eriyor) { siradaki = m; break; }
                }
                if (siradaki != null) { siradaki.Temizle(); sonraki = t + 0.3; }
                else { sonraki = t + 5; asama++; }
                break;
            case 4: Cek("3_kart"); bolum.KartiKapat(); sonraki = t + 3.2; asama++; break;
            case 5: Cek("4_yutkunma"); sonraki = t + 5; asama++; break;
            case 6:
                Cek("5_bitis");
                Bitir(hata == 0 && bolum.DurumAdi == "Bitti" ? 0 : 1, $"durum={bolum.DurumAdi} hata={hata}");
                break;
        }
    }

    static void Bitir(int kod, string ozet)
    {
        Debug.Log($"AGIZ_TESTI kod={kod} {ozet}");
        SessionState.SetBool(Anahtar, false);
        EditorApplication.Exit(kod);
    }

    // Ana kameradan (arayüz dahil) bir kare alıp PNG olarak kaydeder.
    static void Cek(string ad)
    {
        var kamera = Camera.main;
        if (kamera == null) return;
        string klasor = Environment.GetEnvironmentVariable("VY_CIKTI");
        if (string.IsNullOrEmpty(klasor)) klasor = "Temp/AgizTesti";
        Directory.CreateDirectory(klasor);

        var tuval = AgizBolumu.Ornek.GetComponentInChildren<Canvas>();
        if (tuval != null)
        {
            tuval.renderMode = RenderMode.ScreenSpaceCamera;
            tuval.worldCamera = kamera;
            tuval.planeDistance = 0.5f;
        }

        var hedef = new RenderTexture(1280, 720, 24);
        kamera.targetTexture = hedef;
        kamera.Render();
        RenderTexture.active = hedef;
        var goruntu = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        goruntu.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        goruntu.Apply();
        File.WriteAllBytes(Path.Combine(klasor, ad + ".png"), goruntu.EncodeToPNG());

        RenderTexture.active = null;
        kamera.targetTexture = null;
        if (tuval != null) tuval.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.Object.DestroyImmediate(goruntu);
        hedef.Release();
    }
}
