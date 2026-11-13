using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Tüm bölümlerin ortak akışı:
// başlangıç ekranı -> oyun -> Vücut Kartı -> çıkış sahnesi -> yıldızlı bölüm sonu (-> sonraki bölüm).
// Duraklatma, puanın kaydı ve bölümler arası geçiş de burada. Her bölüm yalnızca kendine özgü kısmı yazar.
public abstract class BolumTemeli : MonoBehaviour
{
    protected enum Durum { Baslik, Oyun, Kart, Cikis, Bitti }

    public static BolumTemeli Ornek { get; private set; }
    public string DurumAdi => durum.ToString();

    protected Arayuz arayuz;
    protected AracKontrol arac;
    protected TakipKamera kamera;
    protected Camera kameraGozu;
    protected Durum durum = Durum.Baslik;
    protected float sure;                     // oyunun başından Vücut Kartı'na kadar geçen süre

    float baslangicZamani;
    bool duraklatildi, kartHazir;

    // ---------- Bölümün tanımlaması gerekenler ----------

    protected abstract int BolumNo { get; }
    protected abstract string BolumAdi { get; }
    protected abstract string SonrakiDurak { get; }
    protected virtual string SonrakiSahne => null;        // derleme listesindeki sahne adı; yoksa null
    protected virtual bool OrtamSesi => true;             // arka planda kalp atışı çalsın mı
    protected abstract string KartBasligi { get; }
    protected abstract string KartMetni { get; }

    protected abstract void SahneyiKur();                 // arac ve kamera burada oluşturulmalı
    protected virtual void ArayuzHazir() { }              // gösterge/ipucu yazılarını ayarla
    protected abstract void OyunBasladi();                // ilk telsiz mesajları
    protected abstract void OyunGuncelle();               // her karede (yalnızca oyun sırasında)
    protected abstract IEnumerator CikisSahnesi();        // karttan sonra, karartmadan önce
    protected abstract int Yildiz();                      // 1..3
    protected abstract string SonucSatiri();              // bölüm sonu istatistik satırı

    // ---------- Akış ----------

    protected virtual void Awake()
    {
        Ornek = this;
        // Parlak yüzeyler gökyüzünü değil, vücudun karanlık içini yansıtsın.
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = Sekil.DuzKupHarita(new Color(0.16f, 0.06f, 0.06f));

        SahneyiKur();
        kameraGozu = kamera.GetComponent<Camera>();
        arayuz = Arayuz.Olustur(transform);
        ArayuzHazir();

        arac.kontrolAcik = false;
        arayuz.Perde(0.55f);
        int enIyi = EnIyiYildiz(BolumNo);
        string rekor = enIyi > 0 ? "\n<color=#FFD040><size=50>" + Yildizlar(enIyi) + "</size></color>" : "";
        arayuz.OrtaYazi($"VÜCUT YOLCULUĞU\n<size=44>Bölüm {BolumNo}: {BolumAdi}</size>{rekor}\n\n" +
                        "<size=34>Başlamak için BOŞLUK tuşuna bas</size>");
    }

    protected virtual void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void Basla()
    {
        if (durum != Durum.Baslik) return;
        durum = Durum.Oyun;
        baslangicZamani = Time.time;
        arac.kontrolAcik = true;
        arayuz.Perde(0f);
        arayuz.OrtaYazi("");
        Ses.Cal(Ses.Tur.Tik);
        if (OrtamSesi) Ses.OrtamBaslat();
        OyunBasladi();
    }

    void Update()
    {
        var kb = Keyboard.current;
        var kol = Gamepad.current;
        bool onay = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                    (kol != null && kol.buttonSouth.wasPressedThisFrame);

        if (durum == Durum.Oyun && ((kb != null && kb.escapeKey.wasPressedThisFrame) ||
                                    (kol != null && kol.startButton.wasPressedThisFrame)))
        {
            Duraklat(!duraklatildi);
        }
        if (duraklatildi) return;

        switch (durum)
        {
            case Durum.Baslik:
                if (onay) Basla();
                break;

            case Durum.Oyun:
                OyunGuncelle();
                break;

            case Durum.Kart:
                if (onay) KartiKapat();
                break;

            case Durum.Bitti:
                if (onay && SonrakiVar)
                {
                    SceneManager.LoadScene(SonrakiSahne);
                }
                else if ((kb != null && kb.rKey.wasPressedThisFrame) || (kol != null && kol.buttonNorth.wasPressedThisFrame))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                break;
        }
    }

