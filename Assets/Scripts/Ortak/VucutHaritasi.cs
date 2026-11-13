using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VucutHaritasi : MonoBehaviour
{
    public bool Acik => gameObject.activeSelf;
    public int Bulunulan {get;private set;}
    public int Secilen {get;private set;}
    readonly DugmeListesi dugmeler=new DugmeListesi();
    Text ad, durum, aciklama, gorev, konum;
    Image[] satirlar=new Image[7];
    RectTransform[] noktalar=new RectTransform[7];
    RectTransform konumIsigi;
    public static readonly Vector2[] OrganKonum={Vector2.zero,new Vector2(.5f,.902f),new Vector2(.54f,.67f),new Vector2(.50f,.577f),new Vector2(.54f,.745f),new Vector2(.43f,.755f),new Vector2(.425f,.64f),new Vector2(.50f,.496f)};
    Action kapat;
    bool menu;
    string aktifGorev;
    public static VucutHaritasi Olustur(Transform parent)
    {
        var root=PremiumTema.Pencere(parent,"VucutHaritasi",new Vector2(1140,680),new Vector2(680,1200));
        var map=root.gameObject.AddComponent<VucutHaritasi>();map.Kur(root);root.gameObject.SetActive(false);return map;
    }
    void Kur(RectTransform root)
    {
        PremiumTema.Metin(root,"Etiket","KAPSÜL NAVİGASYONU",.06f,.87f,.65f,.045f,21,PremiumTema.Turkuaz,true);
        PremiumTema.Metin(root,"Baslik","Neredeyim?",.06f,.76f,.70f,.10f,44,Color.white,true);
        dugmeler.Ekle(PremiumTema.Dugme(root,"HaritayiKapat","×",.77f,.86f,.17f,.075f),Kapat);
        var body=PremiumTema.Kart(root,"VucutPaneli",.06f,.10f,.36f,.62f,PremiumTema.Gece);PremiumTema.Uyarla(body,new Rect(.07f,.445f,.86f,.30f));
        var drawing=PremiumTema.Alan("InsanVucudu",body,.01f,.01f,.98f,.98f);
        var fit=drawing.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=(2f/3f)/.58f;
        var photo=drawing.gameObject.AddComponent<RawImage>();photo.texture=Resources.Load<Texture2D>("Arayuz/Konsol/vucut-atlasi");photo.uvRect=new Rect(0,.42f,1,.58f);photo.raycastTarget=false;
        konumIsigi=PremiumTema.Alan("KonumIsigi",drawing,.45f,.45f,.10f,.07f);var glow=konumIsigi.gameObject.AddComponent<Image>();glow.sprite=Cizim.Daire;glow.color=new Color(1,.65f,.12f,.35f);glow.raycastTarget=false;konumIsigi.gameObject.AddComponent<KonumNabzi>();
        for(int i=0;i<7;i++){
            int no=i+1;var point=HaritaKonumu(no);
            var n=Cizim.Kutu("Organ"+no,drawing,point,point,Cizim.Orta,Vector2.zero,new Vector2(25,25));
            var im=n.gameObject.AddComponent<KonsolYuzeyi>();im.daire=true;im.color=PremiumTema.Turkuaz;
            var label=Cizim.Dolduran("No",n,17,TextAnchor.MiddleCenter,PremiumTema.Gece,0);label.text=no.ToString();label.fontStyle=FontStyle.Bold;
            noktalar[i]=n;dugmeler.Ekle(n,()=>Sec(no));
        }
        konum=PremiumTema.Metin(root,"Bulunulan","",.06f,.045f,.39f,.045f,21,PremiumTema.Altin,true);PremiumTema.Uyarla(konum,new Rect(.07f,.405f,.86f,.035f));
        var info=PremiumTema.Kart(root,"OrganBilgisi",.46f,.35f,.48f,.37f,PremiumTema.Panel);PremiumTema.Uyarla(info,new Rect(.07f,.16f,.86f,.235f));
        durum=PremiumTema.Metin(info,"Durum","",.06f,.82f,.88f,.13f,20,PremiumTema.Turkuaz,true);
        ad=PremiumTema.Metin(info,"OrganAdi","",.06f,.61f,.88f,.20f,34,Color.white,true);
        aciklama=PremiumTema.Metin(info,"Bilgi","",.06f,.32f,.88f,.26f,25,PremiumTema.Soluk);
        gorev=PremiumTema.Metin(info,"Gorev","",.06f,.055f,.88f,.25f,24,Color.white);
        for(int i=0;i<7;i++){
            var chip=PremiumTema.Kart(root,"Durak"+(i+1),.46f+i*.069f,.27f,.060f,.05f,PremiumTema.Panel);PremiumTema.Uyarla(chip,new Rect(.07f+i*.124f,.11f,.115f,.035f));
            var number=PremiumTema.Metin(chip,"No",(i+1).ToString("00"),0,0,1,1,20,Color.white,true);number.alignment=TextAnchor.MiddleCenter;satirlar[i]=chip.GetComponent<Image>();
        }
        var prev=PremiumTema.Dugme(root,"OncekiOrgan","‹",.46f,.10f,.105f,.12f);PremiumTema.Uyarla(prev,new Rect(.07f,.035f,.17f,.06f));dugmeler.Ekle(prev,()=>Sec(Secilen==1?7:Secilen-1));
        var now=PremiumTema.Dugme(root,"KonumumaDon","Konumumu göster",.58f,.10f,.24f,.12f,true);PremiumTema.Uyarla(now,new Rect(.265f,.035f,.47f,.06f));dugmeler.Ekle(now,()=>Sec(Bulunulan));
        var next=PremiumTema.Dugme(root,"SonrakiOrgan","›",.835f,.10f,.105f,.12f);PremiumTema.Uyarla(next,new Rect(.76f,.035f,.17f,.06f));dugmeler.Ekle(next,()=>Sec(Secilen==7?1:Secilen+1));
    }
    static Vector2 HaritaKonumu(int no){var p=OrganKonum[no];return new Vector2(p.x,(p.y-.42f)/.58f);}
    public static string KisaAd(int no){string[] adlar={"Ağız","Yemek borusu / Mide","Bağırsak","Kalp / Damarlar","Akciğer","Böbrek","Mesane"};return adlar[Mathf.Clamp(no-1,0,6)];}
    public void Ac(int no,string hedef,Action geri,bool anaMenu=false)
    {
        Bulunulan=Mathf.Clamp(no,1,7);aktifGorev=hedef;kapat=geri;menu=anaMenu;
        konumIsigi.anchorMin=konumIsigi.anchorMax=HaritaKonumu(Bulunulan);var size=new[]{Vector2.zero,new Vector2(42,32),new Vector2(65,52),new Vector2(76,70),new Vector2(48,58),new Vector2(115,85),new Vector2(42,56),new Vector2(44,36)}[Bulunulan];konumIsigi.sizeDelta=size;gameObject.SetActive(true);transform.SetAsLastSibling();
        konum.text=(menu ? "SIRADAKİ DURAK  ·  " : "BURADASIN  ·  ")+KisaAd(Bulunulan);
        Sec(Bulunulan);
    }
    public void Sec(int no)
    {
        Secilen=no;ad.text=KisaAd(no);
        bool current=no==Bulunulan,done=PlayerPrefs.GetInt("bitti_"+no,0)>0;
        durum.text=current ? (menu ? "SIRADAKİ KEŞFİN" : "ŞU ANDA BURADASIN") : done ? "TAMAMLANAN DURAK" : "DURAK "+no.ToString("00")+" / 07";
        string[] bilgiler={"Sindirim burada başlar. Dişler ve tükürük birlikte çalışır.","Yemek borusu lokmayı taşır; mide besinleri parçalar.","Besinlerin yararlı parçaları bağırsaktan kana geçer.","Kalp, kanı damarlar aracılığıyla vücuda pompalar.","Alveollerde oksijen kana, karbondioksit havaya geçer.","Böbrekler kanı süzer, yararlı maddeleri geri kazanır.","Mesane idrarı biriktirir; yolculuğun son durağıdır."};
        aciklama.text=bilgiler[no-1];gorev.text=current && !menu ? aktifGorev : GorevBilgisi.Hedef(no);
        for(int i=0;i<7;i++){
            var color=i+1==no ? new Color(.12f,.32f,.36f) : PremiumTema.Panel;
            satirlar[i].color=color;var feedback=satirlar[i].GetComponent<PremiumDugmeEfekti>();if(feedback!=null)feedback.RenkGuncelle(color);
            noktalar[i].GetComponent<Image>().color=i+1==Bulunulan ? PremiumTema.Altin : PremiumTema.Turkuaz;
            noktalar[i].GetComponentInChildren<Text>().color=PremiumTema.Gece;
            var fx=noktalar[i].GetComponent<PremiumDugmeEfekti>();if(fx!=null)fx.RenkGuncelle(noktalar[i].GetComponent<Image>().color);
        }
    }
    public void Isle(){DokunmatikGirdi.Guncelle();if(dugmeler.Isle())return;var kb=Keyboard.current;if(kb!=null&&(kb.escapeKey.wasPressedThisFrame||kb.hKey.wasPressedThisFrame))Kapat();}
    public void Kapat(){gameObject.SetActive(false);kapat?.Invoke();}
}
