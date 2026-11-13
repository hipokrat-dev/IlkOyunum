using System;
using System.Collections;
using UnityEngine;

// Çürük bakterisi kolonisi (Streptococcus mutans benzeri): biyofilm üzerinde zincir halinde koklar.
// Temizleme köpüğü değince eriyip kaybolur.
public class Mikrop : MonoBehaviour
{
    public Action<Mikrop> temizlendi;
    public GameObject leke;       // Dişteki çürük/plak lekesi; koloniyle birlikte kaybolur

    public bool Eriyor { get; private set; }

    Vector3 merkez, ilkOlcek;
    float faz;

    public static Mikrop Olustur(Vector3 konum, float boyut, Mesh koloni, Material malzeme)
    {
        var g = Sekil.MeshParca(koloni, "Mikrop", null, konum, Vector3.one * boyut, malzeme,
            Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f));
        g.AddComponent<SphereCollider>().radius = 1.4f;   // koloni ölçeğiyle birlikte küçülür
        return g.AddComponent<Mikrop>();
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
    }

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
        Vector3 bas = transform.localScale;
        Vector3 lekeBas = leke != null ? leke.transform.localScale : Vector3.zero;

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            transform.localScale = bas * (1f - t);
            if (leke != null) leke.transform.localScale = lekeBas * (1f - t);
            yield return null;
        }

        if (leke != null) Destroy(leke);
        Destroy(gameObject);
    }
}
