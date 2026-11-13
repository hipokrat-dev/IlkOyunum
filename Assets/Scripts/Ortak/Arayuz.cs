using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Oyun içi arayüz: mikrop sayacı, profesörün telsiz kutusu, Vücut Kartı ve ekran karartma.
// Tüm bölümler aynı arayüzü kullanır.
public class Arayuz : MonoBehaviour
{
    Font font;
    Text sayac, telsizMetin, kartBaslik, kartGovde, kartAlt, ortaYazi;
    GameObject telsizKutu, kart;
    Image karartma;
    RectTransform hedefIsareti;

    readonly Queue<string> kuyruk = new Queue<string>();
    Coroutine akis;

    public bool TelsizBos => akis == null && kuyruk.Count == 0;

    static readonly Vector2 Orta = new Vector2(0.5f, 0.5f);

    public static Arayuz Olustur(Transform ebeveyn)
    {
        var g = new GameObject("Arayuz", typeof(Canvas), typeof(CanvasScaler));
        g.transform.SetParent(ebeveyn, false);
        g.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var olcek = g.GetComponent<CanvasScaler>();
        olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        olcek.referenceResolution = new Vector2(1920f, 1080f);
        olcek.matchWidthOrHeight = 0.5f;

        var a = g.AddComponent<Arayuz>();
        a.Kur();
        return a;
    }

    void Kur()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        var panelRengi = new Color(0.04f, 0.08f, 0.18f, 0.75f);

