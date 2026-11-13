using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Aracı arkadan-üstten takip eden 3. şahıs kamera. Sinematik görüntü efektlerini de kurar.
public class TakipKamera : MonoBehaviour
{
    public Transform hedef;
    public Vector3 ofset = new Vector3(0f, 6.5f, -8f);
    public float bakisMesafesi = 2f;
    public float yumusaklik = 5f;
    public float sarsinti;                    // > 0 ise kamera titrer, kendiliğinden söner
    public bool yerelTakip;                   // true: ofset ve "yukarı" aracın kendi eksenlerine göre (tüp içinde)
    public Func<Vector3, Vector3> sinir;      // Kamerayı bölümün içinde tutar (duvarın dışına çıkmasın)

    Vector3 konum;

    public static TakipKamera Olustur(Transform hedef, Color arkaPlan)
    {
        MobilKalite.Kur();
        var g = new GameObject("Kamera") { tag = "MainCamera" };
        var kamera = g.AddComponent<Camera>();
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = arkaPlan;
        kamera.fieldOfView = 58f;
        kamera.nearClipPlane = 0.2f;
        kamera.farClipPlane = 120f;
        kamera.allowHDR = true;
        g.AddComponent<AudioListener>();

        var ek = kamera.GetUniversalAdditionalCameraData();
        ek.renderPostProcessing = true;
        // Tablette daha hafif kenar yumuşatma
        ek.antialiasing = MobilKalite.Mobil
            ? AntialiasingMode.FastApproximateAntialiasing
            : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        ek.antialiasingQuality = AntialiasingQuality.High;

        GoruntuEfektleri(g.transform);

        var takip = g.AddComponent<TakipKamera>();
        takip.hedef = hedef;
        takip.konum = takip.HedefKonum();
        takip.LateUpdate();
        return takip;
    }

    // Film görünümü: ACES ton eşleme, parlama, alan derinliği, kenar karartma, hafif gren.
    static void GoruntuEfektleri(Transform ebeveyn)
    {
        var g = new GameObject("GoruntuEfektleri");
        g.transform.SetParent(ebeveyn, false);
        var hacim = g.AddComponent<Volume>();
        hacim.isGlobal = true;
        hacim.priority = 10f;

        var profil = ScriptableObject.CreateInstance<VolumeProfile>();
        hacim.profile = profil;

        profil.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;

        var parlama = profil.Add<Bloom>(true);
        parlama.threshold.value = 1f;
        parlama.intensity.value = 0.24f;
        parlama.scatter.value = 0.65f;

        var renk = profil.Add<ColorAdjustments>(true);
        renk.postExposure.value = 0.2f;
        renk.contrast.value = 7f;
        renk.saturation.value = -6f;

        var kenar = profil.Add<Vignette>(true);
        kenar.intensity.value = 0.20f;
        kenar.smoothness.value = 0.5f;

        // Oynanışta net görüş: alan derinliği ve film greni uygulanmaz.
        g.AddComponent<ProfilOmru>().profil = profil;
    }

    // Kamerayı yumuşatma olmadan hedefin arkasına koyar (ışınlanmadan sonra).
    public void Isinla() => konum = HedefKonum();

    Vector3 HedefKonum()
    {
        if (yerelTakip) return hedef.TransformPoint(ofset);
        Vector3 p = hedef.position + Quaternion.Euler(0f, hedef.eulerAngles.y, 0f) * ofset;
        return sinir != null ? sinir(p) : p;
    }

    void LateUpdate()
    {
        if (hedef == null) return;

        konum = Vector3.Lerp(konum, HedefKonum(), 1f - Mathf.Exp(-yumusaklik * Time.deltaTime));
        if(sinir != null) konum = sinir(konum);
        Vector3 yukari = yerelTakip ? hedef.up : Vector3.up;
        Vector3 bakis = hedef.position + yukari * 1.2f + hedef.forward * (yerelTakip ? 6f : bakisMesafesi);

        transform.position = konum + UnityEngine.Random.insideUnitSphere * sarsinti;
        transform.rotation = Quaternion.LookRotation(bakis - konum, yukari);
        sarsinti = Mathf.MoveTowards(sarsinti, 0f, Time.deltaTime * 0.4f);
    }
}

public class ProfilOmru : MonoBehaviour { public VolumeProfile profil; void OnDestroy() { if(profil != null) { foreach(var c in profil.components) Destroy(c); Destroy(profil); } } }
