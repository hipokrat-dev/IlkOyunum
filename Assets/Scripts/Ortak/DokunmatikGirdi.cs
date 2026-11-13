using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Tablet/telefon için dokunmatik kontroller. Ekranda ölü alan yoktur:
//  - Ekranın sol yarısında nereye dokunulursa orada bir sürüş çubuğu belirir.
//  - Sağ yarının tamamı köpük düğmesidir (dalış düğmesinin alanı hariç).
// Fare tıklaması da "dokunma" sayılır, böylece ekran düğmeleri bilgisayarda da çalışır.
public static class DokunmatikGirdi
{
    public static bool Etkin { get; private set; }           // dokunmatik kullanılıyor mu (ekran kontrolleri gösterilsin)
    public static Vector2 Cubuk { get; private set; }        // sürüş çubuğu, -1..1
    public static bool Ates { get; private set; }
    public static bool Dalis { get; private set; }
    public static bool CubukTutuluyor { get; private set; }
    public static Vector2 CubukMerkezi { get; private set; } // ekran pikseli
    public static bool DokunmaBasladi { get; private set; }  // bu karede yeni bir dokunma/tıklama başladı
    public static Vector2 DokunmaYeri { get; private set; }

    public static Func<Vector2, bool> dalisAlani;            // dalış düğmesi gösteriliyorsa: bu nokta onun üstünde mi
    public static Func<Vector2, bool> digerDugme;            // başka bir ekran düğmesinin üstünde mi (köpük sayılmasın)

    static int cubukParmak = -1, sonKare = -1;

    // Karede bir kez çalışır; kaç yerden çağrılırsa çağrılsın aynı sonucu verir.
    public static void Guncelle()
    {
        if (Time.frameCount == sonKare) return;
        sonKare = Time.frameCount;
        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();

        Ates = Dalis = DokunmaBasladi = false;
        bool cubukBulundu = false;
        var dokunuslar = Touch.activeTouches;
        if (dokunuslar.Count > 0 || Application.isMobilePlatform) Etkin = true;

        float cubukMenzili = Mathf.Max(60f, Screen.height * 0.13f);
        foreach (Touch d in dokunuslar)
        {
            Vector2 p = d.screenPosition;
            bool basladi = d.phase == TouchPhase.Began;
            bool bitti = d.phase == TouchPhase.Ended || d.phase == TouchPhase.Canceled;
            if (basladi)
            {
                DokunmaBasladi = true;
                DokunmaYeri = p;
            }

            if (d.touchId == cubukParmak || (basladi && cubukParmak < 0 && p.x < Screen.width * 0.5f))
            {
                if (basladi)
                {
                    cubukParmak = d.touchId;
                    CubukMerkezi = p;
                }
                if (bitti)
                {
                    cubukParmak = -1;
                    continue;
                }
                cubukBulundu = true;
                Cubuk = Vector2.ClampMagnitude((p - CubukMerkezi) / cubukMenzili, 1f);
            }
            else if (!bitti && p.x >= Screen.width * 0.5f)
            {
                if (dalisAlani != null && dalisAlani(p)) Dalis = true;
                else if (digerDugme == null || !digerDugme(p)) Ates = true;
            }
        }

        if (!cubukBulundu)
        {
            cubukParmak = -1;
            Cubuk = Vector2.zero;
        }
        CubukTutuluyor = cubukBulundu;

        var fare = Mouse.current;
        if (fare != null && fare.leftButton.wasPressedThisFrame && !DokunmaBasladi)
        {
            DokunmaBasladi = true;
            DokunmaYeri = fare.position.ReadValue();
        }
    }
}
