using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Çırağın mini aracı: WASD / ok tuşları ile sürülür, Boşluk ile temizleme köpüğü atar.
[RequireComponent(typeof(Rigidbody))]
public class AracKontrol : MonoBehaviour
{
    public float hiz = 9f;
    public float ivme = 30f;
    public float donusHizi = 150f;
    public float atesAraligi = 0.3f;
    public bool kontrolAcik = true;

    Rigidbody rb;
    Material kopukMalzemesi;
    Vector3 itki;
    float ileriGirdi, donusGirdi, mevcutHiz, atesSayaci;

    public static AracKontrol Olustur(Vector3 konum)
    {
        var kok = new GameObject("Arac");
        kok.transform.position = konum;
        var t = kok.transform;

        // Model ve gövde dokuları Blender'da üretilir (Araclar/blender/arac_modeli.py).
        var boya = Sekil.DokuluMalzeme(Resources.Load<Texture2D>("Ortak/arac_govde_albedo"),
            Resources.Load<Texture2D>("Ortak/arac_govde_normal"), Resources.Load<Texture2D>("Ortak/arac_govde_ao"), 0.72f);
        boya.SetFloat("_Metallic", 0.35f);
        var far = Sekil.Malzeme(Color.white, 0.9f);
        Sekil.Isilda(far, new Color(1f, 0.95f, 0.85f) * 6f);
        var malzemeler = new Dictionary<string, Material>
        {
            { "Arac_Govde", boya },
            { "Arac_Cam", Sekil.Malzeme(new Color(0.35f, 0.5f, 0.55f, 0.32f), 0.97f) },
            { "Arac_Lastik", Sekil.Malzeme(new Color(0.045f, 0.045f, 0.05f), 0.3f) },
            { "Arac_Metal", Sekil.Malzeme(new Color(0.62f, 0.64f, 0.67f), 0.82f, 1f) },
            { "Arac_Far", far }
        };

        int bulunan = 0;
        foreach (Mesh mesh in Resources.LoadAll<Mesh>("Ortak/AracModeli"))
        {
            if (!malzemeler.TryGetValue(mesh.name, out Material malzeme)) continue;
            Sekil.MeshParca(mesh, mesh.name, t, Vector3.zero, Vector3.one, malzeme);
            bulunan++;
        }
        if (bulunan < malzemeler.Count) Debug.LogError("Araç modeli eksik: Resources/Ortak/AracModeli");

        // Karanlık ağız içini aydınlatan far
        var isikNesnesi = new GameObject("FarIsigi");
        isikNesnesi.transform.SetParent(t, false);
        isikNesnesi.transform.localPosition = new Vector3(0f, 0.95f, 1.25f);
        isikNesnesi.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        var isik = isikNesnesi.AddComponent<Light>();
        isik.type = LightType.Spot;
        isik.color = new Color(1f, 0.96f, 0.88f);
        isik.intensity = 90f;
        isik.range = 26f;
        isik.spotAngle = 75f;
        isik.innerSpotAngle = 35f;
        isik.shadows = LightShadows.Soft;

        var kapsul = kok.AddComponent<CapsuleCollider>();
        kapsul.direction = 2;
        kapsul.radius = 0.85f;
        kapsul.height = 2.4f;
        kapsul.center = new Vector3(0f, 0.87f, 0f);
        kapsul.material = new PhysicsMaterial("Kaygan")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        var govde = kok.AddComponent<Rigidbody>();
        govde.mass = 1f;
        govde.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        govde.interpolation = RigidbodyInterpolation.Interpolate;
        govde.collisionDetectionMode = CollisionDetectionMode.Continuous;

        return kok.AddComponent<AracKontrol>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        kopukMalzemesi = Sekil.Malzeme(new Color(0.96f, 0.98f, 1f), 0.35f);
    }

    void Update()
    {
        ileriGirdi = 0f;
        donusGirdi = 0f;
        atesSayaci -= Time.deltaTime;

        var kb = Keyboard.current;
        if (!kontrolAcik || kb == null) return;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) ileriGirdi += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) ileriGirdi -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) donusGirdi += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) donusGirdi -= 1f;

        if (kb.spaceKey.isPressed && atesSayaci <= 0f)
        {
            atesSayaci = atesAraligi;
            KopukAt();
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, donusGirdi * donusHizi * dt, 0f));

        mevcutHiz = Mathf.MoveTowards(mevcutHiz, ileriGirdi * hiz, ivme * dt);
        Vector3 v = transform.forward * mevcutHiz + itki;
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;

        itki = Vector3.MoveTowards(itki, Vector3.zero, 15f * dt);
    }

    // Dışarıdan gelen itme (ör. tükürük dalgası). Zamanla söner.
    public void Itki(Vector3 hizVektoru)
    {
        hizVektoru.y = 0f;
        itki = hizVektoru;
    }

    public void Durdur()
    {
        kontrolAcik = false;
        itki = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    void KopukAt()
    {
        Vector3 cikis = transform.position + Vector3.up * 0.6f + transform.forward * 1.7f;
        Vector3 v = transform.forward * (16f + Mathf.Max(0f, mevcutHiz));
        Kopuk.At(cikis, v, rb, kopukMalzemesi);
    }
}
