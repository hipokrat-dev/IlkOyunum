using System;
using UnityEngine;
using UnityEngine.Rendering;

// Kodla nesne, malzeme, doku ve organik mesh üretmek için yardımcılar.
// Bölümler şimdilik tamamen kodla kuruluyor; ileride taranmış/heykel modellerle değiştirilebilir.
public static class Sekil
{
    static Material sablon;

    // ---------- Malzeme ----------

    public static Material Malzeme(Color renk, float puruzsuzluk = 0.4f, float metalik = 0f,
        Texture2D normal = null, float normalGucu = 1f, float dokuTekrari = 1f)
    {
        if (sablon == null)
        {
            // Projenin varsayılan (URP Lit) malzemesini şablon olarak al.
            var gecici = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sablon = gecici.GetComponent<Renderer>().sharedMaterial;
            UnityEngine.Object.Destroy(gecici);
        }

        var m = new Material(sablon) { color = renk, enableInstancing = true };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", renk);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", puruzsuzluk);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalik);

        if (normal != null && m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", normalGucu);
            m.SetTextureScale("_BaseMap", Vector2.one * dokuTekrari);
            m.EnableKeyword("_NORMALMAP");
        }

        if (renk.a < 0.99f)
        {
            // URP Lit'i saydam moda al.
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        return SahneVarliklari.Kaydet(m);
    }

    // Blender'da pişirilmiş dokularla (renk, normal, ortam gölgesi) malzeme.
    public static Material DokuluMalzeme(Texture2D renkDokusu, Texture2D normal, Texture2D golge,
        float puruzsuzluk, float dokuTekrari = 1f, float normalGucu = 1f)
    {
        var m = Malzeme(Color.white, puruzsuzluk, 0f, normal, normalGucu, dokuTekrari);
        m.SetTexture("_BaseMap", renkDokusu);
        m.SetTextureScale("_BaseMap", Vector2.one * dokuTekrari);
        if (golge != null && m.HasProperty("_OcclusionMap"))
        {
            m.SetTexture("_OcclusionMap", golge);
            m.SetFloat("_OcclusionStrength", 1f);
            m.EnableKeyword("_OCCLUSIONMAP");
        }
        return SahneVarliklari.Kaydet(m);
    }

    // Tek renkli yansıma haritası (gökyüzü yerine).
    public static Cubemap DuzKupHarita(Color renk)
    {
        var harita = new Cubemap(8, TextureFormat.RGBA32, false);
        var pikseller = new Color[64];
        for (int i = 0; i < pikseller.Length; i++) pikseller[i] = renk;
        for (int yuz = 0; yuz < 6; yuz++) harita.SetPixels(pikseller, (CubemapFace)yuz);
        harita.Apply();
        return SahneVarliklari.Kaydet(harita);
    }

    public static void Isilda(Material m, Color isik)
    {
        if (!m.HasProperty("_EmissionColor")) return;
        m.SetColor("_EmissionColor", isik);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    // ---------- Mesh ----------

    // Birim yarıçaplı süper-elipsoid: e küçüldükçe köşeleri yuvarlatılmış kutuya, 1'de küreye benzer.
    // "bozucu" her noktayı yeniden biçimlendirir (diş tümsekleri, sivrilme vb.).
    public static Mesh SuperElips(float eDikey, float eYatay, int dilim, Func<Vector3, Vector3> bozucu = null,
        bool icYuz = false)
    {
        int sutun = dilim * 2, satir = dilim;
        var noktalar = new Vector3[(sutun + 1) * (satir + 1)];
        var uv = new Vector2[noktalar.Length];
        for (int j = 0; j <= satir; j++)
        {
            float v = Mathf.Lerp(-Mathf.PI / 2f, Mathf.PI / 2f, (float)j / satir);
            for (int i = 0; i <= sutun; i++)
            {
                float u = Mathf.Lerp(-Mathf.PI, Mathf.PI, (float)i / sutun);
                var p = new Vector3(
                    Us(Mathf.Cos(v), eDikey) * Us(Mathf.Cos(u), eYatay),
                    Us(Mathf.Sin(v), eDikey),
                    Us(Mathf.Cos(v), eDikey) * Us(Mathf.Sin(u), eYatay));
                int k = j * (sutun + 1) + i;
                noktalar[k] = bozucu != null ? bozucu(p) : p;
                uv[k] = new Vector2((float)i / sutun, (float)j / satir);
            }
        }

        var ucgenler = new int[sutun * satir * 6];
        int t = 0;
        for (int j = 0; j < satir; j++)
        {
            for (int i = 0; i < sutun; i++)
            {
                int a = j * (sutun + 1) + i, b = a + 1, c = a + sutun + 1, d = c + 1;
                if (icYuz)
                {
                    ucgenler[t++] = a; ucgenler[t++] = b; ucgenler[t++] = c;
                    ucgenler[t++] = b; ucgenler[t++] = d; ucgenler[t++] = c;
                }
                else
                {
                    ucgenler[t++] = a; ucgenler[t++] = c; ucgenler[t++] = b;
                    ucgenler[t++] = b; ucgenler[t++] = c; ucgenler[t++] = d;
                }
            }
        }
        return MeshYap(noktalar, uv, ucgenler);
    }

    // XZ düzleminde, yüksekliği bir fonksiyonla verilen ızgara (dil yüzeyi gibi).
    public static Mesh Izgara(Vector2 enAz, Vector2 enCok, int bolmeX, int bolmeZ, Func<float, float, float> yukseklik,
        float uvTekrari)
    {
        var noktalar = new Vector3[(bolmeX + 1) * (bolmeZ + 1)];
        var uv = new Vector2[noktalar.Length];
        for (int j = 0; j <= bolmeZ; j++)
        {
            for (int i = 0; i <= bolmeX; i++)
            {
                float x = Mathf.Lerp(enAz.x, enCok.x, (float)i / bolmeX);
                float z = Mathf.Lerp(enAz.y, enCok.y, (float)j / bolmeZ);
                int k = j * (bolmeX + 1) + i;
                noktalar[k] = new Vector3(x, yukseklik(x, z), z);
                uv[k] = new Vector2((float)i / bolmeX, (float)j / bolmeZ) * uvTekrari;
            }
        }

        var ucgenler = new int[bolmeX * bolmeZ * 6];
        int t = 0;
        for (int j = 0; j < bolmeZ; j++)
        {
            for (int i = 0; i < bolmeX; i++)
            {
                int a = j * (bolmeX + 1) + i, b = a + 1, c = a + bolmeX + 1, d = c + 1;
                ucgenler[t++] = a; ucgenler[t++] = c; ucgenler[t++] = b;
                ucgenler[t++] = b; ucgenler[t++] = c; ucgenler[t++] = d;
            }
        }
        return MeshYap(noktalar, uv, ucgenler);
    }

    // Alyuvar: iki yüzü çukur disk.
    public static Mesh Alyuvar()
    {
        return SuperElips(1f, 1f, 12, p =>
        {
            float d = p.x * p.x + p.z * p.z;
            return new Vector3(p.x, p.y * 0.3f * (1f - 0.72f * Mathf.Exp(-d / 0.2f)), p.z);
        });
    }

    // Z ekseni etrafında simit: R ana yarıçap, r kesit yarıçapı.
    public static Mesh Halka(float R, float r, int dilim, int kesit)
    {
        return Parametrik(dilim, kesit, false, (fu, fv) =>
        {
            float t = fu * 2f * Mathf.PI, f = fv * 2f * Mathf.PI;
            float d = R + r * Mathf.Cos(f);
            return new Vector3(d * Mathf.Cos(t), d * Mathf.Sin(t), r * Mathf.Sin(f));
        }, 6f, 1f);
    }

    // +Z yönünde uzanan, içinden bakılan tüp. yaricap(açı, z) duvarın o noktadaki yarıçapını verir.
    public static Mesh Tup(float uzunluk, int dilim, int bolme, Func<float, float, float> yaricap, float uvU, float uvV)
    {
        return Parametrik(dilim, bolme, true, (fu, fv) =>
        {
            float t = fu * 2f * Mathf.PI, z = fv * uzunluk;
            float r = yaricap(t, z);
            return new Vector3(r * Mathf.Cos(t), r * Mathf.Sin(t), z);
        }, uvU, uvV);
    }

    // İki parametreli (0..1, 0..1) bir fonksiyondan yüzey üretir. icYuz: yüzeye iç tarafından bakılacaksa true.
    public static Mesh Parametrik(int nu, int nv, bool icYuz, Func<float, float, Vector3> nokta, float uvU, float uvV)
    {
        var noktalar = new Vector3[(nu + 1) * (nv + 1)];
        var uv = new Vector2[noktalar.Length];
        for (int j = 0; j <= nv; j++)
        {
            for (int i = 0; i <= nu; i++)
            {
                int k = j * (nu + 1) + i;
                noktalar[k] = nokta((float)i / nu, (float)j / nv);
                uv[k] = new Vector2((float)i / nu * uvU, (float)j / nv * uvV);
            }
        }

        var ucgenler = new int[nu * nv * 6];
        int t = 0;
        for (int j = 0; j < nv; j++)
        {
            for (int i = 0; i < nu; i++)
            {
                int a = j * (nu + 1) + i, b = a + 1, c = a + nu + 1, d = c + 1;
                if (icYuz)
                {
                    ucgenler[t++] = a; ucgenler[t++] = c; ucgenler[t++] = b;
                    ucgenler[t++] = b; ucgenler[t++] = c; ucgenler[t++] = d;
                }
                else
                {
                    ucgenler[t++] = a; ucgenler[t++] = b; ucgenler[t++] = c;
                    ucgenler[t++] = b; ucgenler[t++] = d; ucgenler[t++] = c;
                }
            }
        }
        return MeshYap(noktalar, uv, ucgenler);
    }

    static Mesh MeshYap(Vector3[] noktalar, Vector2[] uv, int[] ucgenler)
    {
        var mesh = new Mesh { indexFormat = IndexFormat.UInt32, vertices = noktalar, uv = uv, triangles = ucgenler };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return SahneVarliklari.Kaydet(mesh);
    }

    static float Us(float deger, float us) => Mathf.Sign(deger) * Mathf.Pow(Mathf.Abs(deger), us);

    // ---------- Nesne ----------

    public static GameObject Parca(PrimitiveType tur, string ad, Transform ebeveyn, Vector3 yerelKonum,
        Vector3 olcek, Material malzeme, bool carpistirici = false, Quaternion? yerelDonus = null)
    {
        var g = GameObject.CreatePrimitive(tur);
        Yerlestir(g, ad, ebeveyn, yerelKonum, olcek, yerelDonus);
        g.GetComponent<Renderer>().sharedMaterial = malzeme;

        if (!carpistirici)
        {
            var c = g.GetComponent<Collider>();
            c.enabled = false;
            UnityEngine.Object.Destroy(c);
        }
        return g;
    }

    public static GameObject MeshParca(Mesh mesh, string ad, Transform ebeveyn, Vector3 yerelKonum,
        Vector3 olcek, Material malzeme, Quaternion? yerelDonus = null)
    {
        var g = new GameObject(ad, typeof(MeshFilter), typeof(MeshRenderer));
        Yerlestir(g, ad, ebeveyn, yerelKonum, olcek, yerelDonus);
        g.GetComponent<MeshFilter>().sharedMesh = mesh;
        g.GetComponent<MeshRenderer>().sharedMaterial = malzeme;
        return g;
    }

    static void Yerlestir(GameObject g, string ad, Transform ebeveyn, Vector3 konum, Vector3 olcek, Quaternion? donus)
    {
        g.name = ad;
        g.transform.SetParent(ebeveyn, false);
        g.transform.localPosition = konum;
        g.transform.localRotation = donus ?? Quaternion.identity;
        g.transform.localScale = olcek;
    }
}
