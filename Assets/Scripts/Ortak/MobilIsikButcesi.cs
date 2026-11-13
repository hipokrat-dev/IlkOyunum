using UnityEngine;
using System.Collections.Generic;
// Mobilde uzak ışıkların CPU/GPU maliyetini sınırlı tutar. Yeni nesne veya dizi üretmez.
public class MobilIsikButcesi : MonoBehaviour
{
    Light[] isiklar;
    Transform arac;
    float sayac;
    readonly List<Light> yakin = new List<Light>(6);
    void Start()
    {
        isiklar=Object.FindObjectsByType<Light>();
        var a=Object.FindFirstObjectByType<AracKontrol>();if(a!=null)arac=a.transform;
    }
    void Update()
    {
        if(arac==null || isiklar==null)return;
        sayac-=Time.unscaledDeltaTime;if(sayac>0)return;sayac=.5f;
        yakin.Clear();
        foreach(var light in isiklar) {
            if(light==null || light.type==LightType.Directional || light.name=="Isaret" || light.transform.IsChildOf(arac))continue;
            light.enabled=false;
            float distance=(light.transform.position-arac.position).sqrMagnitude;
            if(distance > (light.range+18)*(light.range+18))continue;
            int pos=0;
            while(pos<yakin.Count && (yakin[pos].transform.position-arac.position).sqrMagnitude<distance)pos++;
            if(pos>=4)continue;
            yakin.Insert(pos,light);
            if(yakin.Count>4)yakin.RemoveAt(4);
        }
        foreach(var light in yakin)light.enabled=true;
    }
}
