using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 1 — Ağız. Sahneyi kodla kurar ve bölüm akışını yönetir:
// 10 çürük bakterisi kolonisini temizle -> Vücut Kartı -> yutkunma refleksi -> bölüm sonu.
public class AgizBolumu : BolumTemeli
{
    enum DisTuru { Kesici, Kopek, KucukAzi, Azi }

    class Dis
    {
        public Vector3 konum, iceri;
        public DisTuru tur;
        public float derinlik;
    }

    // Alt çene U biçiminde: x = ±A düz kenarlar (z: ArkaZ..0) ve önde yarım elips (yarıçaplar A, B).
    const float A = 11f, B = 16f, ArkaZ = -12f, UstY = 11.4f;
    const int HedefMikrop = 10;

    public new static AgizBolumu Ornek => BolumTemeli.Ornek as AgizBolumu;
    public IReadOnlyList<Mikrop> Mikroplar => mikroplar;

    readonly List<Dis> disler = new List<Dis>();
    readonly List<Mikrop> mikroplar = new List<Mikrop>();
    readonly HashSet<DisTuru> anlatilanDisler = new HashSet<DisTuru>();
    readonly Dictionary<DisTuru, Mesh> disMeshleri = new Dictionary<DisTuru, Mesh>();
    readonly Dictionary<DisTuru, Material> disMalzemeleri = new Dictionary<DisTuru, Material>();
    readonly List<CombineInstance> disEtiBirlesimleri = new List<CombineInstance>();
    readonly Dictionary<string, Mesh> modeller = new Dictionary<string, Mesh>();

    Material disEti, dilMalzemesi, agizIci, tukuruk, bakteri, plak;
    Mesh kure;

    Collider arkaDuvar;
    Transform kok;

    int temizlenen, dalgaCarpma;
    float dalgaSayaci = 22f, disKontrolSayaci;
    bool ilkDalga = true, carpmaAnlatildi;

    protected override int BolumNo => 1;
    protected override string BolumAdi => "Ağız";
    protected override string SonrakiDurak => "Yemek Borusu";
    protected override string SonrakiSahne => "YemekBorusuMide";
    protected override string KartBasligi => "VÜCUT KARTI: AĞIZ";
    protected override string KartMetni =>
        "•  Diş minesi vücudumuzun en sert maddesidir; kemikten bile serttir.\n\n" +
        "•  Tükürük bezlerimiz her gün yaklaşık 1 litre tükürük üretir.\n\n" +
        "•  Dilimizin üzerinde tatları algılayan binlerce tat tomurcuğu vardır.";

    // ------------------------------------------------------------------ Kurulum

    protected override void SahneyiKur()
    {
        kok = new GameObject("Agiz").transform;

        MalzemeleriHazirla();
        IsiklariKur();
        AgizIciniKur();
        DisleriKur();
        DisEtleriniKur();
        AgizAnatomisi.Kur(kok, disEti, dilMalzemesi);

        arac = AracKontrol.Olustur(new Vector3(0f, 0.1f, 5f));
        arac.ziplamaAcik=true;
        arac.Isinla(new Vector3(0f,.1f,5f),Quaternion.Euler(0f,180f,0f));
        kamera = TakipKamera.Olustur(arac.transform, new Color(0.05f, 0.01f, 0.015f));
        kamera.sinir = KameraSiniri;
        kamera.ofset = new Vector3(0f, 4.2f, -7.8f);
        kamera.bakisMesafesi = 4f;
        kamera.GetComponent<Camera>().fieldOfView = 60f;
        kamera.Isinla();
        AgizGorsel.KameraAyarla(kamera.GetComponent<Camera>());

        MikroplariKur();
    }

    protected override void ArayuzHazir()
    {
        arayuz.Gosterge($"Mikrop: 0 / {HedefMikrop}");
        arayuz.Ipucu("WASD: Sür  •  BOŞLUK: Zıpla  •  CTRL / COMMAND: Köpük  •  H: Harita");
    }

    void Start()
    {
        YansimaSondasi(new Vector3(0f, 4.5f, 2f), new Vector3(60f, 40f, 70f)).transform.SetParent(kok, true);
    }

