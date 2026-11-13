using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Arayuz : MonoBehaviour
{
    RectTransform guvenli,hud,cubukTaban,cubukTopuz,atesDugmesi,dalisAlan,ziplaAlan,hedefIsareti;
    GameObject gorevPaneli,kart,dokunmatikKatman,dalisDugmesi,bitisDugmeleri,telsizKutu,pausePanel;
    Text sayac,ipucu,telsizMetin,kartBaslik,kartGovde,kartAlt,ortaYazi,atesYazisi,gorevSeridi,konumYazi,muzikYazi;
    Text[] bilgiler=new Text[3];
    RectTransform kartDevam;
    Image karartma,canResim;RectTransform canDolgu;Text canMetin,hasarMetin;float hasarSure;
    VucutHaritasi harita;
    int bolumNo=1;
    bool kontrollerAcik,dalisVar;
    public bool HaritaAcik => harita!=null && harita.Acik;
    public string GorevMetni {get;private set;}
    public bool TelsizBos => akis==null && kuyruk.Count==0;
    public bool DuraklatIstendi {get;private set;}
    public bool HaritaIstendi {get;private set;}
    public bool DevamIstendi {get;private set;}
    public bool KartDevamIstendi {get;private set;}
    public bool MenuIstendi {get;private set;}
    readonly DugmeListesi dugmeler=new DugmeListesi(),gorevDugmeleri=new DugmeListesi(),kartDugmeleri=new DugmeListesi(),pauseDugmeleri=new DugmeListesi(),sonDugmeleri=new DugmeListesi();
    readonly Queue<string> kuyruk=new Queue<string>();
    Coroutine akis;
    public static Arayuz Olustur(Transform parent){var c=Cizim.Tuval("Arayuz",parent,out var safe);var a=c.gameObject.AddComponent<Arayuz>();a.guvenli=safe;a.Kur();return a;}
    void Kur()
    {
        karartma=Cizim.Panel("Karartma",transform,Vector2.zero,Vector2.one,Cizim.Orta,Vector2.zero,Vector2.zero,Color.clear);karartma.transform.SetAsFirstSibling();
        hud=PremiumTema.Alan("OyunGostergeleri",guvenli,0,0,1,1);
        var loc=PremiumTema.Uyarla(PremiumTema.Kart(hud,"BulunulanBolge",.025f,.855f,.20f,.115f,PremiumTema.Gece),new Rect(.035f,.915f,.49f,.065f));
        PremiumTema.Metin(loc,"Etiket","GÖREV BÖLGESİ",.07f,.55f,.86f,.38f,20,PremiumTema.Turkuaz,true);
        konumYazi=PremiumTema.Metin(loc,"Konum","Ağız",.07f,.13f,.86f,.44f,28,Color.white,true);
        var counter=PremiumTema.Uyarla(PremiumTema.Kart(hud,"Sayac",.24f,.855f,.23f,.115f,PremiumTema.Gece),new Rect(.035f,.845f,.49f,.06f));
        PremiumTema.Metin(counter,"Etiket","GÖREV DURUMU",.07f,.55f,.86f,.38f,20,PremiumTema.Soluk,true);
        sayac=PremiumTema.Metin(counter,"Metin","",.07f,.13f,.86f,.44f,28,Color.white,true);
        var map=PremiumTema.Uyarla(PremiumTema.Dugme(hud,"Neredeyim","Neredeyim?",.70f,.855f,.17f,.115f),new Rect(.55f,.915f,.28f,.065f));
        dugmeler.Ekle(map,()=>HaritaIstendi=true);
        var pause=PremiumTema.Uyarla(PremiumTema.Dugme(hud,"Duraklat","",.89f,.855f,.085f,.115f),new Rect(.85f,.915f,.115f,.065f));PremiumTema.Ikon(pause,"pause",.20f,.20f,.60f,.60f);
        dugmeler.Ekle(pause,()=>DuraklatIstendi=true);
        var health=PremiumTema.Uyarla(PremiumTema.Kart(hud,"CanBar",.49f,.855f,.19f,.115f,PremiumTema.Gece),new Rect(.55f,.845f,.415f,.06f));
        canMetin=PremiumTema.Metin(health,"CanDegeri","CAN  100 / 100",.07f,.51f,.86f,.35f,22,Color.white,true);
        var track=PremiumTema.Kart(health,"CanYuvasi",.07f,.19f,.86f,.23f,new Color(.06f,.10f,.13f));
        canDolgu=PremiumTema.Alan("Dolgu",track,.02f,.12f,.96f,.76f);canResim=canDolgu.gameObject.AddComponent<Image>();canResim.sprite=PremiumTema.Yuvarlak;canResim.type=Image.Type.Sliced;canResim.color=PremiumTema.Turkuaz;canResim.raycastTarget=false;
        hasarMetin=PremiumTema.Metin(hud,"HasarUyarisi","",.30f,.57f,.40f,.07f,29,PremiumTema.Altin,true);hasarMetin.alignment=TextAnchor.MiddleCenter;PremiumTema.Uyarla(hasarMetin,new Rect(.06f,.64f,.88f,.065f));
        var task=PremiumTema.Uyarla(PremiumTema.Kart(hud,"GorevSeridi",.025f,.745f,.50f,.09f,PremiumTema.Gece),new Rect(.035f,.755f,.93f,.075f));
        gorevSeridi=PremiumTema.Metin(task,"Hedef","",.04f,.12f,.92f,.76f,25,PremiumTema.Altin);
        ipucu=PremiumTema.Metin(hud,"Ipucu","WASD: Sür  •  Ctrl / Command: Köpük  •  H: Harita",.23f,.012f,.54f,.05f,20,PremiumTema.Soluk);ipucu.alignment=TextAnchor.MiddleCenter;
        var radio=PremiumTema.Uyarla(PremiumTema.Kart(guvenli,"Telsiz",.26f,.07f,.48f,.18f,PremiumTema.Gece),new Rect(.035f,.36f,.93f,.14f));telsizKutu=radio.gameObject;
        PremiumTema.Metin(radio,"Etiket","PROFESÖR  /  BAĞLANTI AKTİF",.05f,.72f,.90f,.20f,20,PremiumTema.Turkuaz,true);
        telsizMetin=PremiumTema.Metin(radio,"Mesaj","",.05f,.09f,.90f,.58f,26,Color.white);telsizMetin.alignment=TextAnchor.UpperLeft;telsizKutu.SetActive(false);
        DokunmatikKur();
        hedefIsareti=PremiumTema.Kart(guvenli,"HedefIsareti",.5f,.5f,.015f,.025f,PremiumTema.Altin);hedefIsareti.gameObject.SetActive(false);
        var card=PremiumTema.Pencere(guvenli,"VucutKarti",new Vector2(1020,640),new Vector2(680,1080));kart=card.gameObject;
        PremiumTema.Metin(card,"Etiket","ARAŞTIRMA ARŞİVİ  /  YENİ KEŞİF",.07f,.87f,.86f,.05f,22,PremiumTema.Turkuaz,true);
        kartBaslik=PremiumTema.Metin(card,"Baslik","",.07f,.73f,.86f,.12f,44,Color.white,true);
        for(int i=0;i<3;i++){
            var row=PremiumTema.Kart(card,"Bilgi"+i,.07f,.54f-i*.17f,.86f,.15f,PremiumTema.Panel);
            PremiumTema.Metin(row,"No",(i+1).ToString("00"),.035f,.15f,.09f,.70f,30,PremiumTema.Altin,true);
            bilgiler[i]=PremiumTema.Metin(row,"Bilgi","",.15f,.12f,.80f,.76f,28,Color.white);
        }
        kartAlt=PremiumTema.Metin(card,"Alt","",.07f,.06f,.36f,.10f,22,PremiumTema.Soluk);
        kartDevam=PremiumTema.Dugme(card,"KartDevam","Koleksiyona ekle  ›",.45f,.06f,.48f,.10f,true);
        kartDugmeleri.Ekle(kartDevam,()=>KartDevamIstendi=true);kart.SetActive(false);
        var end=PremiumTema.Pencere(guvenli,"BolumSonu",new Vector2(1000,600),new Vector2(680,900));bitisDugmeleri=end.gameObject;
        PremiumTema.Metin(end,"Etiket","KAPSÜL  /  GÖREV RAPORU",.07f,.85f,.86f,.07f,22,PremiumTema.Turkuaz,true);
        ortaYazi=PremiumTema.Metin(end,"Sonuc","",.07f,.32f,.86f,.46f,44,Color.white,true);ortaYazi.alignment=TextAnchor.MiddleCenter;bitisDugmeleri.SetActive(false);
        var pauseRoot=PremiumTema.Pencere(guvenli,"DuraklatMenusu",new Vector2(650,600),new Vector2(650,850));pausePanel=pauseRoot.gameObject;
        PremiumTema.Metin(pauseRoot,"Etiket","KAPSÜL BEKLEMEDE",.09f,.85f,.82f,.06f,22,PremiumTema.Turkuaz,true);
        PremiumTema.Metin(pauseRoot,"Baslik","Kısa bir mola",.09f,.69f,.82f,.14f,46,Color.white,true);
        pauseDugmeleri.Ekle(PremiumTema.Dugme(pauseRoot,"OyunaDon","Oyuna dön  ›",.09f,.49f,.82f,.14f,true),()=>DevamIstendi=true);
        pauseDugmeleri.Ekle(PremiumTema.Dugme(pauseRoot,"HaritaAc","Neredeyim?",.09f,.31f,.39f,.14f),()=>HaritaIstendi=true);
        var music=PremiumTema.Dugme(pauseRoot,"Muzik","Müzik: Açık",.52f,.31f,.39f,.14f);muzikYazi=music.GetComponentInChildren<Text>();pauseDugmeleri.Ekle(music,()=>{OyunMuzigi.Degistir();MuzikYenile();});
        pauseDugmeleri.Ekle(PremiumTema.Dugme(pauseRoot,"AnaMenu","Ana menüye dön",.09f,.13f,.82f,.14f),()=>MenuIstendi=true);pausePanel.SetActive(false);
        harita=VucutHaritasi.Olustur(guvenli);
        DokunmatikGirdi.digerDugme=p=>HaritaAcik || (pausePanel!=null&&pausePanel.activeSelf) || (gorevPaneli!=null&&gorevPaneli.activeSelf) || kart.activeSelf || bitisDugmeleri.activeSelf || RectTransformUtility.RectangleContainsScreenPoint(map,p,null) || RectTransformUtility.RectangleContainsScreenPoint(pause,p,null) || (ziplaAlan.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(ziplaAlan,p,null));
    }
    void DokunmatikKur()
    {
        var touch=PremiumTema.Alan("Dokunmatik",guvenli,0,0,1,1);dokunmatikKatman=touch.gameObject;
        cubukTaban=Daire(touch,"Surus",.12f,.19f,156);cubukTopuz=Daire(cubukTaban,"Topuz",.5f,.5f,66);
        atesDugmesi=Daire(touch,"Ates",.89f,.19f,142);atesDugmesi.GetComponent<Image>().color=new Color(.27f,.49f,.54f,.9f);PremiumTema.Ikon(atesDugmesi,"kopuk",.27f,.40f,.46f,.40f);
        atesYazisi=PremiumTema.Metin(atesDugmesi,"Yazi","KÖPÜK",.1f,.14f,.8f,.25f,23,Color.white,true);atesYazisi.alignment=TextAnchor.MiddleCenter;
        ziplaAlan=Daire(touch,"Zipla",.76f,.30f,116);PremiumTema.Ikon(ziplaAlan,"zipla",.28f,.42f,.44f,.39f,PremiumTema.Altin);var z=PremiumTema.Metin(ziplaAlan,"Yazi","ZIPLA",.1f,.14f,.8f,.25f,22,Color.white,true);z.alignment=TextAnchor.MiddleCenter;
        dugmeler.Ekle(ziplaAlan,()=>UnityEngine.Object.FindFirstObjectByType<AracKontrol>()?.Zipla());
        dalisAlan=Daire(touch,"Dalis",.76f,.30f,116);dalisDugmesi=dalisAlan.gameObject;PremiumTema.Ikon(dalisAlan,"dal",.28f,.42f,.44f,.39f);var d=PremiumTema.Metin(dalisAlan,"Yazi","DAL",.1f,.14f,.8f,.25f,22,Color.white,true);d.alignment=TextAnchor.MiddleCenter;
        dalisDugmesi.SetActive(false);ziplaAlan.gameObject.SetActive(false);touch.gameObject.SetActive(false);
    }
    RectTransform Daire(Transform parent,string name,float x,float y,float size){var r=Cizim.Kutu(name,parent,new Vector2(x,y),new Vector2(x,y),Cizim.Orta,Vector2.zero,new Vector2(size,size));var im=r.gameObject.AddComponent<KonsolYuzeyi>();im.daire=true;im.dugme=true;im.color=new Color(.13f,.20f,.27f,.88f);im.raycastTarget=false;return r;}
    public void BolumAyarla(int no){bolumNo=no;konumYazi.text=VucutHaritasi.KisaAd(no);ziplaAlan.gameObject.SetActive(no==1);}
    void Update(){
        if(hud==null)return;
        if(hasarSure>0){hasarSure-=Time.deltaTime;if(hasarSure<=0)hasarMetin.text="";}
        DokunmatikGirdi.Guncelle();hud.gameObject.SetActive(kontrollerAcik);dokunmatikKatman.SetActive(kontrollerAcik&&DokunmatikGirdi.Etkin);
        ipucu.gameObject.SetActive(!DokunmatikGirdi.Etkin);
        if(dokunmatikKatman.activeSelf){
            KontrolYerlesimi();
            if(DokunmatikGirdi.CubukTutuluyor)cubukTaban.position=DokunmatikGirdi.CubukMerkezi;
            else {cubukTaban.anchoredPosition=Vector2.zero;}
            cubukTopuz.anchoredPosition=DokunmatikGirdi.Cubuk*42f;
            atesDugmesi.localScale=Vector3.one*(DokunmatikGirdi.Ates?.94f:1);
        }
    }
    public void KontrolYerlesimi(){
            bool portrait=guvenli.rect.width<guvenli.rect.height;
            cubukTaban.anchorMin=cubukTaban.anchorMax=portrait?new Vector2(.19f,.12f):new Vector2(.12f,.19f);
            atesDugmesi.anchorMin=atesDugmesi.anchorMax=portrait?new Vector2(.81f,.12f):new Vector2(.89f,.19f);
            ziplaAlan.anchorMin=ziplaAlan.anchorMax=dalisAlan.anchorMin=dalisAlan.anchorMax=portrait?new Vector2(.80f,.255f):new Vector2(.76f,.30f);
    }
    public bool DugmeleriIsle(){
        if(pausePanel==null)return false;
        DuraklatIstendi=HaritaIstendi=DevamIstendi=KartDevamIstendi=MenuIstendi=false;DokunmatikGirdi.Guncelle();
        if(HaritaAcik){harita.Isle();return true;}
        if(pausePanel.activeSelf)return pauseDugmeleri.Isle();
        if(gorevPaneli!=null&&gorevPaneli.activeSelf)return gorevDugmeleri.Isle();
        if(kart.activeSelf)return kartDugmeleri.Isle();
        if(bitisDugmeleri.activeSelf)return sonDugmeleri.Isle();
        return dugmeler.Isle();
    }
    public void HaritayiAc(Action kapat){GameObject previous=null;foreach(var g in new[]{gorevPaneli,pausePanel,kart,bitisDugmeleri})if(g!=null&&g.activeSelf){previous=g;g.SetActive(false);break;}harita.Ac(bolumNo,GorevMetni,()=>{if(previous!=null)previous.SetActive(true);kapat?.Invoke();});}
    public void HaritaIsle()=>harita.Isle();
    public void DuraklatGoster(bool on){pausePanel.SetActive(on);if(on){pausePanel.transform.SetAsLastSibling();MuzikYenile();}}
    void MuzikYenile(){muzikYazi.text="Müzik: "+(OyunMuzigi.Acik?"Açık":"Kapalı");}
    public void GorevGoster(int no,string baslik,Action basla)
    {
        if(gorevPaneli!=null)Destroy(gorevPaneli);
        var p=PremiumTema.Alan("GorevBrifingi",guvenli,0,0,1,1);gorevPaneli=p.gameObject;
        var backdrop=PremiumTema.Alan("BolgeSinematikGorseli",p,0,0,1,1);var image=backdrop.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("Arayuz/Konsol/bolge-"+no);image.raycastTarget=false;backdrop.gameObject.AddComponent<KonsolResim>();PremiumTema.SinematikPerde(p);
        var caption=PremiumTema.Metin(p,"Etiket","GÖREV BÖLGESİ  /  "+no.ToString("00")+" – 07",.065f,.80f,.74f,.06f,23,PremiumTema.Altin,true);PremiumTema.Uyarla(caption,new Rect(.07f,.74f,.86f,.05f));
        var title=PremiumTema.Metin(p,"Baslik",VucutHaritasi.KisaAd(no),.06f,.57f,.75f,.20f,78,Color.white,true);PremiumTema.Uyarla(title,new Rect(.07f,.59f,.86f,.14f));
        var line=PremiumTema.Alan("AltinCizgi",p,.065f,.54f,.12f,.004f);var li=line.gameObject.AddComponent<Image>();li.color=PremiumTema.Altin;li.raycastTarget=false;PremiumTema.Uyarla(line,new Rect(.07f,.555f,.21f,.003f));
        var label=PremiumTema.Metin(p,"GorevEtiketi","ÇIRAĞIN GÖREVİ",.065f,.445f,.72f,.05f,22,PremiumTema.Turkuaz,true);PremiumTema.Uyarla(label,new Rect(.07f,.485f,.86f,.045f));
        var goal=PremiumTema.Metin(p,"Gorev",GorevBilgisi.Hedef(no),.065f,.30f,.56f,.14f,32,Color.white,true);PremiumTema.Uyarla(goal,new Rect(.07f,.365f,.86f,.115f));
        var note=PremiumTema.Metin(p,"Dikkat",GorevBilgisi.Dikkat(no),.065f,.24f,.60f,.08f,25,PremiumTema.Soluk);PremiumTema.Uyarla(note,new Rect(.07f,.265f,.86f,.085f));
        var controls=PremiumTema.Metin(p,"Kontrol",GorevBilgisi.Kontrol(no),.065f,.165f,.80f,.05f,20,PremiumTema.Soluk);PremiumTema.Uyarla(controls,new Rect(.07f,.155f,.86f,.07f));
        var map=PremiumTema.Dugme(p,"GorevHaritasi","Neredeyim?",.065f,.025f,.24f,.12f);PremiumTema.Uyarla(map,new Rect(.07f,.05f,.32f,.075f));gorevDugmeleri.Ekle(map,()=>HaritaIstendi=true);
        var start=PremiumTema.Dugme(p,"GoreveBasla",no==5?"Keşfe başla  ›":"Göreve başla  ›",.33f,.025f,.32f,.12f,true);PremiumTema.Uyarla(start,new Rect(.42f,.05f,.51f,.075f));gorevDugmeleri.Ekle(start,basla);
        p.gameObject.AddComponent<KonsolAcilis>();title.transform.parent.gameObject.AddComponent<SinematikBaslikHareketi>();
        GorevGuncelle(GorevBilgisi.Hedef(no));
    }
    public void GorevGizle(){if(gorevPaneli!=null)gorevPaneli.SetActive(false);}
    public void GorevGuncelle(string t){GorevMetni=t;gorevSeridi.text=t;}
    public void Kontroller(bool acik)=>kontrollerAcik=acik;
    public void DalisDugmesi(bool on){dalisVar=on;dalisDugmesi.SetActive(on);DokunmatikGirdi.dalisAlani=on ? p=>RectTransformUtility.RectangleContainsScreenPoint(dalisAlan,p,null) : (Func<Vector2,bool>)null;}
    public void AtesEtiketi(string text)=>atesYazisi.text=text;
    public void CanGuncelle(float value){float v=Mathf.Clamp01(value/100f);canMetin.text="CAN  "+Mathf.CeilToInt(value)+" / 100";canDolgu.anchorMax=new Vector2(.02f+.96f*v,.88f);canResim.color=v>.5f?PremiumTema.Turkuaz:v>.25f?PremiumTema.Altin:new Color(1,.30f,.27f);}
    public void HasarGoster(string reason,int amount){hasarMetin.text="−"+amount+" CAN  ·  "+reason;hasarSure=1.2f;}
    public void TelsiziKapat(){kuyruk.Clear();if(akis!=null)StopCoroutine(akis);akis=null;telsizKutu.SetActive(false);}
    public void Gosterge(string text,bool vurgu=false){sayac.text=text;}
    public void Ipucu(string text)=>ipucu.text=text;
    public void Perde(float a)=>karartma.color=new Color(0,0,0,a);
    public void HedefIsareti(Vector2? p){hedefIsareti.gameObject.SetActive(p.HasValue);if(p.HasValue){hedefIsareti.anchorMin=hedefIsareti.anchorMax=p.Value;hedefIsareti.sizeDelta=new Vector2(22,22);}}
    public void KartGoster(string title,string body,string alt){
        kartBaslik.text=title.Replace("VÜCUT KARTI: ","");var lines=body.Split(new[]{'•'},StringSplitOptions.RemoveEmptyEntries);
        for(int i=0;i<3;i++)bilgiler[i].text=i<lines.Length?lines[i].Trim():"";
        KartAltYazi(alt);kart.SetActive(true);kart.transform.SetAsLastSibling();
    }
    public void KartAltYazi(string text){kartAlt.text=string.IsNullOrEmpty(text)?"Kart hazırlanıyor…":"Koleksiyonuna eklendi.";kartDevam.gameObject.SetActive(!string.IsNullOrEmpty(text));}
    public void KartGizle()=>kart.SetActive(false);
    public void OrtaYazi(string text)=>ortaYazi.text=text;
    public void BitisDugmeleri(Action devam,Action tekrar,Action menu,string label="Devam"){
        var parent=bitisDugmeleri.transform;var items=new List<(string text,Action action)>();
        if(devam!=null)items.Add((label,devam));if(tekrar!=null)items.Add(("Tekrar dene",tekrar));if(menu!=null)items.Add(("Ana menü",menu));
        for(int i=0;i<items.Count;i++){float w=(.86f-.025f*(items.Count-1))/items.Count;var button=PremiumTema.Dugme(parent,"Sonuc"+i,items[i].text,.07f+i*(w+.025f),.08f,w,.13f,i==0);PremiumTema.Uyarla(button,new Rect(.07f,.07f+(items.Count-1-i)*.11f,.86f,.095f));sonDugmeleri.Ekle(button,items[i].action);}
        bitisDugmeleri.SetActive(true);bitisDugmeleri.transform.SetAsLastSibling();
    }
    void OnDestroy(){DokunmatikGirdi.digerDugme=null;DokunmatikGirdi.dalisAlani=null;}
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

    IEnumerator TelsizAkisi()
    {
        // Sürüş sırasında açıklama kutusu açılmaz; varsa anlatım yalnızca ses olarak sürer.
        telsizKutu.SetActive(false);
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
