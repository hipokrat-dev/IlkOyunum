using System;
using UnityEngine;

// Ağzın önünden boğaza doğru akan tükürük dalgası. Ağzın yalnızca bir kısmını kaplar;
// oyuncu boş tarafa kaçmazsa dalga aracı boğaza doğru sürükler.
public class TukurukDalgasi : MonoBehaviour
{
    public Action carpti;

    AracKontrol arac;
    float xMerkez, yariGenislik, hiz, zBaslangic, zBitis;
    bool bildirildi;

    public static TukurukDalgasi Olustur(AracKontrol arac, Material malzeme, float xMerkez, float yariGenislik,
        float hiz, float zBaslangic, float zBitis)
    {
        var g = Sekil.Parca(PrimitiveType.Capsule, "TukurukDalgasi", null, new Vector3(xMerkez, 0.9f, zBaslangic),
            Vector3.one * 0.1f, malzeme, false, Quaternion.Euler(0f, 0f, 90f));
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var d = g.AddComponent<TukurukDalgasi>();
        d.arac = arac;
        d.xMerkez = xMerkez;
        d.yariGenislik = yariGenislik;
        d.hiz = hiz;
        d.zBaslangic = zBaslangic;
        d.zBitis = zBitis;
        return d;
    }

    void Update()
    {
        transform.position += Vector3.back * hiz * Time.deltaTime;
        float z = transform.position.z;

        // Ağzın önü dar olduğu için dalga ilerledikçe genişler.
        float buyume = Mathf.Clamp((zBaslangic - z) / 7f, 0.15f, 1f);
        float yari = yariGenislik * buyume;
        float kalinlik = 2.4f * (0.85f + 0.15f * Mathf.Sin(Time.time * 6f));
        transform.localScale = new Vector3(kalinlik, yari, kalinlik);

        if (arac != null)
        {
            Vector3 a = arac.transform.position;
            if (Mathf.Abs(a.z - z) < 1.7f && Mathf.Abs(a.x - xMerkez) < yari + 0.5f)
            {
                arac.Itki(Vector3.back * hiz * 1.5f);
                if (!bildirildi)
                {
                    bildirildi = true;
                    carpti?.Invoke();
                }
            }
        }

        if (z < zBitis)
        {
            Baloncuk.Sac(transform.position, new Color(0.9f, 0.95f, 1f), 8, 0.5f);
            Destroy(gameObject);
        }
    }
}
