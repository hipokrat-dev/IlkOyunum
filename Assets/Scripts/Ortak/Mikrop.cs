using System;
using System.Collections;
using UnityEngine;

// Çürük bakterisi kolonisi (Streptococcus mutans benzeri): biyofilm üzerinde zincir halinde koklar.
// Temizleme köpüğü değince eriyip kaybolur.
public class Mikrop : MonoBehaviour, IKopukHedefi
{
    public Action<Mikrop> temizlendi;
    public GameObject leke;       // Dişteki çürük/plak lekesi; koloniyle birlikte kaybolur

    public bool Eriyor { get; private set; }

    Light isaret;
    Vector3 merkez, ilkOlcek;
    float faz;

    public static Mikrop Olustur(Vector3 konum, float boyut, Mesh koloni, Material malzeme)
    {
        var g = Sekil.MeshParca(koloni, "Mikrop", null, konum, Vector3.one * boyut, malzeme,
            Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f));
        g.AddComponent<SphereCollider>().radius = 1.4f;   // koloni ölçeğiyle birlikte küçülür

        // Karanlıkta koloninin yerini belli eden, nabız gibi atan soluk ışık
        var isik = new GameObject("Isaret").AddComponent<Light>();
        isik.transform.SetParent(g.transform, false);
        isik.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        isik.type = LightType.Point;
        isik.color = new Color(0.75f, 1f, 0.35f);
        isik.range = 4.5f;
        isik.shadows = LightShadows.None;

        var mikrop = g.AddComponent<Mikrop>();
        mikrop.isaret = isik;
        return mikrop;
    }

    void Start()
    {
        merkez = transform.position;
        ilkOlcek = transform.localScale;
        faz = UnityEngine.Random.value * 10f;
    }

    void Update()
    {
        if (Eriyor) return;

        float z = Time.time * 1.6f + faz;
        transform.position = merkez + new Vector3(Mathf.Sin(z * 0.6f) * 0.05f, Mathf.Sin(z) * 0.04f, Mathf.Cos(z * 0.5f) * 0.05f);
        float nabiz = 1f + Mathf.Sin(z * 1.7f) * 0.05f;
        transform.localScale = new Vector3(ilkOlcek.x * nabiz, ilkOlcek.y / nabiz, ilkOlcek.z * nabiz);
        if (isaret != null) isaret.intensity = 1.6f + 0.9f * Mathf.Sin(z * 2.2f);
    }

    public void KopukDegdi() => Temizle();

    public void Temizle()
    {
        if (Eriyor) return;
        Eriyor = true;
        GetComponent<Collider>().enabled = false;
        temizlendi?.Invoke(this);
        StartCoroutine(Eri());
    }

    IEnumerator Eri()
    {
        Baloncuk.Sac(transform.position, Color.white, 12, 0.4f);
        Ses.Cal(Ses.Tur.Erime, 0.8f, UnityEngine.Random.Range(0.92f, 1.1f));
        Vector3 bas = transform.localScale;
        Vector3 lekeBas = leke != null ? leke.transform.localScale : Vector3.zero;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            transform.localScale = bas * (1f - t);
            if (leke != null) leke.transform.localScale = lekeBas * (1f - t);
            if (isaret != null) isaret.intensity = 4f * (1f - t);
            yield return null;
        }

        if (leke != null) Destroy(leke);
        Destroy(gameObject);
    }
}