    protected override void OyunBasladi()
    {
        arayuz.Telsiz("Çırak, beni duyuyor musun? Küçülme ışını seni ve kapsülü bir insanın ağzına göndermiş! Sakin ol, sana yol göstereceğim.");
        arayuz.Telsiz("Altındaki pürüzlü zemin dil. Çevrendeki dev beyaz kayalar ise dişler.");
        arayuz.Telsiz("Dişlerin dibindeki yeşilimsi kümeler çürük bakterileri. Kapsülü sür, temizleme köpüğüyle bakterileri hedefle. 10 koloniyi de temizle!");
    }

    // Modeller ve dokular Blender'da üretilir (Araclar/blender/agiz_modelleri.py) ve
    // Assets/Resources/Agiz klasöründen yüklenir.
    static Texture2D Doku(string ad)
    {
        var doku = Resources.Load<Texture2D>("Agiz/" + ad);
        if (doku == null) Debug.LogError("Doku bulunamadı: Resources/Agiz/" + ad);
        return doku;
    }

    Mesh Model(string ad)
    {
        if (!modeller.TryGetValue(ad, out Mesh mesh)) Debug.LogError("Model bulunamadı: " + ad);
        return mesh;
    }

    void MalzemeleriHazirla()
    {
        foreach (Mesh mesh in Resources.LoadAll<Mesh>("Mobil/AgizModelleri")) modeller[mesh.name] = mesh;
        foreach (Mesh mesh in Resources.LoadAll<Mesh>("AgizV2/Disler")) modeller[mesh.name] = mesh;

        var disAdlari = new Dictionary<DisTuru, string>
        {
            { DisTuru.Azi, "Azi" }, { DisTuru.KucukAzi, "KucukAzi" }, { DisTuru.Kopek, "Kopek" }, { DisTuru.Kesici, "Kesici" }
        };
        foreach (var cift in disAdlari)
        {
            string dosya = "dis_" + cift.Value.ToLowerInvariant();
            disMeshleri[cift.Key] = Model("Dis_" + cift.Value + "_LOD0");
            disMalzemeleri[cift.Key] = AgizGorsel.Malzeme(dosya, .78f, 1f, .3f, true);
        }

        disEti = AgizGorsel.Malzeme("mukoza", .9f, 1.15f, .28f);
        agizIci = AgizGorsel.Malzeme("mukoza", .85f, 3f, .24f);
        agizIci.SetColor("_BaseColor", new Color(.76f, .68f, .71f));
        dilMalzemesi = AgizGorsel.Malzeme("dil", .76f, 3.5f, .38f);

        tukuruk = Sekil.Malzeme(new Color(0.85f, 0.93f, 0.97f, 0.38f), 0.97f);
        plak = Sekil.Malzeme(new Color(.48f, .34f, .18f), .46f);
        bakteri = Sekil.Malzeme(new Color(0.42f, 0.49f, 0.20f), 0.58f);
        Sekil.Isilda(bakteri, new Color(0.1f, 0.13f, 0.03f));

        kure = Sekil.SuperElips(1f, 1f, 12);
    }

