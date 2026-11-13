using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Açılış ekranı ve ana menü: profesör ile çırağın görseli, OYNA düğmesi, bölüm seçimi
// (kazanılan yıldızlarla) ve Vücut Kartı koleksiyonu.
// Fare, dokunma ve klavyeyle (BOŞLUK: oyna, B: bölümler, 1-7: bölüm, K: kartlar, ESC: geri) kullanılır.
public class AnaMenu : MonoBehaviour
{
    readonly DugmeListesi dugmeler = new DugmeListesi();
    readonly List<int> toplananKartlar = new List<int>();

    RectTransform guvenli;
    GameObject kartPaneli, bolumPaneli;
    Text kartBaslik, kartGovde, kartSayfa;
    Vector2Int sonEkran;
    int kartSirasi;

    GameObject anaIcerik;
    VucutHaritasi harita;Text muzikEtiketi;
    readonly DugmeListesi anaDugmeler = new DugmeListesi();
    readonly DugmeListesi bolumDugmeleri = new DugmeListesi();
    readonly DugmeListesi kartDugmeleri = new DugmeListesi();

    void Awake()
    {
        OyunMuzigi.Kur();
        Time.timeScale=1f; AudioListener.pause=false;
        var kamera=new GameObject("Kamera") { tag="MainCamera" }.AddComponent<Camera>();
        kamera.clearFlags=CameraClearFlags.SolidColor; kamera.backgroundColor=PremiumTema.Gece;
        kamera.gameObject.AddComponent<AudioListener>();
        var tuval=Cizim.Tuval("Menu",transform,out guvenli);
        PremiumTema.ArkaPlan(tuval.transform);
        guvenli.SetAsLastSibling();
        var home=PremiumTema.Alan("AnaIcerik",guvenli,0,0,1,1); anaIcerik=home.gameObject;
        var marka=PremiumTema.Metin(home,"Marka","BİR MİKRO DÜNYA MACERASI",.055f,.83f,.65f,.055f,22,PremiumTema.Turkuaz,true);PremiumTema.Uyarla(marka,new Rect(.07f,.88f,.86f,.05f));
        var title=PremiumTema.Metin(home,"Baslik","VÜCUT\nYOLCULUĞU",.05f,.45f,.70f,.35f,84,Color.white,true);PremiumTema.Uyarla(title,new Rect(.07f,.67f,.88f,.20f));
        var sub=PremiumTema.Metin(home,"AltBaslik","Rotanı seç. Keşfe başla.",.055f,.32f,.65f,.08f,28,PremiumTema.Soluk);PremiumTema.Uyarla(sub,new Rect(.07f,.55f,.86f,.06f));
        int siradaki=SiradakiBolum();var b=BolumListesi.Tumu[siradaki];
        var ordered=PremiumTema.Dugme(home,"SiraylaOyna","Sırayla oyna  ›",.055f,.13f,.27f,.12f,true);PremiumTema.Uyarla(ordered,new Rect(.07f,.245f,.86f,.085f));anaDugmeler.Ekle(ordered,()=>BolumuAc(b.sahne));
        var choose=PremiumTema.Dugme(home,"Bolumler","Bölüm seç",.35f,.13f,.24f,.12f);PremiumTema.Uyarla(choose,new Rect(.07f,.135f,.86f,.085f));anaDugmeler.Ekle(choose,()=>PanelAc(bolumPaneli));
        var archive=PremiumTema.Dugme(home,"Kartlar","Keşif kartları",.055f,.035f,.22f,.07f);PremiumTema.Uyarla(archive,new Rect(.07f,.04f,.42f,.065f));anaDugmeler.Ekle(archive,KartlariAc);
        var music=PremiumTema.Dugme(home,"Muzik","Müzik: "+(OyunMuzigi.Acik?"Açık":"Kapalı"),.79f,.88f,.165f,.08f);PremiumTema.Uyarla(music,new Rect(.52f,.04f,.41f,.065f));muzikEtiketi=music.GetComponentInChildren<Text>();anaDugmeler.Ekle(music,()=>{OyunMuzigi.Degistir();muzikEtiketi.text="Müzik: "+(OyunMuzigi.Acik?"Açık":"Kapalı");});
        BolumPaneliniKur(); KartPaneliniKur();harita=VucutHaritasi.Olustur(guvenli);
    }
    static int SiradakiBolum()
    {
        for(int i=0;i<BolumListesi.Tumu.Length;i++)if(PlayerPrefs.GetInt("bitti_"+BolumListesi.Tumu[i].no,0)==0)return i;
        return 0;
    }
    void PanelAc(GameObject panel){anaIcerik.SetActive(false);panel.SetActive(true);}
    void PanelKapat(GameObject panel){panel.SetActive(false);anaIcerik.SetActive(true);}
    RectTransform PanelOlustur(string ad,string baslik,string alt)
    {
        var r=PremiumTema.Pencere(guvenli,ad,new Vector2(1160,670),new Vector2(680,1200));
        PremiumTema.Metin(r,"Etiket",alt,.06f,.86f,.70f,.05f,21,PremiumTema.Turkuaz,true);
        PremiumTema.Metin(r,"Baslik",baslik,.06f,.75f,.85f,.10f,42,Color.white,true);
        return r;
    }
    void BolumPaneliniKur()
    {
        var panel=PanelOlustur("BolumPaneli","Yolculuk rotası","TÜM BÖLGELER AÇIK  /  İSTEDİĞİNİ SEÇ");bolumPaneli=panel.gameObject;
        for(int i=0;i<BolumListesi.Tumu.Length;i++){
            var b=BolumListesi.Tumu[i];int stars=Mathf.Clamp(BolumTemeli.EnIyiYildiz(b.no),0,3);
            var r=PremiumTema.Kart(panel,"Bolum"+b.no,.06f+(i%4)*.225f,i<4?.405f:.095f,.205f,.285f,PremiumTema.Panel);PremiumTema.Uyarla(r,new Rect(.065f+(i%2)*.45f,.565f-(i/2)*.168f,.42f,.152f));
            PremiumTema.BolgeResmi(r,b.no,.03f,.38f,.94f,.57f);
            PremiumTema.Metin(r,"Ad",b.no.ToString("00")+"  "+VucutHaritasi.KisaAd(b.no),.07f,.13f,.86f,.25f,24,Color.white,true);
            PremiumTema.Metin(r,"Durum",b.puanli?new string('★',stars)+new string('☆',3-stars):"GEZİNTİ",.07f,.035f,.86f,.12f,20,PremiumTema.Altin);
            string scene=b.sahne;bolumDugmeleri.Ekle(r,()=>BolumuAc(scene));
        }
        bolumDugmeleri.Ekle(PremiumTema.Dugme(panel,"Kapat","‹ Geri",.74f,.86f,.20f,.075f),()=>PanelKapat(bolumPaneli));bolumPaneli.SetActive(false);
    }
    Text[] kartBilgiler=new Text[3];
    void KartPaneliniKur()
    {
        var panel=PanelOlustur("KartPaneli","Keşif koleksiyonu","ARAŞTIRMA ARŞİVİ");kartPaneli=panel.gameObject;
        kartBaslik=PremiumTema.Metin(panel,"KartBaslik","",.07f,.64f,.86f,.09f,34,PremiumTema.Altin,true);
        kartGovde=PremiumTema.Metin(panel,"BosKoleksiyon","",.10f,.25f,.80f,.34f,28,Color.white);
        for(int i=0;i<3;i++){var row=PremiumTema.Kart(panel,"Bilgi"+i,.07f,.48f-i*.145f,.86f,.13f,PremiumTema.Panel);PremiumTema.Metin(row,"No",(i+1).ToString("00"),.03f,.15f,.08f,.70f,28,PremiumTema.Altin,true);kartBilgiler[i]=PremiumTema.Metin(row,"Metin","",.14f,.1f,.81f,.80f,27,Color.white);}
        kartSayfa=PremiumTema.Metin(panel,"Sayfa","",.27f,.07f,.46f,.08f,22,PremiumTema.Soluk);kartSayfa.alignment=TextAnchor.MiddleCenter;
        kartDugmeleri.Ekle(PremiumTema.Dugme(panel,"Onceki","‹",.07f,.055f,.16f,.09f),()=>KartDegistir(-1));kartDugmeleri.Ekle(PremiumTema.Dugme(panel,"Sonraki","›",.77f,.055f,.16f,.09f),()=>KartDegistir(1));
        kartDugmeleri.Ekle(PremiumTema.Dugme(panel,"Kapat","‹ Geri",.74f,.86f,.20f,.075f),()=>PanelKapat(kartPaneli));kartPaneli.SetActive(false);
    }

