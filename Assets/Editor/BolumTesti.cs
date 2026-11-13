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
//   Unity -batchmode -projectPath . -executeMethod BolumTesti.Bagirsak
//   Unity -batchmode -projectPath . -executeMethod BolumTesti.Kalp
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
    static float ilkZ, ilkAci;
    static int cekilen;
    static bool araGoruntu;

    static BolumTesti()
    {
        if (SessionState.GetString(Anahtar, "") != "") Baglan();
    }

    public static void Agiz() => Calistir("Agiz");
    public static void Mide() => Calistir("YemekBorusuMide");
    public static void Bagirsak() => Calistir("Bagirsak");
    public static void Kalp() => Calistir("KanKalp");
    public static void Akciger() => Calistir("Akciger");

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
            // Editörün kendi arama dizini hataları oyunla ilgili değil; sayılmaz.
            if (iz != null && iz.Contains("UnityEditor.Search")) return;
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
        if (bolum == "Agiz") AgizAdimi(t);
        else if (bolum == "Bagirsak") BagirsakAdimi(t);
        else if (bolum == "KanKalp") KalpAdimi(t);
        else if (bolum == "Akciger") AkcigerAdimi(t);
        else MideAdimi(t);
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
                Sonra(0.5, t, 20); break;

            // Duvara çarpma: araç dönerek dişlere çarpar; tuşlar bırakılınca kendi kendine dönmemeli
            case 20: Tus(Key.W, Key.D); Sonra(2.5, t, 21); break;
            case 21: Tus(Key.W); Sonra(1.5, t, 22); break;
            case 22: Tus(); Sonra(0.6, t, 23); break;
            case 23: ilkAci = GameObject.Find("Arac").transform.eulerAngles.y; Sonra(0.5, t, 24); break;
            case 24:
                float donme = Mathf.Abs(Mathf.DeltaAngle(ilkAci, GameObject.Find("Arac").transform.eulerAngles.y)) / 0.5f;
                Debug.Log($"DONME tuşsuz dönüş hızı={donme:0} derece/sn");
                if (donme > 15f) Debug.LogError("DONME: araç tuşa basılmadan kendi kendine dönüyor");
                Sonra(1.0, t, 5); break;
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

    // ---------------------------------------------------------------- Bölüm 3

    static void BagirsakAdimi(double t)
    {
        var b = BagirsakBolumu.Ornek;
        switch (asama)
        {
            case 0: Cek("bagirsak_0_baslik"); Sonra(0.5, t, 1); break;
            case 1: Tus(Key.Space); Sonra(0.3, t, 2); break;
            case 2:
                Tus();
                if (b.DurumAdi != "Oyun") Debug.LogError("GIRDI: BOŞLUK oyunu başlatmadı");
                Sonra(1.5, t, 3); break;
            case 3: Cek("bagirsak_1_tunel"); Sonra(0.1, t, 4); break;
            case 4:
                // Yolu izleyerek sür. Kuşatılmış bir koloniye gelince dur, en yakın saldırgana
                // dönüp nişan alınca köpük at (dostları vurmamak için hizalıyken ateş et).
                if (b.DurumAdi == "Kart") { Tus(); Sonra(5, t, 5); break; }
                var arac = GameObject.Find("Arac").transform;
                bool savas = b.YakinSaldirgan(out Vector3 dusman);
                Vector3 yon = (savas ? dusman : b.IleridekiNokta()) - arac.position;
                yon.y = 0f;
                float aci = Vector3.SignedAngle(arac.forward, yon, Vector3.up);
                var tuslar = new System.Collections.Generic.List<Key>();
                if (savas)
                {
                    if (Mathf.Abs(aci) < 5f) tuslar.Add(Key.Space);
                    if (yon.magnitude > 9f && Mathf.Abs(aci) < 40f) tuslar.Add(Key.W);
                }
                else if (Mathf.Abs(aci) < 75f) tuslar.Add(Key.W);
                if (aci > 3f) tuslar.Add(Key.D); else if (aci < -3f) tuslar.Add(Key.A);
                Tus(tuslar.ToArray());
                double gecen = t - asamaBasi;
                if (!araGoruntu && gecen > 5) { araGoruntu = true; Cek("bagirsak_2_kusatma"); }
                if (gecen > 40 && gecen < 40.2) Cek("bagirsak_3_yol");
                sonraki = t + 0.03;
                break;
            case 5: Cek("bagirsak_4_kart"); Tus(Key.Space); Sonra(0.3, t, 6); break;
            case 6: Tus(); Sonra(8, t, 7); break;
            case 7:
                Cek("bagirsak_5_bitis");
                Bitir(hata == 0 && b.DurumAdi == "Bitti" ? 0 : 1,
                    $"durum={b.DurumAdi} hata={hata} kurtarilan={b.Kurtarilan} sure={t - baslangic:0}");
                break;
        }
    }

    // ---------------------------------------------------------------- Bölüm 4

    static void KalpAdimi(double t)
    {
        var b = KalpBolumu.Ornek;
        switch (asama)
        {
            case 0: Cek("kalp_0_baslik"); Sonra(0.5, t, 1); break;
            case 1: Tus(Key.Space); Sonra(0.3, t, 2); break;
            case 2:
                Tus();
                if (b.DurumAdi != "Oyun") Debug.LogError("GIRDI: BOŞLUK oyunu başlatmadı");
                Sonra(4, t, 3); break;
            case 3: Cek("kalp_1_damar"); Tus(Key.D, Key.W); Sonra(0.8, t, 4); break;
            case 4:
                // Damar: köpük atarak akışla ilerle (akyuvarları sakinleştirir)
                if (b.AsamaAdi != "Damar") { Tus(); Cek("kalp_3_kulakcik"); Sonra(0.5, t, 5); break; }
                Tus(Key.Space);
                if (!araGoruntu && t - asamaBasi > 12) { araGoruntu = true; Cek("kalp_2_akyuvar"); }
                sonraki = t + 0.2;
                break;
            case 5:
                // Kalp: sıradaki kapak açılır açılmaz BOŞLUK ile atıl
                if (b.DurumAdi == "Kart") { Tus(); Sonra(5, t, 6); break; }
                if (b.AsamaAdi == "Atardamar" && !araGoruntu) { araGoruntu = true; Cek("kalp_4_atardamar"); }
                if (b.SiradakiKapakAcik) Tus(Key.Space); else Tus();
                sonraki = t + 0.03;
                break;
            case 6: Cek("kalp_5_kart"); Tus(Key.Space); Sonra(0.3, t, 7); break;
            case 7: Tus(); Sonra(8, t, 8); break;
            case 8:
                Cek("kalp_6_bitis");
                Bitir(hata == 0 && b.DurumAdi == "Bitti" ? 0 : 1, $"durum={b.DurumAdi} hata={hata} sure={t - baslangic:0}");
                break;
        }
    }

    // ---------------------------------------------------------------- Bölüm 5 (gezinti)

    static void AkcigerAdimi(double t)
    {
        var b = BolumTemeli.Ornek;
        switch (asama)
        {
            case 0: Cek("akciger_0_baslik"); Sonra(0.5, t, 1); break;
            case 1: Tus(Key.Space); Sonra(0.3, t, 2); break;
            case 2: Tus(); Sonra(0.1, t, 3); break;
            case 3:
                // Oynanış yok: yalnızca izle, yol boyunca görüntü al
                double gecen = t - asamaBasi;
                foreach (int an in new[] { 3, 9, 15, 21, 27 })
                {
                    if (gecen > an && gecen < an + 0.25 && cekilen < an) { cekilen = an; Cek("akciger_" + an + "sn"); }
                }
                if (b.DurumAdi == "Kart") Sonra(5, t, 4); else sonraki = t + 0.2;
                break;
            case 4: Cek("akciger_kart"); Tus(Key.Space); Sonra(0.3, t, 5); break;
            case 5: Tus(); Sonra(8, t, 6); break;
            case 6:
                Cek("akciger_bitis");
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
