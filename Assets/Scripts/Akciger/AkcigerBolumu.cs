using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 5 — Akciğer (gezinti). Oynanmaz: araç kan akışıyla alveollerin arasından geçer,
// oyuncu izler ve profesör anlatır. Akciğer atardamarı -> alveoller (gaz değişimi; alyuvarlar
// koyu kırmızıdan parlak kırmızıya döner) -> akciğer toplardamarı.
public class AkcigerBolumu : BolumTemeli
{
    const float Uzunluk = 262f, DegisimBasi = 105f, DegisimSonu = 150f, NefesSuresi = 4.2f;
    const int HucreAdet = 48;

    static readonly Color KoyuKan = new Color(0.45f, 0.04f, 0.07f), ParlakKan = new Color(0.95f, 0.1f, 0.08f);

    readonly List<Transform> kumeler = new List<Transform>();
    readonly List<Vector3> kumeOlcegi = new List<Vector3>();
    readonly Transform[] hucreler = new Transform[HucreAdet];
    readonly Renderer[] hucreGorseli = new Renderer[HucreAdet];
    readonly float[] hucreS = new float[HucreAdet];
    readonly Vector3[] hucreKaymasi = new Vector3[HucreAdet];
    readonly Vector3[] hucreDonusu = new Vector3[HucreAdet];

    // Anlatım: (yol üzerindeki konum, mesaj)
    readonly (float s, string mesaj)[] anlatim =
    {
        (4f, "Kalp seni akciğerlere pompaladı. Bu kan oksijenini vücuda bırakmış; o yüzden rengi koyu."),
        (52f, "İşte akciğerin içi! Üzüm salkımına benzeyen bu kesecikler alveoller. Her nefeste hava ile dolup boşalırlar."),
        (98f, "Alveollerin duvarı incecik kılcal damarlarla sarılı. Oksijen buradan kana geçer, karbondioksit ise kandan çıkıp nefesle atılır."),
        (150f, "Bak, alyuvarlar oksijeni yüklenince parlak kırmızıya döndü!"),
        (190f, "İki akciğerimizde yaklaşık 480 milyon alveol vardır. Hepsinin yüzeyi yan yana serilse bir sınıfın zemini kadar yer kaplar."),
    };

    MaterialPropertyBlock renkBlogu;
    float s = 2f, hiz = 10f, oksijenSayaci;
    int siradakiAnlatim, sonNefes = -1;

    protected override int BolumNo => 5;
    protected override string BolumAdi => "Akciğer";
    protected override string SonrakiDurak => "Böbrek";
    protected override string SonrakiSahne => "Bobrek";
    protected override bool Puanli => false;
    protected override string KartBasligi => "VÜCUT KARTI: AKCİĞER";
    protected override string KartMetni =>
        "•  İki akciğerimizde yaklaşık 480 milyon alveol vardır.\n\n" +
        "•  Alveollerin toplam yüzeyi yaklaşık 70 metrekaredir; bir sınıfın zemini kadar.\n\n" +
        "•  Dinlenirken günde yaklaşık 20.000 kez nefes alırız.";

    // ------------------------------------------------------------------ Yol

    // Aracın izlediği yol: başta ve sonda düz (damarın içinde), ortada alveollerin arasında kıvrılır.
    static Vector3 Yol(float s)
    {
        float kivrim = Mathf.SmoothStep(0f, 1f, (s - 40f) / 45f) * (1f - Mathf.SmoothStep(0f, 1f, (s - 185f) / 40f));
        return new Vector3(13f * Mathf.Sin(s * 0.036f) * kivrim, 4.5f * Mathf.Sin(s * 0.052f + 1f) * kivrim, s);
    }

    static Vector3 Yon(float s) => (Yol(s + 0.5f) - Yol(s - 0.5f)).normalized;

    // ------------------------------------------------------------------ Kurulum

    protected override void SahneyiKur()
    {
        var kok = new GameObject("Akciger").transform;
        renkBlogu = new MaterialPropertyBlock();

        // Akciğer dokusu açık pembe ve havadar
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.36f, 0.35f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.3f, 0.12f, 0.12f);
        RenderSettings.fogDensity = 0.016f;

        // Alveol kümeleri (Blender: Araclar/blender/akciger_modelleri.py)
        var alveol = Sekil.DokuluMalzeme(Resources.Load<Texture2D>("Akciger/alveol_albedo"),
            Resources.Load<Texture2D>("Akciger/alveol_normal"), null, 0.72f, 2f, 1f);
        Sekil.Isilda(alveol, new Color(0.1f, 0.035f, 0.035f));        // ince duvardan sızan ışık hissi
        var modeller = new List<Mesh>(Resources.LoadAll<Mesh>("Akciger/AkcigerModelleri"));
        if (modeller.Count == 0) Debug.LogError("Alveol modelleri bulunamadı: Resources/Akciger/AkcigerModelleri");