    void KartlariAc()
    {
        toplananKartlar.Clear();
        foreach (var b in BolumListesi.Tumu)
        {
            if (PlayerPrefs.HasKey("kart_baslik_" + b.no)) toplananKartlar.Add(b.no);
        }
        kartSirasi = 0;
        PanelAc(kartPaneli);
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
        foreach(var t in kartBilgiler)t.transform.parent.parent.gameObject.SetActive(toplananKartlar.Count>0);
        kartGovde.transform.parent.gameObject.SetActive(toplananKartlar.Count==0);
        if (toplananKartlar.Count == 0)
        {
            kartBaslik.text = "VÜCUT KARTLARI";
            kartGovde.text = "Henüz kart toplamadın.\n\nHer bölümün sonunda o organla ilgili üç ilginç bilgi içeren bir kart kazanırsın.";
            kartSayfa.text = $"0 / {toplam} kart";
            return;
        }
        int no = toplananKartlar[kartSirasi];
        kartBaslik.text = PlayerPrefs.GetString("kart_baslik_" + no);
        var lines=PlayerPrefs.GetString("kart_metin_"+no).Split(new[]{'•'},System.StringSplitOptions.RemoveEmptyEntries);for(int i=0;i<3;i++)kartBilgiler[i].text=i<lines.Length?lines[i].Trim():"";
        kartSayfa.text = $"{kartSirasi + 1} / {toplananKartlar.Count} kart";
    }

