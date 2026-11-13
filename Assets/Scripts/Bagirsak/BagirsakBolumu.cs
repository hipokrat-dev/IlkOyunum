using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 3 — Bağırsak.
// Kıvrıla kıvrıla ilerleyen, duvarları villuslarla kaplı bir tünel. Yol boyunca beş dost bakteri
// kolonisi var; araç yaklaşınca zararlı bakteriler onları kuşatır. Oyuncu köpükle zararlıları
// temizleyip kolonileri kurtarır (dostları vurmadan) ve tünelin sonundaki kapağa ulaşır.
public class BagirsakBolumu : BolumTemeli
{
    const int YolAdet = 260, KoloniAdet = 5, BaslangicIndeksi = 16;
    const float YariGenislik = 7.2f, Yukseklik = 9f, DuvarMesafesi = 5.3f;

    public new static BagirsakBolumu Ornek => BolumTemeli.Ornek as BagirsakBolumu;
    public int Kurtarilan => kurtarilan;

    readonly List<Vector3> yol = new List<Vector3>();          // tünelin orta çizgisi
    readonly List<IyiKoloni> koloniler = new List<IyiKoloni>();
    readonly List<Transform> villuslar = new List<Transform>();
    readonly List<Quaternion> villusDurusu = new List<Quaternion>();

    Transform kok;
    GameObject duvarlar;
    Material mukoza;
    int yakinIndeks, kurtarilan, kaybedilen;
    bool kusatmaAnlatildi, kurtarmaAnlatildi, kayipAnlatildi, dostAtesiAnlatildi, villusAnlatildi;

    protected override int BolumNo => 3;
    protected override string BolumAdi => "Bağırsak";
    protected override string SonrakiDurak => "Kan Damarları";
    protected override string SonrakiSahne => "KanKalp";
    protected override string KartBasligi => "VÜCUT KARTI: BAĞIRSAK";
    protected override string KartMetni =>
        "•  İnce bağırsak yaklaşık 6-7 metre uzunluğundadır; karnımıza kıvrıla kıvrıla sığar.\n\n" +
        "•  Villus denen milyonlarca parmaksı çıkıntı, sindirilen besinleri emip kana geçirir.\n\n" +
        "•  Bağırsağımızda trilyonlarca faydalı bakteri yaşar; sindirime yardım eder, vitamin üretir ve zararlı mikropları uzak tutar.";

    // ------------------------------------------------------------------ Kurulum

    protected override void SahneyiKur()
    {
        kok = new GameObject("Bagirsak").transform;
        var renk = Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var normal = Resources.Load<Texture2D>("Agiz/mukoza_normal");
        mukoza = Sekil.DokuluMalzeme(renk, normal, null, 0.55f, 1f, 0.6f);
        mukoza.SetColor("_BaseColor", new Color(1f, 0.78f, 0.68f));

        // Orta çizgi: sağa sola kıvrılarak ilerleyen yol
        for (int i = 0; i < YolAdet; i++)
        {
            float u = 4f * Mathf.PI * i / (YolAdet - 1);
            yol.Add(new Vector3(24f * Mathf.Sin(u), 0f, 9.5f * u));
        }

        TuneliKur();
        VilluslariKur();
        KolonileriKur();

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.36f, 0.25f, 0.2f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.1f, 0.045f, 0.03f);
        RenderSettings.fogDensity = 0.014f;

