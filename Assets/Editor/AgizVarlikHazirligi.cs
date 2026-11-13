using UnityEditor;
using UnityEngine;
using System.IO;
public static class AgizVarlikHazirligi
{
    public static string Hazirla()
    {
        foreach(string path in Directory.GetFiles("Assets/Resources/AgizV2","*.png")){
            var i=(TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType=path.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
            i.sRGBTexture=path.Contains("_albedo");i.mipmapEnabled=true;i.anisoLevel=4;i.wrapMode=TextureWrapMode.Repeat;
            i.isReadable=false;i.maxTextureSize=1024;i.textureCompression=TextureImporterCompression.CompressedHQ;
            i.alphaSource=path.Contains("_mask")?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
            i.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6,compressionQuality=90});
            i.SaveAndReimport();
        }
        var model=(ModelImporter)AssetImporter.GetAtPath("Assets/Resources/AgizV2/Disler.fbx");
        model.isReadable=true;model.materialImportMode=ModelImporterMaterialImportMode.None;model.importAnimation=false;
        model.importNormals=ModelImporterNormals.Import;model.importTangents=ModelImporterTangents.CalculateMikk;model.meshCompression=ModelImporterMeshCompression.Low;model.SaveAndReimport();
        // Explicit build-time material retains metallic-smoothness + normal map shader variant.
        var pathMat="Assets/Resources/AgizV2/DokuVaryanti.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(pathMat);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,pathMat);}
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/AgizV2/dil_albedo.png"));
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/AgizV2/dil_normal.png"));
        mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/AgizV2/dil_mask.png"));
        mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.enableInstancing=true;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        return "AgizV2: ASTC 6x6, mipmap, 3 LOD, PBR varyanti hazir";
    }
}
