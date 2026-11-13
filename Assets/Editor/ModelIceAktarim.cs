using UnityEditor;
using UnityEngine;

// Assets/Resources altındaki (Blender'dan gelen) model ve dokuların içe aktarma ayarları.
class ModelIceAktarim : AssetPostprocessor
{
    bool Ilgili => assetPath.StartsWith("Assets/Resources/");

    void OnPreprocessTexture()
    {
        if (!Ilgili) return;
        var doku = (TextureImporter)assetImporter;
        if (assetPath.EndsWith("_normal.png")) doku.textureType = TextureImporterType.NormalMap;
        else if (assetPath.EndsWith("_ao.png")) doku.sRGBTexture = false;
        doku.wrapMode = TextureWrapMode.Repeat;
        doku.anisoLevel = 8;
    }

    void OnPreprocessModel()
    {
        if (!Ilgili) return;
        var model = (ModelImporter)assetImporter;
        model.materialImportMode = ModelImporterMaterialImportMode.None;   // malzemeler kodda kuruluyor
        model.importAnimation = false;
        model.importCameras = false;
        model.importLights = false;
        model.importTangents = ModelImporterTangents.CalculateMikk;
    }
}
