using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Birleştirilmiş çevre geometrisi: damak kıvrımları, küçük dil ve dil papillaları.
public static class AgizAnatomisi
{
    public static void Kur(Transform parent, Material mukoza, Material dil)
    {
        var root=new GameObject("AgizAnatomisi").transform;root.SetParent(parent,false);
        var parts=new List<CombineInstance>();
        for(int k=0;k<7;k++){
            float z=4+k*1.45f;
            for(int side=-1;side<=1;side+=2){int yon=side;
                Fitil(parts,t=>new Vector3(yon*(.3f+7.4f*t),13.7f-1.4f*t*t,z-1.3f*Mathf.Sin(t*Mathf.PI)),.19f,24);
            }
        }
        var damak=Sekil.Parametrik(48,12,false,(u,v)=>{
            float a=u*Mathf.PI;
            return new Vector3(Mathf.Cos(a)*(7+11*v),-1.2f+Mathf.Sin(a)*(9f+9*v),
                -16f+5f*v-1.5f*Mathf.Sin(a)+.3f*Mathf.Sin(v*Mathf.PI));
        },3,1);
        // Kameraya bakan ön yüz.
        var normals=damak.normals;float face=0;foreach(var n in normals)face+=n.z;
        if(face<0){var ids=damak.triangles;for(int i=0;i<ids.Length;i+=3){int tmp=ids[i];ids[i]=ids[i+1];ids[i+1]=tmp;}damak.triangles=ids;damak.RecalculateNormals();damak.RecalculateTangents();}
        parts.Add(new CombineInstance{mesh=damak,transform=Matrix4x4.identity});
        parts.Add(new CombineInstance{mesh=Sekil.SuperElips(.9f,1,16),transform=Matrix4x4.TRS(new Vector3(0,7.6f,-17.1f),Quaternion.identity,new Vector3(.62f,1.45f,.6f))});
        for(int side=-1;side<=1;side+=2){int yon=side;
            Fitil(parts,t=>new Vector3(yon*(.8f+6.6f*t),7.8f-9f*t*t,-17.5f+2f*t),.48f,32);
        }
        Birles(root,"DamakVeKucukDil",parts,mukoza);
        parts.Clear();var tiny=Sekil.SuperElips(1,1,8);
        for(int k=0;k<11;k++){
            float x=(k-5)*1.1f,z=-10+Mathf.Abs(x)*.4f;
            parts.Add(new CombineInstance{mesh=tiny,transform=Matrix4x4.TRS(new Vector3(x,AgizGorsel.DilYuksekligi(x,z),z),Quaternion.identity,new Vector3(.26f,.12f,.26f))});
        }
        Birles(root,"DilKokuPapillalari",parts,dil);
        // Tek mesh, aynı dünya UV'leri: yakın görüşte hacim, uzak görüşte normal haritası.
        var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();var rng=new System.Random(814);
        for(int k=0;k<750;k++){
            float x=(float)rng.NextDouble()*17-8.5f,z=(float)rng.NextDouble()*25-11;
            if(Mathf.Abs(x)<.25f)continue;
            float y=AgizGorsel.DilYuksekligi(x,z),r=.045f+(float)rng.NextDouble()*.065f,h=r*.5f;
            int start=verts.Count;verts.Add(new Vector3(x,y+h,z));uv.Add(new Vector2((x+17)/34,(z+19)/41));
            for(int n=0;n<6;n++){
                float a=n*Mathf.PI/3;var p=new Vector3(x+r*Mathf.Cos(a),y,z+r*Mathf.Sin(a));verts.Add(p);uv.Add(new Vector2((p.x+17)/34,(p.z+19)/41));
            }
            for(int n=0;n<6;n++){tris.Add(start);tris.Add(start+1+(n+1)%6);tris.Add(start+1+n);}
        }
        var mesh=SahneVarliklari.Kaydet(new Mesh{name="DilYuzeyPapillalari"});mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        var go=Sekil.MeshParca(mesh,"DilYuzeyPapillalari",root,Vector3.zero,Vector3.one,dil);go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Fitil(List<CombineInstance> parts,Func<float,Vector3> path,float r,int steps)
    {
        var mesh=Sekil.Parametrik(steps,8,false,(u,v)=>{
            var p=path(u);var tan=(path(Mathf.Min(1,u+.005f))-path(Mathf.Max(0,u-.005f))).normalized;
            var side=Vector3.Cross(tan,Vector3.up).normalized;var up=Vector3.Cross(tan,side);float a=v*2*Mathf.PI;
            float taper=.45f+.55f*Mathf.Sin(Mathf.PI*u);return p+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*r*taper;
        },3,1);
        parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});
    }
    static void Birles(Transform parent,string name,List<CombineInstance> parts,Material mat)
    {
        var mesh=SahneVarliklari.Kaydet(new Mesh{name=name});mesh.CombineMeshes(parts.ToArray());
        var g=Sekil.MeshParca(mesh,name,parent,Vector3.zero,Vector3.one,mat);g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
}
