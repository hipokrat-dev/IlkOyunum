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
    protected enum Durum { Sinematik, Baslik, Oyun, Kart, Cikis, Bitti, Kaybetti }

    public static BolumTemeli Ornek { get; private set; }
    public static bool kayitKapali;           // otomatik testler oyuncunun yıldızlarını değiştirmesin
    public string DurumAdi => durum.ToString();

    protected Arayuz arayuz;
    protected AracKontrol arac;
    protected TakipKamera kamera;
    protected Camera kameraGozu;
    protected Durum durum = Durum.Baslik;
    protected float sure;                     // oyunun başından Vücut Kartı'na kadar geçen süre

    public float Can {get;private set;}=100f;
    float hasarBekleme;
    float baslangicZamani;
    bool duraklatildi, kartHazir;
    bool haritaOncesiKontrol,haritaOncesiSes;float haritaOncesiZaman;

    // ---------- Bölümün tanımlaması gerekenler ----------

    protected abstract int BolumNo { get; }
    protected abstract string BolumAdi { get; }
    protected abstract string SonrakiDurak { get; }
    protected virtual string SonrakiSahne => null;        // derleme listesindeki sahne adı; yoksa null
    protected virtual bool OrtamSesi => true;             // arka planda kalp atışı çalsın mı
    // Bölüm sonu ekranının yazıları (final bölümü kendi özetini gösterir)
    protected virtual string BitisBasligi => $"BÖLÜM {BolumNo} TAMAMLANDI";
    protected virtual string SonrakiSatiri => $"Sıradaki durak: {SonrakiDurak}";
    protected virtual string DevamYazisi => "BOŞLUK: Devam   •   R: Tekrar   •   M: Ana menü";
    protected virtual string DevamDugmesi => "DEVAM";
    protected virtual bool Puanli => true;                // false: gezinti bölümü, yıldız/istatistik yok
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
        OrganDetaylari.Kur(gameObject, BolumNo);
        if(MobilKalite.Mobil) gameObject.AddComponent<MobilIsikButcesi>();
        kameraGozu = kamera.GetComponent<Camera>();
        arayuz = Arayuz.Olustur(transform);
        arayuz.BolumAyarla(BolumNo);
        arayuz.CanGuncelle(Can);
        OyunMuzigi.Kur();
        ArayuzHazir();

        arac.kontrolAcik = false;
        arayuz.Perde(0.55f);
        if(kayitKapali) arayuz.GorevGoster(BolumNo, BolumAdi, Basla);
        else { durum=Durum.Sinematik; StartCoroutine(Giris()); }
    }

    IEnumerator Giris()
    {
        yield return SinemaGiris.Oynat(SceneManager.GetActiveScene().name,transform);
        durum=Durum.Baslik;
        arayuz.GorevGoster(BolumNo,BolumAdi,Basla);
    }

    protected virtual void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void Basla()
    {
        if (durum != Durum.Baslik) return;
        arayuz.GorevGizle();
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
        if(YuklemeEkrani.Yukleniyor)return;
        if(arayuz.HaritaAcik){arayuz.HaritaIsle();return;}
        var kb = Keyboard.current;
        var kol = Gamepad.current;
        bool dugme = arayuz.DugmeleriIsle();
        if(arayuz.MenuIstendi){Time.timeScale=1;AudioListener.pause=false;SahneAc(AnaMenuSahnesi);return;}
        if(arayuz.HaritaIstendi || (kb!=null && kb.hKey.wasPressedThisFrame && durum!=Durum.Sinematik && durum!=Durum.Cikis)){HaritayiAc();return;}
        if(arayuz.DevamIstendi){Duraklat(false);return;}
        bool tus = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                   (kol != null && kol.buttonSouth.wasPressedThisFrame);
        bool onay = tus || arayuz.KartDevamIstendi;       // ekrana dokunmak da onaydır
        bool duraklatTusu = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                            (kol != null && kol.startButton.wasPressedThisFrame) || arayuz.DuraklatIstendi;

        if (durum == Durum.Oyun && duraklatTusu) Duraklat(!duraklatildi);
        else if (duraklatildi && onay) Duraklat(false);
        arayuz.Kontroller(durum == Durum.Oyun && !duraklatildi && arac.kontrolAcik);
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

            case Durum.Kaybetti:
                if(kb!=null&&(kb.rKey.wasPressedThisFrame||kb.enterKey.wasPressedThisFrame))SahneAc(null);
                else if(kb!=null&&kb.mKey.wasPressedThisFrame)SahneAc(AnaMenuSahnesi);
                break;
            case Durum.Bitti:
                // Dokunmatikte ekrandaki düğmeler kullanılır; klavyede kısayollar
                if (tus && SonrakiVar) SahneAc(SonrakiSahne);
                else if ((kb != null && kb.rKey.wasPressedThisFrame) || (kol != null && kol.buttonNorth.wasPressedThisFrame)) SahneAc(null);
                else if (kb != null && kb.mKey.wasPressedThisFrame && MenuVar) SahneAc(AnaMenuSahnesi);
                break;
        }
    }

    // Darbeler ortak bekleme süresini paylaşır; asit en fazla 0.75 saniyede bir hasar verir.
    public bool HasarAl(float miktar,string neden,float aralik=.9f)
    {
        if(durum!=Durum.Oyun || duraklatildi || arayuz.HaritaAcik || Time.timeScale==0 || Time.time<hasarBekleme || miktar<=0)return false;
        hasarBekleme=Time.time+aralik;Can=Mathf.Max(0,Can-miktar);arayuz.CanGuncelle(Can);arayuz.HasarGoster(neden,Mathf.RoundToInt(miktar));
        kamera.sarsinti=Mathf.Max(kamera.sarsinti,.16f);
        if(Can<=0){
            durum=Durum.Kaybetti;arac.kontrolAcik=false;arac.Durdur();arayuz.Kontroller(false);arayuz.HedefIsareti(null);Ses.KonusmayiKes();arayuz.TelsiziKapat();
            arayuz.Perde(.65f);arayuz.OrtaYazi("Kapsül hasar aldı\n\nCan tükendi. Bölgeyi yeniden deneyebilirsin.");
            arayuz.BitisDugmeleri(null,()=>SahneAc(null),()=>SahneAc(AnaMenuSahnesi));
        }
        return true;
    }

    public const string AnaMenuSahnesi = "AnaMenu";

    static bool MenuVar => Application.CanStreamedLevelBeLoaded(AnaMenuSahnesi);

    // null: bu bölümü yeniden yükle
    static void SahneAc(string sahne) => YuklemeEkrani.Ac(sahne ?? SceneManager.GetActiveScene().name);

    bool SonrakiVar => !string.IsNullOrEmpty(SonrakiSahne) && Application.CanStreamedLevelBeLoaded(SonrakiSahne);

    void Duraklat(bool dur)
    {
        duraklatildi = dur;
        Time.timeScale = dur ? 0f : 1f;
        AudioListener.pause = dur;
        arayuz.Perde(dur ? 0.55f : 0f);
        arayuz.DuraklatGoster(dur);
        arac.kontrolAcik=!dur;
    }

    public void HaritayiAc()
    {
        if(arayuz.HaritaAcik || durum==Durum.Sinematik || durum==Durum.Cikis)return;
        haritaOncesiKontrol=arac.kontrolAcik;haritaOncesiSes=AudioListener.pause;haritaOncesiZaman=Time.timeScale;
        arac.kontrolAcik=false;Time.timeScale=0;AudioListener.pause=true;arayuz.Kontroller(false);
        arayuz.HaritayiAc(()=>{Time.timeScale=haritaOncesiZaman;AudioListener.pause=haritaOncesiSes;arac.kontrolAcik=haritaOncesiKontrol;});
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
        if (!kayitKapali)
        {
            // Kart koleksiyona eklenir (ana menüden bakılabilir)
            PlayerPrefs.SetString("kart_baslik_" + BolumNo, KartBasligi);
            PlayerPrefs.SetString("kart_metin_" + BolumNo, KartMetni);
            PlayerPrefs.Save();
        }
        yield return new WaitForSeconds(1.2f);

        arayuz.KartAltYazi("Devam etmek için ekrana dokun ya da BOŞLUK'a bas");
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
        if (Puanli && !kayitKapali && yildiz > EnIyiYildiz(BolumNo))
        {
            PlayerPrefs.SetInt("yildiz_" + BolumNo, yildiz);
            PlayerPrefs.Save();
        }

        if (!kayitKapali)
        {
            PlayerPrefs.SetInt("bitti_" + BolumNo, 1);
            PlayerPrefs.Save();
        }
        string devam = DokunmatikGirdi.Etkin ? "" : SonrakiVar ? DevamYazisi : "R: Tekrar   •   M: Ana menü";
        arayuz.BitisDugmeleri(SonrakiVar ? () => SahneAc(SonrakiSahne) : (System.Action)null, () => SahneAc(null),
            MenuVar ? () => SahneAc(AnaMenuSahnesi) : (System.Action)null, DevamDugmesi);
        string puan = Puanli
            ? $"<color=#FFD040><size=90>{Yildizlar(yildiz)}</size></color>\n<size=36>{SonucSatiri()}</size>\n"
            : "";
        arayuz.OrtaYazi($"{BitisBasligi}\n{puan}\n<size=44>{SonrakiSatiri}</size>\n<size=34>{devam}</size>");
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
        sonda.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        sonda.resolution = 128;
        sonda.hdr = true;
        sonda.RenderProbe();
        return sonda;
    }
}
