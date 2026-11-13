#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class AgizV2Dogrulama:MonoBehaviour
{
    const string Dir="/Users/gokhanyener/Documents/Playground/ilkoyunum-mouth-v2";
    List<string> checks=new List<string>();int errors;
    public static void Baslat(){new GameObject("AgizV2Dogrulama").AddComponent<AgizV2Dogrulama>();}
    void Check(bool yes,string label){checks.Add((yes?"PASS ":"FAIL ")+label);if(!yes){errors++;Debug.LogError("AGIZ_V2_FAIL "+label);}}
    void Log(string m,string st,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!m.StartsWith("AGIZ_V2_FAIL")){errors++;checks.Add("ERROR "+m);}}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;
        Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        BolumTemeli.kayitKapali=true;MobilKalite.TestMobil=true;Time.timeScale=1;SceneManager.LoadScene("Agiz");yield return new WaitForSecondsRealtime(1.5f);
        var b=AgizBolumu.Ornek;var car=UnityEngine.Object.FindFirstObjectByType<AracKontrol>();b.Basla();
        typeof(AgizBolumu).GetField("dalgaSayaci",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(b,999f);
        yield return new WaitForSeconds(.3f);Check(b.Mikroplar.Count==10&&b.Can==100,"10 hedef, 100 can");
        var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);Check(groups.Length==32,"32 dis icin LOD");
        bool lod=true;foreach(var g in groups){var a=g.GetLODs();lod&=a.Length==3;if(a.Length==3){uint prev=uint.MaxValue;foreach(var l in a){uint count=l.renderers[0].GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0);lod&=count<prev;prev=count;}}}Check(lod,"3 azalan ayrinti seviyesi");
        bool pbr=true;foreach(var n in new[]{"Dil","AltDisEti","UstDisEti"}){var m=GameObject.Find(n).GetComponent<Renderer>().sharedMaterial;pbr&=m.GetTexture("_BaseMap")!=null&&m.GetTexture("_BumpMap")!=null&&m.GetTexture("_MetallicGlossMap")!=null&&m.shader.isSupported;}Check(pbr,"PBR doku setleri ve shader");
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2400,1080),new Vector2Int(1600,1200)}){
            CaptureSize("agiz-v2-son",size.x,size.y);
            File.Copy("/Users/gokhanyener/Documents/Playground/ilkoyunum-console/renders/agiz-v2-son-"+size.x+"x"+size.y+".png",Dir+"/renders/agiz-"+size.x+"x"+size.y+".png",true);yield return null;
        }
        checks.Add("Editor render observation triangles="+UnityStats.triangles+" drawCalls="+UnityStats.drawCalls+" setPass="+UnityStats.setPassCalls);
        var kb=InputSystem.AddDevice<Keyboard>();var start=car.transform.position;var forward=car.transform.forward;
        InputSystem.QueueStateEvent(kb,new KeyboardState(Key.W));yield return new WaitForSeconds(.8f);InputSystem.QueueStateEvent(kb,new KeyboardState());yield return new WaitForSeconds(.15f);
        Check(Vector3.Dot(car.transform.position-start,forward)>2,"yeni giris yonunde surus");
        car.Isinla(new Vector3(0,.1f,0),Quaternion.identity);yield return new WaitForSeconds(.2f);bool jump=car.Zipla();yield return new WaitForSeconds(.45f);
        Check(jump&&car.transform.position.y>TukurukDalgasi.GuvenliZiplamaYuksekligi,"tukurukten yuksek ziplama");yield return new WaitForSeconds(1.2f);Check(car.transform.position.y<.3f,"dil zeminine inis");
        var mat=Sekil.Malzeme(new Color(.7f,.9f,1,.4f),.9f);var wave=TukurukDalgasi.Olustur(car,mat,0,6,7,4.5f,-10);wave.carpti=()=>b.HasarAl(20,"Tükürük");
        car.Zipla();yield return new WaitForSeconds(1.3f);Check(b.Can==100,"ziplama dalga hasarini onler");if(wave!=null)Destroy(wave.gameObject);
        yield return new WaitForSeconds(.4f);car.Isinla(new Vector3(0,.1f,0),Quaternion.identity);yield return new WaitForSeconds(.2f);
        wave=TukurukDalgasi.Olustur(car,mat,0,6,7,4.5f,-10);wave.carpti=()=>b.HasarAl(20,"Tükürük");yield return new WaitForSeconds(.9f);Check(b.Can==80,"yerde dalga 20 can azaltir");if(wave!=null)Destroy(wave.gameObject);
        var target=b.Mikroplar[0];var dir=new Vector3(-target.transform.position.x,0,3-target.transform.position.z).normalized;
        var p=target.transform.position+dir*3;p.y=.1f;car.Isinla(p,Quaternion.LookRotation(-dir));yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(kb,new KeyboardState(Key.LeftCtrl));yield return new WaitForSeconds(.15f);Check(GameObject.Find("Kopuk")!=null,"ctrl kopuk");yield return new WaitForSeconds(1.6f);InputSystem.QueueStateEvent(kb,new KeyboardState());
        Check(target==null||target.Eriyor,"kopuk yeni dislerin yanindaki hedefe ulasir");
        car.Isinla(new Vector3(7,.1f,-2),Quaternion.Euler(0,90,0));yield return new WaitForSeconds(.2f);InputSystem.QueueStateEvent(kb,new KeyboardState(Key.W));yield return new WaitForSeconds(1.3f);InputSystem.QueueStateEvent(kb,new KeyboardState());Check(car.transform.position.x<10.2f,"dis ve cene carpismalari");
        b.HaritayiAc();yield return null;Check(Time.timeScale==0&&GameObject.Find("InsanVucudu")!=null,"neredeyim ve duraklama");UnityEngine.Object.FindFirstObjectByType<VucutHaritasi>().Kapat();
        foreach(var m in b.Mikroplar)if(m!=null&&!m.Eriyor)m.Temizle();yield return new WaitForSecondsRealtime(3);
        Check(b.DurumAdi=="Kart","10 koloni sonrasi bolum ilerler");InputSystem.RemoveDevice(kb);
        File.WriteAllText(Dir+"/validation.txt","AGIZ_V2 errors="+errors+"\n"+string.Join("\n",checks));Debug.Log("AGIZ_V2_COMPLETE errors="+errors);
        BolumTemeli.kayitKapali=true;Time.timeScale=1;SceneManager.LoadScene("Agiz");Destroy(gameObject);
    }
    void OnDestroy(){Application.logMessageReceived-=Log;}
    public static void CaptureSize(string name,int w,int h){
        string dir="/Users/gokhanyener/Documents/Playground/ilkoyunum-console/renders";Directory.CreateDirectory(dir);
        var cam=Camera.main;if(cam==null)return;var rt=new RenderTexture(w,h,24);cam.targetTexture=rt;cam.aspect=(float)w/h;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>();float scale=w<h?w/720f:Mathf.Min(w/1280f,h/720f);
        foreach(var c in canvases){var sc=c.GetComponent<CanvasScaler>();if(sc!=null)sc.enabled=false;c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;c.scaleFactor=scale;}
        Canvas.ForceUpdateCanvases();foreach(var l in UnityEngine.Object.FindObjectsByType<KonsolDuzeni>())l.Uygula();Canvas.ForceUpdateCanvases();foreach(var ui in UnityEngine.Object.FindObjectsByType<Arayuz>())ui.KontrolYerlesimi();foreach(var a in UnityEngine.Object.FindObjectsByType<KonsolResim>())a.SendMessage("LateUpdate");
        cam.Render();RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,name+"-"+w+"x"+h+".png"),tex.EncodeToPNG());
        cam.targetTexture=null;cam.ResetAspect();RenderTexture.active=null;foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;var sc=c.GetComponent<CanvasScaler>();if(sc!=null){sc.enabled=true;sc.SendMessage("Update");}}
        Canvas.ForceUpdateCanvases();foreach(var l in UnityEngine.Object.FindObjectsByType<KonsolDuzeni>())l.Uygula();Canvas.ForceUpdateCanvases();Destroy(tex);rt.Release();Destroy(rt);
    }
}
#endif