        // Sol üst: mikrop sayacı
        var sayacPanel = Panel("Sayac", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -30f), new Vector2(380f, 84f), panelRengi);
        sayac = Dolduran("Metin", sayacPanel.transform, 40, TextAnchor.MiddleCenter, Color.white, 10f);
        sayac.fontStyle = FontStyle.Bold;

        // Sağ üst: kontrol ipucu
        var ipucuPanel = Panel("Ipucu", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-30f, -30f), new Vector2(680f, 84f), panelRengi);
        Dolduran("Metin", ipucuPanel.transform, 26, TextAnchor.MiddleCenter, new Color(0.85f, 0.92f, 1f), 10f)
            .text = "WASD / Oklar: Sür    BOŞLUK: Köpük    ESC: Duraklat";

        // Alt orta: telsiz kutusu
        var telsizPanel = Panel("Telsiz", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(1400f, 190f), panelRengi);
        telsizKutu = telsizPanel.gameObject;
        var etiket = Yazi("Etiket", telsizPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -12f), new Vector2(-60f, 40f), 26, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.25f));
        etiket.text = "PROFESÖR  (telsiz)";
        etiket.fontStyle = FontStyle.Bold;
        telsizMetin = Yazi("Mesaj", telsizPanel.transform, Vector2.zero, Vector2.one, Orta,
            new Vector2(0f, -22f), new Vector2(-60f, -70f), 34, TextAnchor.UpperLeft, Color.white);
        telsizKutu.SetActive(false);

        // Orta: Vücut Kartı
        var cerceve = Panel("VucutKarti", transform, Orta, Orta, Orta, Vector2.zero, new Vector2(1000f, 680f),
            new Vector3(1f, 0.6f, 0.15f).ToColor());
        kart = cerceve.gameObject;
        var ic = Panel("Ic", cerceve.transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, new Vector2(-24f, -24f),
            new Color(1f, 0.97f, 0.88f));
        var koyuYazi = new Color(0.2f, 0.12f, 0.1f);
        kartBaslik = Yazi("Baslik", ic.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(-60f, 80f), 56, TextAnchor.MiddleCenter, new Color(0.8f, 0.25f, 0.3f));
        kartBaslik.fontStyle = FontStyle.Bold;
        kartGovde = Yazi("Govde", ic.transform, Vector2.zero, Vector2.one, Orta,
            new Vector2(0f, -20f), new Vector2(-100f, -240f), 34, TextAnchor.UpperLeft, koyuYazi);
        kartGovde.lineSpacing = 1.15f;
        kartAlt = Yazi("Alt", ic.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(-60f, 50f), 28, TextAnchor.MiddleCenter, new Color(0.45f, 0.35f, 0.3f));
        kart.SetActive(false);

        // Ekran dışında kalan en yakın mikrobun yönünü gösteren işaret
        var isaret = Panel("HedefIsareti", transform, Orta, Orta, Orta, Vector2.zero, new Vector2(30f, 30f),
            new Color(0.8f, 1f, 0.35f, 0.9f));
        hedefIsareti = (RectTransform)isaret.transform;
        hedefIsareti.localRotation = Quaternion.Euler(0f, 0f, 45f);
        isaret.gameObject.SetActive(false);

        // En üstte: karartma ve bölüm sonu yazısı
        karartma = Panel("Karartma", transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0f));
        ortaYazi = Yazi("OrtaYazi", transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, new Vector2(-200f, -200f),
            60, TextAnchor.MiddleCenter, Color.white);
        ortaYazi.fontStyle = FontStyle.Bold;
        ortaYazi.lineSpacing = 1.2f;
    }

    // ---------- Dışarıya açık işlevler ----------

    public void Sayac(int temizlenen, int toplam)
    {
        sayac.text = $"Mikrop: {temizlenen} / {toplam}";
        if (temizlenen > 0) StartCoroutine(SayacVurgusu());
    }

    IEnumerator SayacVurgusu()
    {
        var panel = sayac.transform.parent;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            panel.localScale = Vector3.one * (1f + 0.28f * Mathf.Sin(Mathf.PI * t));
            yield return null;
        }
        panel.localScale = Vector3.one;
    }

    // Ekranı kısmen karartır (başlangıç ve duraklatma ekranları). 0 = açık.
    public void Perde(float alfa) => karartma.color = new Color(0f, 0f, 0f, alfa);

    // Hedef işaretini ekran oranı (0..1) cinsinden bir noktaya koyar; null ise gizler.
    public void HedefIsareti(Vector2? ekranOrani)
    {
        hedefIsareti.gameObject.SetActive(ekranOrani.HasValue);
        if (!ekranOrani.HasValue) return;
        hedefIsareti.anchorMin = hedefIsareti.anchorMax = ekranOrani.Value;
        hedefIsareti.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f));
    }

    // Profesörün telsiz mesajı. Öncelikli mesaj, bekleyenleri silip hemen gösterilir.
    public void Telsiz(string mesaj, bool oncelikli = false)
    {
        if (oncelikli)
        {
            kuyruk.Clear();
            if (akis != null) StopCoroutine(akis);
            akis = null;
        }
        kuyruk.Enqueue(mesaj);
        if (akis == null) akis = StartCoroutine(TelsizAkisi());
    }

    public void KartGoster(string baslik, string govde, string alt)
    {
        kartBaslik.text = baslik;
        kartGovde.text = govde;
        kartAlt.text = alt;
        kart.SetActive(true);
    }

    public void KartAltYazi(string alt) => kartAlt.text = alt;

    public void KartGizle() => kart.SetActive(false);

    public IEnumerator Karart(float sure)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / sure)
        {
            karartma.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }
        karartma.color = Color.black;
    }

    public void OrtaYazi(string metin) => ortaYazi.text = metin;

    // ---------- İç işleyiş ----------

    IEnumerator TelsizAkisi()
    {
        telsizKutu.SetActive(true);
        while (kuyruk.Count > 0)
        {
            string mesaj = kuyruk.Dequeue();
            Ses.Cal(Ses.Tur.Telsiz, 0.6f);
            for (float harf = 0f; harf < mesaj.Length; harf += Time.deltaTime * 45f)
            {
                telsizMetin.text = mesaj.Substring(0, (int)harf);
                yield return null;
            }
            telsizMetin.text = mesaj;
            yield return new WaitForSeconds(2.2f + mesaj.Length * 0.03f);
        }
        telsizKutu.SetActive(false);
        akis = null;
    }

    RectTransform Kutu(string ad, Transform ebeveyn, Vector2 capaMin, Vector2 capaMax, Vector2 pivot,
        Vector2 konum, Vector2 boyut)
    {
        var g = new GameObject(ad, typeof(RectTransform));
        var r = (RectTransform)g.transform;
        r.SetParent(ebeveyn, false);
        r.anchorMin = capaMin;
        r.anchorMax = capaMax;
        r.pivot = pivot;
        r.anchoredPosition = konum;
        r.sizeDelta = boyut;
        return r;
    }

    Image Panel(string ad, Transform ebeveyn, Vector2 capaMin, Vector2 capaMax, Vector2 pivot,
        Vector2 konum, Vector2 boyut, Color renk)
    {
        var resim = Kutu(ad, ebeveyn, capaMin, capaMax, pivot, konum, boyut).gameObject.AddComponent<Image>();
        resim.color = renk;
        resim.raycastTarget = false;
        return resim;
    }

    Text Yazi(string ad, Transform ebeveyn, Vector2 capaMin, Vector2 capaMax, Vector2 pivot,
        Vector2 konum, Vector2 boyut, int punto, TextAnchor hiza, Color renk)
    {
        var yazi = Kutu(ad, ebeveyn, capaMin, capaMax, pivot, konum, boyut).gameObject.AddComponent<Text>();
        yazi.font = font;
        yazi.fontSize = punto;
        yazi.alignment = hiza;
        yazi.color = renk;
        yazi.horizontalOverflow = HorizontalWrapMode.Wrap;
        yazi.verticalOverflow = VerticalWrapMode.Overflow;
        yazi.raycastTarget = false;
        return yazi;
    }

    // Ebeveynini (kenar boşluğu bırakarak) dolduran yazı.
    Text Dolduran(string ad, Transform ebeveyn, int punto, TextAnchor hiza, Color renk, float bosluk)
    {
        return Yazi(ad, ebeveyn, Vector2.zero, Vector2.one, Orta, Vector2.zero,
            new Vector2(-2f * bosluk, -2f * bosluk), punto, hiza, renk);
    }
}

static class RenkUzantilari
{
    public static Color ToColor(this Vector3 v) => new Color(v.x, v.y, v.z);
}
