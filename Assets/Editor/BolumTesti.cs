using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Bölümlerin otomatik oynanış testi. Komut satırından çalıştırılır:
//   Unity -batchmode -projectPath . -executeMethod BolumTesti.Agiz
//   Unity -batchmode -projectPath . -executeMethod BolumTesti.Mide
// Bölümü sanal bir klavyeyle oynar, bölüm sonuna ulaşıldığını doğrular ve her aşamada
// VY_CIKTI klasörüne (yoksa Temp/BolumTesti) ekran görüntüsü kaydeder.
[InitializeOnLoad]
static class BolumTesti
{
    const string Anahtar = "VY.BolumTesti";
    static bool bagli;
    static string bolum;
    static int asama, hata;
    static double sonraki, baslangic, asamaBasi;
    static Keyboard klavye;
    static float ilkZ;
    static bool araGoruntu;

    static BolumTesti()
    {
        if (SessionState.GetString(Anahtar, "") != "") Baglan();
    }

    public static void Agiz() => Calistir("Agiz");
    public static void Mide() => Calistir("YemekBorusuMide");

    static void Calistir(string sahne)
    {
        BolumSahneleri.SahneleriOlustur();
        SessionState.SetString(Anahtar, sahne);
        EditorSceneManager.OpenScene(BolumSahneleri.Yol(sahne));
        Baglan();
        EditorApplication.EnterPlaymode();
    }

    static void Baglan()
    {
        if (bagli) return;
        bagli = true;
        bolum = SessionState.GetString(Anahtar, "");
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
        if (t - baslangic > 300) Bitir(2, "zaman aşımı, aşama " + asama);
        if (!EditorApplication.isPlaying || BolumTemeli.Ornek == null || t < sonraki) return;

        if (klavye == null)
        {
            // Sanal klavye: editör penceresi odakta olmasa da girdiler oyuna ulaşsın
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            klavye = InputSystem.AddDevice<Keyboard>();
        }

        int onceki = asama;
        if (bolum == "Agiz") AgizAdimi(t); else MideAdimi(t);
        if (asama != onceki) { asamaBasi = t; araGoruntu = false; }
    }

    static void Tus(params Key[] tuslar) => InputSystem.QueueStateEvent(klavye, new KeyboardState(tuslar));

    static void Sonra(double saniye, double t, int yeniAsama)
    {
        sonraki = t + saniye;
        asama = yeniAsama;
    }

    // ---------------------------------------------------------------- Bölüm 1

    static void AgizAdimi(double t)
    {
        var b = AgizBolumu.Ornek;
        switch (asama)
        {
            case 0: Cek("agiz_0_baslik"); Sonra(0.5, t, 1); break;
            case 1: Tus(Key.Space); Sonra(0.3, t, 2); break;
            case 2:
                Tus();
                if (b.DurumAdi != "Oyun") Debug.LogError("GIRDI: BOŞLUK oyunu başlatmadı");
                Sonra(0.3, t, 3); break;
            case 3:
                ilkZ = GameObject.Find("Arac").transform.position.z;
                Tus(Key.W, Key.Space);
                Sonra(1.2, t, 4); break;
            case 4:
                float gidilen = GameObject.Find("Arac").transform.position.z - ilkZ;
                bool kopuk = GameObject.Find("Kopuk") != null;
                Tus();
                Debug.Log($"GIRDI gidilen={gidilen:0.0} kopuk={kopuk}");
                if (gidilen < 4f) Debug.LogError("GIRDI: W tuşu aracı ilerletmedi");
                if (!kopuk) Debug.LogError("GIRDI: BOŞLUK köpük atmadı");
                Sonra(1.5, t, 5); break;
            case 5: Cek("agiz_1_oyun"); b.DalgaGonder(); Sonra(1.6, t, 6); break;
            case 6: Cek("agiz_2_dalga"); Sonra(0.3, t, 7); break;
            case 7:
                Mikrop siradaki = null;
                foreach (var m in b.Mikroplar)
                {
                    if (m != null && !m.Eriyor) { siradaki = m; break; }
                }
                if (siradaki != null) { siradaki.Temizle(); sonraki = t + 0.3; }
                else Sonra(5, t, 8);
                break;
            case 8: Cek("agiz_3_kart"); Tus(Key.Space); Sonra(0.3, t, 9); break;
            case 9: Tus(); Sonra(9, t, 10); break;
            case 10:
                Cek("agiz_4_bitis");
                Bitir(hata == 0 && b.DurumAdi == "Bitti" ? 0 : 1, $"durum={b.DurumAdi} hata={hata}");
                break;
        }
    }

    // ---------------------------------------------------------------- Bölüm 2

    static void MideAdimi(double t)
    {
        var b = MideBolumu.Ornek;
        switch (asama)
        {
            case 0: Cek("mide_0_baslik"); Sonra(0.5, t, 1); break;
            case 1: Tus(Key.Space); Sonra(0.3, t, 2); break;
            case 2:
                Tus();
                if (b.DurumAdi != "Oyun") Debug.LogError("GIRDI: BOŞLUK oyunu başlatmadı");
                Sonra(3, t, 3); break;
            case 3: Cek("mide_1_boru"); Tus(Key.D); Sonra(0.6, t, 4); break;
            case 4:
                // Yemek borusu: köpük atarak in (lokmaları dağıtır), mideye varana kadar bekle
                Tus(Key.Space);
                if (!araGoruntu && t - asamaBasi > 8) { araGoruntu = true; Cek("mide_2_boru_kasilma"); }
                if (b.AsamaAdi == "Mide") Sonra(1.5, t, 5); else sonraki = t + 0.25;
                break;
            case 5: Cek("mide_3_mide"); Sonra(0.1, t, 6); break;
            case 6:
                // Mide: dümdüz ilerle, köpükle asidi nötrleyerek çıkışa ulaş
                Tus(Key.W, Key.Space);
                if (!araGoruntu && t - asamaBasi > 3.5) { araGoruntu = true; Cek("mide_4_asit"); }
                if (b.DurumAdi == "Kart") { Tus(); Sonra(5, t, 7); } else sonraki = t + 0.25;
                break;
            case 7: Cek("mide_5_kart"); Tus(Key.Space); Sonra(0.3, t, 8); break;
            case 8: Tus(); Sonra(8, t, 9); break;
            case 9:
                Cek("mide_6_bitis");
                Bitir(hata == 0 && b.DurumAdi == "Bitti" ? 0 : 1, $"durum={b.DurumAdi} hata={hata} sure={t - baslangic:0}");
                break;
        }
    }

    static void Bitir(int kod, string ozet)
    {
        Debug.Log($"BOLUM_TESTI {bolum} kod={kod} {ozet}");
        SessionState.SetString(Anahtar, "");
        EditorApplication.Exit(kod);
    }

    // Ana kameradan (arayüz dahil) bir kare alıp PNG olarak kaydeder.
    static void Cek(string ad)
    {
        var kamera = Camera.main;
        if (kamera == null) return;
        string klasor = Environment.GetEnvironmentVariable("VY_CIKTI");
        if (string.IsNullOrEmpty(klasor)) klasor = "Temp/BolumTesti";
        Directory.CreateDirectory(klasor);

        var tuval = BolumTemeli.Ornek.GetComponentInChildren<Canvas>();
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
