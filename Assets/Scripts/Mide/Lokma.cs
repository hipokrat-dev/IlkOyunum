using UnityEngine;

// Yemek borusunda duvara yapışmış lokma parçası. Araç çarparsa yavaşlar; köpük değerse dağılır.
public class Lokma : MonoBehaviour, IKopukHedefi
{
    public float z, aci;                 // tüp boyunca konum ve duvardaki açı (derece)
    public bool Dagildi { get; private set; }

    Color renk;

    public static Lokma Olustur(Transform ebeveyn, Mesh mesh, Material malzeme, Color renk, Vector3 konum,
        Vector3 olcek, float z, float aci)
    {
        var g = Sekil.MeshParca(mesh, "Lokma", ebeveyn, konum, olcek, malzeme, Random.rotation);
        g.AddComponent<SphereCollider>().radius = 1f;
        var lokma = g.AddComponent<Lokma>();
        lokma.z = z;
        lokma.aci = aci;
        lokma.renk = renk;
        return lokma;
    }

    public void KopukDegdi() => Dagit();

    public void Dagit()
    {
        if (Dagildi) return;
        Dagildi = true;
        Baloncuk.Sac(transform.position, renk, 10, 0.6f);
        Ses.Cal(Ses.Tur.Erime, 0.6f, Random.Range(0.7f, 0.85f));
        Destroy(gameObject);
    }
}
