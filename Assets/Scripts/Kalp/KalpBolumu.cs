using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 4 — Kan damarları ve kalp.
// 1) Toplardamar: kan akışı aracı kalbe taşır. Oyuncu WASD ile damarın içinde süzülür; alyuvarlar
//    yavaşlatır, akyuvarlar kapsülü mikrop sanıp yakalamaya çalışır (köpük onları sakinleştirir).
// 2) Kalp: sağ kulakçık -> sağ karıncık -> akciğer atardamarı. Kapakçıklar kalp atışıyla açılıp
//    kapanır; oyuncu kapak açıkken BOŞLUK ile atılıp geçer.
public class KalpBolumu : BolumTemeli
{
    enum Asama { Damar, Kulakcik, Karincik, Atardamar }

    // Damar +Z yönünde uzanır. Kalp odacıkları damarın genişlediği yerlerdir.
    const float DamarL = 360f, KulakcikZ = DamarL + 16f, Kapak1Z = DamarL + 32f;
    const float KarincikZ = DamarL + 50f, Kapak2Z = DamarL + 68f, BitisZ = DamarL + 135f;
    const float AtisSuresi = 1.3f, BeklemeMesafesi = 9f, AtilmaHizi = 26f;
    const int HucreAdet = 70;

    public new static KalpBolumu Ornek => BolumTemeli.Ornek as KalpBolumu;
    public string AsamaAdi => asama.ToString();
    public bool SiradakiKapakAcik => asama == Asama.Kulakcik ? Kapak1Acik : asama == Asama.Karincik && Kapak2Acik;

    Asama asama = Asama.Damar;
    Transform kok;
    Mesh kure;

    // Araç: z damar boyunca, (x, y) enine kesitte
    Vector3 konum = new Vector3(0f, -0.8f, 6f);
    Vector2 yanalHiz;
    float hiz = 12f, atilma, geriItis;
    bool oncekiAtes, kapaktanGecti;

    // Alyuvar havuzu
    readonly Transform[] hucreler = new Transform[HucreAdet];
    readonly Vector3[] hucreHizi = new Vector3[HucreAdet];
    readonly Vector3[] hucreDonusu = new Vector3[HucreAdet];
    float carpmaSesi;

    // Akyuvarlar
    readonly List<Akyuvar> akyuvarlar = new List<Akyuvar>();
    Mesh akyuvarMesh;
    float akyuvarSayaci = 9f, yakalanmaSuresi;
    int yakalanma;
    bool akyuvarAnlatildi, sakinAnlatildi;

    // Kalp
    readonly Transform[][] kapaklar = new Transform[2][];
    readonly Light[] kapakIsiklari = new Light[2];
    readonly float[] kapakAcikligi = new float[2];
    float atisZamani;
    int sonVurus = -1, kacirma;
    bool kacirmaAnlatildi;

    float Faz => atisZamani % AtisSuresi / AtisSuresi;
    // Kulakçık-karıncık kapağı kalp gevşerken, akciğer atardamarı kapağı karıncık kasılırken açılır.
    bool Kapak1Acik => Faz > 0.45f && Faz < 0.95f;
    bool Kapak2Acik => Faz < 0.38f;

    protected override int BolumNo => 4;
    protected override string BolumAdi => "Kan Damarları ve Kalp";
    protected override string SonrakiDurak => "Akciğer";
    protected override string SonrakiSahne => "Akciger";
    protected override bool OrtamSesi => false;      // bu bölümde kalp atışını bölümün kendisi çalar
    protected override string KartBasligi => "VÜCUT KARTI: KALP VE KAN";
    protected override string KartMetni =>
        "•  Kalbimizin dört odacığı vardır: sağ taraf kanı akciğerlere, sol taraf tüm vücuda pompalar.\n\n" +
        "•  Kalp günde yaklaşık 100.000 kez atar ve hiç dinlenmez.\n\n" +
        "•  Alyuvarlar oksijen taşır, akyuvarlar mikroplarla savaşır, trombositler yaraları kapatır.";

