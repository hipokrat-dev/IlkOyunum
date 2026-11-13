using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Hareket yolunu ve çarpışmaları değiştirmeyen anatomik çevre ayrıntıları.
public class AgizAnatomisi : MonoBehaviour
{
    readonly List<Mesh> meshes = new List<Mesh>();
    public static void Kur(Transform parent, Material mukoza, Material dil)
    {
        var root = new GameObject("AgizAnatomisi"); root.transform.SetParent(parent,false);
        root.AddComponent<AgizAnatomisi>().Olustur(mukoza,dil);
    }
    void Olustur(Material mukoza,Material dil)
    {
        // Damak kıvrımları iki yana yayılır; üst ön dişlerin arkasında.
        for(int k=0;k<7;k++) {
            float z=4+k*1.45f;
            for(int side=-1;side<=1;side+=2) {
                int yon=side;
                Fitil("DamakKivrimi",t => new Vector3(yon*(.3f+7.4f*t),
                    13.7f-1.4f*t*t,z-1.3f*Mathf.Sin(t*Mathf.PI)),.16f,24,mukoza);
            }
        }
        // Yumuşak damak ve küçük dil; çıkış koridorunun üstünde kalır.
        Parca(Sekil.SuperElips(.9f,1,16),"KucukDil",new Vector3(0,10.2f,-13.5f),new Vector3(.7f,1.8f,.65f),mukoza);
        for(int side=-1;side<=1;side+=2) {
            int yon=side;
            Fitil("DamakYayi",t => new Vector3(yon*(3+7*t),12-8*t,-13+.8f*Mathf.Sin(t*Mathf.PI)),.5f,32,mukoza);
        }
        // Dil kökündeki seyrek büyük papillalar, sürüş yüzeyinden çok az yükselir.
        var tiny=Sekil.SuperElips(1,1,8);
        meshes.Add(tiny);
        var combines=new List<CombineInstance>();
        for(int k=0;k<11;k++) {
            float x=(k-5)*1.1f,z=-10+Mathf.Abs(x)*.4f;
            combines.Add(new CombineInstance { mesh=tiny, transform=Matrix4x4.TRS(new Vector3(x,-.02f,z),Quaternion.identity,new Vector3(.22f,.08f,.22f)) });
        }
        var combined=new Mesh {name="DilKokuPapillalari"}; combined.CombineMeshes(combines.ToArray());
        Parca(combined,"DilKokuPapillalari",Vector3.zero,Vector3.one,dil);
    }
    void Fitil(string name,Func<float,Vector3> path,float r,int steps,Material mat)
    {
        var mesh=Sekil.Parametrik(steps,8,false,(u,v)=>{
            var p=path(u);var tan=(path(Mathf.Min(1,u+.005f))-path(Mathf.Max(0,u-.005f))).normalized;
            var side=Vector3.Cross(tan,Vector3.up).normalized;
            var up=Vector3.Cross(tan,side);float a=v*2*Mathf.PI;
            float taper=.45f+.55f*Mathf.Sin(Mathf.PI*u);
            return p+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*r*taper;
        },3,1);
        Parca(mesh,name,Vector3.zero,Vector3.one,mat);
    }
    void Parca(Mesh mesh,string name,Vector3 pos,Vector3 scale,Material mat)
    {
        meshes.Add(mesh);
        var g=Sekil.MeshParca(mesh,name,transform,pos,scale,mat);
        g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    void OnDestroy(){foreach(var m in meshes)if(m!=null)Destroy(m);}
}
