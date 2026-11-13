using UnityEngine;

// Aracın bir damar/kanal içinde akışla taşındığı bölümlerin ortak temeli.
// Araç +Z yönünde akar; oyuncu WASD ile kanalın enine kesitinde süzülür.
public abstract class AkisBolumu : BolumTemeli
{
    protected Vector3 konum;          // z: kanal boyunca, (x, y): enine kesit
    protected Vector2 yanalHiz;
    protected float hiz;

    // Kanalın z konumundaki yarıçapı
    protected abstract float KanalYaricapi(float z);

    // Aracı ray modunda, çevre ışığı ve arkadan takip eden kamerayla kurar.
    protected void AkisAraciniKur(Vector3 baslangic, Color arkaPlan)
    {
        konum = baslangic;
        arac = AracKontrol.Olustur(konum);
        arac.rayModu = true;
        arac.GetComponent<Rigidbody>().isKinematic = true;

        var cevre = new GameObject("CevreIsigi").AddComponent<Light>();
        cevre.transform.SetParent(arac.transform, false);
        cevre.transform.localPosition = new Vector3(0f, 2f, 0f);
        cevre.type = LightType.Point;
        cevre.color = new Color(1f, 0.88f, 0.8f);
        cevre.intensity = 26f;
        cevre.range = 20f;
        cevre.shadows = LightShadows.None;

        kamera = TakipKamera.Olustur(arac.transform, arkaPlan);
        kamera.yerelTakip = true;
        kamera.ofset = new Vector3(0f, 2.4f, -8.5f);
        AraciYerlestir();
        kamera.Isinla();
    }

    // Enine kesitte süzülme. oyuncu = false ise araç kendiliğinden kanalın ortasına gelir.
    protected void Suzul(float dt, bool oyuncu)
    {
        Vector2 girdi = oyuncu
            ? new Vector2(arac.DonusGirdi, arac.IleriGirdi)
            : -new Vector2(konum.x, konum.y + 0.8f) * 0.35f;
        yanalHiz = Vector2.MoveTowards(yanalHiz, girdi * 7f, 22f * dt);

        Vector2 yanal = new Vector2(konum.x, konum.y) + yanalHiz * dt;
        float sinir = KanalYaricapi(konum.z) - 2f;
        Vector2 merkezden = yanal + Vector2.up * 0.8f;          // aracın gövde merkezi orijinin üstünde
        if (merkezden.magnitude > sinir) yanal = merkezden.normalized * sinir - Vector2.up * 0.8f;
        konum.x = yanal.x;
        konum.y = yanal.y;
    }

    protected Vector3 GovdeMerkezi => konum + Vector3.up * 0.8f;

    // Aracı konumuna koyar; süzülürken hafifçe yatar.
    protected void AraciYerlestir()
    {
        arac.rayHizi = hiz;
        arac.transform.SetPositionAndRotation(konum,
            Quaternion.Euler(-yanalHiz.y * 1.5f, yanalHiz.x * 1.2f, -yanalHiz.x * 2.5f));
    }
}
