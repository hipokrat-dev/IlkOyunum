using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Bölüm 6 — Böbrek.
// 1) Böbrek atardamarı -> glomerulus (kılcal damar yumağı): kan basıncı aracı süzgeçten geçirir.
// 2) Nefron kanalı: süzülen sıvıdaki yararlı maddelere (su, glikoz, tuz) dokunup onları kana geri
//    kazandır (geri emilim); atıklara (üre, zehir) dokunma, idrarla atılsınlar.
public class BobrekBolumu : AkisBolumu
{
    enum Tur { Su, Glikoz, Tuz, Ure, Zehir }

    class Madde
    {
        public Transform govde;
        public bool yararli, alindi;
        public Vector3 kacis;
    }

    const float SuzgecZ = 100f, KanalBasi = 125f, BitisZ = 420f;
    const int HucreAdet = 36;

    public new static BobrekBolumu Ornek => BolumTemeli.Ornek as BobrekBolumu;
    public int GeriKazanilan => geriKazanilan;

    readonly List<Madde> maddeler = new List<Madde>();
    readonly Transform[] hucreler = new Transform[HucreAdet];
    readonly Vector3[] hucreDonusu = new Vector3[HucreAdet];

    Transform kok;
    Mesh kure;
    int yararliToplam, geriKazanilan, atikHatasi;
    float suzulmeEfekti;
    bool suzuldu, kanalAnlatildi, emilimAnlatildi, atikAnlatildi, ortaAnlatildi, yumakAnlatildi;

    static readonly Color KanSisi = new Color(0.22f, 0.02f, 0.03f), IdrarSisi = new Color(0.34f, 0.27f, 0.08f);

    protected override int BolumNo => 6;
    protected override string BolumAdi => "Böbrek";
    protected override string SonrakiDurak => "Mesane";
    protected override string SonrakiSahne => "Mesane";
    protected override string KartBasligi => "VÜCUT KARTI: BÖBREK";
    protected override string KartMetni =>
        "•  Her böbrekte nefron adı verilen yaklaşık 1 milyon minik süzgeç vardır.\n\n" +
        "•  Böbrekler günde yaklaşık 180 litre sıvı süzer; bunun yalnızca 1-2 litresi idrar olur, gerisi geri emilir.\n\n" +
        "•  İdrarın yaklaşık %95'i sudur; kalanı üre ve tuzlar gibi atıklardır.";

    // ------------------------------------------------------------------ Kurulum

    // Atardamar -> genişleyen glomerulus boşluğu -> dar nefron kanalı
    protected override float KanalYaricapi(float z)
    {
        float d = Mathf.Abs(z - 80f);
        float yumak = d < 22f ? 0.5f * (1f + Mathf.Cos(Mathf.PI * d / 22f)) : 0f;
        return 5f + 6.5f * yumak - 0.5f * Mathf.SmoothStep(0f, 1f, (z - SuzgecZ) / 10f);
    }

