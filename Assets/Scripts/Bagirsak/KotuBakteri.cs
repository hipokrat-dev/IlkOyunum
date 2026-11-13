using System.Collections;
using UnityEngine;

// Dost koloniye saldıran zararlı bakteri. Koloninin çevresinde döner; köpük değince erir.
public class KotuBakteri : MonoBehaviour, IKopukHedefi
{
    IyiKoloni koloni;
    float yaricap, aci, hiz, faz;
    bool eriyor;

    public static KotuBakteri Olustur(Transform ebeveyn, IyiKoloni koloni, Mesh mesh, Material malzeme, float yaricap, float aci)
    {
        var g = Sekil.MeshParca(mesh, "KotuBakteri", ebeveyn, koloni.transform.position, Vector3.one * 0.42f, malzeme);
        g.AddComponent<SphereCollider>().radius = 1.9f;

        var isik = new GameObject("Isilti").AddComponent<Light>();
        isik.transform.SetParent(g.transform, false);
        isik.transform.localPosition = Vector3.up * 1.5f;
        isik.type = LightType.Point;
        isik.color = new Color(1f, 0.2f, 0.25f);
        isik.intensity = 1.6f;
        isik.range = 3.5f;
        isik.shadows = LightShadows.None;

        var b = g.AddComponent<KotuBakteri>();
        b.koloni = koloni;
        b.yaricap = yaricap;
        b.aci = aci;
        b.hiz = Random.Range(0.7f, 1.1f) * (Random.value < 0.5f ? -1f : 1f);
        b.faz = Random.value * 10f;
        b.Update();
        return b;
    }

    void Update()
    {
        if (eriyor || koloni == null) return;
        // Kuşatma başlayınca hızlanır ve koloniye sokulur.
        bool saldiriyor = koloni.Kusatiliyor;
        aci += hiz * (saldiriyor ? 1.3f : 0.4f) * Time.deltaTime;
        float r = yaricap * (saldiriyor ? 0.85f + 0.15f * Mathf.Sin(Time.time * 3f + faz) : 1f);
        transform.position = koloni.transform.position +
                             new Vector3(Mathf.Cos(aci) * r, 0.45f + 0.12f * Mathf.Sin(Time.time * 2f + faz), Mathf.Sin(aci) * r);
        transform.Rotate(0f, 60f * Time.deltaTime, 0f);
    }

    public void KopukDegdi()
    {
        if (eriyor) return;
        eriyor = true;
        GetComponent<Collider>().enabled = false;
        if (koloni != null) koloni.SaldirganEridi(this);
        StartCoroutine(Eri());
    }

    // Koloni kaybedilince saldırganlar dağılır.
    public void Dagil()
    {
        if (eriyor) return;
        eriyor = true;
        GetComponent<Collider>().enabled = false;
        StartCoroutine(Eri(false));
    }

    IEnumerator Eri(bool sesli = true)
    {
        if (sesli)
        {
            Baloncuk.Sac(transform.position, Color.white, 10, 0.35f);
            Ses.Cal(Ses.Tur.Erime, 0.7f, Random.Range(0.95f, 1.15f));
        }
        Vector3 bas = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
        {
            transform.localScale = bas * (1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
