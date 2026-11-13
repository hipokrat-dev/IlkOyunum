using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 2 — Yemek borusu ve mide.
// 1) Yemek borusu: araç tüpün içinde kendiliğinden kayar; oyuncu A/D ile duvarda sağa sola kayıp
//    lokma parçalarından kaçar (ya da köpükle dağıtır). Arkadan gelen kasılma dalgası (peristaltik
//    hareket) aracı hızlandırır.
// 2) Mide: asit gölleri kalkanı eritir. Köpük asidi kısa süreliğine nötrler; oyuncu kendine yol açıp
//    mide çıkışına ulaşır.
public class MideBolumu : BolumTemeli
{
    enum Asama { Boru, Gecis, Mide }

    // Yemek borusu: +Z yönünde uzanan tüp (mideden uzakta, x = BoruX hattında)
    const float BoruX = -500f, BoruR = 5f, BoruL = 480f, AracR = 4.65f;
    const float NormalHiz = 15f, ItisHizi = 27f;

    // Mide: tabanı elips (merkez z = MideZ, yarıçaplar MideA x MideB), çıkış +Z ucunda
    const float MideZ = 36f, MideA = 17f, MideB = 42f, CikisZ = 71f;

    public new static MideBolumu Ornek => BolumTemeli.Ornek as MideBolumu;
    public string AsamaAdi => asama.ToString();

    Asama asama = Asama.Boru;
    Transform boruKok, mideKok;
    GameObject mideDuvarlari;
    Material mukoza, kasMalzemesi;
    Mesh kure;

    // Yemek borusu durumu
    readonly List<Lokma> lokmalar = new List<Lokma>();
    Transform kasilma;
    float boruZ = 4f, boruAci, hiz, itisSuresi, kasilmaZ, kasilmaSayaci = 7f;
    bool kasilmaItti, kasilmaAnlatildi, carpmaAnlatildi, kapakAnlatildi;
    int lokmaCarpma;

    // Mide durumu
    static readonly float[] GuvenliNoktalar = { 6f, 22f, 40f, 58f };
    float kalkan = 100f, asitDisiSure, cizirti, kasilmaSuresi, mideKasilmaSayaci = 12f;
    int kalkanYenileme, guvenliNokta;
    bool notrAnlatildi, ortaAnlatildi, mideKasilmaAnlatildi, kalkanAnlatildi;

    protected override int BolumNo => 2;
    protected override string BolumAdi => "Yemek Borusu ve Mide";
    protected override string SonrakiDurak => "Bağırsak";
    protected override string SonrakiSahne => "Bagirsak";
    protected override string KartBasligi => "VÜCUT KARTI: MİDE";
    protected override string KartMetni =>
        "•  Yemek borusundaki kasılma dalgaları sayesinde baş aşağı dursan bile lokma mideye iner.\n\n" +
        "•  Mide asidi bazı metalleri bile aşındıracak kadar güçlüdür; mide kendini kalın bir mukus tabakasıyla korur.\n\n" +
        "•  Yiyecekler midede birkaç saat çalkalanıp bulamaç haline gelir, sonra bağırsağa geçer.";

    // ------------------------------------------------------------------ Kurulum