    void IsiklariKur()
    {
        // Tek gölgeli ana ışık; diğer yüzeylere gölgesiz, sıcak ortam dolgusu.
        var ana = new GameObject("DudakIsigi").AddComponent<Light>();
        ana.type = LightType.Directional;
        ana.color = new Color(1f, .86f, .75f);
        ana.intensity = 1.15f;
        ana.shadows = LightShadows.Soft;
        ana.shadowStrength = .58f;
        ana.shadowBias = .035f;
        ana.shadowNormalBias = .25f;
        ana.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        ana.transform.SetParent(kok, false);

        var dolgu = new GameObject("YumusakDokuDolgusu").AddComponent<Light>();
        dolgu.type = LightType.Directional;
        dolgu.color = new Color(1f,.78f,.72f);
        dolgu.intensity = .48f;
        dolgu.shadows = LightShadows.None;
        dolgu.transform.rotation = Quaternion.Euler(35f,140f,0);
        dolgu.transform.SetParent(kok,false);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.48f, .35f, .32f);
        RenderSettings.ambientEquatorColor = new Color(.34f, .23f, .22f);
        RenderSettings.ambientGroundColor = new Color(.20f, .10f, .10f);
        RenderSettings.reflectionIntensity = .85f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.13f, .035f, .045f);
        RenderSettings.fogDensity = .019f;
    }

    void AgizIciniKur()
    {
        // Dil: ortasında oluk olan, hafif dalgalı yüzey. Fizik için altında düz bir zemin var.
        var dilMesh = Sekil.Izgara(new Vector2(-17f, -19f), new Vector2(17f, 22f), 64, 80,
            AgizGorsel.DilYuksekligi, 1f);
        var dil = Sekil.MeshParca(dilMesh, "Dil", kok, Vector3.zero, Vector3.one, dilMalzemesi);
        dil.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        var zemin = new GameObject("DilZemini").AddComponent<BoxCollider>();
        zemin.transform.SetParent(kok, false);
        zemin.center = new Vector3(0f, -0.5f, 3.5f);
        zemin.size = new Vector3(36f, 1f, 37f);

        // Yanaklar, damak ve boğaz: her şeyi saran, içe bakan kabuk.
        var kabuk = Sekil.MeshParca(Sekil.SuperElips(1f, 1f, 32, null, true), "AgizIci", kok,
            new Vector3(0f, 3f, 2f), new Vector3(17.5f, 11.5f, 23f), agizIci);
        kabuk.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Boğaz tarafı: yutkunana kadar kapalı görünmez duvar.
        var duvar = new GameObject("BogazDuvari").AddComponent<BoxCollider>();
        duvar.transform.SetParent(kok, false);
        duvar.center = new Vector3(0f, 4f, ArkaZ - 1f);
        duvar.size = new Vector3(2f * A + 8f, 10f, 1f);
        arkaDuvar = duvar;
    }

    // ------------------------------------------------------------------ Dişler

    void DisleriKur()
    {
        foreach (int yon in new[] { 1, -1 })
        {
            foreach (float z in new[] { -8.5f, -4.5f, -0.5f })
            {
                DisEkle(new Vector3(yon * A, 0f, z), new Vector3(-yon, 0f, 0f), DisTuru.Azi);
            }
            YayDisi(yon, 14f, DisTuru.KucukAzi);
            YayDisi(yon, 32f, DisTuru.KucukAzi);
            YayDisi(yon, 50f, DisTuru.Kopek);
            YayDisi(yon, 66f, DisTuru.Kesici);
            YayDisi(yon, 82f, DisTuru.Kesici);
        }
    }

    void YayDisi(int yon, float derece, DisTuru tur)
    {
        float r = derece * Mathf.Deg2Rad;
        var p = new Vector3(yon * A * Mathf.Cos(r), 0f, B * Mathf.Sin(r));
        var iceri = new Vector3(-p.x / (A * A), 0f, -p.z / (B * B)).normalized;
        DisEkle(p, iceri, tur);
    }

    void DisEkle(Vector3 konum, Vector3 iceri, DisTuru tur)
    {
        Vector3 yari;   // kuronun yarı boyutları (x: çene boyunca, y: yükseklik, z: içe doğru)
        switch (tur)
        {
            case DisTuru.Azi: yari = new Vector3(1.6f, 1.3f, 1.5f); break;
            case DisTuru.KucukAzi: yari = new Vector3(1.2f, 1.3f, 1.25f); break;
            case DisTuru.Kopek: yari = new Vector3(1f, 1.75f, 1.05f); break;
            default: yari = new Vector3(1.25f, 1.55f, 0.8f); break;
        }
        var donus = Quaternion.LookRotation(iceri) * Quaternion.Euler(0, Mathf.Sin(konum.x * 2f + konum.z) * 3f, 0);

        // Alt diş (çarpışmalı). Modelin orijini diş eti hizasında, kökü aşağıda.
        string ad = tur.ToString();
        var alt = AgizGorsel.Dis(kok, "Dis_" + ad, konum, donus, disMalzemeleri[tur],
            Model("Dis_"+ad+"_LOD0"), Model("Dis_"+ad+"_LOD1"), Model("Dis_"+ad+"_LOD2"), true);
        var kutu = alt.AddComponent<BoxCollider>();
        kutu.center = new Vector3(0f, yari.y, 0f);
        kutu.size = yari * 2f;

        var ustDonus = donus * Quaternion.Euler(0f, 0f, 180f);
        var ustKonum = konum + Vector3.up * UstY;
        AgizGorsel.Dis(kok, "UstDis_" + ad, ustKonum, ustDonus, disMalzemeleri[tur],
            Model("Dis_"+ad+"_LOD0"), Model("Dis_"+ad+"_LOD1"), Model("Dis_"+ad+"_LOD2"), false);
        var collet = Model("Collet_" + ad);
        disEtiBirlesimleri.Add(new CombineInstance {mesh=collet, transform=Matrix4x4.TRS(konum,donus,Vector3.one)});
        disEtiBirlesimleri.Add(new CombineInstance {mesh=collet, transform=Matrix4x4.TRS(ustKonum,ustDonus,Vector3.one)});

        disler.Add(new Dis { konum = konum, iceri = iceri, tur = tur, derinlik = yari.z * 2f });
    }

    void DisEtleriniKur()
    {
        var model = Model("DisEti");
        Sekil.MeshParca(model, "AltDisEti", kok, Vector3.zero, Vector3.one, disEti);
        Sekil.MeshParca(model, "UstDisEti", kok, Vector3.up * UstY, Vector3.one, disEti, Quaternion.Euler(0f, 0f, 180f));

        var birlesik = SahneVarliklari.Kaydet(new Mesh {name="DisBoynuDisEti"});
        birlesik.CombineMeshes(disEtiBirlesimleri.ToArray());
        var boyunlar = Sekil.MeshParca(birlesik,"DisBoynuDisEti",kok,Vector3.zero,Vector3.one,disEti);
        boyunlar.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Araç dişlerin arasından dışarı kaçamasın: U biçimli çene yolu boyunca görünmez duvarlar
        var yol = new List<Vector3>();
        for (int i = 0; i <= 4; i++) yol.Add(new Vector3(A, 0f, Mathf.Lerp(ArkaZ - 2f, 0f, i / 4f)));
        for (int i = 1; i < 24; i++)
        {
            float r = Mathf.PI * i / 24f;
            yol.Add(new Vector3(A * Mathf.Cos(r), 0f, B * Mathf.Sin(r)));
        }
        for (int i = 0; i <= 4; i++) yol.Add(new Vector3(-A, 0f, Mathf.Lerp(0f, ArkaZ - 2f, i / 4f)));

        for (int i = 0; i < yol.Count - 1; i++)
        {
            Vector3 yon = yol[i + 1] - yol[i];
            float uzunluk = yon.magnitude;
            yon /= uzunluk;
            Vector3 disari = Vector3.Cross(Vector3.up, yon);

            var duvar = new GameObject("CeneDuvari").AddComponent<BoxCollider>();
            duvar.transform.SetParent(kok, false);
            duvar.transform.SetPositionAndRotation((yol[i] + yol[i + 1]) * 0.5f + disari * 1.6f, Quaternion.LookRotation(yon));
            duvar.center = new Vector3(0f, 4f, 0f);
            duvar.size = new Vector3(2.2f, 9f, uzunluk + 2f);
        }
    }

    // ------------------------------------------------------------------ Mikroplar

    void MikroplariKur()
    {
        int[] secilenDisler = { 0, 2, 3, 5, 7, 9, 10, 12, 13, 14 };
        foreach (int i in secilenDisler)
        {
            Dis dis = disler[i];
            Vector3 yer = dis.konum + dis.iceri * (dis.derinlik * 0.5f + 1.4f) + Vector3.up * 0.33f;
            var mikrop = Mikrop.Olustur(yer, Random.Range(0.5f, 0.62f),
                Model("Bakteri_" + Random.Range(1, 4)), bakteri);
            mikrop.transform.SetParent(kok, true);
            mikrop.temizlendi = MikropTemizlendi;

            // Biyofilm diş boynuna yayılan düzensiz birikim; koloni temizlenince kaldırılır.
            mikrop.leke = AgizGorsel.Biyofilm(kok,dis.konum + dis.iceri * (dis.derinlik * .5f - .17f)
                + Vector3.up * .68f,Quaternion.LookRotation(dis.iceri),plak,i);

            mikroplar.Add(mikrop);
        }
    }

    void MikropTemizlendi(Mikrop mikrop)
    {
        temizlenen++;
        kamera.sarsinti = Mathf.Max(kamera.sarsinti, 0.05f);
        arayuz.Gosterge($"Mikrop: {temizlenen} / {HedefMikrop}", true);

        if (temizlenen == 1)
        {
            arayuz.Telsiz("Harika! Bu bakteriler şekerle beslenip asit üretir. Asit de dişin minesini eritir; çürük işte böyle oluşur.", true);
        }
        else if (temizlenen == HedefMikrop / 2)
        {
            arayuz.Telsiz("Yarısı tamam! Dişleri günde iki kez fırçalamak bu bakterileri uzak tutar.", true);
        }

        if (temizlenen >= HedefMikrop) BolumuBitir("Hepsini temizledin, aferin çırak! Bu dişler artık güvende.");
    }

    // ------------------------------------------------------------------ Oyun döngüsü

    protected override void OyunGuncelle()
    {
        DisleriAnlat();
        HedefiGoster();
        dalgaSayaci -= Time.deltaTime;
        if (dalgaSayaci <= 0f)
        {
            dalgaSayaci = Random.Range(13f, 19f);
            DalgaGonder();
        }
    }

    // En yakın koloni ekranda değilse, ekranın kenarında yönünü gösteren işaret çıkar.
    void HedefiGoster()
    {
        Mikrop yakin = null;
        float enAz = float.MaxValue;
        foreach (Mikrop m in mikroplar)
        {
            if (m == null || m.Eriyor) continue;
            float uzaklik = (m.transform.position - arac.transform.position).sqrMagnitude;
            if (uzaklik < enAz)
            {
                enAz = uzaklik;
                yakin = m;
            }
        }
        if (yakin == null)
        {
            arayuz.HedefIsareti(null);
            return;
        }

        Vector3 g = kameraGozu.WorldToViewportPoint(yakin.transform.position + Vector3.up);
        if (g.z > 0f && g.x > 0.04f && g.x < 0.96f && g.y > 0.04f && g.y < 0.96f)
        {
            arayuz.HedefIsareti(null);      // zaten görünüyor
            return;
        }
        Vector2 yon = new Vector2(g.x - 0.5f, g.y - 0.5f) * (g.z < 0f ? -1f : 1f);
        yon *= 0.44f / Mathf.Max(Mathf.Abs(yon.x), Mathf.Abs(yon.y), 0.001f);
        arayuz.HedefIsareti(new Vector2(0.5f, 0.5f) + yon);
    }

    // Araç bir diş türüne ilk kez yaklaşınca profesör o dişi anlatır.
    void DisleriAnlat()
    {
        disKontrolSayaci -= Time.deltaTime;
        if (disKontrolSayaci > 0f) return;
        disKontrolSayaci = 0.3f;
        if (!arayuz.TelsizBos) return;

        Vector3 a = arac.transform.position;
        foreach (Dis dis in disler)
        {
            if (anlatilanDisler.Contains(dis.tur)) continue;
            if ((dis.konum - a).sqrMagnitude > 5.5f * 5.5f) continue;

            anlatilanDisler.Add(dis.tur);
            switch (dis.tur)
            {
                case DisTuru.Kesici:
                    arayuz.Telsiz("Öndeki yassı dişler kesici dişler. Makas gibi çalışıp lokmayı ısırır ve keserler.");
                    break;
                case DisTuru.Kopek:
                    arayuz.Telsiz("Bu sivri diş köpek dişi. Yiyecekleri tutup koparmaya yarar.");
                    break;
                case DisTuru.KucukAzi:
                    arayuz.Telsiz("Bunlar küçük azı dişleri. Lokmayı ezip parçalamaya başlarlar.");
                    break;
                case DisTuru.Azi:
                    arayuz.Telsiz("Arkadaki geniş dişler büyük azı dişleri. Değirmen taşı gibi yiyecekleri öğütürler.");
                    break;
            }
            return;
        }
    }

    // Ağzın bir kısmını kaplayan tükürük dalgası gönderir; kalan boşluk kaçış yoludur.
    public void DalgaGonder()
    {
        float xMerkez, yari;
        switch (Random.Range(0, 3))
        {
            case 0: xMerkez = -4f; yari = 6f; break;    // sol taraf dolu, sağ boş
            case 1: xMerkez = 4f; yari = 6f; break;     // sağ taraf dolu, sol boş
            default: xMerkez = 0f; yari = 5f; break;    // orta dolu, kenarlar boş
        }

        var dalga = TukurukDalgasi.Olustur(arac, tukuruk, xMerkez, yari, 7f, 12f, ArkaZ - 1f);
        Ses.Cal(Ses.Tur.Dalga, 0.7f);
        dalga.carpti = () =>
        {
            if (durum != Durum.Oyun) return;
            dalgaCarpma++;
            HasarAl(20,"Tükürük dalgası");
            Ses.Cal(Ses.Tur.Carpma);
            kamera.sarsinti = 0.2f;
            if (carpmaAnlatildi) return;
            carpmaAnlatildi = true;
            arayuz.Telsiz("Dalga seni boğaza doğru sürükledi! Bir dahakinde dalganın boş tarafına kaç.", true);
        };

        if (ilkDalga)
        {
            ilkDalga = false;
            arayuz.Telsiz("Dikkat, tükürük dalgası geliyor! Dalganın boş tarafına kaç ya da üzerinden zıpla!", true);
            arayuz.Telsiz("Tükürük aslında dostumuz: lokmayı yumuşatır, sindirimi daha ağızdayken başlatır ve dişleri korur.");
        }
        else if (arayuz.TelsizBos)
        {
            arayuz.Telsiz("Yeni bir tükürük dalgası! Boş tarafa geç.");
        }
    }

    // ------------------------------------------------------------------ Bölüm sonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Eyvah... Yutkunma refleksi geliyor! Sıkı tutun çırak!", true);
        kamera.sarsinti = 0.12f;
        yield return new WaitForSeconds(2f);

        // Ağzı boydan boya kaplayan dev dalga aracı boğaza sürükler.
        Ses.Cal(Ses.Tur.Yutkunma);
        TukurukDalgasi.Olustur(arac, tukuruk, 0f, A - 1f, 15f, 13f, ArkaZ - 8f);
        arkaDuvar.enabled = false;
        var bogaz = new Vector3(0f, 0f, ArkaZ - 9f);

        for (float t = 0f; t < 3.2f; t += Time.deltaTime)
        {
            Vector3 yon = bogaz - arac.transform.position;
            yon.y = 0f;
            arac.Itki(yon.normalized * 13f);
            kamera.sarsinti = 0.25f;
            yield return null;
        }
    }

    // Puan: dalgalara yakalanmadan ve hızlı bitirene 3 yıldız
    protected override int Yildiz() => dalgaCarpma == 0 && sure <= 100f ? 3 : dalgaCarpma <= 1 && sure <= 170f ? 2 : 1;

    protected override string SonucSatiri() => $"{SureYazisi}     Dalgaya yakalanma: {dalgaCarpma}";

    // Kamerayı çenenin içinde tutar; diş etlerinin arkasına geçip görüşü kapatmasın.
    static Vector3 KameraSiniri(Vector3 p)
    {
        const float a = A - 2f, b = B - 2f;
        if (p.z > 0f)
        {
            float k = p.x * p.x / (a * a) + p.z * p.z / (b * b);
            if (k > 1f)
            {
                float s = 1f / Mathf.Sqrt(k);
                p.x *= s;
                p.z *= s;
            }
        }
        else
        {
            p.x = Mathf.Clamp(p.x, -a, a);
        }
        p.z = Mathf.Max(p.z, -17f);
        return p;
    }
}
