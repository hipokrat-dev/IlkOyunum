using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ana menü: bölüm seçimi (kazanılan yıldızlarla) ve Vücut Kartı koleksiyonu.
// Fare, dokunma ve klavyeyle (1-7: bölüm, K: kartlar, ESC: geri) kullanılır.
public class AnaMenu : MonoBehaviour
{
    readonly DugmeListesi dugmeler = new DugmeListesi();
    readonly List<int> toplananKartlar = new List<int>();

    RectTransform guvenli;
    GameObject kartPaneli;
    Text kartBaslik, kartGovde, kartSayfa;
    Transform dekor;
    Vector2Int sonEkran;
    int kartSirasi;

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        ArkaPlaniKur();

        Cizim.Tuval("Menu", transform, out guvenli);
        var sari = new Color(1f, 0.82f, 0.25f);

        var baslik = Cizim.Yazi("Baslik", guvenli, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(0f, 130f), 96, TextAnchor.MiddleCenter, Color.white);
        baslik.text = "VÜCUT YOLCULUĞU";
        baslik.fontStyle = FontStyle.Bold;
        var altBaslik = Cizim.Yazi("AltBaslik", guvenli, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -175f), new Vector2(0f, 50f), 34, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.8f));
        altBaslik.text = "Bir bölüm seç";

        // Bölüm düğmeleri: üstte 4, altta 3
        var bolumler = BolumListesi.Tumu;
        for (int i = 0; i < bolumler.Length; i++)
        {
            var b = bolumler[i];
            int satir = i < 4 ? 0 : 1, sutun = i < 4 ? i : i - 4;
            float x = (sutun - (satir == 0 ? 1.5f : 1f)) * 440f;
            float y = satir == 0 ? 105f : -165f;

            bool bitti = PlayerPrefs.GetInt("bitti_" + b.no, 0) == 1;
            int yildiz = BolumTemeli.EnIyiYildiz(b.no);
            string durum = b.puanli
                ? "<color=#FFD040>" + new string('★', yildiz) + new string('☆', 3 - yildiz) + "</color>"
                : bitti ? "<color=#9FE0A0>izlendi</color>" : "";
            var renk = bitti ? new Color(0.14f, 0.4f, 0.3f, 0.92f) : new Color(0.1f, 0.2f, 0.42f, 0.92f);

            var dugme = Cizim.Dugme("Bolum" + b.no, guvenli, Cizim.Orta, Cizim.Orta, new Vector2(x, y), new Vector2(410f, 240f),
                $"<size=30>BÖLÜM {b.no}</size>\n{b.ad}\n<size=44>{durum}</size>", 38, renk, out _);
            string sahne = b.sahne;
            dugmeler.Ekle(dugme, () => BolumuAc(sahne));
        }

        var kartDugmesi = Cizim.Dugme("Kartlar", guvenli, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f),
            new Vector2(520f, 100f), "VÜCUT KARTLARI", 40, new Color(0.75f, 0.42f, 0.08f, 0.95f), out _);
        dugmeler.Ekle(kartDugmesi, KartlariAc);

        KartPaneliniKur();
    }

    // Arka plan: yavaşça dönen bir alveol kümesi
    void ArkaPlaniKur()
    {
        var kamera = new GameObject("Kamera") { tag = "MainCamera" }.AddComponent<Camera>();
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = new Color(0.07f, 0.015f, 0.02f);
        kamera.transform.position = new Vector3(0f, 0f, -9f);
        kamera.gameObject.AddComponent<AudioListener>();

        var isik = new GameObject("Isik").AddComponent<Light>();
        isik.type = LightType.Directional;
        isik.color = new Color(1f, 0.8f, 0.75f);
        isik.intensity = 1.1f;
        isik.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.1f, 0.1f);

        var modeller = Resources.LoadAll<Mesh>("Akciger/AkcigerModelleri");
        if (modeller.Length == 0) return;
        var malzeme = Sekil.DokuluMalzeme(Resources.Load<Texture2D>("Akciger/alveol_albedo"),
            Resources.Load<Texture2D>("Akciger/alveol_normal"), null, 0.7f, 2f, 1f);
        malzeme.SetColor("_BaseColor", new Color(0.4f, 0.28f, 0.28f));       // yazılar öne çıksın diye soluk
        dekor = Sekil.MeshParca(modeller[0], "Dekor", null, new Vector3(0f, -1.2f, 6f), Vector3.one * 1.3f, malzeme).transform;
    }

    void KartPaneliniKur()
    {
        var perde = Cizim.Panel("KartPaneli", transform, Vector2.zero, Vector2.one, Cizim.Orta, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.8f));
        kartPaneli = perde.gameObject;

        var cerceve = Cizim.Panel("Kart", perde.transform, Cizim.Orta, Cizim.Orta, Cizim.Orta, new Vector2(0f, 30f),
            new Vector2(1000f, 680f), new Color(1f, 0.6f, 0.15f));
        var ic = Cizim.Panel("Ic", cerceve.transform, Vector2.zero, Vector2.one, Cizim.Orta, Vector2.zero, new Vector2(-24f, -24f),
            new Color(1f, 0.97f, 0.88f));
        kartBaslik = Cizim.Yazi("Baslik", ic.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(-60f, 80f), 56, TextAnchor.MiddleCenter, new Color(0.8f, 0.25f, 0.3f));
        kartBaslik.fontStyle = FontStyle.Bold;
        kartGovde = Cizim.Yazi("Govde", ic.transform, Vector2.zero, Vector2.one, Cizim.Orta,
            new Vector2(0f, -20f), new Vector2(-100f, -240f), 34, TextAnchor.UpperLeft, new Color(0.2f, 0.12f, 0.1f));
        kartGovde.lineSpacing = 1.15f;
        kartSayfa = Cizim.Yazi("Sayfa", ic.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(-60f, 50f), 28, TextAnchor.MiddleCenter, new Color(0.45f, 0.35f, 0.3f));

        var mavi = new Color(0.12f, 0.25f, 0.5f, 0.95f);
        dugmeler.Ekle(Cizim.Dugme("Onceki", perde.transform, Cizim.Orta, Cizim.Orta, new Vector2(-620f, 30f),
            new Vector2(130f, 200f), "◀", 70, mavi, out _), () => KartDegistir(-1));
        dugmeler.Ekle(Cizim.Dugme("Sonraki", perde.transform, Cizim.Orta, Cizim.Orta, new Vector2(620f, 30f),
            new Vector2(130f, 200f), "▶", 70, mavi, out _), () => KartDegistir(1));
        dugmeler.Ekle(Cizim.Dugme("Kapat", perde.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f),
            new Vector2(360f, 96f), "KAPAT", 38, mavi, out _), () => kartPaneli.SetActive(false));
        kartPaneli.SetActive(false);
    }

    void KartlariAc()
    {
        toplananKartlar.Clear();
        foreach (var b in BolumListesi.Tumu)
        {
            if (PlayerPrefs.HasKey("kart_baslik_" + b.no)) toplananKartlar.Add(b.no);
        }
        kartSirasi = 0;
        kartPaneli.SetActive(true);
        KartiGoster();
    }

    void KartDegistir(int yon)
    {
        if (toplananKartlar.Count == 0) return;
        kartSirasi = (kartSirasi + yon + toplananKartlar.Count) % toplananKartlar.Count;
        KartiGoster();
        Ses.Cal(Ses.Tur.Tik);
    }

    void KartiGoster()
    {
        int toplam = BolumListesi.Tumu.Length;
        if (toplananKartlar.Count == 0)
        {
            kartBaslik.text = "VÜCUT KARTLARI";
            kartGovde.text = "Henüz kart toplamadın.\n\nHer bölümün sonunda o organla ilgili üç ilginç bilgi içeren bir kart kazanırsın.";
            kartSayfa.text = $"0 / {toplam} kart";
            return;
        }
        int no = toplananKartlar[kartSirasi];
        kartBaslik.text = PlayerPrefs.GetString("kart_baslik_" + no);
        kartGovde.text = PlayerPrefs.GetString("kart_metin_" + no);
        kartSayfa.text = $"Kart {kartSirasi + 1} / {toplananKartlar.Count}     (toplam {toplam} kartın {toplananKartlar.Count} tanesi sende)";
    }

    static void BolumuAc(string sahne)
    {
        if (!Application.CanStreamedLevelBeLoaded(sahne))
        {
            Debug.LogError("Bölüm sahnesi derleme listesinde yok: " + sahne);
            return;
        }
        SceneManager.LoadScene(sahne);
    }

    void Update()
    {
        DokunmatikGirdi.Guncelle();
        if (dekor != null) dekor.Rotate(0f, 8f * Time.deltaTime, 0f);

        var ekran = new Vector2Int(Screen.width, Screen.height);
        if (ekran != sonEkran)
        {
            sonEkran = ekran;
            Cizim.GuvenliAlaniUygula(guvenli);
        }

        if (dugmeler.Isle()) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kartPaneli.activeSelf)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame) kartPaneli.SetActive(false);
            else if (kb.leftArrowKey.wasPressedThisFrame) KartDegistir(-1);
            else if (kb.rightArrowKey.wasPressedThisFrame) KartDegistir(1);
            return;
        }
        if (kb.kKey.wasPressedThisFrame) KartlariAc();
        Key[] rakamlar = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7 };
        for (int i = 0; i < rakamlar.Length && i < BolumListesi.Tumu.Length; i++)
        {
            if (kb[rakamlar[i]].wasPressedThisFrame) BolumuAc(BolumListesi.Tumu[i].sahne);
        }
    }
}
