using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Arayüz parçalarını kodla kurmak için ortak yardımcılar (oyun içi arayüz ve ana menü kullanır).
public static class Cizim
{
    public static readonly Vector2 Orta = new Vector2(0.5f, 0.5f);

    static Font font;
    static Sprite daire;

    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return font;
        }
    }

    // Kenarı yumuşak dolu daire (dokunmatik düğmeler için)
    public static Sprite Daire
    {
        get
        {
            if (daire != null) return daire;
            const int B = 128;
            var doku = new Texture2D(B, B, TextureFormat.RGBA32, false);
            var pikseller = new Color32[B * B];
            for (int y = 0; y < B; y++)
            {
                for (int x = 0; x < B; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(B / 2f, B / 2f)) / (B / 2f);
                    pikseller[y * B + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01((1f - d) * 24f)));
                }
            }
            doku.SetPixels32(pikseller);
            doku.Apply();
            daire = Sprite.Create(doku, new Rect(0, 0, B, B), Orta);
            return daire;
        }
    }

    // Ekranı dolduran tuval. "guvenli": çentik/yuvarlak köşe dışında kalan alan; göstergeler buna yerleşir.
    public static Canvas Tuval(string ad, Transform ebeveyn, out RectTransform guvenli)
    {
        var g = new GameObject(ad, typeof(Canvas), typeof(CanvasScaler));
        g.transform.SetParent(ebeveyn, false);
        var tuval = g.GetComponent<Canvas>();
        tuval.renderMode = RenderMode.ScreenSpaceOverlay;

        var olcek = g.GetComponent<CanvasScaler>();
        olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        olcek.referenceResolution = new Vector2(1920f, 1080f);
        olcek.matchWidthOrHeight = 0.5f;      // her en-boy oranında (telefon, tablet, geniş ekran) dengeli ölçek

        guvenli = Kutu("GuvenliAlan", g.transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, Vector2.zero);
        GuvenliAlaniUygula(guvenli);
        return tuval;
    }

    public static void GuvenliAlaniUygula(RectTransform guvenli)
    {
        Rect alan = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        guvenli.anchorMin = new Vector2(alan.xMin / Screen.width, alan.yMin / Screen.height);
        guvenli.anchorMax = new Vector2(alan.xMax / Screen.width, alan.yMax / Screen.height);
    }

    public static RectTransform Kutu(string ad, Transform ebeveyn, Vector2 capaMin, Vector2 capaMax, Vector2 pivot,
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

    public static Image Panel(string ad, Transform ebeveyn, Vector2 capaMin, Vector2 capaMax, Vector2 pivot,
        Vector2 konum, Vector2 boyut, Color renk)
    {
        var resim = Kutu(ad, ebeveyn, capaMin, capaMax, pivot, konum, boyut).gameObject.AddComponent<Image>();
        resim.color = renk;
        resim.raycastTarget = false;
        return resim;
    }

    public static Text Yazi(string ad, Transform ebeveyn, Vector2 capaMin, Vector2 capaMax, Vector2 pivot,
        Vector2 konum, Vector2 boyut, int punto, TextAnchor hiza, Color renk)
    {
        var yazi = Kutu(ad, ebeveyn, capaMin, capaMax, pivot, konum, boyut).gameObject.AddComponent<Text>();
        yazi.font = Font;
        yazi.fontSize = punto;
        yazi.alignment = hiza;
        yazi.color = renk;
        yazi.horizontalOverflow = HorizontalWrapMode.Wrap;
        yazi.verticalOverflow = VerticalWrapMode.Overflow;
        yazi.raycastTarget = false;
        return yazi;
    }

    // Ebeveynini (kenar boşluğu bırakarak) dolduran yazı.
    public static Text Dolduran(string ad, Transform ebeveyn, int punto, TextAnchor hiza, Color renk, float bosluk)
    {
        return Yazi(ad, ebeveyn, Vector2.zero, Vector2.one, Orta, Vector2.zero,
            new Vector2(-2f * bosluk, -2f * bosluk), punto, hiza, renk);
    }

    // Yazılı düğme (panel + yazı). Tıklama/dokunma DugmeListesi ile işlenir.
    public static RectTransform Dugme(string ad, Transform ebeveyn, Vector2 capa, Vector2 pivot, Vector2 konum,
        Vector2 boyut, string yazi, int punto, Color renk, out Text metin)
    {
        var panel = Panel(ad, ebeveyn, capa, capa, pivot, konum, boyut, renk);
        metin = Dolduran("Yazi", panel.transform, punto, TextAnchor.MiddleCenter, Color.white, 8f);
        metin.text = yazi;
        metin.fontStyle = FontStyle.Bold;
        return (RectTransform)panel.transform;
    }
}

// Fare tıklaması ya da dokunmayla çalışan düğmeler (EventSystem gerektirmez).
public class DugmeListesi
{
    readonly List<(RectTransform alan, Action is_)> dugmeler = new List<(RectTransform, Action)>();

    public void Ekle(RectTransform alan, Action is_) => dugmeler.Add((alan, is_));

    // Bu karede bir düğmeye basıldıysa işini yapar ve true döner.
    public bool Isle()
    {
        if (!DokunmatikGirdi.DokunmaBasladi) return false;
        foreach (var (alan, is_) in dugmeler)
        {
            if (alan == null || !alan.gameObject.activeInHierarchy) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(alan, DokunmatikGirdi.DokunmaYeri, null)) continue;
            is_();
            return true;
        }
        return false;
    }
}