    bool SonrakiVar => !string.IsNullOrEmpty(SonrakiSahne) && Application.CanStreamedLevelBeLoaded(SonrakiSahne);

    void Duraklat(bool dur)
    {
        duraklatildi = dur;
        Time.timeScale = dur ? 0f : 1f;
        AudioListener.pause = dur;
        arayuz.Perde(dur ? 0.55f : 0f);
        arayuz.OrtaYazi(dur ? "DURAKLATILDI\n\n<size=34>Devam etmek için ESC tuşuna bas</size>" : "");
    }

    // Bölümün hedefi tamamlanınca çağrılır: tebrik, Vücut Kartı ve bölüm sonu akışını başlatır.
    protected void BolumuBitir(string tebrik)
    {
        if (durum != Durum.Oyun) return;
        durum = Durum.Kart;
        sure = Time.time - baslangicZamani;
        StartCoroutine(KartAkisi(tebrik));
    }

    IEnumerator KartAkisi(string tebrik)
    {
        arayuz.HedefIsareti(null);
        arayuz.Telsiz(tebrik, true);
        yield return new WaitForSeconds(2.5f);

        arac.kontrolAcik = false;
        Ses.Cal(Ses.Tur.Basari);
        arayuz.KartGoster(KartBasligi, KartMetni, "");
        yield return new WaitForSeconds(1.2f);

        arayuz.KartAltYazi("Devam etmek için BOŞLUK tuşuna bas");
        kartHazir = true;
    }

    public void KartiKapat()
    {
        if (durum != Durum.Kart || !kartHazir) return;
        durum = Durum.Cikis;
        arayuz.KartGizle();
        Ses.Cal(Ses.Tur.Tik);
        StartCoroutine(Bitis());
    }

    IEnumerator Bitis()
    {
        yield return CikisSahnesi();
        yield return arayuz.Karart(1.2f);
        arac.Durdur();
        durum = Durum.Bitti;

        int yildiz = Mathf.Clamp(Yildiz(), 1, 3);
        if (yildiz > EnIyiYildiz(BolumNo))
        {
            PlayerPrefs.SetInt("yildiz_" + BolumNo, yildiz);
            PlayerPrefs.Save();
        }

        string devam = SonrakiVar
            ? "Devam etmek için BOŞLUK   •   Tekrar oynamak için R"
            : "Tekrar oynamak için R tuşuna bas";
        arayuz.OrtaYazi($"BÖLÜM {BolumNo} TAMAMLANDI\n<color=#FFD040><size=90>{Yildizlar(yildiz)}</size></color>\n" +
                        $"<size=36>{SonucSatiri()}</size>\n\nSıradaki durak: {SonrakiDurak}\n<size=34>{devam}</size>");
    }

    // ---------- Yardımcılar ----------

    protected string SureYazisi => $"Süre: {(int)sure / 60}:{(int)sure % 60:00}";

    public static int EnIyiYildiz(int bolumNo) => PlayerPrefs.GetInt("yildiz_" + bolumNo, 0);

    static string Yildizlar(int dolu) => new string('★', dolu) + new string('☆', 3 - dolu);

    // Çevreyi bir kez çekip parlak yüzeylere yansıtan sonda. Sahne değişince RenderProbe() ile yenilenir.
    protected static ReflectionProbe YansimaSondasi(Vector3 konum, Vector3 boyut)
    {
        var sonda = new GameObject("YansimaSondasi").AddComponent<ReflectionProbe>();
        sonda.transform.position = konum;
        sonda.size = boyut;
        sonda.mode = ReflectionProbeMode.Realtime;
        sonda.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        sonda.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        sonda.resolution = 128;
        sonda.hdr = true;
        sonda.RenderProbe();
        return sonda;
    }
}
