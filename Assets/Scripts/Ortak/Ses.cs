using System;
using System.Collections.Generic;
using UnityEngine;

// Oyunun tüm sesleri. Ses dosyası kullanılmaz; her ses başlangıçta kodla üretilir.
public class Ses : MonoBehaviour
{
    public enum Tur { Kopuk, Erime, Telsiz, Dalga, Carpma, Basari, Yutkunma, Tik }

    const int Hz = 44100;
    static Ses ornek;

    readonly Dictionary<Tur, AudioClip> klipler = new Dictionary<Tur, AudioClip>();
    AudioSource tekSes, motor, ortam, konusma;
    float motorHedef;

    static Ses Ornek
    {
        get
        {
            if (ornek == null) ornek = new GameObject("Ses").AddComponent<Ses>();
            return ornek;
        }
    }

    // ---------- Dışarıya açık işlevler ----------

    public static void Cal(Tur tur, float siddet = 1f, float perde = 1f)
    {
        var s = Ornek;
        s.tekSes.pitch = perde;
        s.tekSes.PlayOneShot(s.klipler[tur], siddet);
    }

    // Motor uğultusu: 0 (rölanti) .. 1 (tam hız)
    public static void Motor(float guc) => Ornek.motorHedef = Mathf.Clamp01(guc);

    // Vücudun içinde olma hissi: uzaktan gelen kalp atışı
    public static void OrtamBaslat()
    {
        var s = Ornek;
        if (!s.ortam.isPlaying) s.ortam.Play();
    }

    // Profesörün seslendirmesi (aynı anda tek konuşma)
    public static void Konus(AudioClip klip)
    {
        var s = Ornek;
        s.konusma.Stop();
        s.konusma.clip = klip;
        s.konusma.Play();
    }

    public static void KonusmayiKes()
    {
        if (ornek != null) ornek.konusma.Stop();
    }

    // ---------- Kurulum ----------

    void Awake()
    {
        tekSes = Kaynak(false, 1f);
        motor = Kaynak(true, 0f);
        ortam = Kaynak(true, 0.5f);
        konusma = Kaynak(false, 1f);

        var rastgele = new System.Random(7);
        Func<float> gurultu = () => (float)rastgele.NextDouble() * 2f - 1f;

        // Köpük: kısa, tiz "pfft"
        float alcak = 0f;
        klipler[Tur.Kopuk] = Uret(0.16f, t =>
        {
            float ham = gurultu();
            alcak += (ham - alcak) * 0.35f;
            return (ham - alcak) * Mathf.Exp(-t * 28f) * 0.5f;
        });

        // Erime: peş peşe üç kabarcık
        klipler[Tur.Erime] = Uret(0.5f, t =>
        {
            float toplam = 0f;
            for (int i = 0; i < 3; i++)
            {
                float y = t - i * 0.11f;
                if (y < 0f || y > 0.16f) continue;
                float frekans = Mathf.Lerp(340f + i * 120f, 900f + i * 160f, y / 0.16f);
                toplam += Mathf.Sin(2f * Mathf.PI * frekans * y) * Mathf.Sin(Mathf.PI * y / 0.16f);
            }
            return toplam * 0.45f;
        });

        // Telsiz: cızırtılı çift bip
        klipler[Tur.Telsiz] = Uret(0.22f, t =>
        {
            float bip = t < 0.08f ? Mathf.Sin(2f * Mathf.PI * 880f * t) : t > 0.11f && t < 0.19f ? Mathf.Sin(2f * Mathf.PI * 1175f * t) : 0f;
            return bip * 0.22f + gurultu() * 0.04f * Mathf.Exp(-t * 10f);
        });

        // Tükürük dalgası: kabarıp sönen ıslak uğultu
        float dalgaAlcak = 0f;
        klipler[Tur.Dalga] = Uret(2.2f, t =>
        {
            dalgaAlcak += (gurultu() - dalgaAlcak) * 0.06f;
            float zarf = Mathf.Sin(Mathf.PI * t / 2.2f);
            return dalgaAlcak * zarf * zarf * 2.2f;
        });

        // Dalgaya çarpma: boğuk darbe
        klipler[Tur.Carpma] = Uret(0.4f, t =>
            (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 45f, t / 0.4f) * t) * 0.8f + gurultu() * 0.2f) * Mathf.Exp(-t * 9f));

        // Başarı: yükselen dört nota
        float[] notalar = { 523.25f, 659.25f, 783.99f, 1046.5f };
        klipler[Tur.Basari] = Uret(1.3f, t =>
        {
            float toplam = 0f;
            for (int i = 0; i < notalar.Length; i++)
            {
                float y = t - i * 0.14f;
                if (y < 0f) continue;
                toplam += (Mathf.Sin(2f * Mathf.PI * notalar[i] * y) + 0.3f * Mathf.Sin(4f * Mathf.PI * notalar[i] * y)) * Mathf.Exp(-y * 4f);
            }
            return toplam * 0.2f;
        });

        // Yutkunma: derin, alçalan gurultu
        float yutAlcak = 0f;
        klipler[Tur.Yutkunma] = Uret(1.6f, t =>
        {
            yutAlcak += (gurultu() - yutAlcak) * 0.03f;
            float zarf = Mathf.Sin(Mathf.PI * t / 1.6f);
            return (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 50f, t / 1.6f) * t) * 0.6f + yutAlcak * 2f) * zarf;
        });

        // Arayüz tıkı (kart, başlat, duraklat)
        klipler[Tur.Tik] = Uret(0.07f, t => Mathf.Sin(2f * Mathf.PI * 660f * t) * Mathf.Exp(-t * 60f) * 0.4f);

        // Motor: kesintisiz dönen alçak uğultu (tam 1 sn = tam periyotlar)
        motor.clip = Uret(1f, t =>
            (Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 124f * t) * 0.25f +
             Mathf.Sin(2f * Mathf.PI * 187f * t) * 0.12f) * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 9f * t)));
        motor.Play();

        // Ortam: kalp atışı ("lub-dub"), 0,9 sn'de bir
        ortam.clip = Uret(0.9f, t =>
        {
            float lub = Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 16f);
            float y = t - 0.26f;
            float dub = y > 0f ? Mathf.Sin(2f * Mathf.PI * 62f * y) * Mathf.Exp(-y * 20f) * 0.7f : 0f;
            return (lub + dub) * 0.9f * Mathf.Clamp01((0.9f - t) * 20f);
        });
    }

    void Update()
    {
        motor.volume = Mathf.MoveTowards(motor.volume, 0.05f + 0.16f * motorHedef, Time.unscaledDeltaTime * 0.6f);
        motor.pitch = Mathf.MoveTowards(motor.pitch, 0.8f + 0.7f * motorHedef, Time.unscaledDeltaTime * 1.5f);
    }

    AudioSource Kaynak(bool dongu, float siddet)
    {
        var k = gameObject.AddComponent<AudioSource>();
        k.playOnAwake = false;
        k.loop = dongu;
        k.volume = siddet;
        k.spatialBlend = 0f;
        return k;
    }

    static AudioClip Uret(float sure, Func<float, float> dalga)
    {
        int n = Mathf.CeilToInt(sure * Hz);
        var veri = new float[n];
        for (int i = 0; i < n; i++) veri[i] = Mathf.Clamp(dalga((float)i / Hz), -1f, 1f);
        var klip = AudioClip.Create("Ses", n, 1, Hz, false);
        klip.SetData(veri, 0);
        return klip;
    }
}
