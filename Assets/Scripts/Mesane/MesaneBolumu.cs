using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 7 — Mesane ve çıkış (final).
// Mesane idrarla dolarken çıkışın önünde birikmiş idrar kristallerini köpükle erit.
// Kristaller bitince (ya da mesane dolunca) çıkış açılır ve araç idrarla birlikte vücuttan çıkar.
public class MesaneBolumu : BolumTemeli
{
    const float MerkezZ = 26f, YariX = 20f, YariZ = 27f, CikisZ = 50f, DolmaSuresi = 85f;
    const int KristalAdet = 7, PuanliBolumSayisi = 6;

    public new static MesaneBolumu Ornek => BolumTemeli.Ornek as MesaneBolumu;
    public int KalanKristal => kalan;

    readonly List<Kristal> kristaller = new List<Kristal>();
    Transform kok, idrarYuzeyi;
    GameObject duvarlar;
    float doluluk;
    int kalan = KristalAdet;
    bool ilkErime, yariAnlatildi;

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
            foreach (int bolum in new[] { 1, 2, 3, 4, 6, 7 }) toplam += EnIyiYildiz(bolum);
            return $"Vücuttan çıktın! Profesör kapsülü yeniden büyüttü: laboratuvara hoş geldin çırak.\n" +
                   $"<color=#FFD040>Toplam yıldız: {toplam} / {PuanliBolumSayisi * 3}</color>";
        }
    }
    protected override string DevamYazisi => "Yolculuğa baştan başlamak için BOŞLUK   •   Bu bölümü tekrar oynamak için R";

    // ------------------------------------------------------------------ Kurulum

    protected override void SahneyiKur()
    {
        kok = new GameObject("Mesane").transform;
        var renk = Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var normal = Resources.Load<Texture2D>("Agiz/mukoza_normal");

        // İdrarın içindeyiz: sarımsı, hafif bulanık
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.42f, 0.24f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.36f, 0.29f, 0.07f);
        RenderSettings.fogDensity = 0.014f;

        var tabanMalzemesi = Sekil.DokuluMalzeme(renk, normal, null, 0.55f, 7f, 0.6f);
        tabanMalzemesi.SetColor("_BaseColor", new Color(1f, 0.82f, 0.62f));
        var taban = Sekil.Izgara(new Vector2(-26f, -8f), new Vector2(26f, 62f), 70, 100, (x, z) =>
        {
            float e = x * x / (YariX * YariX) + (z - MerkezZ) * (z - MerkezZ) / (YariZ * YariZ);
            float kenar = Mathf.Max(0f, e - 0.8f);
            return 0.1f * Mathf.Sin(x * 0.6f + 1.4f * Mathf.Sin(z * 0.3f)) + 5f * kenar * kenar;
        }, 1f);
        Sekil.MeshParca(taban, "MesaneTabani", kok, Vector3.zero, Vector3.one, tabanMalzemesi);

        var zemin = new GameObject("Zemin").AddComponent<BoxCollider>();
        zemin.transform.SetParent(kok, false);
        zemin.center = new Vector3(0f, -0.5f, MerkezZ + 10f);
        zemin.size = new Vector3(70f, 1f, 110f);

        var duvarMalzemesi = Sekil.DokuluMalzeme(renk, normal, null, 0.5f, 6f, 0.5f);
        duvarMalzemesi.SetColor("_BaseColor", new Color(0.92f, 0.7f, 0.52f));
        Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 32, null, true), "MesaneDuvari", kok, new Vector3(0f, 4f, MerkezZ),
            new Vector3(YariX + 5f, 13f, YariZ + 6f), duvarMalzemesi).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

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
            duvar.center = new Vector3(0f, 4f, 0f);
            duvar.size = new Vector3(1.5f, 9f, (p1 - p0).magnitude + 1.5f);
        }

        // Yükselen idrar yüzeyi
        var idrar = Sekil.Malzeme(new Color(0.95f, 0.8f, 0.2f, 0.3f), 0.97f);
        var yuzey = Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 16), "IdrarYuzeyi", kok, new Vector3(0f, 0.05f, MerkezZ),
            new Vector3(YariX + 3f, 0.04f, YariZ + 3f), idrar);
        yuzey.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        idrarYuzeyi = yuzey.transform;

        // Çıkış: mesane boynundaki kas halkası
        var kas = Sekil.DokuluMalzeme(renk, normal, null, 0.75f, 1f, 0.9f);
        kas.SetColor("_BaseColor", new Color(1f, 0.6f, 0.5f));
        Sekil.MeshParca(Sekil.Halka(4.4f, 1.4f, 48, 16), "MesaneCikisi", kok, new Vector3(0f, 3.2f, CikisZ + 3.5f), Vector3.one, kas);
        Isik(new Vector3(0f, 3.5f, CikisZ + 1f), new Color(1f, 0.9f, 0.6f), 45f, 16f);
        Isik(new Vector3(0f, 11f, 12f), new Color(1f, 0.88f, 0.6f), 110f, 45f);
        Isik(new Vector3(0f, 11f, 40f), new Color(1f, 0.88f, 0.6f), 110f, 45f);

        // Kristaller: üçü çıkışın önünde, diğerleri dağınık
        var diken = Sekil.SuperElips(2.4f, 2.4f, 12);
        var malzeme = Sekil.Malzeme(new Color(0.96f, 0.93f, 0.75f), 0.85f);
        Sekil.Isilda(malzeme, new Color(0.14f, 0.12f, 0.05f));
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

        arac = AracKontrol.Olustur(new Vector3(0f, 0.2f, 12f));
        kamera = TakipKamera.Olustur(arac.transform, new Color(0.36f, 0.29f, 0.07f));
        kamera.sinir = KameraSiniri;
        kamera.Isinla();
    }

    void Isik(Vector3 konum, Color renk, float siddet, float menzil)
    {
        var isik = new GameObject("Isik").AddComponent<Light>();
        isik.transform.SetParent(kok, false);
        isik.transform.position = konum;
        isik.type = LightType.Point;
        isik.color = renk;
        isik.intensity = siddet;
        isik.range = menzil;
        isik.shadows = LightShadows.None;
    }

    protected override void ArayuzHazir() => GostergeyiYaz(false);

    void GostergeyiYaz(bool vurgu)
    {
        arayuz.Gosterge($"Kristal: {KristalAdet - kalan}/{KristalAdet}  Dolu: %{Mathf.FloorToInt(doluluk * 100f)}", vurgu);
    }

    void Start()
    {
        YansimaSondasi(new Vector3(0f, 5f, MerkezZ), new Vector3(60f, 40f, 80f)).transform.SetParent(kok, true);
    }

    // ------------------------------------------------------------------ Oyun

    protected override void OyunBasladi()
    {
        arayuz.Telsiz("İdrar borusundan mesaneye indin. Mesane, idrarı biriktiren esnek bir torbadır.");
        arayuz.Telsiz("Çıkışın önünde ve çevrede idrar kristalleri birikmiş; büyürlerse taşa dönüşürler. Köpükle erit: her kristale üç kez vur!");
        arayuz.Telsiz("Mesane doldukça duvarları gerilir ve beyne 'tuvalet zamanı' sinyali gider. Dolmadan bitirmeye çalış.");
    }

    protected override void OyunGuncelle()
    {
        doluluk = Mathf.Min(1f, doluluk + Time.deltaTime / DolmaSuresi);
        idrarYuzeyi.position = new Vector3(0f, 0.05f + 1.5f * doluluk, MerkezZ);
        RenderSettings.fogDensity = Mathf.Lerp(0.014f, 0.024f, doluluk);
        GostergeyiYaz(false);

        // Yön işareti: en yakın kristal
        if (YakinKristal(out Vector3 hedef)) arayuz.HedefIsareti(EkranDisiYonu(hedef + Vector3.up));

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

    // ------------------------------------------------------------------ Final

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Çıkış kası gevşedi! İdrarla birlikte dışarı akıyoruz... Işığı görüyorum çırak, yolculuk bitiyor!", true);
        kamera.sarsinti = 0.1f;
        yield return new WaitForSeconds(2f);

        Ses.Cal(Ses.Tur.Yutkunma);
        duvarlar.SetActive(false);
        var hedef = new Vector3(0f, 0f, CikisZ + 30f);
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            Vector3 yon = hedef - arac.transform.position;
            yon.y = 0f;
            arac.Itki(yon.normalized * 13f);
            kamera.sarsinti = 0.2f;
            yield return null;
        }
        Ses.Cal(Ses.Tur.Basari);
    }

    // Puan: mesane dolmadan bütün kristalleri eritene 3 yıldız
    protected override int Yildiz() => kalan == 0 ? 3 : kalan <= 2 ? 2 : 1;

    protected override string SonucSatiri() => $"{SureYazisi}     Eritilen kristal: {KristalAdet - kalan} / {KristalAdet}";
}