    protected override void SahneyiKur()
    {
        AsitHavuzu.Tasma = 1f;
        kure = Sekil.SuperElips(1f, 1f, 14);
        var renk = Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var normal = Resources.Load<Texture2D>("Agiz/mukoza_normal");
        mukoza = Sekil.DokuluMalzeme(renk, normal, null, 0.72f, 1f, 0.8f);
        kasMalzemesi = Sekil.DokuluMalzeme(renk, normal, null, 0.8f, 1f, 0.9f);
        kasMalzemesi.SetColor("_BaseColor", new Color(1f, 0.72f, 0.7f));

        BoruyuKur();
        MideyiKur();
        Ortam(new Color(0.3f, 0.19f, 0.19f), new Color(0.1f, 0.03f, 0.04f), 0.02f);

        arac = AracKontrol.Olustur(Vector3.zero);
        arac.rayModu = true;
        arac.GetComponent<Rigidbody>().isKinematic = true;
        AraciBoruyaKoy();

        // Farın yetişmediği yerleri hafifçe aydınlatan çevre ışığı
        var cevre = new GameObject("CevreIsigi").AddComponent<Light>();
        cevre.transform.SetParent(arac.transform, false);
        cevre.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        cevre.type = LightType.Point;
        cevre.color = new Color(1f, 0.85f, 0.8f);
        cevre.intensity = 22f;
        cevre.range = 16f;
        cevre.shadows = LightShadows.None;

        kamera = TakipKamera.Olustur(arac.transform, new Color(0.04f, 0.01f, 0.012f));
        kamera.yerelTakip = true;
        kamera.ofset = new Vector3(0f, 3.1f, -7.5f);
        kamera.Isinla();
    }

    protected override void ArayuzHazir()
    {
        arayuz.Ipucu("A / D: Kay    BOŞLUK: Köpük    ESC: Duraklat");
        arayuz.Gosterge($"Mideye: {(int)(BoruL - boruZ)} m");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Kopuk.zeminKontrolu = null;
    }

