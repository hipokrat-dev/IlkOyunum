using System;
using System.Collections.Generic;
using UnityEngine;

// Faydalı (dost) bakteri kolonisi: mavi ışıldayan çubuk bakteriler.
// Araç yaklaşınca zararlı bakterilerin kuşatması başlar ve koloninin sağlığı azalır.
// Tüm saldırganlar temizlenirse koloni kurtulur; sağlığı biterse kaybedilir.
public class IyiKoloni : MonoBehaviour, IKopukHedefi
{
    public enum Hal { Bekliyor, Kusatma, Kurtuldu, Kayboldu }

    public Action<IyiKoloni> kurtuldu, kayboldu, dostAtesi;
    public Hal Durum { get; private set; } = Hal.Bekliyor;
    public bool Kusatiliyor => Durum == Hal.Kusatma;
    public float Saglik { get; private set; } = 100f;

    readonly List<KotuBakteri> saldirganlar = new List<KotuBakteri>();
    Light isik;
    Vector3 ilkOlcek;

    public static IyiKoloni Olustur(Transform ebeveyn, Vector3 konum, Material malzeme)
    {
        var g = new GameObject("DostKoloni");
        g.transform.SetParent(ebeveyn, false);
        g.transform.position = konum;

        // Çubuk bakteriler (laktobasil benzeri): kimi yatık, kimi dik
        for (int i = 0; i < 11; i++)
        {
            Vector2 yer = UnityEngine.Random.insideUnitCircle * 1.3f;
            var donus = Quaternion.Euler(UnityEngine.Random.Range(20f, 90f), UnityEngine.Random.value * 360f, 0f);
            Sekil.Parca(PrimitiveType.Capsule, "Cubuk", g.transform, new Vector3(yer.x, UnityEngine.Random.Range(0.3f, 0.7f), yer.y),
                new Vector3(0.3f, UnityEngine.Random.Range(0.45f, 0.7f), 0.3f), malzeme, false, donus);
        }
        g.AddComponent<SphereCollider>().radius = 1.5f;

        var isik = new GameObject("Isilti").AddComponent<Light>();
        isik.transform.SetParent(g.transform, false);
        isik.transform.localPosition = Vector3.up * 1.6f;
        isik.type = LightType.Point;
        isik.color = new Color(0.3f, 0.8f, 1f);
        isik.range = 8f;
        isik.shadows = LightShadows.None;

        var koloni = g.AddComponent<IyiKoloni>();
        koloni.isik = isik;
        koloni.ilkOlcek = Vector3.one;
        return koloni;
    }

    public void SaldirganEkle(KotuBakteri b) => saldirganlar.Add(b);

    public IReadOnlyList<KotuBakteri> Saldirganlar => saldirganlar;

    public void KusatmayiBaslat()
    {
        if (Durum == Hal.Bekliyor) Durum = Hal.Kusatma;
    }

    void Update()
    {
        float t = Time.time;
        switch (Durum)
        {
            case Hal.Kusatma:
                Saglik -= 1f * saldirganlar.Count * Time.deltaTime;
                if (Saglik <= 0f)
                {
                    Durum = Hal.Kayboldu;
                    foreach (var s in saldirganlar) if (s != null) s.Dagil();
                    saldirganlar.Clear();
                    Ses.Cal(Ses.Tur.Carpma, 0.7f, 0.7f);
                    kayboldu?.Invoke(this);
                }
                // Sağlık azaldıkça küçülür ve ışığı titrer
                transform.localScale = ilkOlcek * Mathf.Lerp(0.45f, 1f, Saglik / 100f);
                isik.intensity = (2f + 4f * Saglik / 100f) * (0.7f + 0.3f * Mathf.Sin(t * 12f));
                break;

            case Hal.Kurtuldu:
                transform.localScale = Vector3.Lerp(transform.localScale, ilkOlcek * 1.15f, 3f * Time.deltaTime);
                isik.intensity = 9f + 2f * Mathf.Sin(t * 2f);
                break;

            case Hal.Kayboldu:
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 2f * Time.deltaTime);
                isik.intensity = Mathf.MoveTowards(isik.intensity, 0f, 6f * Time.deltaTime);
                break;

            default:
                isik.intensity = 5f + 1.5f * Mathf.Sin(t * 2f);
                break;
        }
    }

    public void SaldirganEridi(KotuBakteri b)
    {
        saldirganlar.Remove(b);
        if (saldirganlar.Count > 0 || Durum == Hal.Kayboldu || Durum == Hal.Kurtuldu) return;
        Durum = Hal.Kurtuldu;
        Baloncuk.Sac(transform.position + Vector3.up, new Color(0.4f, 0.9f, 1f), 16, 0.4f);
        Ses.Cal(Ses.Tur.Basari, 0.6f, 1.25f);
        kurtuldu?.Invoke(this);
    }

    // Köpük dostlara da zarar verir: oyuncu nişan almalı.
    public void KopukDegdi()
    {
        if (Durum == Hal.Kayboldu || Durum == Hal.Kurtuldu) return;
        Saglik = Mathf.Max(1f, Saglik - 5f);
        dostAtesi?.Invoke(this);
    }
}
