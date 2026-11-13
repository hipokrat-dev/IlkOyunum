using UnityEditor;
using UnityEngine;

public class PremiumVarlikAyarlari : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool organ=assetPath.StartsWith("Assets/Resources/Gercekci/");
        bool menu=assetPath.Contains("yolculuk-premium");
        if(!organ && !menu)return;
        var t=(TextureImporter)assetImporter;t.maxTextureSize=organ ? 1024 : 2048;
        t.isReadable=false;t.mipmapEnabled=organ;t.wrapMode=organ ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        t.textureCompression=TextureImporterCompression.CompressedHQ;
        t.SetPlatformTextureSettings(new TextureImporterPlatformSettings {name="Android",overridden=true,maxTextureSize=organ ? 1024 : 2048,format=TextureImporterFormat.ASTC_6x6,compressionQuality=80});
    }
}