    // ------------------------------------------------------------------ Kurulum

    // Damarın z konumundaki yarıçapı: odacıklarda genişler, kapakçıklarda daralır.
    static float Yaricap(float z)
    {
        float r = 6f + 8.5f * Tumsek(z, KulakcikZ, 15f) + 9f * Tumsek(z, KarincikZ, 16f);
        return r - 0.8f * Mathf.SmoothStep(0f, 1f, (z - Kapak2Z) / 8f);
    }

    static float Tumsek(float z, float merkez, float genislik)
    {
        float d = Mathf.Abs(z - merkez);
        return d < genislik ? 0.5f * (1f + Mathf.Cos(Mathf.PI * d / genislik)) : 0f;
    }

    protected override void SahneyiKur()
    {
        kok = new GameObject("DamarVeKalp").transform;
        kure = Sekil.SuperElips(1f, 1f, 14);
        var renk = Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var normal = Resources.Load<Texture2D>("Agiz/mukoza_normal");

        // Damar ve kalp duvarı: tek parça, içinden bakılan tüp
        var duvar = Sekil.DokuluMalzeme(renk, normal, null, 0.55f, 1f, 0.6f);
        duvar.SetColor("_BaseColor", new Color(0.85f, 0.32f, 0.32f));
        var tup = Sekil.Tup(BitisZ + 60f, 56, 520, (aci, z) =>
            Yaricap(z - 20f) * (1f + 0.035f * Mathf.Cos(aci * 7f + z * 0.25f)), 6f, 90f);
        Sekil.MeshParca(tup, "Duvar", kok, new Vector3(0f, 0f, -20f), Vector3.one, duvar)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Kandaki bulanıklık: koyu kırmızı sis
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.14f, 0.14f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.22f, 0.02f, 0.03f);
        RenderSettings.fogDensity = 0.022f;

        HucreleriKur();
        KapaklariKur();

        arac = AracKontrol.Olustur(konum);
        arac.rayModu = true;
        arac.GetComponent<Rigidbody>().isKinematic = true;

        var cevre = new GameObject("CevreIsigi").AddComponent<Light>();
        cevre.transform.SetParent(arac.transform, false);
        cevre.transform.localPosition = new Vector3(0f, 2f, 0f);
        cevre.type = LightType.Point;
        cevre.color = new Color(1f, 0.8f, 0.78f);
        cevre.intensity = 30f;
        cevre.range = 20f;
        cevre.shadows = LightShadows.None;

