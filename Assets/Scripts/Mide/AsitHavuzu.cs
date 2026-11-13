using System.Collections.Generic;
using UnityEngine;

// Mide tabanındaki asit gölü. Üstündeki aracın kalkanını eritir.
// Temizleme köpüğü değince birkaç saniyeliğine nötrleşir (beyazlaşır) ve üstünden geçilebilir.
public class AsitHavuzu : MonoBehaviour
{
    public static readonly List<AsitHavuzu> Tumu = new List<AsitHavuzu>();
    public static float Tasma = 1f;      // mide kasılınca göller taşar (1 = normal)

    const float NotrSuresi = 6f;
    static readonly Color AsitRengi = new Color(0.72f, 0.88f, 0.12f, 0.88f);
    static readonly Color NotrRengi = new Color(0.93f, 0.96f, 0.96f, 0.8f);

    float yaricap, salinim, notr, faz, kabarcik;
    Vector3 merkez;
    Material malzeme;
    Light isik;

    public bool Aktif => notr <= 0f;
    float Genislik => yaricap * (Aktif ? Tasma : 1f);

    // salinim > 0 ise göl x ekseninde sağa sola süzülür.
    public static AsitHavuzu Olustur(Transform ebeveyn, Mesh kure, Vector3 merkez, float yaricap, float salinim)
    {
        var malzeme = Sekil.Malzeme(AsitRengi, 0.96f);
        var g = Sekil.MeshParca(kure, "AsitGolu", ebeveyn, merkez, Vector3.one, malzeme);
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var isik = new GameObject("Isilti").AddComponent<Light>();
        isik.transform.SetParent(g.transform, false);
        isik.transform.localPosition = Vector3.up * 1.2f;
        isik.type = LightType.Point;
        isik.range = yaricap + 4f;
        isik.shadows = LightShadows.None;

        var havuz = g.AddComponent<AsitHavuzu>();
        havuz.merkez = merkez;
        havuz.yaricap = yaricap;
        havuz.salinim = salinim;
        havuz.malzeme = malzeme;
        havuz.isik = isik;
        havuz.faz = Random.value * 10f;
        havuz.Update();
        return havuz;
    }

    void OnEnable() => Tumu.Add(this);
    void OnDisable() => Tumu.Remove(this);

    void Update()
    {
        notr -= Time.deltaTime;
        float t = Time.time + faz;
        transform.position = merkez + Vector3.right * (salinim * Mathf.Sin(t * 0.5f));
        float g = Genislik * (1f + 0.03f * Mathf.Sin(t * 2f));
        transform.localScale = new Vector3(g, 0.14f, g);

        // Nötrleşince beyazlaşır, süre dolarken yeniden asit rengine döner.
        float asitlik = Mathf.Clamp01(1f - notr / 0.8f);
        Color renk = Color.Lerp(NotrRengi, AsitRengi, asitlik);
        malzeme.SetColor("_BaseColor", renk);
        malzeme.SetColor("_EmissionColor", new Color(0.35f, 0.5f, 0.02f) * (asitlik * (1.2f + 0.3f * Mathf.Sin(t * 3f))));
        malzeme.EnableKeyword("_EMISSION");
        isik.color = Color.Lerp(Color.white, new Color(0.8f, 1f, 0.2f), asitlik);
        isik.intensity = 2f + 5f * asitlik;

        kabarcik -= Time.deltaTime;
        if (kabarcik <= 0f && Aktif)
        {
            kabarcik = Random.Range(0.6f, 1.8f);
            Vector2 yer = Random.insideUnitCircle * yaricap * 0.7f;
            Baloncuk.Sac(transform.position + new Vector3(yer.x, 0.2f, yer.y), new Color(0.85f, 1f, 0.3f), 2, 0.35f);
        }
    }

    public bool Ustunde(Vector3 nokta)
    {
        float dx = nokta.x - transform.position.x, dz = nokta.z - transform.position.z;
        float r = Genislik * 0.93f;
        return dx * dx + dz * dz < r * r;
    }

    public void Notrle()
    {
        if (!Aktif) return;
        notr = NotrSuresi;
        Baloncuk.Sac(transform.position + Vector3.up * 0.3f, Color.white, 14, 0.5f);
        Ses.Cal(Ses.Tur.Erime, 0.7f, 1.2f);
    }
}