        arac = AracKontrol.Olustur(yol[BaslangicIndeksi] + Vector3.up * 0.2f);
        arac.transform.rotation = Quaternion.LookRotation(Teget(BaslangicIndeksi));
        yakinIndeks = BaslangicIndeksi;
        kamera = TakipKamera.Olustur(arac.transform, new Color(0.05f, 0.02f, 0.012f));
        kamera.sinir = KameraSiniri;
        kamera.ofset = new Vector3(0f, 5.2f, -7.5f);      // tavandaki villuslara girmesin
        kamera.Isinla();
    }

    protected override void ArayuzHazir()
    {
        arayuz.Gosterge($"Dost koloni: 0 / {KoloniAdet}");
    }

    void Start()
    {
        YansimaSondasi(yol[20] + Vector3.up * 4f, new Vector3(200f, 60f, 300f)).transform.SetParent(kok, true);
    }

    Vector3 Teget(int i)
    {
        return (yol[Mathf.Min(i + 1, YolAdet - 1)] - yol[Mathf.Max(i - 1, 0)]).normalized;
    }

    Vector3 Sag(int i) => Vector3.Cross(Vector3.up, Teget(i));

    void TuneliKur()
    {
        // Kemerli tünel duvarı (içinden bakılır)
        var kemer = Sekil.Parametrik(YolAdet - 1, 20, true, (fu, fv) =>
        {
            int i = Mathf.RoundToInt(fu * (YolAdet - 1));
            float a = fv * Mathf.PI;
            float dalga = 1f + 0.05f * Mathf.Sin(i * 0.9f + fv * 9f);          // halka halka kıvrımlar
            return yol[i] + Sag(i) * (YariGenislik * dalga * Mathf.Cos(a)) + Vector3.up * (Yukseklik * dalga * Mathf.Sin(a));
        }, 40f, 4f);
        Sekil.MeshParca(kemer, "Tunel", kok, Vector3.zero, Vector3.one, mukoza)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Taban
        var tabanMalzemesi = Sekil.DokuluMalzeme(mukoza.GetTexture("_BaseMap") as Texture2D,
            mukoza.GetTexture("_BumpMap") as Texture2D, null, 0.6f, 9f, 0.7f);
        tabanMalzemesi.SetColor("_BaseColor", new Color(0.95f, 0.72f, 0.6f));
        var taban = Sekil.Izgara(new Vector2(-40f, -16f), new Vector2(40f, 136f), 60, 110,
            (x, z) => 0.08f * Mathf.Sin(x * 0.7f + z * 0.5f), 1f);
        Sekil.MeshParca(taban, "Taban", kok, Vector3.zero, Vector3.one, tabanMalzemesi);

        var zemin = new GameObject("Zemin").AddComponent<BoxCollider>();
        zemin.transform.SetParent(kok, false);
        zemin.center = new Vector3(0f, -0.5f, 60f);
        zemin.size = new Vector3(120f, 1f, 190f);

        // Görünmez yan duvarlar ve baştaki arka duvar
        duvarlar = new GameObject("Duvarlar");
        duvarlar.transform.SetParent(kok, false);
        for (int i = 0; i < YolAdet - 1; i += 2)
        {
            int j = Mathf.Min(i + 2, YolAdet - 1);
            foreach (float taraf in new[] { -1f, 1f })
            {
                Vector3 a = yol[i] + Sag(i) * (taraf * DuvarMesafesi), b = yol[j] + Sag(j) * (taraf * DuvarMesafesi);
                Duvar((a + b) * 0.5f, b - a, (b - a).magnitude + 0.6f);
            }
        }
        Duvar(yol[BaslangicIndeksi - 5], Sag(BaslangicIndeksi - 5), 16f);

        // Yol boyunca loş ışıklar
        for (int i = 10; i < YolAdet; i += 26)
        {
            var isik = new GameObject("Isik").AddComponent<Light>();
            isik.transform.SetParent(kok, false);
            isik.transform.position = yol[i] + Vector3.up * 6f;
            isik.type = LightType.Point;
            isik.color = new Color(1f, 0.8f, 0.65f);
            isik.intensity = 55f;
            isik.range = 26f;
            isik.shadows = LightShadows.None;
        }

        // Tünelin sonu: kalın bağırsağa açılan kapak
        var kas = Sekil.DokuluMalzeme(mukoza.GetTexture("_BaseMap") as Texture2D,
            mukoza.GetTexture("_BumpMap") as Texture2D, null, 0.75f, 1f, 0.9f);
        kas.SetColor("_BaseColor", new Color(1f, 0.6f, 0.55f));
        Sekil.MeshParca(Sekil.Halka(4.6f, 1.5f, 48, 16), "Kapak", kok, yol[YolAdet - 1] + Vector3.up * 3.4f,
            Vector3.one, kas, Quaternion.LookRotation(Teget(YolAdet - 1)));
    }

    void Duvar(Vector3 merkez, Vector3 yon, float uzunluk)
    {
        var duvar = new GameObject("Duvar").AddComponent<BoxCollider>();
        duvar.transform.SetParent(duvarlar.transform, false);
        duvar.transform.SetPositionAndRotation(merkez, Quaternion.LookRotation(yon));
        duvar.center = new Vector3(0f, 4f, 0f);
        duvar.size = new Vector3(1f, 9f, uzunluk);
    }

    // Villuslar: duvarlardan içeri uzanan, hafifçe salınan parmaksı çıkıntılar
    void VilluslariKur()
    {
        var parmak = Sekil.SuperElips(0.9f, 1f, 8, p => new Vector3(p.x, p.y + 1f, p.z));     // tabanı orijinde
        var malzeme = Sekil.DokuluMalzeme(mukoza.GetTexture("_BaseMap") as Texture2D,
            mukoza.GetTexture("_BumpMap") as Texture2D, null, 0.65f, 1f, 0.7f);
        malzeme.SetColor("_BaseColor", new Color(1f, 0.7f, 0.62f));

        for (int i = 2; i < YolAdet - 2; i += 2)
        {
            foreach (float taraf in new[] { -1f, 1f })
            {
                Vector3 iceri = -Sag(i) * taraf;
                // Üç sıra: tabanda dik duranlar, duvarın ortasında ve yukarısında içeri uzananlar
                for (int sira = 0; sira < 3; sira++)
                {
                    float a = (sira == 0 ? 0.04f : sira == 1 ? 0.2f : 0.36f) * Mathf.PI;
                    Vector3 duvarNoktasi = yol[i] - iceri * (YariGenislik * 0.97f * Mathf.Cos(a)) + Vector3.up * (Yukseklik * 0.97f * Mathf.Sin(a));
                    Vector3 yukari = (iceri * Mathf.Cos(a) * 1.2f + Vector3.up * (sira == 0 ? 1.6f : -0.2f)).normalized;
                    var donus = Quaternion.FromToRotation(Vector3.up, yukari);
                    var olcek = new Vector3(0.4f, sira == 0 ? Random.Range(0.9f, 1.4f) : Random.Range(0.55f, 0.9f), 0.4f);
                    var g = Sekil.MeshParca(parmak, "Villus", kok, duvarNoktasi + Random.insideUnitSphere * 0.3f, olcek, malzeme, donus);
                    g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    villuslar.Add(g.transform);
                    villusDurusu.Add(donus);
                }
            }
        }
    }

    void KolonileriKur()
    {
        var dost = Sekil.Malzeme(new Color(0.2f, 0.65f, 0.85f), 0.85f);
        Sekil.Isilda(dost, new Color(0.05f, 0.35f, 0.55f));
        var zararli = Sekil.Malzeme(new Color(0.75f, 0.12f, 0.2f), 0.85f);
        Sekil.Isilda(zararli, new Color(0.35f, 0.02f, 0.05f));
        var modeller = new List<Mesh>();
        foreach (Mesh m in Resources.LoadAll<Mesh>("Agiz/AgizModelleri"))
        {
            if (m.name.StartsWith("Bakteri_")) modeller.Add(m);
        }
        if (modeller.Count == 0) Debug.LogError("Bakteri modelleri bulunamadı: Resources/Agiz/AgizModelleri");

        float[] yerler = { 0.2f, 0.36f, 0.52f, 0.69f, 0.86f };
        for (int k = 0; k < KoloniAdet; k++)
        {
            int i = Mathf.RoundToInt(yerler[k] * (YolAdet - 1));
            float taraf = k % 2 == 0 ? 1f : -1f;
            var koloni = IyiKoloni.Olustur(kok, yol[i] + Sag(i) * (taraf * 1.8f), dost);
            koloni.kurtuldu = KoloniKurtuldu;
            koloni.kayboldu = KoloniKayboldu;
            koloni.dostAtesi = DostAtesi;
            koloniler.Add(koloni);

            int saldirgan = k == 0 ? 3 : 4;
            for (int s = 0; s < saldirgan; s++)
            {
                koloni.SaldirganEkle(KotuBakteri.Olustur(kok, koloni, modeller[Random.Range(0, modeller.Count)], zararli,
                    Random.Range(2.9f, 3.4f), 2f * Mathf.PI * s / saldirgan));
            }
        }
    }

    // ------------------------------------------------------------------ Oyun

    protected override void OyunBasladi()
    {
        arayuz.Telsiz("Burası ince bağırsak: yaklaşık 6-7 metre uzunluğunda, karnımıza kıvrıla kıvrıla sığan bir tüp.");
        arayuz.Telsiz("Duvarlardaki parmak gibi çıkıntılara villus denir. Sindirilen besinleri emip kana geçirirler; sayıları milyonlarcadır.");
        arayuz.Telsiz("İleride mavi ışıldayan dost bakteriler var. Kırmızı zararlı bakteriler onlara saldırıyor: köpükle kırmızıları temizle, mavileri vurma!");
    }

    protected override void OyunGuncelle()
    {
        Vector3 p = arac.transform.position;
        yakinIndeks = EnYakin(p, yakinIndeks);

        // Villuslar hafifçe salınır
        float t = Time.time;
        for (int i = 0; i < villuslar.Count; i++)
        {
            villuslar[i].localRotation = villusDurusu[i] *
                Quaternion.Euler(6f * Mathf.Sin(t * 1.3f + i * 0.7f), 0f, 6f * Mathf.Cos(t * 1.1f + i * 0.4f));
        }

        // Araç yaklaşınca kuşatma başlar
        IyiKoloni siradaki = null;
        foreach (IyiKoloni koloni in koloniler)
        {
            if (koloni.Durum == IyiKoloni.Hal.Bekliyor && (koloni.transform.position - p).sqrMagnitude < 17f * 17f)
            {
                koloni.KusatmayiBaslat();
                Ses.Cal(Ses.Tur.Carpma, 0.5f, 1.3f);
                if (!kusatmaAnlatildi)
                {
                    kusatmaAnlatildi = true;
                    arayuz.Telsiz("Dost koloni saldırı altında! Çevresinde dönen kırmızı bakterileri köpükle temizle, çabuk!", true);
                }
            }
            if (siradaki == null && (koloni.Durum == IyiKoloni.Hal.Bekliyor || koloni.Durum == IyiKoloni.Hal.Kusatma)) siradaki = koloni;
        }

        // Yön işareti: sıradaki koloni, hepsi bittiyse çıkış kapağı
        Vector3 hedef = siradaki != null ? siradaki.transform.position + Vector3.up : yol[YolAdet - 1] + Vector3.up * 3f;
        arayuz.HedefIsareti(EkranDisiYonu(hedef));

        if (!villusAnlatildi && kurtarilan + kaybedilen >= 3 && arayuz.TelsizBos)
        {
            villusAnlatildi = true;
            arayuz.Telsiz("Villuslar bağırsağın iç yüzeyini kat kat büyütür; böylece besinlerin neredeyse tamamı emilir.");
        }

        if (yakinIndeks >= YolAdet - 5)
        {
            BolumuBitir(kurtarilan == KoloniAdet
                ? "Bağırsağın sonuna ulaştın ve bütün dost kolonileri kurtardın! Mükemmel iş çırak."
                : "Bağırsağın sonuna ulaştın! Kurtardığın dost bakteriler sana minnettar.");
        }
    }

    void KoloniKurtuldu(IyiKoloni koloni)
    {
        kurtarilan++;
        arayuz.Gosterge($"Dost koloni: {kurtarilan} / {KoloniAdet}", true);
        kamera.sarsinti = Mathf.Max(kamera.sarsinti, 0.05f);
        if (kurtarmaAnlatildi) return;
        kurtarmaAnlatildi = true;
        arayuz.Telsiz("Koloniyi kurtardın! Faydalı bakteriler sindirime yardım eder, vitamin üretir ve zararlı mikroplara yer bırakmaz.", true);
    }

    void KoloniKayboldu(IyiKoloni koloni)
    {
        kaybedilen++;
        if (kayipAnlatildi) return;
        kayipAnlatildi = true;
        arayuz.Telsiz("O koloniyi kaybettik... Üzülme, diğerlerine yetiş! Kırmızıları ne kadar hızlı temizlersen o kadar iyi.", true);
    }

    void DostAtesi(IyiKoloni koloni)
    {
        if (dostAtesiAnlatildi) return;
        dostAtesiAnlatildi = true;
        arayuz.Telsiz("Dikkat, mavi olanlar dost! Köpük onlara da zarar veriyor; yalnızca kırmızıları vur.", true);
    }

    // ------------------------------------------------------------------ Yardımcılar

    // Orta çizgide p'ye en yakın noktanın indeksi (önceki indeksin çevresinde aranır).
    int EnYakin(Vector3 p, int onceki)
    {
        int enIyi = onceki;
        float enAz = float.MaxValue;
        for (int i = Mathf.Max(0, onceki - 12); i <= Mathf.Min(YolAdet - 1, onceki + 12); i++)
        {
            float d = (yol[i] - p).sqrMagnitude;
            if (d < enAz)
            {
                enAz = d;
                enIyi = i;
            }
        }
        return enIyi;
    }

    // Otomatik test için: aracın biraz ilerisindeki yol noktası.
    public Vector3 IleridekiNokta() => yol[Mathf.Min(YolAdet - 1, yakinIndeks + 7)];

    // Otomatik test için: yakındaki kuşatılmış koloninin araca en yakın saldırganı.
    public bool YakinSaldirgan(out Vector3 konum)
    {
        konum = Vector3.zero;
        Vector3 p = arac.transform.position;
        float enAz = float.MaxValue;
        foreach (IyiKoloni koloni in koloniler)
        {
            if (!koloni.Kusatiliyor || (koloni.transform.position - p).sqrMagnitude > 15f * 15f) continue;
            foreach (KotuBakteri b in koloni.Saldirganlar)
            {
                float d = (b.transform.position - p).sqrMagnitude;
                if (d < enAz)
                {
                    enAz = d;
                    konum = b.transform.position;
                }
            }
        }
        return enAz < float.MaxValue;
    }

    Vector2? EkranDisiYonu(Vector3 dunyaNoktasi)
    {
        Vector3 g = kameraGozu.WorldToViewportPoint(dunyaNoktasi);
        if (g.z > 0f && g.x > 0.04f && g.x < 0.96f && g.y > 0.04f && g.y < 0.96f) return null;
        Vector2 yon = new Vector2(g.x - 0.5f, g.y - 0.5f) * (g.z < 0f ? -1f : 1f);
        yon *= 0.44f / Mathf.Max(Mathf.Abs(yon.x), Mathf.Abs(yon.y), 0.001f);
        return new Vector2(0.5f, 0.5f) + yon;
    }

    // Kamerayı tünelin içinde tutar.
    Vector3 KameraSiniri(Vector3 p)
    {
        int i = EnYakin(p, Mathf.Max(0, yakinIndeks - 8));
        Vector3 sag = Sag(i);
        float yanal = Vector3.Dot(p - yol[i], sag);
        float sinir = YariGenislik - 3.2f;
        if (Mathf.Abs(yanal) > sinir) p -= sag * (yanal - Mathf.Sign(yanal) * sinir);
        return p;
    }

    // ------------------------------------------------------------------ Bölüm sonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Kapak açılıyor... Emilen besinlerle birlikte kana karışıyoruz! Sıradaki durak: kan damarları.", true);
        kamera.sarsinti = 0.1f;
        yield return new WaitForSeconds(1.8f);

        Ses.Cal(Ses.Tur.Yutkunma);
        duvarlar.SetActive(false);
        Vector3 hedef = yol[YolAdet - 1] + Teget(YolAdet - 1) * 30f;
        for (float t = 0f; t < 2.6f; t += Time.deltaTime)
        {
            Vector3 yon = hedef - arac.transform.position;
            yon.y = 0f;
            arac.Itki(yon.normalized * 12f);
            kamera.sarsinti = 0.2f;
            yield return null;
        }
    }

    // Puan: beş koloninin hepsini kurtarana 3, en az üçünü kurtarana 2 yıldız
    protected override int Yildiz() => kurtarilan >= KoloniAdet ? 3 : kurtarilan >= 3 ? 2 : 1;

    protected override string SonucSatiri() => $"{SureYazisi}     Kurtarılan dost koloni: {kurtarilan} / {KoloniAdet}";
}
