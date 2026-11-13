using System.Text;
using UnityEngine;

// Profesörün telsiz konuşmalarının seslendirmesi.
// Her cümlenin ses dosyası, cümlenin metninden hesaplanan bir anahtarla
// Assets/Resources/Seslendirme/<anahtar> olarak saklanır (bkz. Araclar/seslendirme).
// Ses dosyası yoksa cümle yalnızca yazıyla gösterilir.
public static class Seslendirme
{
    // Metnin kararlı anahtarı: UTF-8 baytları üzerinden FNV-1a (32 bit), "s_" + 8 onaltılık hane.
    public static string Anahtar(string metin)
    {
        uint h = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(metin))
        {
            h ^= b;
            h *= 16777619;
        }
        return "s_" + h.ToString("x8");
    }

    public static AudioClip Bul(string metin) => Resources.Load<AudioClip>("Seslendirme/" + Anahtar(metin));
}
