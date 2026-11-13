using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 7 — Mesane ve çıkış (final).
// Araç idrar borusunun ağzından mesaneye düşer ve idrarın yüzeyinde yüzer (kaldırma kuvveti).
// Kristaller ağır olduğu için dibe çökmüştür: oyuncu DAL düğmesini basılı tutunca kapsül içine
// sıvı alıp ağırlaşır ve batar, bırakınca yeniden yükselir. Dipteki kristaller köpükle eritilir.
// Kristaller bitince (ya da mesane dolunca) çıkış açılır; araç idrar yolundan vücudun dışına çıkar.
public class MesaneBolumu : BolumTemeli
{
    const float MerkezZ = 26f, YariX = 20f, YariZ = 27f, CikisZ = 50f, DolmaSuresi = 110f;
    const float IlkSeviye = 5f, SonSeviye = 9.5f, DusmeYuksekligi = 15f;
    const float UretraX = 400f, UretraL = 190f;
    const int KristalAdet = 7, PuanliBolumSayisi = 6;

    public new static MesaneBolumu Ornek => BolumTemeli.Ornek as MesaneBolumu;
    public int KalanKristal => kalan;

    readonly List<Kristal> kristaller = new List<Kristal>();
    Transform kok, idrarYuzeyi, cikisHalkasi;
    GameObject duvarlar;
    Rigidbody govde;
    Light cikisIsigi;
    float doluluk, yuzeyY = IlkSeviye;
    int kalan = KristalAdet;
    bool sudaydi, ilkDalis, ilkErime, yariAnlatildi, dalisAnlatildi, cekiliyor;

    static readonly Color IdrarSisi = new Color(0.36f, 0.29f, 0.07f);

    protected override int BolumNo => 7;
    protected override string BolumAdi => "Mesane ve Çıkış";
    protected override string SonrakiDurak => "Laboratuvar";
    protected override string SonrakiSahne => "Agiz";
    protected override string KartBasligi => "VÜCUT KARTI: MESANE";
    protected override string KartMetni =>
        "•  Mesane esnek bir torbadır; yaklaşık yarım litre idrar tutabilir.\n\n" +
        "•  Dolunca duvarındaki sinirler beyne sinyal gönderir ve tuvalete gitme isteği duyarız.\n\n" +
        "•  Bol su içmek böbrekleri ve idrar yollarını temiz tutar, kristal ve taş oluşumunu önler.";

    // Final ekranı: yolculuğun özeti
    protected override string BitisBasligi => "YOLCULUK TAMAMLANDI";
    protected override string SonrakiSatiri
    {
        get
        {
            int toplam = 0;
            foreach (var b in BolumListesi.Tumu) if (b.puanli) toplam += EnIyiYildiz(b.no);
            return $"Vücuttan çıktın! Profesör kapsülü yeniden büyüttü: laboratuvara hoş geldin çırak.\n" +
                   $"<color=#FFD040>Toplam yıldız: {toplam} / {PuanliBolumSayisi * 3}</color>";
        }
    }
    protected override string DevamYazisi => "BOŞLUK: Baştan başla   •   R: Bu bölümü tekrar oyna   •   M: Ana menü";
    protected override string DevamDugmesi => "BAŞTAN BAŞLA";

    // ------------------------------------------------------------------ Kurulum

