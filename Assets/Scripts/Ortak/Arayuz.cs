using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Oyun içi arayüz: gösterge, profesörün telsiz kutusu, Vücut Kartı, ekran karartma,
// bölüm sonu düğmeleri ve tablet için dokunmatik kontroller. Tüm bölümler aynı arayüzü kullanır.
// Her en-boy oranında ekranı doldurur; göstergeler çentik/yuvarlak köşe dışındaki güvenli alana yerleşir.
public class Arayuz : MonoBehaviour
{
    Text sayac, ipucu, telsizMetin, kartBaslik, kartGovde, kartAlt, ortaYazi, atesYazisi;
    GameObject telsizKutu, kart, ipucuPanel, dokunmatikKatman, dalisDugmesi, bitisDugmeleri;
    Image karartma;
    RectTransform guvenli, hedefIsareti, cubukTaban, cubukTopuz, atesDugmesi, duraklatDugmesi;
    Text devamYazisi;
    RectTransform devamDugmesi, tekrarDugmesi, menuDugmesi;
    Action devamIsi, tekrarIsi, menuIsi;

    readonly Queue<string> kuyruk = new Queue<string>();
    readonly DugmeListesi dugmeler = new DugmeListesi();
    Coroutine akis;
    Vector2Int sonEkran;

    public bool TelsizBos => akis == null && kuyruk.Count == 0;
    public bool DuraklatIstendi { get; private set; }       // duraklat düğmesine bu karede basıldı

    static Vector2 Orta => Cizim.Orta;

    public static Arayuz Olustur(Transform ebeveyn)
    {
        var tuval = Cizim.Tuval("Arayuz", ebeveyn, out RectTransform guvenli);
        var a = tuval.gameObject.AddComponent<Arayuz>();
        a.guvenli = guvenli;
        a.Kur();
        return a;
    }

    void Kur()
    {
        var panelRengi = new Color(0.04f, 0.08f, 0.18f, 0.75f);

        // Sol üst: gösterge
        var sayacPanel = Cizim.Panel("Sayac", guvenli, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -30f), new Vector2(400f, 84f), panelRengi);
        sayac = Cizim.Dolduran("Metin", sayacPanel.transform, 38, TextAnchor.MiddleCenter, Color.white, 10f);
        sayac.fontStyle = FontStyle.Bold;

