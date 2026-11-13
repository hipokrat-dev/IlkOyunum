using UnityEngine;

// İdrar kristali kümesi (büyürse böbrek taşına dönüşür). Köpük her değdiğinde küçülür; üç vuruşta erir.
public class Kristal : MonoBehaviour, IKopukHedefi
{
    public System.Action<Kristal> eridi;
    public bool Eridi { get; private set; }

    int can = 3;
    Vector3 ilkOlcek, hedefOlcek;

    public static Kristal Olustur(Transform ebeveyn, Mesh diken, Material malzeme, Vector3 konum, float boyut)
    {
        var g = new GameObject("Kristal");
        g.transform.SetParent(ebeveyn, false);
        g.transform.position = konum;
        g.transform.localScale = Vector3.one * boyut;
        for (int i = 0; i < 5; i++)
        {
            Vector3 yer = Random.insideUnitSphere * 0.55f;
            yer.y = Mathf.Abs(yer.y) * 0.8f;
            Sekil.MeshParca(diken, "Parca", g.transform, yer, Vector3.one * Random.Range(0.55f, 0.95f), malzeme, Quaternion.Euler(Random.Range(-30f,30f), Random.Range(0f,360f), Random.Range(-30f,30f)));
        }
        g.AddComponent<SphereCollider>().radius = 1.15f;
        var k = g.AddComponent<Kristal>();
        k.ilkOlcek = k.hedefOlcek = g.transform.localScale;
        return k;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, hedefOlcek, 8f * Time.deltaTime);
        if (Eridi && transform.localScale.x < 0.06f) Destroy(gameObject);
    }

    public void KopukDegdi()
    {
        if (Eridi) return;
        can--;
        Baloncuk.Sac(transform.position + Vector3.up * 0.5f, new Color(1f, 0.97f, 0.8f), 6, 0.3f);
        if (can > 0)
        {
            hedefOlcek = ilkOlcek * (0.4f + 0.2f * can);
            Ses.Cal(Ses.Tur.Tik, 0.7f, 1.4f);
            return;
        }
        Eridi = true;
        hedefOlcek = Vector3.zero;
        GetComponent<Collider>().enabled = false;
        Ses.Cal(Ses.Tur.Erime, 0.8f, 1.1f);
        eridi?.Invoke(this);
    }
}