    protected override void SahneyiKur()
    {
        kok = new GameObject("Bobrek").transform;
        kure = Sekil.SuperElips(1f, 1f, 12);
        var renk = Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var normal = Resources.Load<Texture2D>("Agiz/mukoza_normal");

        var duvar = Sekil.DokuluMalzeme(renk, normal, null, 0.55f, 1f, 0.6f);
        duvar.SetColor("_BaseColor", new Color(0.85f, 0.42f, 0.34f));        // böbrek dokusu: kahverengimsi kırmızı
        var tup = Sekil.Tup(BitisZ + 70f, 48, 480, (aci, z) =>
            KanalYaricapi(z - 20f) * (1f + 0.04f * Mathf.Cos(aci * 6f + z * 0.3f)), 5f, 85f);
        Sekil.MeshParca(tup, "Kanal", kok, new Vector3(0f, 0f, -20f), Vector3.one, duvar)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        OrtamiAyarla(0f);

        // Glomerulus: birbirine dolanmış kılcal damar halkaları
        var kilcal = Sekil.DokuluMalzeme(renk, normal, null, 0.75f, 3f, 0.8f);
        kilcal.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f));
        var halka = Sekil.Halka(1f, 0.16f, 40, 12);
        for (int i = 0; i < 16; i++)
        {
            float aci = Random.value * 2f * Mathf.PI, uzak = Random.Range(4.5f, 8f);
            Sekil.MeshParca(halka, "KilcalDamar", kok,
                new Vector3(Mathf.Cos(aci) * uzak, Mathf.Sin(aci) * uzak, Random.Range(66f, 95f)),
                Vector3.one * Random.Range(3f, 5f), kilcal, Random.rotation);
        }

        // Süzgeç: gözenekli zar
        var zar = Sekil.Malzeme(new Color(1f, 0.85f, 0.7f, 0.35f), 0.9f);
        Sekil.MeshParca(kure, "SuzgecZari", kok, new Vector3(0f, 0f, SuzgecZ), new Vector3(5f, 5f, 0.12f), zar);
        Sekil.MeshParca(Sekil.Halka(5f, 0.9f, 48, 14), "SuzgecHalkasi", kok, new Vector3(0f, 0f, SuzgecZ), Vector3.one, kilcal);

        // Kandaki alyuvarlar (süzgeçten geçemezler)
        var alyuvar = Sekil.Alyuvar();
        var kan = Sekil.Malzeme(new Color(0.85f, 0.08f, 0.08f), 0.7f);
        for (int i = 0; i < HucreAdet; i++)
        {
            float z = Random.Range(10f, 94f);
            Vector2 yer = Random.insideUnitCircle * (KanalYaricapi(z) - 1.4f);
            hucreler[i] = Sekil.MeshParca(alyuvar, "Alyuvar", kok, new Vector3(yer.x, yer.y, z),
                Vector3.one * Random.Range(0.75f, 0.95f), kan, Random.rotation).transform;
            hucreDonusu[i] = Random.insideUnitSphere * 40f;
        }

        MaddeleriKur();
        AkisAraciniKur(new Vector3(0f, -0.8f, 6f), KanSisi);
        arac.kopukAcik = false;
        hiz = 11f;
    }

    void MaddeleriKur()
    {
        var su = Sekil.Malzeme(new Color(0.35f, 0.7f, 1f, 0.7f), 0.95f);
        Sekil.Isilda(su, new Color(0.1f, 0.3f, 0.6f));
        var glikoz = Sekil.Malzeme(new Color(1f, 0.92f, 0.55f), 0.6f);
        Sekil.Isilda(glikoz, new Color(0.5f, 0.4f, 0.1f));
        var tuz = Sekil.Malzeme(Color.white, 0.8f);
        Sekil.Isilda(tuz, new Color(0.35f, 0.35f, 0.35f));
        var ure = Sekil.Malzeme(new Color(0.42f, 0.27f, 0.1f), 0.35f);
        var zehir = Sekil.Malzeme(new Color(0.3f, 0.08f, 0.4f), 0.5f);
        Sekil.Isilda(zehir, new Color(0.12f, 0.02f, 0.2f));

        var kutu = Sekil.SuperElips(0.3f, 0.3f, 10);        // köşeleri yuvarlak küp (tuz kristali)
        var altigen = Sekil.SuperElips(0.6f, 0.35f, 10);    // yassı, köşeli tane (glikoz)
        var diken = Sekil.SuperElips(2.4f, 2.4f, 12);       // içe çökük, sivri uçlu yıldız (atık)

        for (float z = KanalBasi; z < BitisZ - 20f; z += Random.Range(4.5f, 7f))
        {
            var tur = (Tur)Random.Range(0, 5);
            // Yararlı maddeler biraz daha sık çıksın
            if ((tur == Tur.Ure || tur == Tur.Zehir) && Random.value < 0.25f) tur = Tur.Su;
            Vector2 yer = Random.insideUnitCircle * 2.9f;
            var konumu = new Vector3(yer.x, yer.y, z);

            GameObject g;
            switch (tur)
            {
                case Tur.Su: g = Sekil.MeshParca(kure, "Su", kok, konumu, Vector3.one * 0.6f, su); break;
                case Tur.Glikoz: g = Sekil.MeshParca(altigen, "Glikoz", kok, konumu, new Vector3(0.6f, 0.3f, 0.6f), glikoz, Random.rotation); break;
                case Tur.Tuz: g = Sekil.MeshParca(kutu, "Tuz", kok, konumu, Vector3.one * 0.42f, tuz, Random.rotation); break;
                case Tur.Ure: g = Sekil.MeshParca(diken, "Ure", kok, konumu, Vector3.one * 0.95f, ure, Random.rotation); break;
                default: g = Sekil.MeshParca(diken, "Zehir", kok, konumu, Vector3.one * 0.85f, zehir, Random.rotation); break;
            }
            bool yararli = tur == Tur.Su || tur == Tur.Glikoz || tur == Tur.Tuz;
            if (yararli) yararliToplam++;
            maddeler.Add(new Madde { govde = g.transform, yararli = yararli });
        }
    }

    // gecis: 0 = kan (kırmızı), 1 = süzülmüş sıvı (sarımsı)
    void OrtamiAyarla(float gecis)
    {
        Color sis = Color.Lerp(KanSisi, IdrarSisi, gecis);
        RenderSettings.fogColor = sis;
        RenderSettings.fogDensity = Mathf.Lerp(0.022f, 0.016f, gecis);
        RenderSettings.ambientLight = Color.Lerp(new Color(0.42f, 0.14f, 0.14f), new Color(0.5f, 0.42f, 0.25f), gecis);
        if (kameraGozu != null) kameraGozu.backgroundColor = sis;
    }

    protected override void ArayuzHazir()
    {
        arayuz.Ipucu("WASD: Süzül    ESC: Duraklat");
        arayuz.Gosterge("Böbrek atardamarı");
    }

    // ------------------------------------------------------------------ Oyun

    protected override void OyunBasladi()
    {
        arayuz.Telsiz("Kalp seni böbrek atardamarına pompaladı. Böbrekler kanı süzüp temizleyen, fasulye biçimli iki organdır.");
    }

    protected override void OyunGuncelle()
    {
        float dt = Time.deltaTime;
        Suzul(dt, suzuldu || konum.z < SuzgecZ - 14f);
        hiz = Mathf.MoveTowards(hiz, 11f, 6f * dt);
        konum.z += hiz * dt;
        AraciYerlestir();

        foreach (Transform h in hucreler)
        {
            if (h.position.z < 96f) h.position += Vector3.forward * (5f * dt);
            h.Rotate(hucreDonusu[0] * dt);
        }

        if (!yumakAnlatildi && konum.z > 52f)
        {
            yumakAnlatildi = true;
            arayuz.Gosterge("Glomerulus");
            arayuz.Telsiz("Bu kılcal damar yumağına glomerulus denir: böbreğin süzgeci. Kan basıncı sıvıyı incecik deliklerden iter!", true);
        }

        // Süzgeçten geçiş
        if (!suzuldu && konum.z >= SuzgecZ)
        {
            suzuldu = true;
            suzulmeEfekti = 1.6f;
            hiz = 3f;
            kamera.sarsinti = 0.3f;
            Ses.Cal(Ses.Tur.Yutkunma, 0.9f, 1.3f);
            arayuz.Gosterge($"Geri emilim: 0 / {yararliToplam}");
            arayuz.Telsiz("Süzüldün! Alyuvarlar ve proteinler geçemeyecek kadar büyük; onlar kanda kaldı.", true);
        }
        if (suzuldu)
        {
            suzulmeEfekti -= dt;
            OrtamiAyarla(Mathf.Clamp01(1f - suzulmeEfekti / 1.6f));
        }
        kameraGozu.fieldOfView = Mathf.Lerp(kameraGozu.fieldOfView, suzulmeEfekti > 0.8f ? 40f : 60f, 5f * dt);

        if (suzuldu && !kanalAnlatildi && konum.z > KanalBasi - 16f)
        {
            kanalAnlatildi = true;
            arayuz.Telsiz("Şimdi nefron kanalındasın. Süzülen sıvıda hâlâ yararlı maddeler var: mavi su damlaları, sarı glikoz, beyaz tuz. Onlara dokun, kana geri dönsünler!", true);
            arayuz.Telsiz("Kahverengi ve mor dikenli parçalar atık: üre ve zehirler. Onlara dokunma, idrarla atılsınlar.");
        }
        if (!ortaAnlatildi && konum.z > 270f && arayuz.TelsizBos)
        {
            ortaAnlatildi = true;
            arayuz.Telsiz("Böbrekler günde yaklaşık 180 litre sıvı süzer ama neredeyse hepsini geri emer. Sen de şu an tam bunu yapıyorsun!");
        }

        MaddeleriGuncelle(dt);

        if (konum.z >= BitisZ) BolumuBitir("Nefronun sonuna geldin! Yararlı maddeler kana döndü, atıklar idrar oldu.");
    }

    void MaddeleriGuncelle(float dt)
    {
        Vector3 govde = GovdeMerkezi;
        foreach (Madde m in maddeler)
        {
            if (m.govde == null) continue;
            if (m.alindi)
            {
                // Geri emilim: madde kanal duvarına (kılcal damarlara) doğru süzülüp kaybolur
                m.govde.position += m.kacis * (9f * dt);
                m.govde.localScale *= 1f - 2.5f * dt;
                if (m.govde.localScale.x < 0.05f) Destroy(m.govde.gameObject);
                continue;
            }

            m.govde.Rotate(30f * dt, 45f * dt, 0f);
            if ((m.govde.position - govde).sqrMagnitude > 1.5f * 1.5f) continue;

            m.alindi = true;
            Vector3 disari = new Vector3(m.govde.position.x, m.govde.position.y, 0f);
            m.kacis = (disari.sqrMagnitude > 0.01f ? disari.normalized : Vector3.up) + Vector3.forward * 0.6f;
            if (m.yararli)
            {
                geriKazanilan++;
                arayuz.Gosterge($"Geri emilim: {geriKazanilan} / {yararliToplam}", true);
                Baloncuk.Sac(m.govde.position, new Color(0.6f, 0.9f, 1f), 5, 0.25f);
                Ses.Cal(Ses.Tur.Erime, 0.6f, 1.3f + 0.02f * (geriKazanilan % 8));
                if (!emilimAnlatildi)
                {
                    emilimAnlatildi = true;
                    arayuz.Telsiz("Güzel! Buna geri emilim denir: yararlı madde kanalın duvarından geçip kana döndü.", true);
                }
            }
            else
            {
                atikHatasi++;
                kamera.sarsinti = 0.18f;
                Ses.Cal(Ses.Tur.Carpma, 0.8f);
                if (!atikAnlatildi)
                {
                    atikAnlatildi = true;
                    arayuz.Telsiz("O bir atıktı! Atıklara dokunursan kana geri karışırlar. Dikenli parçalardan uzak dur.", true);
                }
            }
        }
    }

    // Otomatik test için: ilerideki en yakın yararlı maddenin enine konumu (yoksa kanalın ortası).
    public Vector2 SiradakiYararli()
    {
        foreach (Madde m in maddeler)
        {
            if (m.govde == null || m.alindi || !m.yararli) continue;
            float ileri = m.govde.position.z - konum.z;
            if (ileri > 0.5f && ileri < 16f) return new Vector2(m.govde.position.x, m.govde.position.y);
        }
        return new Vector2(0f, 0f);
    }

    public Vector2 AracYanal => new Vector2(konum.x, konum.y + 0.8f);

    // ------------------------------------------------------------------ Bölüm sonu

    protected override IEnumerator CikisSahnesi()
    {
        arayuz.Telsiz("Geriye kalan atık sıvı artık idrar. İdrar borusundan mesaneye iniyoruz!", true);
        yield return new WaitForSeconds(1.2f);

        Ses.Cal(Ses.Tur.Dalga, 0.9f, 1.3f);
        for (float t = 0f; t < 2.6f; t += Time.deltaTime)
        {
            hiz = 24f;
            konum.z += hiz * Time.deltaTime;
            AraciYerlestir();
            kamera.sarsinti = 0.12f;
            yield return null;
        }
    }

    // Puan: yararlı maddelerin çoğunu geri kazanıp atıklara pek dokunmayana 3 yıldız
    protected override int Yildiz()
    {
        float oran = yararliToplam > 0 ? (float)geriKazanilan / yararliToplam : 0f;
        return oran >= 0.75f && atikHatasi <= 2 ? 3 : oran >= 0.45f && atikHatasi <= 5 ? 2 : 1;
    }

    protected override string SonucSatiri() =>
        $"{SureYazisi}     Geri kazanılan: {geriKazanilan} / {yararliToplam}     Dokunulan atık: {atikHatasi}";
}