        // Sağ üst: duraklat düğmesi ve (klavyede) kontrol ipucu
        duraklatDugmesi = Cizim.Dugme("Duraklat", guvenli, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-30f, -30f), new Vector2(84f, 84f), "II", 40, panelRengi, out _);
        dugmeler.Ekle(duraklatDugmesi, () => DuraklatIstendi = true);

        var ipucuResmi = Cizim.Panel("Ipucu", guvenli, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-130f, -30f), new Vector2(660f, 84f), panelRengi);
        ipucuPanel = ipucuResmi.gameObject;
        ipucu = Cizim.Dolduran("Metin", ipucuResmi.transform, 26, TextAnchor.MiddleCenter, new Color(0.85f, 0.92f, 1f), 10f);
        ipucu.text = "WASD / Oklar: Sür    BOŞLUK: Köpük    ESC: Duraklat";

        // Alt orta: telsiz kutusu
        var telsizPanel = Cizim.Panel("Telsiz", guvenli, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(1200f, 190f), panelRengi);
        telsizKutu = telsizPanel.gameObject;
        var etiket = Cizim.Yazi("Etiket", telsizPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -12f), new Vector2(-60f, 40f), 26, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.25f));
        etiket.text = "PROFESÖR  (telsiz)";
        etiket.fontStyle = FontStyle.Bold;
        telsizMetin = Cizim.Yazi("Mesaj", telsizPanel.transform, Vector2.zero, Vector2.one, Orta,
            new Vector2(0f, -22f), new Vector2(-60f, -70f), 32, TextAnchor.UpperLeft, Color.white);
        telsizKutu.SetActive(false);

        DokunmatikKatmaniKur();

        // Ekran dışında kalan hedefin yönünü gösteren işaret
        var isaret = Cizim.Panel("HedefIsareti", transform, Orta, Orta, Orta, Vector2.zero, new Vector2(30f, 30f),
            new Color(0.8f, 1f, 0.35f, 0.9f));
        hedefIsareti = (RectTransform)isaret.transform;
        hedefIsareti.localRotation = Quaternion.Euler(0f, 0f, 45f);
        isaret.gameObject.SetActive(false);

        // Orta: Vücut Kartı
        var cerceve = Cizim.Panel("VucutKarti", guvenli, Orta, Orta, Orta, Vector2.zero, new Vector2(1000f, 680f),
            new Color(1f, 0.6f, 0.15f));
        kart = cerceve.gameObject;
        var ic = Cizim.Panel("Ic", cerceve.transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, new Vector2(-24f, -24f),
            new Color(1f, 0.97f, 0.88f));
        kartBaslik = Cizim.Yazi("Baslik", ic.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(-60f, 80f), 56, TextAnchor.MiddleCenter, new Color(0.8f, 0.25f, 0.3f));
        kartBaslik.fontStyle = FontStyle.Bold;
        kartGovde = Cizim.Yazi("Govde", ic.transform, Vector2.zero, Vector2.one, Orta,
            new Vector2(0f, -20f), new Vector2(-100f, -240f), 34, TextAnchor.UpperLeft, new Color(0.2f, 0.12f, 0.1f));
        kartGovde.lineSpacing = 1.15f;
        kartAlt = Cizim.Yazi("Alt", ic.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(-60f, 50f), 28, TextAnchor.MiddleCenter, new Color(0.45f, 0.35f, 0.3f));
        kart.SetActive(false);

        // En üstte: tüm ekranı kaplayan karartma, orta yazı ve bölüm sonu düğmeleri
        karartma = Cizim.Panel("Karartma", transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0f));
        ortaYazi = Cizim.Yazi("OrtaYazi", transform, Vector2.zero, Vector2.one, Orta, new Vector2(0f, 40f), new Vector2(-200f, -200f),
            60, TextAnchor.MiddleCenter, Color.white);
        ortaYazi.fontStyle = FontStyle.Bold;
        ortaYazi.lineSpacing = 1.2f;

        var satir = Cizim.Kutu("BitisDugmeleri", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 70f), new Vector2(1200f, 110f));
        bitisDugmeleri = satir.gameObject;
        var yesil = new Color(0.15f, 0.55f, 0.3f, 0.95f);
        var mavi = new Color(0.12f, 0.25f, 0.5f, 0.95f);
        devamDugmesi = Cizim.Dugme("Devam", satir, Orta, Orta, new Vector2(-400f, 0f), new Vector2(360f, 100f), "DEVAM", 40, yesil, out devamYazisi);
        tekrarDugmesi = Cizim.Dugme("Tekrar", satir, Orta, Orta, new Vector2(0f, 0f), new Vector2(360f, 100f), "TEKRAR OYNA", 36, mavi, out _);
        menuDugmesi = Cizim.Dugme("Menu", satir, Orta, Orta, new Vector2(400f, 0f), new Vector2(360f, 100f), "ANA MENÜ", 36, mavi, out _);
        dugmeler.Ekle(devamDugmesi, () => devamIsi?.Invoke());
        dugmeler.Ekle(tekrarDugmesi, () => tekrarIsi?.Invoke());
        dugmeler.Ekle(menuDugmesi, () => menuIsi?.Invoke());
        bitisDugmeleri.SetActive(false);
    }

    // Tablet kontrolleri: solda sürüş çubuğu, sağda köpük ve (gerekirse) dalış düğmesi
    void DokunmatikKatmaniKur()
    {
        var katman = Cizim.Kutu("Dokunmatik", transform, Vector2.zero, Vector2.one, Orta, Vector2.zero, Vector2.zero);
        dokunmatikKatman = katman.gameObject;
        var soluk = new Color(1f, 1f, 1f, 0.16f);

        cubukTaban = (RectTransform)Cizim.Panel("CubukTaban", katman, Vector2.zero, Vector2.zero, Orta,
            new Vector2(230f, 230f), new Vector2(300f, 300f), soluk).transform;
        cubukTaban.GetComponent<Image>().sprite = Cizim.Daire;
        cubukTopuz = (RectTransform)Cizim.Panel("CubukTopuz", cubukTaban, Orta, Orta, Orta, Vector2.zero,
            new Vector2(130f, 130f), new Color(1f, 1f, 1f, 0.4f)).transform;
        cubukTopuz.GetComponent<Image>().sprite = Cizim.Daire;

        atesDugmesi = (RectTransform)Cizim.Panel("Ates", katman, new Vector2(1f, 0f), new Vector2(1f, 0f), Orta,
            new Vector2(-200f, 300f), new Vector2(230f, 230f), new Color(1f, 1f, 1f, 0.2f)).transform;
        atesDugmesi.GetComponent<Image>().sprite = Cizim.Daire;
        atesYazisi = Cizim.Dolduran("Yazi", atesDugmesi, 34, TextAnchor.MiddleCenter, Color.white, 6f);
        atesYazisi.text = "KÖPÜK";
        atesYazisi.fontStyle = FontStyle.Bold;

        var dalis = Cizim.Panel("Dalis", katman, new Vector2(1f, 0f), new Vector2(1f, 0f), Orta,
            new Vector2(-440f, 180f), new Vector2(200f, 200f), new Color(0.4f, 0.75f, 1f, 0.3f));
        dalis.sprite = Cizim.Daire;
        dalisDugmesi = dalis.gameObject;
        var dalisYazisi = Cizim.Dolduran("Yazi", dalis.transform, 34, TextAnchor.MiddleCenter, Color.white, 6f);
        dalisYazisi.text = "DAL";
        dalisYazisi.fontStyle = FontStyle.Bold;
        dalisDugmesi.SetActive(false);

        DokunmatikGirdi.dalisAlani = null;
        DokunmatikGirdi.digerDugme = p => RectTransformUtility.RectangleContainsScreenPoint(duraklatDugmesi, p, null);
        dokunmatikKatman.SetActive(false);
    }

    void OnDestroy()
    {
        DokunmatikGirdi.dalisAlani = null;
        DokunmatikGirdi.digerDugme = null;
    }

    void Update()
    {
        DokunmatikGirdi.Guncelle();

        // Ekran boyutu ya da yönü değişirse güvenli alanı yeniden uygula
        var ekran = new Vector2Int(Screen.width, Screen.height);
        if (ekran != sonEkran)
        {
            sonEkran = ekran;
            Cizim.GuvenliAlaniUygula(guvenli);
        }

        // Dokunmatik kontroller yalnızca dokunmatik kullanılırken ve oyun sırasında görünür
        bool goster = DokunmatikGirdi.Etkin && kontrollerAcik;
        if (dokunmatikKatman.activeSelf != goster) dokunmatikKatman.SetActive(goster);
        ipucuPanel.SetActive(!DokunmatikGirdi.Etkin);
        if (!goster) return;

        // Çubuk, parmağın dokunduğu yerde belirir; bırakılınca köşesine döner
        float menzil = Mathf.Max(60f, Screen.height * 0.13f);
        if (DokunmatikGirdi.CubukTutuluyor) cubukTaban.position = DokunmatikGirdi.CubukMerkezi;
        else cubukTaban.anchoredPosition = new Vector2(230f, 230f);
        cubukTopuz.position = cubukTaban.position + (Vector3)(DokunmatikGirdi.Cubuk * menzil);
        atesDugmesi.localScale = Vector3.one * (DokunmatikGirdi.Ates ? 0.9f : 1f);
    }

    bool kontrollerAcik;

    // ---------- Dışarıya açık işlevler ----------

    // Bu karede bir ekran düğmesine basıldıysa işini yapar ve true döner.
    public bool DugmeleriIsle()
    {
        DuraklatIstendi = false;
        DokunmatikGirdi.Guncelle();
        return dugmeler.Isle();
    }

    // Oyun sırasında dokunmatik kontrolleri (çubuk, köpük) gösterir/gizler.
    public void Kontroller(bool acik) => kontrollerAcik = acik;

    // Dalış düğmesi (yalnızca mesanede)
    public void DalisDugmesi(bool goster)
    {
        dalisDugmesi.SetActive(goster);
        var alan = (RectTransform)dalisDugmesi.transform;
        DokunmatikGirdi.dalisAlani = goster
            ? p => RectTransformUtility.RectangleContainsScreenPoint(alan, p, null)
            : (Func<Vector2, bool>)null;
    }

    // Köpük düğmesinin üstündeki yazı (ör. kalpte "ATIL")
    public void AtesEtiketi(string yazi) => atesYazisi.text = yazi;

    // Sol üstteki gösterge (mikrop sayısı, kalkan, kalan yol...). vurgu: kısa bir büyüyüp küçülme.
    public void Gosterge(string metin, bool vurgu = false)
    {
        sayac.text = metin;
        if (vurgu) StartCoroutine(SayacVurgusu());
    }

    // Sağ üstteki kontrol ipucu (yalnızca klavyeyle oynarken görünür)
    public void Ipucu(string metin) => ipucu.text = metin;

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
            Ses.KonusmayiKes();
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

    // Ekranı verilen renge (varsayılan siyah) kapatır.
    public IEnumerator Karart(float sure, Color? renk = null)
    {
        Color bas = karartma.color;
        Color hedef = renk ?? Color.black;
        hedef.a = 1f;
        if (bas.a < 0.01f) bas = new Color(hedef.r, hedef.g, hedef.b, 0f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / sure)
        {
            karartma.color = Color.Lerp(bas, hedef, t);
            yield return null;
        }
        karartma.color = hedef;
    }

    public IEnumerator Aydinlat(float sure)
    {
        Color bas = karartma.color;
        for (float t = 0f; t < 1f; t += Time.deltaTime / sure)
        {
            karartma.color = new Color(bas.r, bas.g, bas.b, bas.a * (1f - t));
            yield return null;
        }
        karartma.color = Color.clear;
    }

    public void OrtaYazi(string metin) => ortaYazi.text = metin;

    // Bölüm sonu düğmeleri. İşi null olan düğme gösterilmez.
    public void BitisDugmeleri(Action devam, Action tekrar, Action menu, string devamEtiketi = "DEVAM")
    {
        devamIsi = devam;
        tekrarIsi = tekrar;
        menuIsi = menu;
        devamYazisi.text = devamEtiketi;
        devamDugmesi.gameObject.SetActive(devam != null);
        tekrarDugmesi.gameObject.SetActive(tekrar != null);
        menuDugmesi.gameObject.SetActive(menu != null);
        bitisDugmeleri.SetActive(true);
    }

    // ---------- İç işleyiş ----------

    IEnumerator TelsizAkisi()
    {
        telsizKutu.SetActive(true);
        while (kuyruk.Count > 0)
        {
            string mesaj = kuyruk.Dequeue();
            Ses.Cal(Ses.Tur.Telsiz, 0.6f);

            // Seslendirme varsa yazı konuşmayla aynı hızda akar ve konuşma bitene kadar ekranda kalır
            AudioClip konusma = Seslendirme.Bul(mesaj);
            float yazmaHizi = 45f, bekleme = 2.2f + mesaj.Length * 0.03f;
            if (konusma != null)
            {
                yield return new WaitForSeconds(0.25f);
                Ses.Konus(konusma);
                yazmaHizi = Mathf.Max(20f, mesaj.Length / (konusma.length * 0.85f));
                bekleme = konusma.length * 0.15f + 0.6f;
            }

            for (float harf = 0f; harf < mesaj.Length; harf += Time.deltaTime * yazmaHizi)
            {
                telsizMetin.text = mesaj.Substring(0, (int)harf);
                yield return null;
            }
            telsizMetin.text = mesaj;
            yield return new WaitForSeconds(bekleme);
        }
        telsizKutu.SetActive(false);
        akis = null;
    }
}