        for (float ks = 52f; ks < 208f; ks += 2.6f)
        {
            // Yolun çevresinde, sapı dışarıda ve kesecikleri yola dönük
            float aci = Random.value * 2f * Mathf.PI;
            var disari = new Vector3(Mathf.Cos(aci), Mathf.Sin(aci), 0f);
            float olcek = Random.Range(1.6f, 2.4f);
            var donus = Quaternion.FromToRotation(Vector3.up, -disari) * Quaternion.Euler(0f, Random.value * 360f, 0f);
            var kume = Sekil.MeshParca(modeller[Random.Range(0, modeller.Count)], "AlveolKumesi", kok,
                Yol(ks) + disari * (Random.Range(9.5f, 16f) + olcek * 3.2f), Vector3.one * olcek, alveol, donus);
            kumeler.Add(kume.transform);
            kumeOlcegi.Add(kume.transform.localScale);
        }

        // Her şeyi saran akciğer dokusu (arkada boşluk görünmesin)
        var doku = Sekil.DokuluMalzeme(Resources.Load<Texture2D>("Agiz/mukoza_albedo"),
            Resources.Load<Texture2D>("Agiz/mukoza_normal"), null, 0.5f, 8f, 0.6f);
        doku.SetColor("_BaseColor", new Color(0.9f, 0.62f, 0.6f));
        Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 32, null, true), "AkcigerDokusu", kok, new Vector3(0f, 0f, 130f),
            new Vector3(40f, 36f, 125f), doku).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Giriş: akciğer atardamarı (koyu kan), çıkış: akciğer toplardamarı (oksijenli kan)
        Damar(kok, -20f, 62f, new Color(0.7f, 0.25f, 0.27f));
        Damar(kok, 210f, 90f, new Color(1f, 0.42f, 0.38f));

        // Alveollerin arasını aydınlatan yumuşak ışıklar
        for (float ls = 55f; ls < 210f; ls += 28f)
        {
            var isik = new GameObject("Isik").AddComponent<Light>();
            isik.transform.SetParent(kok, false);
            isik.transform.position = Yol(ls) + Vector3.up * 3f;
            isik.type = LightType.Point;
            isik.color = new Color(1f, 0.88f, 0.84f);
            isik.intensity = 90f;
            isik.range = 34f;
            isik.shadows = LightShadows.None;
        }

        // Kan akışındaki alyuvarlar
        var alyuvar = Sekil.Alyuvar();
        var kan = Sekil.Malzeme(Color.white, 0.75f);
        for (int i = 0; i < HucreAdet; i++)
        {
            var g = Sekil.MeshParca(alyuvar, "Alyuvar", kok, Vector3.zero, Vector3.one * Random.Range(0.7f, 0.9f), kan, Random.rotation);
            hucreler[i] = g.transform;
            hucreGorseli[i] = g.GetComponent<Renderer>();
            hucreS[i] = Random.Range(6f, 80f);
            // Aracın çevresinde halka gibi dizilsinler; aracı ve görüşü kapatmasınlar
            Vector2 yan = Random.insideUnitCircle.normalized * Random.Range(2.6f, 4.8f);
            hucreKaymasi[i] = new Vector3(yan.x, yan.y, Random.Range(-2f, 2f));
            hucreDonusu[i] = Random.insideUnitSphere * 35f;
        }

        arac = AracKontrol.Olustur(Yol(s));
        arac.rayModu = true;
        arac.kopukAcik = false;
        arac.GetComponent<Rigidbody>().isKinematic = true;

        var cevre = new GameObject("CevreIsigi").AddComponent<Light>();
        cevre.transform.SetParent(arac.transform, false);
        cevre.transform.localPosition = new Vector3(0f, 2f, 0f);
        cevre.type = LightType.Point;
        cevre.color = new Color(1f, 0.85f, 0.82f);
        cevre.intensity = 14f;
        cevre.range = 20f;
        cevre.shadows = LightShadows.None;

        kamera = TakipKamera.Olustur(arac.transform, new Color(0.3f, 0.12f, 0.12f));
        kamera.yerelTakip = true;
        kamera.ofset = new Vector3(0f, 2.2f, -9f);
        kamera.yumusaklik = 3f;
        Yerlestir();
        kamera.Isinla();
    }

    static void Damar(Transform kok, float z, float uzunluk, Color renk)
    {
        var malzeme = Sekil.DokuluMalzeme(Resources.Load<Texture2D>("Agiz/mukoza_albedo"),
            Resources.Load<Texture2D>("Agiz/mukoza_normal"), null, 0.55f, 1f, 0.6f);
        malzeme.SetColor("_BaseColor", renk);
        var tup = Sekil.Tup(uzunluk, 40, 60, (aci, tz) => 5f * (1f + 0.04f * Mathf.Cos(aci * 7f + tz * 0.3f)), 5f, uzunluk / 6f);
        Sekil.MeshParca(tup, "Damar", kok, new Vector3(0f, 0f, z), Vector3.one, malzeme)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    protected override void ArayuzHazir()
    {
        arayuz.Ipucu("Arkana yaslan ve izle    ESC: Duraklat");
        arayuz.Gosterge("Akciğer gezintisi");
        HucreleriGuncelle(0f);
    }

    // ------------------------------------------------------------------ Gezinti

    protected override void OyunBasladi() { }

    protected override void OyunGuncelle()
    {
        float dt = Time.deltaTime;

        // Alveollerin arasında yavaşla ki iyice görülsün
        bool alveollerde = s > 55f && s < 200f;
        hiz = Mathf.MoveTowards(hiz, alveollerde ? 7f : 11f, 4f * dt);
        s += hiz * dt;
        Yerlestir();

        // Kamera hafifçe sağa sola süzülerek çevreyi gösterir
        kamera.ofset = new Vector3(3.5f * Mathf.Sin(Time.time * 0.35f), 2.2f + 1.2f * Mathf.Sin(Time.time * 0.27f), -9f);

        Nefes();
        HucreleriGuncelle(dt);

        // Gaz değişimi bölgesinde alveollerden kana süzülen oksijen
        oksijenSayaci -= dt;
        if (oksijenSayaci <= 0f && s > DegisimBasi - 12f && s < DegisimSonu + 12f)
        {
            oksijenSayaci = 0.12f;
            Baloncuk.Sac(Yol(s + Random.Range(5f, 24f)) + Random.insideUnitSphere * 3.5f, new Color(0.6f, 0.92f, 1f), 2, 0.2f);
        }

        if (siradakiAnlatim < anlatim.Length && s >= anlatim[siradakiAnlatim].s)
        {
            arayuz.Telsiz(anlatim[siradakiAnlatim].mesaj, true);
            siradakiAnlatim++;
        }

        if (s >= Uzunluk - 22f) BolumuBitir("Oksijenle doldun! Şimdi kalbin sol tarafı seni tüm vücuda pompalayacak.");
    }

    void Yerlestir()
    {
        arac.rayHizi = hiz;
        arac.transform.SetPositionAndRotation(Yol(s) - Vector3.up * 0.8f, Quaternion.LookRotation(Yon(s)));
    }

    // Nefes: alveoller hava ile dolup boşalır
    void Nefes()
    {
        float faz = Time.time / NefesSuresi;
        float dolu = 1f + 0.07f * Mathf.Sin(faz * 2f * Mathf.PI);
        for (int i = 0; i < kumeler.Count; i++) kumeler[i].localScale = kumeOlcegi[i] * dolu;

        int nefes = (int)faz;
        if (nefes == sonNefes) return;
        sonNefes = nefes;
        Ses.Cal(Ses.Tur.Dalga, 0.3f, 0.75f);
    }

    void HucreleriGuncelle(float dt)
    {
        for (int i = 0; i < HucreAdet; i++)
        {
            hucreS[i] += 6f * dt;
            if (hucreS[i] < s - 14f) hucreS[i] = s + Random.Range(45f, 80f);

            hucreler[i].position = Yol(hucreS[i]) + hucreKaymasi[i];
            hucreler[i].Rotate(hucreDonusu[i] * dt);

            // Oksijeni yüklenen alyuvar koyu kırmızıdan parlak kırmızıya döner
            float oksijen = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DegisimBasi, DegisimSonu, hucreS[i]));
            renkBlogu.SetColor("_BaseColor", Color.Lerp(KoyuKan, ParlakKan, oksijen));
            hucreGorseli[i].SetPropertyBlock(renkBlogu);
        }
    }

    // ------------------------------------------------------------------ Bölüm sonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Temiz kanla birlikte böbreklere gidiyoruz: vücudun süzgeçlerine!", true);
        yield return new WaitForSeconds(1.2f);

        Ses.Cal(Ses.Tur.Dalga, 0.9f, 1.4f);
        for (float t = 0f; t < 2.6f; t += Time.deltaTime)
        {
            hiz = 24f;
            s += hiz * Time.deltaTime;
            Yerlestir();
            HucreleriGuncelle(Time.deltaTime);
            kamera.sarsinti = 0.12f;
            yield return null;
        }
    }

    protected override int Yildiz() => 3;
    protected override string SonucSatiri() => "";
}
