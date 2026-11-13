using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class PremiumDogrulama
{
    public static void Calistir()
    {
        SessionState.SetBool("PremiumTest",true);
        EditorSceneManager.OpenScene("Assets/Scenes/AnaMenu.unity");
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Kaydet(){EditorApplication.playModeStateChanged+= s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("PremiumTest",false)){
        SessionState.SetBool("PremiumTest",false);BolumTemeli.kayitKapali=true;new GameObject("PremiumTestRunner").AddComponent<PremiumTestRunner>();
    }};}
}
public class PremiumTestRunner : MonoBehaviour
{
    int errors;
    string dir="/Users/gokhanyener/Documents/Playground/ilkoyunum-premium/renders";
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;
        Directory.CreateDirectory(dir);
        yield return new WaitForSeconds(1);
        Capture("menu");
        var menu=UnityEngine.Object.FindFirstObjectByType<AnaMenu>();
        foreach(var r in menu.GetComponentsInChildren<RectTransform>(true))if(r.name=="BolumPaneli"){
            menu.SendMessage("PanelAc",r.gameObject);yield return null;Capture("bolumler");menu.SendMessage("PanelKapat",r.gameObject);break;
        }
        menu.SendMessage("KartlariAc");yield return null;Capture("kartlar");
        foreach(var b in BolumListesi.Tumu){
            BolumTemeli.kayitKapali=false;
            SceneManager.LoadScene(b.sahne);yield return new WaitForSeconds(.3f);
            float limit=Time.realtimeSinceStartup+20;bool videoGoruldu=false;
            while(BolumTemeli.Ornek!=null && BolumTemeli.Ornek.DurumAdi=="Sinematik" && Time.realtimeSinceStartup<limit){
                var vp=UnityEngine.Object.FindFirstObjectByType<UnityEngine.Video.VideoPlayer>();
                if(vp!=null && vp.isPlaying && vp.frame>0)videoGoruldu=true;
                yield return null;
            }
            if(!videoGoruldu || BolumTemeli.Ornek.DurumAdi!="Baslik"){Debug.LogError("SINEMATIK_TEST başarısız: "+b.sahne);}
            else Debug.Log("SINEMATIK_TEST başarılı: "+b.sahne);
            BolumTemeli.kayitKapali=true;
            yield return new WaitForSeconds(.2f);
            Capture(b.sahne+"-gorev");
            var bolum=BolumTemeli.Ornek;if(bolum==null){errors++;continue;}
            bolum.Basla();yield return new WaitForSeconds(2);
            Capture(b.sahne+"-oyun");
        }
        Debug.Log("PREMIUM_TEST tamamlandi hata="+errors);
        EditorApplication.Exit(errors==0 ? 0 : 1);
    }
    void Log(string message,string stack,LogType type){if(type==LogType.Exception || type==LogType.Error)errors++;}
    void Capture(string name)
    {
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2400,1080),new Vector2Int(1600,1200)}) CaptureSize(name,size.x,size.y);
    }
    void CaptureSize(string name,int w,int h)
    {
        var camera=Camera.main;
        var target=new RenderTexture(w,h,24);camera.targetTexture=target;camera.aspect=(float)w/h;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(dir,name+"-"+w+"x"+h+".png"),texture.EncodeToPNG());
        camera.ResetAspect();camera.targetTexture=null;RenderTexture.active=null;
        foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;
        Destroy(texture);target.Release();Destroy(target);
    }
}
