using UnityEngine;
using UnityEngine.UI;

public static class PremiumTema
{
    public static readonly Color Gece=new Color(.055f,.085f,.12f,.99f),Panel=new Color(.14f,.21f,.28f,.98f),Turkuaz=new Color(.42f,.91f,.90f),Altin=new Color(.96f,.73f,.38f),Soluk=new Color(.73f,.81f,.87f);
    public static RectTransform Alan(string ad,Transform p,float x,float y,float w,float h){return Cizim.Kutu(ad,p,new Vector2(x,y),new Vector2(x+w,y+h),Cizim.Orta,Vector2.zero,Vector2.zero);}
    public static RectTransform Uyarla(RectTransform r,Rect portrait){var land=new Rect(r.anchorMin.x,r.anchorMin.y,r.anchorMax.x-r.anchorMin.x,r.anchorMax.y-r.anchorMin.y);var l=r.gameObject.AddComponent<KonsolDuzeni>();l.yatay=land;l.dikey=portrait;l.Uygula();return r;}
    public static void Uyarla(Text t,Rect portrait){Uyarla(t.transform.parent as RectTransform,portrait);}
    public static Text Metin(Transform p,string ad,string text,float x,float y,float w,float h,int size,Color color,bool bold=false){var a=Alan(ad,p,x,y,w,h);var t=Cizim.Dolduran("Yazi",a,size,TextAnchor.MiddleLeft,color,0);t.text=text;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(20,size-4);t.resizeTextMaxSize=size;t.verticalOverflow=size<=24?VerticalWrapMode.Overflow:VerticalWrapMode.Truncate;return t;}
    public static RectTransform Kart(Transform p,string ad,float x,float y,float w,float h,Color color){var r=Alan(ad,p,x,y,w,h);var im=r.gameObject.AddComponent<KonsolYuzeyi>();im.color=color;im.raycastTarget=false;return r;}
    public static RectTransform Dugme(Transform p,string ad,string text,float x,float y,float w,float h,bool primary=false){
        var r=Kart(p,ad,x,y,w,h,primary?new Color(.82f,.53f,.20f):new Color(.19f,.28f,.36f));r.GetComponent<KonsolYuzeyi>().dugme=true;
        var t=Cizim.Dolduran("Yazi",r,28,TextAnchor.MiddleCenter,primary?new Color(.07f,.055f,.035f):Color.white,14);t.text=text;t.fontStyle=FontStyle.Bold;t.resizeTextForBestFit=true;t.resizeTextMinSize=24;t.resizeTextMaxSize=28;return r;
    }
    public static RectTransform Pencere(Transform p,string ad,Vector2 landscape,Vector2 portrait){
        var r=Alan(ad,p,0,0,1,1);var opaque=r.gameObject.AddComponent<Image>();opaque.sprite=Yuvarlak;opaque.type=Image.Type.Sliced;opaque.color=new Color(.025f,.035f,.055f,1);opaque.raycastTarget=false;var front=Alan("TitanyumCerceve",r,0,0,1,1);var im=front.gameObject.AddComponent<Image>();im.sprite=Cerceve;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=3.5f;im.raycastTarget=false;
        var layout=r.gameObject.AddComponent<KonsolDuzeni>();layout.pencere=true;layout.yatayBoyut=landscape;layout.dikeyBoyut=portrait;layout.Uygula();r.gameObject.AddComponent<KonsolAcilis>();return r;
    }
    public static void Ikon(Transform p,string type,float x,float y,float w,float h,Color? color=null){var box=Alan("Ikon-"+type,p,x,y,w,h);var r=Alan("Cizim",box,0,0,1,1);var fit=r.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;var im=r.gameObject.AddComponent<KonsolIkon>();im.tur=type;im.color=color??Turkuaz;im.raycastTarget=false;}
    public static RectTransform BolgeResmi(Transform p,int no,float x,float y,float w,float h){var r=Kart(p,"BolgeGorseli",x,y,w,h,Panel);var a=Alan("Gorsel",r,.012f,.025f,.976f,.95f);var raw=a.gameObject.AddComponent<RawImage>();raw.texture=Resources.Load<Texture2D>("Arayuz/Konsol/bolge-"+no);raw.raycastTarget=false;a.gameObject.AddComponent<KonsolResim>();return r;}
    static Sprite cerceve,yuvarlak;
    public static Sprite Cerceve {get{if(cerceve==null){var t=Resources.Load<Texture2D>("Arayuz/Konsol/konsol-cerceve");if(t!=null)cerceve=Sprite.Create(t,new Rect(0,0,t.width,t.height),Cizim.Orta,100,0,SpriteMeshType.FullRect,new Vector4(t.width*.15f,t.height*.15f,t.width*.15f,t.height*.15f));}return cerceve;}}
    public static Sprite Yuvarlak {get{if(yuvarlak!=null)return yuvarlak;var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);var colors=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){var p=new Vector2(Mathf.Max(Mathf.Abs(x-31.5f)-19.5f,0),Mathf.Max(Mathf.Abs(y-31.5f)-19.5f,0));colors[y*64+x]=new Color(1,1,1,Mathf.Clamp01(12-p.magnitude));}tex.SetPixels(colors);tex.Apply();yuvarlak=Sprite.Create(tex,new Rect(0,0,64,64),Cizim.Orta,100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));return yuvarlak;}}
    public static void SinematikPerde(Transform parent){var shade=Alan("SinematikPerde",parent,0,0,1,1).gameObject.AddComponent<RawImage>();var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float bottom=Mathf.Lerp(.94f,.10f,Mathf.Pow(y/127f,.7f));float left=Mathf.Lerp(.68f,.03f,x/127f);tex.SetPixel(x,y,new Color(.015f,.025f,.04f,Mathf.Max(bottom,left)));}tex.Apply();shade.texture=tex;shade.raycastTarget=false;shade.gameObject.AddComponent<GeciciDoku>();}
    public static void ArkaPlan(Transform parent){
        var r=Alan("YolculukGorseli",parent,0,0,1,1);var im=r.gameObject.AddComponent<RawImage>();im.texture=Resources.Load<Texture2D>("Arayuz/yolculuk-premium");im.raycastTarget=false;r.gameObject.AddComponent<KonsolResim>();
        var shade=Alan("Okunurluk",parent,0,0,1,1).gameObject.AddComponent<RawImage>();var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float a=Mathf.Max(Mathf.Lerp(.75f,.05f,x/127f),Mathf.Lerp(.85f,.05f,y/127f));tex.SetPixel(x,y,new Color(.015f,.025f,.04f,a));}tex.Apply();shade.texture=tex;shade.raycastTarget=false;shade.gameObject.AddComponent<GeciciDoku>();
    }
}
public class GeciciDoku:MonoBehaviour {void OnDestroy(){var im=GetComponent<RawImage>();if(im!=null&&im.texture!=null)Destroy(im.texture);}}
