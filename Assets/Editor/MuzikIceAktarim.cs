using UnityEditor;
using UnityEngine;
public class MuzikIceAktarim : AssetPostprocessor
{
    void OnPreprocessAudio(){
        if(!assetPath.Contains("/Muzik/BouncyAdventure"))return;
        var a=(AudioImporter)assetImporter;var s=a.defaultSampleSettings;
        s.loadType=AudioClipLoadType.Streaming;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.65f;s.sampleRateSetting=AudioSampleRateSetting.OptimizeSampleRate;
        a.defaultSampleSettings=s;a.loadInBackground=true;
    }
}
