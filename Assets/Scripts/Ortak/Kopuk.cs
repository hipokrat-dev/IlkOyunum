using System;
using UnityEngine;

// Temizleme köpüğünün etkilediği her şey (mikrop, lokma...) bunu uygular.
public interface IKopukHedefi
{
    void KopukDegdi();
}

// Araçtan atılan temizleme köpüğü. Bir hedefe değerse onu etkiler, başka bir şeye değerse patlar.
public class Kopuk : MonoBehaviour
{
    static readonly Collider[] tampon = new Collider[8];

    // Zemindeki hedefler (ör. asit gölleri) için: köpük bu noktanın üstündeyken bir şeye değdi mi?
    public static Func<Vector3, bool> zeminKontrolu;

    Vector3 hiz;
    float omur = 0.9f;
    Rigidbody sahip;

    public static void At(Vector3 konum, Vector3 hiz, Rigidbody sahip, Material malzeme)
    {
        var g = Sekil.Parca(PrimitiveType.Sphere, "Kopuk", null, konum, Vector3.one * 0.55f, malzeme);
        var k = g.AddComponent<Kopuk>();
        k.hiz = hiz;
        k.sahip = sahip;
    }

    void Update()
    {
        transform.position += hiz * Time.deltaTime;
        omur -= Time.deltaTime;

        int n = Physics.OverlapSphereNonAlloc(transform.position, 0.3f, tampon, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = tampon[i];
            if (c.attachedRigidbody == sahip) continue;

            c.GetComponentInParent<IKopukHedefi>()?.KopukDegdi();
            Patla();
            return;
        }

        if (zeminKontrolu != null && zeminKontrolu(transform.position))
        {
            Patla();
            return;
        }

        if (omur <= 0f) Patla();
    }

    void Patla()
    {
        Baloncuk.Sac(transform.position, Color.white, 5, 0.25f);
        Destroy(gameObject);
    }
}
