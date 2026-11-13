using System;
using UnityEngine;
using UnityEngine.Rendering;

// Hareket eden ince sıvı tabakası, kıvrılan dalga tepesi ve köpüklü ön kenar.
public class TukurukDalgasi : MonoBehaviour
{
    public Action carpti;
    public const float GuvenliZiplamaYuksekligi=1.4f;
    AracKontrol arac;float yariGenislik,hiz,zBaslangic,zBitis;bool bildirildi;
    Mesh mesh;Vector3[] vertices;Vector2[] uv;LineRenderer kopuk;Vector3[] kopukNoktalari=new Vector3[33];
    Material su,foam;
    const int X=32,Z=14;
    public static TukurukDalgasi Olustur(AracKontrol arac,Material material,float x,float yari,float hiz,float zBas,float zSon)
    {
        var g=new GameObject("TukurukDalgasi");g.transform.position=new Vector3(x,.025f,zBas);
        var d=g.AddComponent<TukurukDalgasi>();d.arac=arac;d.yariGenislik=yari;d.hiz=hiz;d.zBaslangic=zBas;d.zBitis=zSon;d.Kur(material);return d;
    }
    void Kur(Material original)
    {
        su=new Material(original);su.color=new Color(.68f,.84f,.87f,.38f);su.SetColor("_BaseColor",su.color);su.SetFloat("_Smoothness",.94f);
        vertices=new Vector3[(X+1)*(Z+1)];uv=new Vector2[vertices.Length];var tris=new int[X*Z*6];int k=0;
        for(int z=0;z<=Z;z++)for(int x=0;x<=X;x++){int n=z*(X+1)+x;uv[n]=new Vector2((float)x/X,(float)z/Z);if(x==X||z==Z)continue;tris[k++]=n;tris[k++]=n+X+1;tris[k++]=n+1;tris[k++]=n+1;tris[k++]=n+X+1;tris[k++]=n+X+2;}
        mesh=new Mesh{name="CanliTukurukYuzeyi"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris;
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=su;r.shadowCastingMode=ShadowCastingMode.Off;
        var edge=new GameObject("DalgaKopugu");edge.transform.SetParent(transform,false);kopuk=edge.AddComponent<LineRenderer>();kopuk.useWorldSpace=false;kopuk.positionCount=X+1;kopuk.widthMultiplier=.055f;kopuk.numCornerVertices=2;kopuk.numCapVertices=2;
        foam=Sekil.Malzeme(new Color(.88f,.96f,1,.58f),.65f);kopuk.sharedMaterial=foam;kopuk.shadowCastingMode=ShadowCastingMode.Off;
        GuncelleMesh(.15f);
    }
    void GuncelleMesh(float growth)
    {
        for(int z=0;z<=Z;z++)for(int x=0;x<=X;x++){
            float u=(float)x/X,v=(float)z/Z;
            float lateral=(u-.5f)*2;float edge=Mathf.Pow(Mathf.Clamp01(1-lateral*lateral),.3f);
            float crest=Mathf.Exp(-Mathf.Pow((v-.24f)*7,2));
            float wave=.09f*Mathf.Sin(u*24+Time.time*4)+.04f*Mathf.Sin(u*43-Time.time*6);
            float height=(.045f+crest*(1.12f+wave))*edge;
            vertices[z*(X+1)+x]=new Vector3(lateral*yariGenislik*growth,height,(v-.38f)*3.4f+.13f*Mathf.Sin(u*13+Time.time*2));
        }
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        for(int x=0;x<=X;x++)kopukNoktalari[x]=vertices[3*(X+1)+x]+Vector3.up*.015f;
        kopuk.SetPositions(kopukNoktalari);
    }
    void Update()
    {
        if(Time.deltaTime<=0)return;
        transform.position+=Vector3.back*hiz*Time.deltaTime;float z=transform.position.z;
        float growth=Mathf.Clamp((zBaslangic-z)/7,.15f,1);GuncelleMesh(growth);
        if(arac!=null){var p=arac.transform.position;
            if(p.y<GuvenliZiplamaYuksekligi && Mathf.Abs(p.z-z)<1.7f && Mathf.Abs(p.x-transform.position.x)<yariGenislik*growth+.5f){
                arac.Itki(Vector3.back*hiz*1.5f);if(!bildirildi){bildirildi=true;carpti?.Invoke();}
            }
        }
        if(z<zBitis)Destroy(gameObject);
    }
    void OnDestroy(){if(mesh!=null)Destroy(mesh);if(su!=null)Destroy(su);if(foam!=null)Destroy(foam);}
}