    static void BolumuAc(string sahne)
    {
        if (!Application.CanStreamedLevelBeLoaded(sahne))
        {
            Debug.LogError("Bölüm sahnesi derleme listesinde yok: " + sahne);
            return;
        }
        YuklemeEkrani.Ac(sahne);
    }

    void Update()
    {
        DokunmatikGirdi.Guncelle();

        var ekran = new Vector2Int(Screen.width, Screen.height);
        if (ekran != sonEkran)
        {
            sonEkran = ekran;
            Cizim.GuvenliAlaniUygula(guvenli);
        }

        if (YuklemeEkrani.Yukleniyor) return;
        if(harita.Acik){harita.Isle();return;}
        if ((kartPaneli.activeSelf ? kartDugmeleri : bolumPaneli.activeSelf ? bolumDugmeleri : anaDugmeler).Isle()) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kartPaneli.activeSelf)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame) PanelKapat(kartPaneli);
            else if (kb.leftArrowKey.wasPressedThisFrame) KartDegistir(-1);
            else if (kb.rightArrowKey.wasPressedThisFrame) KartDegistir(1);
            return;
        }
        if (bolumPaneli.activeSelf)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.bKey.wasPressedThisFrame) PanelKapat(bolumPaneli);
        }
        else if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
        {
            BolumuAc(BolumListesi.Tumu[SiradakiBolum()].sahne);
        }
        else if (kb.bKey.wasPressedThisFrame) PanelAc(bolumPaneli);
        if (kb.kKey.wasPressedThisFrame) KartlariAc();
        Key[] rakamlar = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7 };
        for (int i = 0; i < rakamlar.Length && i < BolumListesi.Tumu.Length; i++)
        {
            if (kb[rakamlar[i]].wasPressedThisFrame) BolumuAc(BolumListesi.Tumu[i].sahne);
        }
    }
}