    protected override void SahneyiKur()
    {
        kok = new GameObject("Mesane").transform;
        var renk = Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var normal = Resources.Load<Texture2D>("Agiz/mukoza_normal");

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.42f, 0.24f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = IdrarSisi;
        RenderSettings.fogDensity = 0.012f;

        // Taban ve kenarlarda yukarı kıvrılan duvar
        var tabanMalzemesi = Sekil.DokuluMalzeme(renk, normal, null, 0.55f, 7f, 0.6f);
        tabanMalzemesi.SetColor("_BaseColor", new Color(1f, 0.82f, 0.62f));
        var taban = Sekil.Izgara(new Vector2(-26f, -8f), new Vector2(26f, 62f), 70, 100, (x, z) =>
        {
            float e = x * x / (YariX * YariX) + (z - MerkezZ) * (z - MerkezZ) / (YariZ * YariZ);
            float kenar = Mathf.Max(0f, e - 0.8f);
            return 0.1f * Mathf.Sin(x * 0.6f + 1.4f * Mathf.Sin(z * 0.3f)) + 9f * kenar * kenar;
        }, 1f);
        Sekil.MeshParca(taban, "MesaneTabani", kok, Vector3.zero, Vector3.one, tabanMalzemesi);

        var zemin = new GameObject("Zemin").AddComponent<BoxCollider>();
        zemin.transform.SetParent(kok, false);
        zemin.center = new Vector3(0f, -0.5f, MerkezZ + 10f);
        zemin.size = new Vector3(70f, 1f, 110f);

        var duvarMalzemesi = Sekil.DokuluMalzeme(renk, normal, null, 0.5f, 6f, 0.5f);
        duvarMalzemesi.SetColor("_BaseColor", new Color(0.92f, 0.7f, 0.52f));
        Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 32, null, true), "MesaneDuvari", kok, new Vector3(0f, 7f, MerkezZ),
            new Vector3(YariX + 5f, 19f, YariZ + 6f), duvarMalzemesi).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        duvarlar = new GameObject("Duvarlar");
        duvarlar.transform.SetParent(kok, false);
        const int Parca = 44;
        for (int i = 0; i < Parca; i++)
        {
            float a0 = 2f * Mathf.PI * i / Parca, a1 = 2f * Mathf.PI * (i + 1) / Parca;
            var p0 = new Vector3(YariX * Mathf.Cos(a0), 0f, MerkezZ + YariZ * Mathf.Sin(a0));
            var p1 = new Vector3(YariX * Mathf.Cos(a1), 0f, MerkezZ + YariZ * Mathf.Sin(a1));
            var duvar = new GameObject("Duvar").AddComponent<BoxCollider>();
            duvar.transform.SetParent(duvarlar.transform, false);
            duvar.transform.SetPositionAndRotation((p0 + p1) * 0.5f, Quaternion.LookRotation(p1 - p0));
            duvar.center = new Vector3(0f, 10f, 0f);
            duvar.size = new Vector3(1.5f, 24f, (p1 - p0).magnitude + 1.5f);
        }

        // İdrar yüzeyi (doldukça yükselir)
        var idrar = Sekil.Malzeme(new Color(0.95f, 0.8f, 0.2f, 0.35f), 0.97f);
        var yuzey = Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 16), "IdrarYuzeyi", kok, new Vector3(0f, yuzeyY, MerkezZ),
            new Vector3(YariX + 4f, 0.05f, YariZ + 4f), idrar);
        yuzey.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        idrarYuzeyi = yuzey.transform;

        // İdrar borusunun ağzı (aracın düştüğü yer) ve dipteki çıkış halkası
        var kas = Sekil.DokuluMalzeme(renk, normal, null, 0.75f, 1f, 0.9f);
        kas.SetColor("_BaseColor", new Color(1f, 0.6f, 0.5f));
        Sekil.MeshParca(Sekil.Halka(2.6f, 0.9f, 40, 14), "IdrarBorusuAgzi", kok, new Vector3(0f, DusmeYuksekligi + 2.5f, 12f),
            Vector3.one, kas, Quaternion.Euler(90f, 0f, 0f));
        cikisHalkasi = Sekil.MeshParca(Sekil.Halka(3.4f, 1.6f, 48, 16), "MesaneCikisi", kok, new Vector3(0f, 2.6f, CikisZ + 3.5f),
            Vector3.one, kas).transform;
        cikisIsigi = Isik(kok, new Vector3(0f, 3f, CikisZ + 1f), new Color(1f, 0.9f, 0.6f), 45f, 16f);
        Isik(kok, new Vector3(0f, 15f, 12f), new Color(1f, 0.9f, 0.65f), 130f, 50f);
        Isik(kok, new Vector3(0f, 15f, 40f), new Color(1f, 0.9f, 0.65f), 130f, 50f);

        // Kristaller dibe çökmüş: üçü çıkışın önünde, diğerleri dağınık
        var diken = Sekil.SuperElips(2.4f, 2.4f, 12);
        var malzeme = Sekil.Malzeme(new Color(0.96f, 0.93f, 0.75f), 0.85f);
        Sekil.Isilda(malzeme, new Color(0.16f, 0.14f, 0.06f));
        Vector3[] yerler =
        {
            new Vector3(-3.4f, 0.6f, CikisZ - 4f), new Vector3(0f, 0.6f, CikisZ - 3f), new Vector3(3.4f, 0.6f, CikisZ - 4f),
            new Vector3(-11f, 0.6f, 20f), new Vector3(10f, 0.6f, 14f), new Vector3(-8f, 0.6f, 36f), new Vector3(12f, 0.6f, 33f)
        };
        foreach (Vector3 yer in yerler)
        {
            var kristal = Kristal.Olustur(kok, diken, malzeme, yer, Random.Range(1.2f, 1.6f));
            kristal.eridi = KristalEridi;
            kristaller.Add(kristal);
        }

        UretrayiKur(renk, normal);

        // Araç idrar borusunun ağzında asılı bekler; oyun başlayınca düşer
        arac = AracKontrol.Olustur(new Vector3(0f, DusmeYuksekligi, 12f));
        arac.rayModu = true;
        govde = arac.GetComponent<Rigidbody>();
        govde.isKinematic = true;
        kamera = TakipKamera.Olustur(arac.transform, IdrarSisi);
        kamera.sinir = KameraSiniri;
        kamera.Isinla();
    }

    // Çıkış animasyonu için idrar yolu (üretra): ucunda gün ışığı olan tüp
    void UretrayiKur(Texture2D renk, Texture2D normal)
    {
        var uretra = new GameObject("Uretra").transform;
        uretra.SetParent(kok, false);
        uretra.position = new Vector3(UretraX, 0f, 0f);

        var duvar = Sekil.DokuluMalzeme(renk, normal, null, 0.55f, 1f, 0.6f);
        duvar.SetColor("_BaseColor", new Color(1f, 0.68f, 0.6f));
        var tup = Sekil.Tup(UretraL + 20f, 40, 120, (aci, z) => 4f * (1f + 0.05f * Mathf.Cos(aci * 6f + z * 0.35f)), 5f, 34f);
        Sekil.MeshParca(tup, "Duvar", uretra, new Vector3(0f, 0f, -20f), Vector3.one, duvar)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Tünelin ucundaki gün ışığı
        var isikli = Sekil.Malzeme(Color.white, 0f);
        Sekil.Isilda(isikli, Color.white * 9f);
        Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 12), "GunIsigi", uretra, new Vector3(0f, 0f, UretraL), new Vector3(3.6f, 3.6f, 0.2f), isikli);
        Isik(uretra, new Vector3(UretraX, 0f, UretraL - 12f), Color.white, 500f, 90f);
    }

    static Light Isik(Transform ebeveyn, Vector3 konum, Color renk, float siddet, float menzil)
    {
        var isik = new GameObject("Isik").AddComponent<Light>();
        isik.transform.SetParent(ebeveyn, false);
        isik.transform.position = konum;
        isik.type = LightType.Point;
        isik.color = renk;
        isik.intensity = siddet;
        isik.range = menzil;
        isik.shadows = LightShadows.None;
        return isik;
    }

    protected override void ArayuzHazir()
    {
        arayuz.Ipucu("WASD: Sür   SHIFT: Dal   BOŞLUK: Köpük   ESC: Duraklat");
        arayuz.DalisDugmesi(true);
        GostergeyiYaz(false);
    }

    void GostergeyiYaz(bool vurgu)
    {
        arayuz.Gosterge($"Kristal: {KristalAdet - kalan}/{KristalAdet}  Dolu: %{Mathf.FloorToInt(doluluk * 100f)}", vurgu);
    }

    void Start()
    {
        YansimaSondasi(new Vector3(0f, 6f, MerkezZ), new Vector3(60f, 40f, 80f)).transform.SetParent(kok, true);
    }

    // ------------------------------------------------------------------ Oyun

    protected override void OyunBasladi()
    {
        // Araç serbest kalır ve yerçekimiyle mesaneye düşer
        arac.rayModu = false;
        govde.isKinematic = false;
        arayuz.Telsiz("İdrar borusunun ucundasın... Sıkı tutun, mesaneye düşüyorsun!");
    }

    protected override void OyunGuncelle()
    {
        doluluk = Mathf.Min(1f, doluluk + Time.deltaTime / DolmaSuresi);
        yuzeyY = Mathf.Lerp(IlkSeviye, SonSeviye, doluluk);
        idrarYuzeyi.position = new Vector3(0f, yuzeyY, MerkezZ);
        GostergeyiYaz(false);

        // Yüzeyin altında görüş bulanıklaşır
        bool kameraSuda = kameraGozu.transform.position.y < yuzeyY;
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, kameraSuda ? 0.03f : 0.012f, 4f * Time.deltaTime);

        if (YakinKristal(out Vector3 hedef)) arayuz.HedefIsareti(EkranDisiYonu(hedef + Vector3.up));

        if (ilkDalis && !dalisAnlatildi && arac.DalisGirdi)
        {
            dalisAnlatildi = true;
            arayuz.Telsiz("Batıyorsun! Kapsül içine sıvı alınca ağırlaştı. DAL'ı bırakınca sıvıyı boşaltır ve yeniden yükselirsin; denizaltılar da böyle çalışır.", true);
        }
        if (!yariAnlatildi && doluluk > 0.5f && arayuz.TelsizBos)
        {
            yariAnlatildi = true;
            arayuz.Telsiz("Mesane yarı yarıya doldu. Yetişkin bir mesane yaklaşık yarım litre idrar tutabilir.");
        }

        if (doluluk >= 1f)
        {
            kamera.sarsinti = 0.2f;
            BolumuBitir("Mesane doldu, kaslar kasılıyor! Kalan kristaller de akıntıyla sürüklenecek.");
        }
    }

    // Kaldırma kuvveti: sıvıya batan cisim, taşırdığı sıvının ağırlığı kadar yukarı itilir.
    void FixedUpdate()
    {
        if (govde == null || govde.isKinematic) return;
        float derinlik = yuzeyY - (govde.position.y + 0.7f);         // gövde merkezinin yüzeyden derinliği
        bool suda = derinlik > -0.7f;

        if (suda && !sudaydi && govde.linearVelocity.y < -3f) Sicrat();
        sudaydi = suda;
        if (!suda) return;

        // Batan kısım arttıkça kaldırma kuvveti artar. DAL: kapsül ağırlaşır, kuvvet ağırlığı taşıyamaz.
        float batanOran = Mathf.Clamp01((derinlik + 0.7f) / 1.4f);
        float kaldirma = cekiliyor || (durum == Durum.Oyun && arac.DalisGirdi) ? 0.35f : 1.9f;
        govde.AddForce(Vector3.up * (9.81f * kaldirma * batanOran), ForceMode.Acceleration);

        // Sıvı direnci düşey hareketi yavaşlatır
        Vector3 v = govde.linearVelocity;
        v.y *= Mathf.Exp(-2.6f * Time.fixedDeltaTime);
        govde.linearVelocity = v;
    }

    void Sicrat()
    {
        Vector3 p = arac.transform.position;
        Baloncuk.Sac(new Vector3(p.x, yuzeyY, p.z), new Color(1f, 0.92f, 0.5f), 18, 0.5f);
        Ses.Cal(Ses.Tur.Dalga, 0.9f, 1.6f);
        kamera.sarsinti = 0.25f;
        if (ilkDalis) return;
        ilkDalis = true;
        arayuz.Telsiz("Cup! Mesanedesin: idrarı biriktiren esnek bir torba. Kapsül batmadı, yüzüyor; çünkü sıvı onu yukarı itiyor. Buna kaldırma kuvveti denir.", true);
        arayuz.Telsiz("Dipte idrar kristalleri var; ağır oldukları için çökmüşler. Büyürlerse taşa dönüşürler! Dalmak için DAL'ı basılı tut, kristallere üçer kez köpük at.");
        arayuz.Telsiz("Mesane doldukça duvarları gerilir ve beyne 'tuvalet zamanı' sinyali gider. Dolmadan bitirmeye çalış.");
    }

    void KristalEridi(Kristal kristal)
    {
        kalan--;
        GostergeyiYaz(true);
        kamera.sarsinti = Mathf.Max(kamera.sarsinti, 0.05f);
        if (!ilkErime)
        {
            ilkErime = true;
            arayuz.Telsiz("Bir kristal eridi! Bol su içmek bu kristallerin oluşmasını önler.", true);
        }
        if (kalan <= 0) BolumuBitir("Bütün kristalleri erittin! Çıkış tertemiz, tam zamanında.");
    }

    // En yakın erimemiş kristal (yön işareti ve otomatik test için)
    public bool YakinKristal(out Vector3 konum)
    {
        konum = Vector3.zero;
        float enAz = float.MaxValue;
        foreach (Kristal k in kristaller)
        {
            if (k == null || k.Eridi) continue;
            float d = (k.transform.position - arac.transform.position).sqrMagnitude;
            if (d < enAz)
            {
                enAz = d;
                konum = k.transform.position;
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

    static Vector3 KameraSiniri(Vector3 p)
    {
        const float a = YariX - 2.5f, b = YariZ - 2.5f;
        float k = p.x * p.x / (a * a) + (p.z - MerkezZ) * (p.z - MerkezZ) / (b * b);
        if (k > 1f)
        {
            float s = 1f / Mathf.Sqrt(k);
            p.x *= s;
            p.z = MerkezZ + (p.z - MerkezZ) * s;
        }
        return p;
    }

    // ------------------------------------------------------------------ Çıkış animasyonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.DalisDugmesi(false);
        arayuz.Telsiz("Çıkış kası gevşiyor! İdrar boşalırken akıntı seni de çıkışa çekecek.", true);
        kamera.sarsinti = 0.1f;

        // 1) Kas halkası açılır, akıntı aracı dipteki çıkışa çeker
        Ses.Cal(Ses.Tur.Yutkunma);
        duvarlar.SetActive(false);
        cekiliyor = true;
        var hedef = new Vector3(0f, 0f, CikisZ + 8f);
        for (float t = 0f; t < 3.2f; t += Time.deltaTime)
        {
            cikisHalkasi.localScale = Vector3.one * Mathf.Lerp(1f, 1.7f, t / 1.2f);
            cikisIsigi.intensity = Mathf.Lerp(45f, 140f, t / 1.2f);
            yuzeyY = Mathf.MoveTowards(yuzeyY, 1.5f, 2.2f * Time.deltaTime);       // mesane boşalıyor
            idrarYuzeyi.position = new Vector3(0f, yuzeyY, MerkezZ);
            Vector3 yon = hedef - arac.transform.position;
            yon.y = 0f;
            arac.Itki(yon.normalized * 12f);
            kamera.sarsinti = 0.15f;
            yield return null;
        }
        yield return arayuz.Karart(0.4f);

        // 2) İdrar yolu: araç akıntıyla tünelin ucundaki ışığa doğru hızlanır
        arac.rayModu = true;
        govde.isKinematic = true;
        Vector3 konum = new Vector3(UretraX, -0.8f, 2f);
        arac.transform.SetPositionAndRotation(konum, Quaternion.identity);
        kamera.sinir = null;
        kamera.yerelTakip = true;
        kamera.ofset = new Vector3(0f, 2.2f, -8f);
        kamera.Isinla();
        RenderSettings.fogDensity = 0.012f;
        arayuz.Telsiz("Burası idrar yolu: idrarı vücudun dışına taşır. Işığı görüyorum çırak, yolculuk bitiyor!", true);
        yield return arayuz.Aydinlat(0.4f);

        Ses.Cal(Ses.Tur.Dalga, 0.9f, 1.3f);
        float hiz = 16f;
        while (konum.z < UretraL - 14f)
        {
            float dt = Time.deltaTime;
            hiz = Mathf.MoveTowards(hiz, 60f, 16f * dt);
            konum.z += hiz * dt;
            arac.rayHizi = hiz;
            arac.transform.position = konum;
            kamera.sarsinti = 0.1f;
            kameraGozu.fieldOfView = Mathf.Lerp(kameraGozu.fieldOfView, 82f, 2f * dt);

            // Işığa yaklaştıkça her şey aydınlanır
            float yakinlik = Mathf.InverseLerp(UretraL * 0.45f, UretraL - 14f, konum.z);
            RenderSettings.fogColor = Color.Lerp(IdrarSisi, Color.white, yakinlik);
            kameraGozu.backgroundColor = RenderSettings.fogColor;
            yield return null;
        }

        // 3) Gün ışığı: dışarıdayız
        Ses.Cal(Ses.Tur.Basari);
        yield return arayuz.Karart(0.6f, Color.white);
        yield return new WaitForSeconds(0.8f);
    }

    // Puan: mesane dolmadan bütün kristalleri eritene 3 yıldız
    protected override int Yildiz() => kalan == 0 ? 3 : kalan <= 2 ? 2 : 1;

    protected override string SonucSatiri() => $"{SureYazisi}     Eritilen kristal: {KristalAdet - kalan} / {KristalAdet}";
}
