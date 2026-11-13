using UnityEngine;

// Küçük, kısa ömürlü köpük baloncuğu efekti.
public class Baloncuk : MonoBehaviour
{
    Vector3 hiz, ilkOlcek;
    float omur, gecen;

    public static void Sac(Vector3 konum, Color renk, int adet, float boyut = 0.3f)
    {
        var malzeme = Sekil.Malzeme(renk, 0.8f);
        for (int i = 0; i < adet; i++)
        {
            var g = Sekil.Parca(PrimitiveType.Sphere, "Baloncuk", null,
                konum + Random.insideUnitSphere * 0.4f,
                Vector3.one * boyut * Random.Range(0.5f, 1.2f), malzeme);
            var b = g.AddComponent<Baloncuk>();
            b.hiz = Random.insideUnitSphere * 2f + Vector3.up * 2f;
            b.omur = Random.Range(0.4f, 0.8f);
            b.ilkOlcek = g.transform.localScale;
        }
    }

    void Update()
    {
        gecen += Time.deltaTime;
        transform.position += hiz * Time.deltaTime;
        transform.localScale = ilkOlcek * Mathf.Clamp01(1f - gecen / omur);
        if (gecen >= omur) Destroy(gameObject);
    }
}
