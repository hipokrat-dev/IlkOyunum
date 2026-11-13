using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class YuklemeEkrani : MonoBehaviour
{
    public static bool Yukleniyor { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Sifirla(){Yukleniyor=false;}
    public static void Ac(string sahne)
    {
        if(Yukleniyor || !Application.CanStreamedLevelBeLoaded(sahne))return;
        Yukleniyor=true;
        var g=new GameObject("YuklemeEkrani");DontDestroyOnLoad(g);
        g.AddComponent<YuklemeEkrani>().StartCoroutine(Yukle(sahne,g));
    }
    static IEnumerator Yukle(string sahne,GameObject kok)
    {
        Time.timeScale=1f;AudioListener.pause=false;
        var c=Cizim.Tuval("Yukleme",kok.transform,out var safe);c.sortingOrder=1000;
        PremiumTema.ArkaPlan(c.transform);safe.SetAsLastSibling();
        string ad="Keşif merkezi";
        foreach(var b in BolumListesi.Tumu)if(b.sahne==sahne)ad=b.ad.Replace("\n"," ");
        PremiumTema.Metin(safe,"Etiket","VÜCUT YOLCULUĞU",.06f,.79f,.62f,.07f,28,PremiumTema.Turkuaz,true);
        PremiumTema.Metin(safe,"Hedef",ad,.06f,.51f,.65f,.23f,70,Color.white,true);
        var metin=PremiumTema.Metin(safe,"Durum","Keşif rotası hazırlanıyor…",.06f,.19f,.65f,.08f,30,PremiumTema.Soluk);
        var track=PremiumTema.Kart(safe,"Ilerleme",.06f,.12f,.88f,.012f,PremiumTema.Panel);
        var fill=PremiumTema.Kart(track,"Dolgu",0,0,0,1,PremiumTema.Turkuaz);
        yield return null;yield return new WaitForEndOfFrame();
        AsyncOperation op=SceneManager.LoadSceneAsync(sahne);
        if(op==null){Yukleniyor=false;Destroy(kok);yield break;}
        op.allowSceneActivation=false;
        float bas=Time.realtimeSinceStartup;
        while(op.progress<.9f || Time.realtimeSinceStartup-bas<.65f){
            float ilerleme=Mathf.Clamp01(op.progress/.9f);
            fill.anchorMax=new Vector2(ilerleme,1);metin.text="Keşif rotası hazırlanıyor…  %"+Mathf.RoundToInt(ilerleme*100);
            yield return null;
        }
        fill.anchorMax=Vector2.one;metin.text="Bölgeye giriş yapılıyor…";
        yield return null;op.allowSceneActivation=true;
        while(!op.isDone)yield return null;
        yield return null;
        Yukleniyor=false;Destroy(kok);
    }
    void OnDestroy(){Yukleniyor=false;}
}
