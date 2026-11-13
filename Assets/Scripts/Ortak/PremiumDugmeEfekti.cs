using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class PremiumDugmeEfekti : MonoBehaviour
{
    Image resim;RectTransform alan;Color taban;float vurgu;
    void Awake(){resim=GetComponent<Image>();alan=transform as RectTransform;if(resim!=null)taban=resim.color;}
    public void RenkGuncelle(Color c){taban=c;if(resim!=null)resim.color=c;}
    public void Bas(){vurgu=1;}
    void Update(){
        if(resim==null)return;
        bool hover=Mouse.current!=null && RectTransformUtility.RectangleContainsScreenPoint(alan,Mouse.current.position.ReadValue(),null);
        vurgu=Mathf.MoveTowards(vurgu,0,Time.unscaledDeltaTime*5);
        resim.color=Color.Lerp(taban,Color.white,hover ? .12f : vurgu*.15f);
        transform.localScale=Vector3.one*(1-vurgu*.025f);
    }
    void OnDisable(){transform.localScale=Vector3.one;if(resim!=null)resim.color=taban;vurgu=0;}
}
