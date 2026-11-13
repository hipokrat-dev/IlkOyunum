using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// UI doğal ekran çözünürlüğünde kalır; yalnızca 3B çizim yükü uyarlanır.
public class MobilKalite : MonoBehaviour
{
    public static bool TestMobil;
    public static bool Mobil => Application.isMobilePlatform || TestMobil;
    static MobilKalite ornek;
    UniversalRenderPipelineAsset kaynak, calisma;
    float toplam, bekle = 8f;
    int kare, ekranY;
    float tavan;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OtomatikBaslat() { if(Application.isMobilePlatform) Kur(); }
    public static void Kur()
    {
        if (!Mobil || ornek != null) return;
        ornek = new GameObject("MobilKalite").AddComponent<MobilKalite>();
        DontDestroyOnLoad(ornek.gameObject);
    }
    void Awake()
    {
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        kaynak = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if(kaynak == null) return;
        calisma = Instantiate(kaynak);
        calisma.name = "MobilCalismaProfili";
        calisma.maxAdditionalLightsCount = 2;
        calisma.shadowDistance = 28;
        calisma.shadowCascadeCount = 1;
        calisma.msaaSampleCount = 1;
        calisma.supportsCameraDepthTexture = false;
        calisma.supportsCameraOpaqueTexture = false;
        QualitySettings.renderPipeline = calisma;
        EkranAyarla();
    }
    void EkranAyarla()
    {
        ekranY = Screen.height;
        tavan = Mathf.Clamp(1080f / Mathf.Max(720, ekranY), .5f, .9f);
        calisma.renderScale = tavan;
    }
    void Update()
    {
        if(calisma == null) return;
        if(ekranY != Screen.height) EkranAyarla();
        if(Time.timeScale == 0 || BolumTemeli.Ornek == null || BolumTemeli.Ornek.DurumAdi != "Oyun") {
            toplam = 0; kare = 0; bekle = 3; return;
        }
        if(bekle > 0) { bekle -= Time.unscaledDeltaTime; return; }
        toplam += Time.unscaledDeltaTime; kare++;
        if(toplam < 4) return;
        float fps = kare / toplam;
        float alt = Mathf.Max(.4f, tavan * .72f);
        if(fps < 52) calisma.renderScale = Mathf.Max(alt, calisma.renderScale - .06f);
        else if(fps > 58) calisma.renderScale = Mathf.Min(tavan, calisma.renderScale + .025f);
        toplam = 0; kare = 0; bekle = 5;
    }
    void OnDestroy()
    {
        if(calisma != null) {
            if(QualitySettings.renderPipeline == calisma) QualitySettings.renderPipeline = kaynak;
            Destroy(calisma);
        }
        if(ornek == this) ornek = null;
    }
}
