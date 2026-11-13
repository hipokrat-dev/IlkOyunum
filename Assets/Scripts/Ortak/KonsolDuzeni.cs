using UnityEngine;
using UnityEngine.UI;

// Yatay ve dikey ekranların aynı küçük yazılı düzeni germesini engeller.
public class KonsolDuzeni : MonoBehaviour
{
    public Rect yatay,dikey;public bool pencere;public Vector2 yatayBoyut=new Vector2(1120,650),dikeyBoyut=new Vector2(680,1100);
    RectTransform rt;Canvas canvas;Vector2 once;
    void OnEnable(){rt=transform as RectTransform;canvas=GetComponentInParent<Canvas>();Uygula(true);}
    void LateUpdate(){Uygula(false);}
    public void Uygula(bool force=true){
        if(rt==null)rt=transform as RectTransform;if(canvas==null)canvas=GetComponentInParent<Canvas>();if(canvas==null)return;
        Vector2 size=((RectTransform)canvas.rootCanvas.transform).rect.size;if(!force&&(once-size).sqrMagnitude<.5f)return;once=size;
        bool portrait=size.x<size.y;
        if(pencere){var parent=(RectTransform)rt.parent;var s=portrait?dikeyBoyut:yatayBoyut;rt.anchorMin=rt.anchorMax=rt.pivot=Cizim.Orta;rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(Mathf.Min(s.x,parent.rect.width-32),Mathf.Min(s.y,parent.rect.height-32));}
        else {var r=portrait?dikey:yatay;rt.anchorMin=new Vector2(r.x,r.y);rt.anchorMax=new Vector2(r.xMax,r.yMax);rt.offsetMin=rt.offsetMax=Vector2.zero;}
    }
}
public class KonsolOlcegi:MonoBehaviour
{
    CanvasScaler scaler;Canvas canvas;Vector2 son;
    void Awake(){scaler=GetComponent<CanvasScaler>();canvas=GetComponent<Canvas>();Ayarla();}
    void Update(){Ayarla();}
    public void Ayarla(){
        if(scaler==null)return;var size=new Vector2(Screen.width,Screen.height);
        if(canvas.renderMode==RenderMode.ScreenSpaceCamera&&canvas.worldCamera!=null&&canvas.worldCamera.targetTexture!=null)size=new Vector2(canvas.worldCamera.targetTexture.width,canvas.worldCamera.targetTexture.height);
        if(size==son)return;son=size;scaler.referenceResolution=size.x<size.y?new Vector2(720,1280):new Vector2(1280,720);scaler.matchWidthOrHeight=size.x<size.y?0:(size.x/size.y>1.7778f?1:0);
    }
}
public class KonsolResim:MonoBehaviour
{
    RawImage im;
    void Awake(){im=GetComponent<RawImage>();}
    void LateUpdate(){if(im==null||im.texture==null)return;var r=im.rectTransform.rect;float screen=r.width/r.height,source=(float)im.texture.width/im.texture.height;im.uvRect=screen>source?new Rect(0,(1-source/screen)/2,1,source/screen):new Rect((1-screen/source)/2,0,screen/source,1);}
}
public class KonsolAcilis:MonoBehaviour
{
    CanvasGroup group;float t;
    void OnEnable(){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;t=0;}
    void Update(){t+=Time.unscaledDeltaTime;float v=Mathf.Clamp01(t/.24f);group.alpha=v;transform.localScale=Vector3.one*Mathf.Lerp(.975f,1,1-Mathf.Pow(1-v,3));}
}

public class KonumNabzi:MonoBehaviour
{
    Image glow;float phase;
    void OnEnable(){glow=GetComponent<Image>();phase=0;}
    void Update(){phase+=Time.unscaledDeltaTime;float pulse=.5f+.5f*Mathf.Sin(phase*Mathf.PI*2/1.6f);glow.color=new Color(1f,.71f,.20f,Mathf.Lerp(.08f,.48f,pulse));transform.localScale=Vector3.one*Mathf.Lerp(.94f,1.12f,pulse);}
}
public class SinematikBaslikHareketi:MonoBehaviour
{
    float t;
    void OnEnable(){t=0;}
    void Update(){t+=Time.unscaledDeltaTime;float u=1-Mathf.Pow(1-Mathf.Clamp01(t/.65f),3);transform.localScale=Vector3.one*Mathf.Lerp(.94f,1,u);}
}
