using UnityEngine;

// Akyuvar: vücudun savunma hücresi. Kapsülü mikrop sanıp üstüne gelir.
// Temizleme köpüğü değince kapsülü tanır, sakinleşir ve uzaklaşır (zarar görmez).
public class Akyuvar : MonoBehaviour, IKopukHedefi
{
    public bool Sakin { get; private set; }

    Transform hedef;
    Material malzeme;
    float akisHizi, faz;

    public static Akyuvar Olustur(Transform ebeveyn, Mesh mesh, Vector3 konum, Transform hedef, float akisHizi)
    {
        var malzeme = Sekil.Malzeme(new Color(0.72f, 0.74f, 0.7f), 0.4f);
        Sekil.Isilda(malzeme, new Color(0.04f, 0.04f, 0.035f));
        var g = Sekil.MeshParca(mesh, "Akyuvar", ebeveyn, konum, Vector3.one * 1.7f, malzeme, Random.rotation);
        g.AddComponent<SphereCollider>().radius = 1f;

        var a = g.AddComponent<Akyuvar>();
        a.hedef = hedef;
        a.malzeme = malzeme;
        a.akisHizi = akisHizi;
        a.faz = Random.value * 10f;
        return a;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 p = transform.position;
        p.z += akisHizi * dt;

        // Enine kesitte: sakin değilse kapsüle doğru süzülür, sakinse uzaklaşır
        Vector2 fark = new Vector2(hedef.position.x - p.x, hedef.position.y + 0.8f - p.y);
        if (fark.sqrMagnitude > 0.01f)
        {
            Vector2 adim = fark.normalized * ((Sakin ? -3f : 2.4f) * dt);
            p.x += adim.x;
            p.y += adim.y;
        }
        transform.position = p;
        transform.Rotate(8f * dt, 14f * dt, 0f);
        float nabiz = 1.7f * (1f + 0.05f * Mathf.Sin(Time.time * 3f + faz));
        transform.localScale = Vector3.one * nabiz;
    }

    public void KopukDegdi() => Sakinles();

    public void Sakinles()
    {
        if (Sakin) return;
        Sakin = true;
        malzeme.SetColor("_BaseColor", new Color(0.45f, 0.62f, 0.8f));
        malzeme.SetColor("_EmissionColor", new Color(0.03f, 0.1f, 0.18f));
        Ses.Cal(Ses.Tur.Erime, 0.5f, 1.4f);
    }
}
