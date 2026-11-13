using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Kodla üretilen native mesh/materyaller sahne kapanınca da serbest bırakılır.
// Resources ile yüklenen ortak varlıklar buraya kaydedilmez.
public class SahneVarliklari : MonoBehaviour
{
    static SahneVarliklari sahip;
    readonly HashSet<Object> varliklar = new HashSet<Object>();
    public static T Kaydet<T>(T asset) where T : Object
    {
        if(sahip == null || sahip.gameObject.scene != SceneManager.GetActiveScene())
            sahip = new GameObject("SahneVarliklari").AddComponent<SahneVarliklari>();
        sahip.varliklar.Add(asset);
        return asset;
    }
    void OnDestroy() { foreach(var asset in varliklar) if(asset != null) Destroy(asset); }
}
