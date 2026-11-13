using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Video;
public static class ArayuzDogrulama
{
    public static void Baslat(){BolumTemeli.kayitKapali=true;MobilKalite.TestMobil=true;new GameObject("ArayuzTestRunner").AddComponent<ArayuzTestRunner>();}
}
public class ArayuzTestRunner:MonoBehaviour
{
    int errors;string dir="/Users/gokhanyener/Documents/Playground/ilkoyunum-ui/renders";
    void Check(bool ok,string name){if(!ok){errors++;Debug.LogError("UI_TEST_FAIL "+name);}else Debug.Log("UI_TEST_PASS "+name);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory(dir);Application.logMessageReceived+=Log;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        SceneManager.LoadScene("AnaMenu");yield return new WaitForSecondsRealtime(1);Capture("menu");
        foreach(var b in BolumListesi.Tumu){
            BolumTemeli.kayitKapali=true;SceneManager.LoadScene(b.sahne);yield return new WaitForSecondsRealtime(.5f);
            var level=BolumTemeli.Ornek;var car=UnityEngine.Object.FindFirstObjectByType<AracKontrol>();
            if(b.no==1)Capture("gorev");
            level.HaritayiAc();yield return null;var map=UnityEngine.Object.FindFirstObjectByType<VucutHaritasi>();
            Check(map!=null && map.Bulunulan==b.no && Time.timeScale==0 && !car.kontrolAcik,"harita_"+b.sahne);
            Check(UnityEngine.Object.FindFirstObjectByType<VucutSemasi>().GetComponent<CanvasRenderer>()!=null,"vucut_cizimi_"+b.no);
            if(b.no==1 || b.no==6)Capture("harita-"+b.sahne);
            map.Sec(b.no==6?1:6);Check(map.Bulunulan==b.no && SceneManager.GetActiveScene().name==b.sahne,"organ_secimi_konumu_degistirmez_"+b.no);
            map.Kapat();Check(Time.timeScale==1 && !car.kontrolAcik && level.DurumAdi=="Baslik","goreve_donus_"+b.no);
            if(b.no!=1)continue;
            // Gerçek dokunma ile yeni sağdaki başlat düğmesi.
            var touch=InputSystem.AddDevice<Touchscreen>();Tap(touch,1,UnityEngine.InputSystem.TouchPhase.Began,.70f,.14f);yield return null;
            Tap(touch,1,UnityEngine.InputSystem.TouchPhase.Ended,.70f,.14f);yield return new WaitForSecondsRealtime(.5f);
            Check(level.DurumAdi=="Oyun","dokunarak_baslat");if(level.DurumAdi!="Oyun")level.Basla();
            yield return new WaitForSecondsRealtime(.3f);
            car.Isinla(new Vector3(0,.1f,0),Quaternion.identity);yield return new WaitForSeconds(.2f);
            bool jumped=car.Zipla();yield return new WaitForSeconds(.45f);
            Check(jumped && car.transform.position.y>TukurukDalgasi.GuvenliZiplamaYuksekligi,"ziplama_yuksekligi");Check(!car.Zipla(),"havada_ikinci_ziplama_yok");
            Capture("ziplama");yield return new WaitForSeconds(1.2f);Check(car.transform.position.y<.3f,"zemine_inis");
            // Ayrı zıplama düğmesi köpüğü tetiklememeli.
            car.Isinla(new Vector3(0,.1f,0),Quaternion.identity);yield return new WaitForSeconds(.15f);
            Tap(touch,2,UnityEngine.InputSystem.TouchPhase.Began,.74f,.33f);yield return new WaitForSeconds(.3f);
            Check(car.transform.position.y>.8f && GameObject.Find("Kopuk")==null,"mobil_zipla_kopuk_cakismiyor");
            Tap(touch,2,UnityEngine.InputSystem.TouchPhase.Ended,.74f,.33f);yield return new WaitForSeconds(1.3f);
            Tap(touch,3,UnityEngine.InputSystem.TouchPhase.Began,.70f,.91f);yield return new WaitForSecondsRealtime(.15f);
            Check(Time.timeScale==0 && GameObject.Find("Kopuk")==null,"mobil_harita_kopuk_cakismiyor");
            Tap(touch,3,UnityEngine.InputSystem.TouchPhase.Ended,.70f,.91f);yield return null;
            Tap(touch,4,UnityEngine.InputSystem.TouchPhase.Began,.88f,.91f);yield return new WaitForSecondsRealtime(.2f);
            Check(Time.timeScale==1 && GameObject.Find("Kopuk")==null,"harita_kapatirken_kopuk_yok");
            Tap(touch,4,UnityEngine.InputSystem.TouchPhase.Ended,.88f,.91f);yield return null;
            var material=Sekil.Malzeme(new Color(.7f,.9f,1,.4f),.9f);
            car.Isinla(new Vector3(0,.1f,0),Quaternion.identity);yield return new WaitForSeconds(.15f);
            bool hit=false;var wave=TukurukDalgasi.Olustur(car,material,0,6,7,4.5f,-10);wave.carpti=()=>hit=true;
            car.Zipla();yield return new WaitForSeconds(.6f);Capture("dalga-uzerinden");yield return new WaitForSeconds(.65f);Check(!hit,"ziplayarak_dalgadan_kurtulma");if(wave!=null)Destroy(wave.gameObject);
            yield return new WaitForSeconds(.4f);car.Isinla(new Vector3(0,.1f,0),Quaternion.identity);yield return new WaitForSeconds(.15f);
            wave=TukurukDalgasi.Olustur(car,material,0,6,7,4.5f,-10);hit=false;wave.carpti=()=>hit=true;yield return new WaitForSeconds(.85f);Check(hit,"yerde_dalga_carpmasi");if(wave!=null)Destroy(wave.gameObject);
            var kb=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(kb,new KeyboardState(Key.LeftCtrl));yield return new WaitForSeconds(.2f);Check(GameObject.Find("Kopuk")!=null,"ctrl_kopuk");InputSystem.QueueStateEvent(kb,new KeyboardState());yield return null;InputSystem.RemoveDevice(kb);
            level.HaritayiAc();yield return null;map=UnityEngine.Object.FindFirstObjectByType<VucutHaritasi>();var pos=car.transform.position;yield return new WaitForSecondsRealtime(.3f);Check((car.transform.position-pos).sqrMagnitude<.001f && AudioListener.pause,"harita_fizik_ve_ses_durur");map.Kapat();Check(Time.timeScale==1 && car.kontrolAcik,"oyuna_donus");
            level.SendMessage("Duraklat",true);level.HaritayiAc();yield return null;map.Kapat();Check(Time.timeScale==0 && AudioListener.pause,"duraklatilmis_haritadan_donus");Capture("duraklat");level.SendMessage("Duraklat",false);
            var ui=UnityEngine.Object.FindFirstObjectByType<Arayuz>();ui.KartGoster("VÜCUT KARTI: AĞIZ","• Diş minesi çok serttir.\n\n• Tükürük sindirime yardımcı olur.\n\n• Dil tatları algılar.","Devam");Capture("kesif-karti");ui.KartGizle();InputSystem.RemoveDevice(touch);
        }
        // Giriş videolarının başlık ve efekt sesleri, aynı müzik nesnesiyle tüm bölümlerde.
        foreach(var b in BolumListesi.Tumu){
            BolumTemeli.kayitKapali=false;SceneManager.LoadScene(b.sahne);float until=Time.realtimeSinceStartup+15;VideoPlayer vp=null;
            while(Time.realtimeSinceStartup<until){vp=UnityEngine.Object.FindFirstObjectByType<VideoPlayer>();if(vp!=null && vp.frame>22)break;yield return null;}
            var intro=UnityEngine.Object.FindFirstObjectByType<SinemaGiris>();
            Check(intro!=null && vp!=null && vp.isPlaying && intro.GetComponent<AudioSource>()!=null && intro.GetComponent<AudioSource>().ignoreListenerPause,"sinematik_baslik_ses_"+b.no);
            if(b.no==1)Capture("sinematik");
            Check(UnityEngine.Object.FindObjectsByType<OyunMuzigi>().Length==1,"tek_muzik_kaynagi_"+b.no);
            until=Time.realtimeSinceStartup+12;while(SinemaGiris.Aktif&&Time.realtimeSinceStartup<until)yield return null;
            Check(!AudioListener.pause,"sinematik_sonrasi_ses_"+b.no);
        }
        BolumTemeli.kayitKapali=false;SceneManager.LoadScene("AnaMenu");yield return null;
        File.WriteAllText(Path.Combine(dir,"sonuc.txt"),"UI_TEST tamamlandi hata="+errors);Debug.Log("UI_TEST tamamlandi hata="+errors);Destroy(gameObject);
    }
    void Log(string msg,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception) && !msg.StartsWith("UI_TEST_FAIL"))errors++;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Tap(Touchscreen device,int id,UnityEngine.InputSystem.TouchPhase phase,float x,float y){InputSystem.QueueStateEvent(device,new TouchState{touchId=id,phase=phase,position=new Vector2(Screen.width*x,Screen.height*y)});}
    void Capture(string name){foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2400,1080),new Vector2Int(1600,1200)})CaptureSize(name,size.x,size.y);}
    void CaptureSize(string name,int w,int h){
        var cam=Camera.main;if(cam==null)return;var rt=new RenderTexture(w,h,24);cam.targetTexture=rt;cam.aspect=(float)w/h;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>();foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}
        Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,name+"-"+w+"x"+h+".png"),tex.EncodeToPNG());
        cam.targetTexture=null;cam.ResetAspect();RenderTexture.active=null;foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;Destroy(tex);rt.Release();Destroy(rt);
    }
}
