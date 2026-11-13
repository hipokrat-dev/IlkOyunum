using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Yerel video + görev bölgesi başlığı ve kısa ortam efektleri; konuşma içermez.
public class SinemaGiris : MonoBehaviour
{
    public static bool Aktif { get; private set; }
    VideoPlayer player;
    RenderTexture rt;
    VideoClip clip;
    bool hata,bitti,eskiPause;
    int bolum;AudioClip ortamKlip;AudioSource ortam;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Sifirla(){Aktif=false;}
    public static IEnumerator Oynat(string sahne,Transform parent)
    {
        var req=Resources.LoadAsync<VideoClip>("Sinematik/"+sahne);
        yield return req;
        var video=req.asset as VideoClip;
        if(video==null){Debug.LogWarning("Sinematik bulunamadı: "+sahne);yield break;}
        var go=new GameObject("SinemaGiris");go.transform.SetParent(parent,false);
        var s=go.AddComponent<SinemaGiris>();s.clip=video;
        foreach(var b in BolumListesi.Tumu)if(b.sahne==sahne)s.bolum=b.no;
        yield return s.OynatIc();Destroy(go);
    }
    IEnumerator OynatIc()
    {
        Aktif=true;
        eskiPause=AudioListener.pause;AudioListener.pause=true;
        var canvas=Cizim.Tuval("Sinema",transform,out var safe);canvas.sortingOrder=500;
        Cizim.Panel("Zemin",canvas.transform,Vector2.zero,Vector2.one,Cizim.Orta,Vector2.zero,Vector2.zero,Color.black);
        var raw=PremiumTema.Alan("Video",canvas.transform,0,0,1,1).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;
        rt=new RenderTexture((int)clip.width,(int)clip.height,0,RenderTextureFormat.ARGB32);rt.Create();raw.texture=rt;
        PremiumTema.SinematikPerde(canvas.transform);safe.SetAsLastSibling();
        var baslik=PremiumTema.Alan("SinematikBolgeBasligi",safe,.065f,.10f,.87f,.30f);
        var grup=baslik.gameObject.AddComponent<CanvasGroup>();grup.alpha=0;
        var bg=PremiumTema.Alan("BaslikZemini",baslik,0,0,1,1);
        PremiumTema.Metin(bg,"Etiket","G Ö R E V  B Ö L G E S İ",.045f,.66f,.91f,.20f,24,PremiumTema.Turkuaz,true);
        PremiumTema.Metin(bg,"Bolge",VucutHaritasi.KisaAd(bolum).ToUpper(new System.Globalization.CultureInfo("tr-TR")),.045f,.14f,.91f,.45f,54,Color.white,true);
        ortam=gameObject.AddComponent<AudioSource>();ortam.playOnAwake=false;ortam.ignoreListenerPause=true;ortam.spatialBlend=0;ortam.volume=.65f;
        ortamKlip=SinemaSesi.Uret(bolum);ortam.clip=ortamKlip;bool sesBasladi=false;
        player=gameObject.AddComponent<VideoPlayer>();player.playOnAwake=false;player.clip=clip;
        player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=rt;
        player.audioOutputMode=VideoAudioOutputMode.None;player.isLooping=false;player.waitForFirstFrame=true;
        player.errorReceived+=(p,msg)=>{hata=true;Debug.LogWarning("Sinematik oynatılamadı: "+msg);};
        player.loopPointReached+=p=>bitti=true;
        player.Prepare();float limit=Time.realtimeSinceStartup+12;
        while(!player.isPrepared && !hata && Time.realtimeSinceStartup<limit)yield return null;
        if(player.isPrepared && !hata){
            player.Play();limit=Time.realtimeSinceStartup+12;
            while(!bitti && !hata && Time.realtimeSinceStartup<limit){
                float screen=(float)Screen.width/Screen.height,video=(float)clip.width/clip.height;
                if(screen>video){float h=video/screen;raw.uvRect=new Rect(0,(1-h)/2,1,h);}
                else {float w=screen/video;raw.uvRect=new Rect((1-w)/2,0,w,1);}
                float an=(float)player.time;
                if(!sesBasladi && player.frame>0){ortam.Play();sesBasladi=true;}
                grup.alpha=Mathf.SmoothStep(0,1,(an-.25f)/.6f)*(1-Mathf.SmoothStep(0,1,(an-4.15f)/.7f));
                baslik.anchoredPosition=new Vector2(Mathf.Lerp(-35,0,Mathf.SmoothStep(0,1,(an-.25f)/.65f)),0);
                yield return null;
            }
        }
        player.Stop();AudioListener.pause=eskiPause;Aktif=false;
    }
    void OnDestroy(){Aktif=false;if(player!=null)player.Stop();if(rt!=null){rt.Release();Destroy(rt);}if(clip!=null)Resources.UnloadAsset(clip);if(ortamKlip!=null)Destroy(ortamKlip);AudioListener.pause=eskiPause;}
}
