using System.Collections;
using UnityEngine;
public class OyunMuzigi : MonoBehaviour
{
    static OyunMuzigi ornek;
    AudioSource kaynak;
    public static bool Acik => PlayerPrefs.GetInt("muzik_acik",1)==1;
    public static void Kur(){if(ornek!=null)return;var g=new GameObject("OyunMuzigi");ornek=g.AddComponent<OyunMuzigi>();DontDestroyOnLoad(g);}
    public static void Degistir(){PlayerPrefs.SetInt("muzik_acik",Acik?0:1);PlayerPrefs.Save();}
    IEnumerator Start(){
        kaynak=gameObject.AddComponent<AudioSource>();kaynak.playOnAwake=false;kaynak.loop=true;kaynak.spatialBlend=0;kaynak.volume=0;kaynak.priority=192;
        var req=Resources.LoadAsync<AudioClip>("Muzik/BouncyAdventure");yield return req;kaynak.clip=req.asset as AudioClip;if(kaynak.clip!=null)kaynak.Play();
    }
    void Update(){if(kaynak==null)return;float hedef=Acik ? (Ses.Konusuyor ?.055f:.18f):0;kaynak.volume=Mathf.MoveTowards(kaynak.volume,hedef,Time.unscaledDeltaTime*.22f);}
    void OnDestroy(){if(ornek==this)ornek=null;}
}