        kamera = TakipKamera.Olustur(arac.transform, new Color(0.22f, 0.02f, 0.03f));
        kamera.yerelTakip = true;
        kamera.ofset = new Vector3(0f, 2.4f, -8.5f);
        kamera.Isinla();
    }

    void HucreleriKur()
    {
        // Alyuvar: iki yüzü çukur disk. Toplardamar kanı oksijeni az olduğu için koyu kırmızı.
        var alyuvar = Sekil.Alyuvar();
        var malzeme = Sekil.Malzeme(new Color(0.5f, 0.05f, 0.07f), 0.7f);
        for (int i = 0; i < HucreAdet; i++)
        {
            hucreler[i] = Sekil.MeshParca(alyuvar, "Alyuvar", kok, Vector3.zero, Vector3.one * Random.Range(0.75f, 0.95f), malzeme).transform;
            HucreyiYerlestir(i, Random.Range(14f, 95f));
        }

        akyuvarMesh = Sekil.SuperElips(1f, 1f, 16, p =>
            p * (0.88f + 0.25f * Mathf.PerlinNoise(p.x * 2.4f + 5f, p.y * 2.4f + p.z * 2f + 5f)));
    }

    void HucreyiYerlestir(int i, float ileri)
    {
        float z = konum.z + ileri;
        Vector2 yer = Random.insideUnitCircle * (Yaricap(z) - 1.4f);
        hucreler[i].SetPositionAndRotation(new Vector3(yer.x, yer.y, z), Random.rotation);
        hucreHizi[i] = new Vector3(0f, 0f, Random.Range(5f, 7.5f));
        hucreDonusu[i] = Random.insideUnitSphere * 40f;
    }

    // Kapakçık: üç yaprakçık. Kapalıyken ortada birleşir, açılınca duvara çekilir.
    void KapaklariKur()
    {
        var doku = Sekil.Malzeme(new Color(0.96f, 0.8f, 0.76f), 0.7f);
        float[] yerler = { Kapak1Z, Kapak2Z };
        for (int k = 0; k < 2; k++)
        {
            kapaklar[k] = new Transform[3];
            for (int y = 0; y < 3; y++)
            {
                kapaklar[k][y] = Sekil.MeshParca(kure, "Yaprakcik", kok, Vector3.zero, Vector3.one, doku).transform;
            }
            var isik = new GameObject("KapakIsigi").AddComponent<Light>();
            isik.transform.SetParent(kok, false);
            isik.transform.position = new Vector3(0f, 0f, yerler[k] - 2.5f);
            isik.type = LightType.Point;
            isik.range = 16f;
            isik.shadows = LightShadows.None;
            kapakIsiklari[k] = isik;
        }
        KapaklariGuncelle(1f);
    }

    void KapaklariGuncelle(float dt)
    {
        float[] yerler = { Kapak1Z, Kapak2Z };
        for (int k = 0; k < 2; k++)
        {
            bool acik = k == 0 ? Kapak1Acik : Kapak2Acik;
            kapakAcikligi[k] = Mathf.MoveTowards(kapakAcikligi[k], acik ? 1f : 0f, 7f * dt);
            float a = kapakAcikligi[k], r = Yaricap(yerler[k]);
            for (int y = 0; y < 3; y++)
            {
                float aci = (y * 120f + 90f) * Mathf.Deg2Rad;
                float uzaklik = Mathf.Lerp(r * 0.45f, r * 1.15f, a);
                float boy = Mathf.Lerp(r * 0.66f, r * 0.4f, a);
                kapaklar[k][y].position = new Vector3(Mathf.Cos(aci) * uzaklik, Mathf.Sin(aci) * uzaklik, yerler[k]);
                kapaklar[k][y].localScale = new Vector3(boy, boy, 0.45f);
            }
            // Açıkken yeşil, kapalıyken kırmızı ışık
            kapakIsiklari[k].color = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.4f, 1f, 0.5f), a);
            kapakIsiklari[k].intensity = 30f + 50f * a;
        }
    }

    protected override void ArayuzHazir()
    {
        arayuz.Ipucu("WASD: Süzül    BOŞLUK: Köpük    ESC: Duraklat");
        arayuz.Gosterge($"Kalbe: {(int)(DamarL - konum.z)} m");
        AraciYerlestir(Vector2.zero);
    }

    // ------------------------------------------------------------------ Oyun

    protected override void OyunBasladi()
    {
        arayuz.Telsiz("Besinlerle birlikte kana karıştın! Şu an bir toplardamardasın; kan akışı seni kalbe taşıyor.");
        arayuz.Telsiz("Çevrendeki kırmızı diskler alyuvarlar: oksijen taşırlar. Tek bir damla kanda milyonlarcası vardır. WASD ile aralarından süzül.");
    }

    protected override void OyunGuncelle()
    {
        float dt = Time.deltaTime;
        atisZamani += dt;
        KalpAtisi();
        KapaklariGuncelle(dt);

        // Enine kesitte süzülme (kalpte araç kendiliğinden ortaya gelir)
        Vector2 girdi = asama == Asama.Damar || asama == Asama.Atardamar
            ? new Vector2(arac.DonusGirdi, arac.IleriGirdi)
            : -new Vector2(konum.x, konum.y + 0.8f) * 0.35f;
        yanalHiz = Vector2.MoveTowards(yanalHiz, girdi * 7f, 22f * dt);
        Vector2 yanal = new Vector2(konum.x, konum.y) + yanalHiz * dt;
        float sinir = Yaricap(konum.z) - 2f;
        Vector2 merkezden = yanal + Vector2.up * 0.8f;          // aracın gövde merkezi orijinin üstünde
        if (merkezden.magnitude > sinir) yanal = merkezden.normalized * sinir - Vector2.up * 0.8f;
        konum.x = yanal.x;
        konum.y = yanal.y;

        switch (asama)
        {
            case Asama.Damar: DamarGuncelle(dt); break;
            case Asama.Kulakcik: KapakBekle(dt, Kapak1Z, Asama.Karincik); break;
            case Asama.Karincik: KapakBekle(dt, Kapak2Z, Asama.Atardamar); break;
            case Asama.Atardamar:
                hiz = Mathf.MoveTowards(hiz, 20f, 20f * dt);
                konum.z += hiz * dt;
                if (konum.z >= BitisZ) BolumuBitir("Kalbin sağ tarafını geçtin! Kan seni şimdi akciğerlere pompalıyor.");
                break;
        }

        arac.rayHizi = hiz;
        AraciYerlestir(yanalHiz);
        HucreleriGuncelle(dt);
        kameraGozu.fieldOfView = Mathf.Lerp(kameraGozu.fieldOfView, atilma > 0f || asama == Asama.Atardamar ? 72f : 60f, 4f * dt);
    }

    void AraciYerlestir(Vector2 yanal)
    {
        // Süzülürken hafifçe yatar
        arac.transform.SetPositionAndRotation(konum, Quaternion.Euler(-yanal.y * 1.5f, yanal.x * 1.2f, -yanal.x * 2.5f));
    }

    // Kalp atışı: "lub" (kulakçık-karıncık kapağı kapanır) ve "dub" (atardamar kapağı kapanır)
    void KalpAtisi()
    {
        int vurus = (int)(atisZamani / AtisSuresi) * 2 + (Faz >= 0.38f ? 1 : 0);
        if (vurus == sonVurus) return;
        sonVurus = vurus;
        bool lub = vurus % 2 == 0;
        float yakinlik = Mathf.InverseLerp(DamarL - 150f, DamarL, konum.z);      // kalbe yaklaştıkça yükselir
        Ses.Cal(Ses.Tur.Carpma, Mathf.Lerp(0.15f, 0.9f, yakinlik), lub ? 0.6f : 0.8f);
        if (asama != Asama.Damar) kamera.sarsinti = Mathf.Max(kamera.sarsinti, lub ? 0.07f : 0.04f);
    }

    // ------------------------------------------------------------------ Damar

    void DamarGuncelle(float dt)
    {
        yakalanmaSuresi -= dt;
        hiz = Mathf.MoveTowards(hiz, yakalanmaSuresi > 0f ? 3f : 12f, 9f * dt);
        konum.z += hiz * dt;
        arayuz.Gosterge($"Kalbe: {Mathf.Max(0, (int)(DamarL - konum.z))} m");

        // Akyuvarlar
        akyuvarSayaci -= dt;
        if (akyuvarSayaci <= 0f && konum.z < DamarL - 90f)
        {
            akyuvarSayaci = Random.Range(7f, 10f);
            float z = konum.z + 60f;
            Vector2 yer = Random.insideUnitCircle * 3f;
            akyuvarlar.Add(Akyuvar.Olustur(kok, akyuvarMesh, new Vector3(yer.x, yer.y, z), arac.transform, 4.5f));
            if (!akyuvarAnlatildi)
            {
                akyuvarAnlatildi = true;
                arayuz.Telsiz("Dikkat, ileride bir akyuvar! Vücudun askerleridir, mikropları yutarlar. Seni mikrop sanacak: kaç ya da köpükle kendini tanıt.", true);
            }
        }

        Vector3 govde = konum + Vector3.up * 0.8f;
        for (int i = akyuvarlar.Count - 1; i >= 0; i--)
        {
            Akyuvar a = akyuvarlar[i];
            if (a.transform.position.z < konum.z - 14f)
            {
                Destroy(a.gameObject);
                akyuvarlar.RemoveAt(i);
                continue;
            }
            if (a.Sakin)
            {
                if (!sakinAnlatildi && yakalanmaSuresi <= 0f)
                {
                    sakinAnlatildi = true;
                    arayuz.Telsiz("Köpüğü tanıdı ve sakinleşti. Akyuvarlar dostumuz; yalnızca işlerini yapıyorlar.", true);
                }
                continue;
            }
            if ((a.transform.position - govde).sqrMagnitude < 2.4f * 2.4f)
            {
                yakalanma++;
                HasarAl(15,"Akyuvar teması");
                if(Can<=0)return;
                yakalanmaSuresi = 2.2f;
                kamera.sarsinti = 0.22f;
                Ses.Cal(Ses.Tur.Carpma);
                a.Sakinles();
                sakinAnlatildi = true;
                arayuz.Telsiz("Akyuvar seni yakaladı! Mikrop olmadığını anlayınca bıraktı. Bir dahakinde köpükle kendini tanıt.", true);
            }
        }

        if (konum.z >= DamarL)
        {
            asama = Asama.Kulakcik;
            arayuz.GorevGuncelle("Kapak yeşilken ATIL / BOŞLUK ile sağ karıncığa geç.");
            arac.kopukAcik = false;
            arayuz.AtesEtiketi("ATIL");
            arayuz.Ipucu("BOŞLUK: Kapak açıkken atıl    ESC: Duraklat");
            arayuz.Gosterge("Sağ kulakçık");
            arayuz.Telsiz("Kalbe girdin: burası sağ kulakçık! Kalbin dört odacığı vardır: iki kulakçık ve iki karıncık.", true);
            arayuz.Telsiz("Önündeki kapakçık kalp atışıyla açılıp kapanır. Işık yeşile dönünce BOŞLUK'a basıp atıl!");
        }
    }

    void HucreleriGuncelle(float dt)
    {
        carpmaSesi -= dt;
        Vector3 govde = konum + Vector3.up * 0.8f;
        for (int i = 0; i < HucreAdet; i++)
        {
            Transform h = hucreler[i];
            Vector3 p = h.position + hucreHizi[i] * dt;
            hucreHizi[i] = Vector3.MoveTowards(hucreHizi[i], new Vector3(0f, 0f, hucreHizi[i].z), 4f * dt);

            // Araç çarpınca hücre kenara itilir, araç biraz yavaşlar
            Vector3 fark = p - govde;
            if (fark.sqrMagnitude < 1.6f * 1.6f)
            {
                Vector3 yana = new Vector3(fark.x, fark.y, 0f);
                if (yana.sqrMagnitude < 0.01f) yana = Random.insideUnitCircle;
                hucreHizi[i] += yana.normalized * 14f * dt * 10f;
                if (asama == Asama.Damar) hiz = Mathf.Max(6f, hiz - 18f * dt);
                if (carpmaSesi <= 0f)
                {
                    carpmaSesi = 0.35f;
                    Ses.Cal(Ses.Tur.Tik, 0.5f, 0.5f);
                }
            }

            // Damar duvarının içinde kalsın
            float sinir = Yaricap(p.z) - 1f;
            Vector2 yanal = new Vector2(p.x, p.y);
            if (yanal.magnitude > sinir)
            {
                yanal = yanal.normalized * sinir;
                p.x = yanal.x;
                p.y = yanal.y;
            }
            h.position = p;
            h.Rotate(hucreDonusu[i] * dt);

            if (p.z < konum.z - 12f) HucreyiYerlestir(i, Random.Range(70f, 95f));
        }
    }

    // ------------------------------------------------------------------ Kalp

    void KapakBekle(float dt, float kapakZ, Asama sonraki)
    {
        float bekleme = kapakZ - BeklemeMesafesi;
        bool ates = arac.AtesGirdi;
        bool basildi = ates && !oncekiAtes;
        oncekiAtes = ates;

        if (atilma > 0f)
        {
            // Atılma: kapak düzlemini geçerken kapak açıksa geçer, değilse geri seker
            atilma -= dt;
            float yeniZ = konum.z + AtilmaHizi * dt;
            if (!kapaktanGecti && konum.z < kapakZ - 1.2f && yeniZ >= kapakZ - 1.2f)
            {
                bool acik = sonraki == Asama.Karincik ? Kapak1Acik : Kapak2Acik;
                if (acik)
                {
                    kapaktanGecti = true;
                    Ses.Cal(Ses.Tur.Dalga, 0.7f, 1.5f);
                }
                else
                {
                    atilma = 0f;
                    geriItis = 14f;
                    kacirma++;
                    HasarAl(18,"Kapalı kapak");
                    if(Can<=0)return;
                    kamera.sarsinti = 0.25f;
                    Ses.Cal(Ses.Tur.Carpma);
                    if (!kacirmaAnlatildi)
                    {
                        kacirmaAnlatildi = true;
                        arayuz.Telsiz("Kapak kapalıydı! Kalp atışını dinle; ışık yeşile döner dönmez atıl.", true);
                    }
                    return;
                }
            }
            konum.z = yeniZ;
            hiz = AtilmaHizi;

            if (kapaktanGecti && atilma <= 0f)
            {
                kapaktanGecti = false;
                asama = sonraki;
                if (sonraki == Asama.Karincik)
                {
                    arayuz.Gosterge("Sağ karıncık", true);
                    arayuz.GorevGuncelle("İkinci kapağın açılmasını bekle; yeşilken atıl.");
                    arayuz.Telsiz("Sağ karıncıktasın! Karıncık kasılınca kanı akciğerlere pompalar. Sıradaki kapak karıncık kasıldığı anda açılır.", true);
                }
                else
                {
                    arac.kopukAcik = true;
                    arayuz.AtesEtiketi("KÖPÜK");
                    arayuz.Gosterge("Akciğer atardamarı", true);
                    arayuz.GorevGuncelle("Akışla ilerle ve akciğere ulaş.");
                    arayuz.Ipucu("WASD: Süzül    BOŞLUK: Köpük    ESC: Duraklat");
                    arayuz.Telsiz("Geçtin! Kalp günde yaklaşık 100.000 kez atar ve kanı hiç durmadan pompalar.", true);
                }
            }
            return;
        }

        // Bekleme noktasına doğru süzül (kaçırınca geri itilir)
        geriItis = Mathf.MoveTowards(geriItis, 0f, 30f * dt);
        hiz = Mathf.MoveTowards(hiz, Mathf.Clamp((bekleme - konum.z) * 2f, -6f, 9f), 30f * dt);
        konum.z += (hiz - geriItis) * dt;

        if (basildi && Mathf.Abs(konum.z - bekleme) < 3f)
        {
            atilma = 0.55f;
            kapaktanGecti = false;
            Ses.Cal(Ses.Tur.Kopuk, 0.8f, 0.7f);
        }
    }

    // ------------------------------------------------------------------ Bölüm sonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Akciğerlere doğru hızlanıyoruz! Orada kan temiz oksijenle dolacak.", true);
        yield return new WaitForSeconds(1.2f);

        Ses.Cal(Ses.Tur.Dalga, 0.9f, 1.4f);
        for (float t = 0f; t < 2.6f; t += Time.deltaTime)
        {
            konum.z += 26f * Time.deltaTime;
            AraciYerlestir(Vector2.zero);
            HucreleriGuncelle(Time.deltaTime);
            kamera.sarsinti = 0.15f;
            kameraGozu.fieldOfView = Mathf.Lerp(kameraGozu.fieldOfView, 78f, 3f * Time.deltaTime);
            yield return null;
        }
    }

    // Puan: akyuvarlara yakalanmadan ve kapakları neredeyse hiç kaçırmadan geçene 3 yıldız
    protected override int Yildiz() => yakalanma == 0 && kacirma <= 1 ? 3 : yakalanma <= 1 && kacirma <= 3 ? 2 : 1;

    protected override string SonucSatiri() =>
        $"{SureYazisi}     Akyuvara yakalanma: {yakalanma}     Kaçırılan kapak: {kacirma}";
}
