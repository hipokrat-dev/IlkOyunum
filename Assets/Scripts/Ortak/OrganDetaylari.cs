using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Organ bazlı ıslak doku, mikroyüzey ve gerçek mesh kıvrımları.
// Tek bir ortak doku seti / bölüm; hareketli hedeflere ve çarpışma geometrisine dokunmaz.
public class OrganDetaylari : MonoBehaviour
{
    readonly List<Object> uretilen=new List<Object>();
    public static void Kur(GameObject owner,int organ)
    {
        owner.AddComponent<OrganDetaylari>().Uygula(organ);
    }
    void Uygula(int organ)
    {
        if(organ==1)return; // Bu bölümlerin Blender'da hazırlanmış özgün haritaları korunur.
        var sonRenk=Resources.Load<Texture2D>("Gercekci/"+(organ==4 ? "kalp" : organ==7 ? "mesane" : "mukoza"));
        if(sonRenk==null)sonRenk=Resources.Load<Texture2D>("Agiz/mukoza_albedo");
        var sonNormal=Resources.Load<Texture2D>("Agiz/mukoza_normal");
        var cache=new Dictionary<Material,Material>();
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            string name=r.name;
            bool tissue=name=="Duvar" || name=="Kanal" || name=="Tunel" || name=="Taban" || name=="Villus" || name=="MesaneTabani" || name=="MesaneDuvari" || name=="Yaprakcik" || name=="KilcalDamar" || name=="MideDuvari" || name=="MideTabani" || name=="Damar" || name=="AkcigerDokusu";
            if(!tissue || r.sharedMaterial==null)continue;
            var original=r.sharedMaterial;
            if(!cache.TryGetValue(original,out var material)){
                material=new Material(original);uretilen.Add(material);cache[original]=material;
                material.SetTexture("_BaseMap",sonRenk);material.SetColor("_BaseColor",Color.white);
                material.SetTexture("_BumpMap",sonNormal);material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.16f);
                material.SetFloat("_Smoothness",.55f);material.SetFloat("_Metallic",0);
                material.enableInstancing=true;
            }
            r.sharedMaterial=material;
            // Subtle physical relief on enclosed walls. Original gameplay colliders stay intact.
            if(name=="Duvar" || name=="Kanal" || name=="Tunel" || name=="MesaneDuvari"){
                var f=r.GetComponent<MeshFilter>();if(f==null || f.sharedMesh==null || !f.sharedMesh.isReadable)continue;
                var mesh=Instantiate(f.sharedMesh);uretilen.Add(mesh);var v=mesh.vertices;var normalsMesh=mesh.normals;
                for(int i=0;i<v.Length;i++){
                    float relief=(Mathf.Sin(v[i].z*.7f+v[i].x*.9f)*Mathf.Sin(v[i].y*1.3f+v[i].z*.17f))*.065f;
                    v[i]+=normalsMesh[i]*relief;
                }
                mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();f.sharedMesh=mesh;
            }
        }
        YapiEkle(organ,sonRenk,sonNormal);
        // Organic membrane microdetail on cells without changing their recognizable gameplay colors.
        var cells=new HashSet<Material>();
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(r.name!="Alyuvar" && r.name!="Yaprakcik")continue;
            var m=r.sharedMaterial;if(m==null || !cells.Add(m))continue;
            m.SetTexture("_BumpMap",sonNormal);m.SetFloat("_BumpScale",.22f);m.EnableKeyword("_NORMALMAP");
        }
    }
    void YapiEkle(int organ,Texture2D renk,Texture2D normal)
    {
        var m=Sekil.DokuluMalzeme(renk,normal,null,.52f,1,.18f);uretilen.Add(m);m.enableInstancing=true;
        if(organ==2){
            var root=GameObject.Find("Mide");if(root==null)return;
            // Gastric rugae follow the lateral wall, leaving the driving lane clear.
            for(int k=0;k<22;k++){
                int index=k;float angle=(k+.5f)/22*Mathf.PI*2;
                EgriliDoku(root.transform,"MideRugae",t=>{
                    float z=Mathf.Lerp(3,69,t),radius=Mathf.Sqrt(Mathf.Max(.1f,1-Mathf.Pow((z-36)/48,2)));
                    float a=angle+.07f*Mathf.Sin(t*15+index);
                    return new Vector3(21*radius*Mathf.Cos(a),5+13*radius*Mathf.Sin(a),z);
                },.20f,64,m);
            }
        }
        if(organ==4){
            var root=GameObject.Find("DamarVeKalp");if(root==null)return;
            for(int k=0;k<22;k++){
                int index=k;float angle=k*Mathf.PI*2/22;
                EgriliDoku(root.transform,"KalpKasLifi",t=>{
                    float z=Mathf.Lerp(363,424,t);float r=6+8.5f*Tumsek(z,376,15)+9*Tumsek(z,410,16)-.38f;
                    float a=angle+.12f*Mathf.Sin(t*12+index);
                    return new Vector3(r*Mathf.Cos(a),r*Mathf.Sin(a),z);
                },.23f,64,m);
            }
        }
        if(organ==6){
            var root=GameObject.Find("Bobrek");if(root==null)return;
            m.SetColor("_BaseColor",new Color(.95f,.4f,.4f));
            for(int k=0;k<12;k++){
                float a=k*Mathf.PI*2/12;
                EgriliDoku(root.transform,"GlomerulusKilcalAg",t=>{
                    float theta=t*Mathf.PI*6+a;float z=67+t*24;
                    float r=7.2f+1.1f*Mathf.Sin(t*Mathf.PI*8+a);
                    return new Vector3(r*Mathf.Cos(theta),r*Mathf.Sin(theta),z);
                },.14f,72,m);
            }
        }
        if(organ==7){
            var wall=GameObject.Find("MesaneDuvari");if(wall==null)return;
            for(int k=0;k<24;k++){
                float a=k*Mathf.PI*2/24;
                EgriliDoku(wall.transform,"MesaneMukozaKivrimi",t=>{
                    float v=Mathf.Lerp(-1.05f,1.05f,t),angle=a+.06f*Mathf.Sin(t*18+a);
                    return new Vector3(Mathf.Cos(v)*Mathf.Cos(angle),Mathf.Sin(v),Mathf.Cos(v)*Mathf.Sin(angle))*.98f;
                },.012f,48,m);
            }
        }
    }
    static float Tumsek(float z,float center,float width){float d=Mathf.Abs(z-center);return d<width ? .5f*(1+Mathf.Cos(Mathf.PI*d/width)) : 0;}
    void EgriliDoku(Transform parent,string ad,System.Func<float,Vector3> path,float thickness,int steps,Material material)
    {
        var mesh=Sekil.Parametrik(steps,8,false,(u,v)=>{
            var p=path(u);var tangent=(path(Mathf.Min(1,u+.002f))-path(Mathf.Max(0,u-.002f))).normalized;
            var side=Vector3.Cross(tangent,Vector3.up).normalized;
            if(side.sqrMagnitude<.1f)side=Vector3.Cross(tangent,Vector3.right).normalized;
            var up=Vector3.Cross(tangent,side);float a=v*Mathf.PI*2;
            return p+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*thickness*(.8f+.2f*Mathf.Sin(u*31));
        },6,1);
        uretilen.Add(mesh);var g=Sekil.MeshParca(mesh,ad,parent,Vector3.zero,Vector3.one,material);
        g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    void OnDestroy(){foreach(var o in uretilen)if(o!=null)Destroy(o);}
}