    static void Ortam(Color cevreIsigi, Color sis, float sisYogunlugu)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = cevreIsigi;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = sis;
        RenderSettings.fogDensity = sisYogunlugu;
    }

    void BoruyuKur()
    {
        boruKok = new GameObject("YemekBorusu").transform;
        boruKok.position = new Vector3(BoruX, 0f, 0f);

        // Boyuna kıvrımlı (pilili) tüp duvarı
        var tup = Sekil.Tup(BoruL + 80f, 56, 420, (aci, z) =>
            BoruR + 0.22f * Mathf.Cos(aci * 10f) +
            0.3f * (Mathf.PerlinNoise(Mathf.Cos(aci) * 1.5f + 7f, Mathf.Sin(aci) * 1.5f + 7f + z * 0.08f) - 0.5f),
            6f, 100f);
        var duvar = Sekil.DokuluMalzeme(mukoza.GetTexture("_BaseMap") as Texture2D,
            mukoza.GetTexture("_BumpMap") as Texture2D, null, 0.5f, 1f, 0.5f);
        duvar.SetColor("_BaseColor", new Color(0.95f, 0.76f, 0.73f));       // yemek borusu ağızdan daha soluk
        Sekil.MeshParca(tup, "Duvar", boruKok, new Vector3(0f, 0f, -30f), Vector3.one, duvar)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Lokma parçaları: ekmek, yeşillik, domates tonlarında
        Color[] renkler = { new Color(0.78f, 0.6f, 0.36f), new Color(0.42f, 0.62f, 0.24f), new Color(0.75f, 0.25f, 0.18f) };
        var malzemeler = new Material[renkler.Length];
        for (int i = 0; i < renkler.Length; i++)
        {
            malzemeler[i] = Sekil.Malzeme(renkler[i], 0.3f, 0f, mukoza.GetTexture("_BumpMap") as Texture2D, 1.2f, 2f);
        }
        // Çiğnenmiş lokma: yumrulu, düzensiz bir topak
        var topak = Sekil.SuperElips(0.8f, 0.8f, 16, p =>
            p * (0.8f + 0.5f * Mathf.PerlinNoise(p.x * 1.6f + 3f, p.y * 1.6f + p.z * 1.3f + 3f)));

        for (float z = 45f; z < BoruL - 45f; z += Random.Range(11f, 16f))
        {
            float aci = Random.Range(0f, 360f);
            int adet = Random.value < 0.35f ? 2 : 1;
            for (int i = 0; i < adet; i++)
            {
                int r = Random.Range(0, renkler.Length);
                float a = aci + i * Random.Range(100f, 260f);
                var olcek = new Vector3(Random.Range(1.5f, 2.3f), Random.Range(1.1f, 1.7f), Random.Range(1.5f, 2.3f));
                lokmalar.Add(Lokma.Olustur(boruKok, topak, malzemeler[r], renkler[r],
                    DuvarYonu(a) * (BoruR - 1f) + Vector3.forward * z, olcek, z, a));
            }
        }

        // Tüpün sonunda mide kapağı (alt yemek borusu büzgeni)
        Sekil.MeshParca(Sekil.Halka(BoruR - 0.3f, 2.3f, 48, 16), "MideKapagi", boruKok,
            new Vector3(0f, 0f, BoruL + 8f), Vector3.one, kasMalzemesi);
    }

    void MideyiKur()
    {
        mideKok = new GameObject("Mide").transform;
        var zeminMalzemesi = Sekil.DokuluMalzeme(mukoza.GetTexture("_BaseMap") as Texture2D,
            mukoza.GetTexture("_BumpMap") as Texture2D, null, 0.55f, 7f, 0.6f);
        zeminMalzemesi.SetColor("_BaseColor", new Color(1f, 0.8f, 0.66f));
        var duvarMalzemesi = Sekil.DokuluMalzeme(mukoza.GetTexture("_BaseMap") as Texture2D,
            mukoza.GetTexture("_BumpMap") as Texture2D, null, 0.5f, 6f, 0.5f);
        duvarMalzemesi.SetColor("_BaseColor", new Color(0.85f, 0.6f, 0.5f));

        // Taban: mide kıvrımları (rugae) ve kenarlarda yukarı kıvrılan duvar
        var zemin = Sekil.Izgara(new Vector2(-24f, -14f), new Vector2(24f, 90f), 90, 150, (x, z) =>
        {
            float e = x * x / (MideA * MideA) + (z - MideZ) * (z - MideZ) / (MideB * MideB);
            float kenar = Mathf.Max(0f, e - 0.8f);
            return 0.12f * Mathf.Sin(x * 0.8f + 1.6f * Mathf.Sin(z * 0.22f)) + 5f * kenar * kenar;
        }, 1f);
        Sekil.MeshParca(zemin, "MideTabani", mideKok, Vector3.zero, Vector3.one, zeminMalzemesi);

        var carpisma = new GameObject("Zemin").AddComponent<BoxCollider>();
        carpisma.transform.SetParent(mideKok, false);
        carpisma.center = new Vector3(0f, -0.5f, MideZ + 6f);
        carpisma.size = new Vector3(70f, 1f, 120f);

        var kabuk = Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 32, null, true), "MideDuvari", mideKok,
            new Vector3(0f, 5f, MideZ), new Vector3(MideA + 5f, 14f, MideB + 6f), duvarMalzemesi);
        kabuk.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Araç midenin dışına çıkamasın: elips boyunca görünmez duvarlar
        mideDuvarlari = new GameObject("Duvarlar");
        mideDuvarlari.transform.SetParent(mideKok, false);
        const int Parca = 48;
        for (int i = 0; i < Parca; i++)
        {
            float a0 = 2f * Mathf.PI * i / Parca, a1 = 2f * Mathf.PI * (i + 1) / Parca;
            var p0 = new Vector3(MideA * Mathf.Cos(a0), 0f, MideZ + MideB * Mathf.Sin(a0));
            var p1 = new Vector3(MideA * Mathf.Cos(a1), 0f, MideZ + MideB * Mathf.Sin(a1));
            Vector3 yon = p1 - p0;
            var duvar = new GameObject("Duvar").AddComponent<BoxCollider>();
            duvar.transform.SetParent(mideDuvarlari.transform, false);
            duvar.transform.SetPositionAndRotation((p0 + p1) * 0.5f, Quaternion.LookRotation(yon));
            duvar.center = new Vector3(0f, 4f, 0f);
            duvar.size = new Vector3(1.5f, 9f, yon.magnitude + 1.5f);
        }

        // Asit gölleri: mideyi boydan boya kesen dört sıra + aralarda süzülen iki göl
        foreach (float z in new[] { 16f, 34f, 52f, 66f })
        {
            for (int i = -2; i <= 2; i++)
            {
                AsitHavuzu.Olustur(mideKok, kure, new Vector3(i * 6f, 0.06f, z + (i % 2 == 0 ? 0f : 0.8f)), 3.6f, 0f);
            }
        }
        AsitHavuzu.Olustur(mideKok, kure, new Vector3(0f, 0.06f, 27f), 2.5f, 9f);
        AsitHavuzu.Olustur(mideKok, kure, new Vector3(0f, 0.06f, 45f), 2.5f, 9f);

        // Mide çıkışı (pilor): ışıklı kas halkası
        Sekil.MeshParca(Sekil.Halka(4.4f, 1.4f, 48, 16), "MideCikisi", mideKok, new Vector3(0f, 3.2f, CikisZ + 5.5f),
            Vector3.one, kasMalzemesi);
        Isik(new Vector3(0f, 3.5f, CikisZ + 3f), new Color(1f, 0.8f, 0.55f), 40f, 16f);

        // Genel aydınlatma
        Isik(new Vector3(0f, 11f, 16f), new Color(1f, 0.75f, 0.6f), 110f, 45f);
        Isik(new Vector3(0f, 11f, 54f), new Color(1f, 0.75f, 0.6f), 110f, 45f);
    }

    void Isik(Vector3 konum, Color renk, float siddet, float menzil)
    {
        var isik = new GameObject("Isik").AddComponent<Light>();
        isik.transform.SetParent(mideKok, false);
        isik.transform.position = konum;
        isik.type = LightType.Point;
        isik.color = renk;
        isik.intensity = siddet;
        isik.range = menzil;
        isik.shadows = LightShadows.None;
    }

    // ------------------------------------------------------------------ Oyun

    protected override void OyunBasladi()
    {
        arayuz.Telsiz("Yutuldun çırak! Şu an yemek borusundasın: ağzı mideye bağlayan, yaklaşık 25 santimlik kaslı bir tüp.");
        arayuz.Telsiz("A ve D ile duvarda sağa sola kay, lokma parçalarına çarpma. İstersen köpükle onları dağıtabilirsin.");
    }

    protected override void OyunGuncelle()
    {
        if (asama == Asama.Mide) MideGuncelle();
        else BoruGuncelle();
    }

    // ------------------------------------------------------------------ Yemek borusu

    static Vector3 DuvarYonu(float aciDerece)
    {
        float r = aciDerece * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), -Mathf.Cos(r), 0f);          // 0 derece = tüpün tabanı
    }

    void AraciBoruyaKoy()
    {
        Vector3 disari = DuvarYonu(boruAci);
        arac.transform.SetPositionAndRotation(new Vector3(BoruX, 0f, boruZ) + disari * AracR,
            Quaternion.LookRotation(Vector3.forward, -disari));
    }

    void BoruGuncelle()
    {
        float dt = Time.deltaTime;
        itisSuresi -= dt;
        hiz = Mathf.MoveTowards(hiz, itisSuresi > 0f ? ItisHizi : NormalHiz, (itisSuresi > 0f ? 30f : 7f) * dt);
        boruZ += hiz * dt;
        if (asama == Asama.Boru) boruAci += arac.DonusGirdi * 130f * dt;
        arac.rayHizi = hiz;
        AraciBoruyaKoy();
        kameraGozu.fieldOfView = Mathf.Lerp(kameraGozu.fieldOfView, itisSuresi > 0f ? 76f : 60f, 3f * dt);
        if (asama != Asama.Boru) return;

        arayuz.Gosterge($"Mideye: {Mathf.Max(0, (int)(BoruL - boruZ))} m");

        // Lokmaya çarpma
        foreach (Lokma lokma in lokmalar)
        {
            if (lokma == null || lokma.Dagildi || Mathf.Abs(lokma.z - boruZ) > 1.7f) continue;
            if (Mathf.Abs(Mathf.DeltaAngle(boruAci, lokma.aci)) > 27f) continue;

            lokma.Dagit();
            lokmaCarpma++;
            hiz = 5f;
            itisSuresi = 0f;
            kamera.sarsinti = 0.22f;
            Ses.Cal(Ses.Tur.Carpma);
            if (!carpmaAnlatildi)
            {
                carpmaAnlatildi = true;
                arayuz.Telsiz("Bir lokmaya çarpıp yavaşladın! A ve D ile kenara kay ya da köpükle önünü aç.", true);
            }
        }

        KasilmaGuncelle(dt);

        if (!kapakAnlatildi && boruZ > BoruL - 110f)
        {
            kapakAnlatildi = true;
            arayuz.Telsiz("İleride mide kapağı var. Lokma gelince açılır, sonra asit geri kaçmasın diye sıkıca kapanır.", true);
        }
        if (boruZ >= BoruL) StartCoroutine(MideyeGec());
    }

    // Peristaltik hareket: arkadan gelip aracı ileri iten, sonra önden uzaklaşan kas halkası
    void KasilmaGuncelle(float dt)
    {
        if (kasilma == null)
        {
            kasilmaSayaci -= dt;
            if (kasilmaSayaci > 0f || boruZ > BoruL - 70f) return;
            kasilmaSayaci = Random.Range(9f, 12f);
            kasilmaZ = boruZ - 32f;
            kasilmaItti = false;
            kasilma = Sekil.MeshParca(Sekil.Halka(BoruR + 0.6f, 1.5f, 48, 14), "Kasilma", boruKok,
                Vector3.zero, Vector3.one, kasMalzemesi).transform;
            if (!kasilmaAnlatildi)
            {
                arayuz.Telsiz("Arkandan bir kasılma dalgası geliyor! Buna peristaltik hareket denir; lokmayı sıkıp mideye doğru iter.", true);
            }
        }

        kasilmaZ += (hiz + 12f) * dt;
        kasilma.localPosition = new Vector3(0f, 0f, kasilmaZ);

        if (!kasilmaItti && kasilmaZ >= boruZ)
        {
            kasilmaItti = true;
            itisSuresi = 2.6f;
            kamera.sarsinti = 0.15f;
            Ses.Cal(Ses.Tur.Dalga, 0.8f, 1.3f);
            if (!kasilmaAnlatildi)
            {
                kasilmaAnlatildi = true;
                arayuz.Telsiz("İşte itiş! Bu dalgalar sayesinde baş aşağı dursan bile yuttuğun lokma mideye ulaşır.");
            }
        }
        if (kasilmaZ > boruZ + 70f) Destroy(kasilma.gameObject);
    }

    IEnumerator MideyeGec()
    {
        asama = Asama.Gecis;
        arac.kontrolAcik = false;
        arayuz.HedefIsareti(null);
        Ses.Cal(Ses.Tur.Yutkunma, 0.8f);
        yield return arayuz.Karart(0.5f);

        // Araç artık midenin girişinde, normal sürüşte
        Destroy(boruKok.gameObject);
        arac.rayModu = false;
        arac.Isinla(new Vector3(0f, 0.2f, GuvenliNoktalar[0]), Quaternion.identity);
        kamera.yerelTakip = false;
        kamera.ofset = new Vector3(0f, 6.5f, -8f);
        kamera.sinir = MideSiniri;
        kamera.Isinla();
        kameraGozu.fieldOfView = 58f;
        Ortam(new Color(0.34f, 0.22f, 0.15f), new Color(0.1f, 0.05f, 0.02f), 0.012f);
        YansimaSondasi(new Vector3(0f, 5f, MideZ), new Vector3(60f, 40f, 110f)).transform.SetParent(mideKok, true);
        Kopuk.zeminKontrolu = AsitKontrolu;
        asama = Asama.Mide;

        arayuz.Ipucu("WASD / Oklar: Sür    BOŞLUK: Köpük    ESC: Duraklat");
        arayuz.Gosterge("Kalkan: %100");
        yield return arayuz.Aydinlat(0.7f);

        if (durum != Durum.Oyun) yield break;
        arac.kontrolAcik = true;
        arayuz.Telsiz("Mideye hoş geldin! Yerdeki sarı-yeşil göller mide asidi. Kapsülün kalkanını eritir, üstünde durma!", true);
        arayuz.Telsiz("Köpüğün asidi kısa süreliğine etkisiz hale getirir. Önündeki göle köpük at, beyazlaşınca üstünden geç.");
        arayuz.Telsiz("Hedefin tam karşıdaki ışıklı halka: mide çıkışı.");
    }

    // ------------------------------------------------------------------ Mide

    // Köpük bir asit gölünün üstündeyse onu nötrler.
    bool AsitKontrolu(Vector3 nokta)
    {
        if (nokta.y > 2f) return false;
        foreach (AsitHavuzu havuz in AsitHavuzu.Tumu)
        {
            if (!havuz.Aktif || !havuz.Ustunde(nokta)) continue;
            havuz.Notrle();
            if (!notrAnlatildi)
            {
                notrAnlatildi = true;
                arayuz.Telsiz("İşte böyle! Mide ilaçları (antasitler) de asidi aynı şekilde etkisiz hale getirir. Beyazken geç!", true);
            }
            return true;
        }
        return false;
    }

    void MideGuncelle()
    {
        float dt = Time.deltaTime;
        Vector3 p = arac.transform.position;

        // Hedef: mide çıkışı
        if (p.z > CikisZ && Mathf.Abs(p.x) < 5.5f)
        {
            BolumuBitir("Mide çıkışına ulaştın! Harika iş çırak, asit seni durduramadı.");
            return;
        }

        // Son geçilen güvenli nokta (kalkan biterse araç buraya döner)
        while (guvenliNokta + 1 < GuvenliNoktalar.Length && p.z > GuvenliNoktalar[guvenliNokta + 1] + 1f) guvenliNokta++;

        // Asit hasarı
        bool asitte = false;
        foreach (AsitHavuzu havuz in AsitHavuzu.Tumu)
        {
            if (havuz.Aktif && havuz.Ustunde(p)) { asitte = true; break; }
        }
        if (asitte)
        {
            asitDisiSure = 0f;
            kalkan -= 40f * dt;
            kamera.sarsinti = Mathf.Max(kamera.sarsinti, 0.06f);
            cizirti -= dt;
            if (cizirti <= 0f)
            {
                cizirti = 0.3f;
                Ses.Cal(Ses.Tur.Kopuk, 0.7f, 0.55f);
                Baloncuk.Sac(p + Vector3.up * 0.4f, new Color(0.85f, 1f, 0.3f), 3, 0.3f);
            }
            if (!kalkanAnlatildi)
            {
                kalkanAnlatildi = true;
                arayuz.Telsiz("Asidin içindesin, kalkan eriyor! Hemen çık ya da köpükle asidi etkisiz hale getir.", true);
            }
        }
        else
        {
            asitDisiSure += dt;
            if (asitDisiSure > 1.5f) kalkan = Mathf.Min(100f, kalkan + 6f * dt);
        }

        if (kalkan <= 0f)
        {
            kalkanYenileme++;
            kalkan = 70f;
            arac.Isinla(new Vector3(0f, 0.2f, GuvenliNoktalar[guvenliNokta]), Quaternion.identity);
            kamera.Isinla();
            Ses.Cal(Ses.Tur.Carpma);
            arayuz.Telsiz("Kalkan tükendi! Seni güvenli bir noktaya çektim ve kalkanı yeniledim. Asidi köpükle beyazlatmadan geçme.", true);
        }
        arayuz.Gosterge($"Kalkan: %{Mathf.CeilToInt(Mathf.Max(0f, kalkan))}");
        arayuz.HedefIsareti(HedefYonu());

        // Mide kasılması: göller bir süreliğine taşar
        mideKasilmaSayaci -= dt;
        if (mideKasilmaSayaci <= 0f)
        {
            mideKasilmaSayaci = Random.Range(13f, 17f);
            kasilmaSuresi = 4.5f;
            kamera.sarsinti = 0.14f;
            Ses.Cal(Ses.Tur.Yutkunma, 0.5f, 0.8f);
            if (!mideKasilmaAnlatildi)
            {
                mideKasilmaAnlatildi = true;
                arayuz.Telsiz("Mide kasılıyor! Yiyecekleri çalkalayıp bulamaç haline getirir. Asit gölleri taşabilir, dikkat!", true);
            }
        }
        kasilmaSuresi -= dt;
        AsitHavuzu.Tasma = Mathf.MoveTowards(AsitHavuzu.Tasma, kasilmaSuresi > 0f ? 1.3f : 1f, 0.35f * dt);

        if (!ortaAnlatildi && p.z > 30f && arayuz.TelsizBos)
        {
            ortaAnlatildi = true;
            arayuz.Telsiz("Mide asidi yiyecekleri parçalar ve mikropların çoğunu öldürür. Mide duvarı ise kalın bir mukus tabakasıyla kendini korur.");
        }
    }

    // Mide çıkışı ekranda değilse kenarda yönünü gösterir.
    Vector2? HedefYonu()
    {
        Vector3 g = kameraGozu.WorldToViewportPoint(new Vector3(0f, 3f, CikisZ + 5f));
        if (g.z > 0f && g.x > 0.04f && g.x < 0.96f && g.y > 0.04f && g.y < 0.96f) return null;
        Vector2 yon = new Vector2(g.x - 0.5f, g.y - 0.5f) * (g.z < 0f ? -1f : 1f);
        yon *= 0.44f / Mathf.Max(Mathf.Abs(yon.x), Mathf.Abs(yon.y), 0.001f);
        return new Vector2(0.5f, 0.5f) + yon;
    }

    static Vector3 MideSiniri(Vector3 p)
    {
        const float a = MideA - 2.5f, b = MideB - 2.5f;
        float k = p.x * p.x / (a * a) + (p.z - MideZ) * (p.z - MideZ) / (b * b);
        if (k > 1f)
        {
            float s = 1f / Mathf.Sqrt(k);
            p.x *= s;
            p.z = MideZ + (p.z - MideZ) * s;
        }
        return p;
    }

    // ------------------------------------------------------------------ Bölüm sonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Mide çıkışı açılıyor! Bulamaçla birlikte ince bağırsağa doğru gidiyorsun...", true);
        kamera.sarsinti = 0.1f;
        yield return new WaitForSeconds(1.8f);

        Ses.Cal(Ses.Tur.Yutkunma);
        mideDuvarlari.SetActive(false);
        var hedef = new Vector3(0f, 0f, CikisZ + 30f);
        for (float t = 0f; t < 2.6f; t += Time.deltaTime)
        {
            Vector3 yon = hedef - arac.transform.position;
            yon.y = 0f;
            arac.Itki(yon.normalized * 12f);
            kamera.sarsinti = 0.2f;
            yield return null;
        }
    }

    // Puan: kalkanı hiç bitirmeden ve lokmalara pek çarpmadan bitirene 3 yıldız
    protected override int Yildiz() =>
        kalkanYenileme == 0 && lokmaCarpma <= 2 ? 3 : kalkanYenileme <= 1 && lokmaCarpma <= 5 ? 2 : 1;

    protected override string SonucSatiri() =>
        $"{SureYazisi}     Lokmaya çarpma: {lokmaCarpma}     Kalkan yenileme: {kalkanYenileme}";
}
