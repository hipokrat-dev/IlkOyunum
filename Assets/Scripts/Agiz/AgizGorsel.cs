using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// Ağıza özel PBR varlıkları; üç kademe ayrıntı ve tek çizimde diş eti birleşimleri.
public static class AgizGorsel
{
    public static Material Malzeme(string ad,float parlaklik,float tekrar,float normal,bool ao=false)
    {
        var m=Sekil.DokuluMalzeme(Resources.Load<Texture2D>("AgizV2/"+ad+"_albedo"),
            Resources.Load<Texture2D>("AgizV2/"+ad+"_normal"),
            ao?Resources.Load<Texture2D>("AgizV2/"+ad+"_ao"):null,parlaklik,tekrar,normal);
        var mask=Resources.Load<Texture2D>("AgizV2/"+ad+"_mask");
        if(mask!=null){m.SetTexture("_MetallicGlossMap",mask);m.EnableKeyword("_METALLICSPECGLOSSMAP");}
        if(ao)m.SetFloat("_OcclusionStrength",.5f);
        return m;
    }
    public static GameObject Dis(Transform parent,string name,Vector3 pos,Quaternion rot,Material mat,Mesh yakin,Mesh orta,Mesh uzak,bool golge)
    {
        var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.SetLocalPositionAndRotation(pos,rot);
        var lods=new LOD[3];var meshes=new[]{yakin,orta,uzak};float[] limits={.19f,.075f,.001f};
        for(int i=0;i<3;i++){
            var g=Sekil.MeshParca(meshes[i],"Ayrinti_"+i,root.transform,Vector3.zero,Vector3.one,mat);
            var r=g.GetComponent<Renderer>();r.shadowCastingMode=golge?ShadowCastingMode.On:ShadowCastingMode.Off;
            lods[i]=new LOD(limits[i],new[]{r});
        }
        var group=root.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.None;group.SetLODs(lods);group.RecalculateBounds();
        return root;
    }
    public static float DilYuksekligi(float x,float z)
    {
        // Büyük dil oluğu ve hafif kıvrımlar; bütün sürüş yüzeyi düz fizik zemininin altında.
        float groove=-.20f*Mathf.Exp(-x*x/.52f);
        float fold=.035f*Mathf.Sin(z*.58f+Mathf.Abs(x)*.8f)+.025f*Mathf.Sin(x*1.4f+z*.23f);
        float y=-.07f+groove+fold;
        float arka=Mathf.Max(0,-14-z);return y-.35f*arka*arka;
    }
    public static GameObject Biyofilm(Transform parent,Vector3 pos,Quaternion rot,Material mat,int seed)
    {
        var root=new GameObject("Biyofilm");root.transform.SetParent(parent,false);root.transform.SetLocalPositionAndRotation(pos,rot);
        var shape=Sekil.SuperElips(.8f,1,6);var parts=new List<CombineInstance>();var random=new System.Random(719+seed);
        for(int i=0;i<9;i++){
            float x=(float)(random.NextDouble()-.5)*.65f,y=(float)(random.NextDouble()-.5)*.24f;
            float s=.1f+(float)random.NextDouble()*.13f;
            parts.Add(new CombineInstance {mesh=shape,transform=Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.identity,new Vector3(s,s*.6f,.025f))});
        }
        var mesh=SahneVarliklari.Kaydet(new Mesh {name="BiyofilmBirlesik"});mesh.CombineMeshes(parts.ToArray());
        var go=Sekil.MeshParca(mesh,"Birikim",root.transform,Vector3.zero,Vector3.one,mat);
        go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return root;
    }
    public static void KameraAyarla(Camera cam)
    {
        var volume=cam.GetComponentInChildren<Volume>();if(volume==null)return;
        ColorAdjustments color;if(volume.profile.TryGet(out color)){
            color.postExposure.value=.22f;color.contrast.value=5;color.saturation.value=-3;
        }
        Bloom bloom;if(volume.profile.TryGet(out bloom)){
            bloom.threshold.value=1.15f;bloom.intensity.value=.18f;bloom.scatter.value=.55f;
            bloom.highQualityFiltering.value=false;bloom.downscale.value=BloomDownscaleMode.Quarter;
        }
        Vignette vignette;if(volume.profile.TryGet(out vignette))vignette.intensity.value=.14f;
    }
}
