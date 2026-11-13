using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Bölüm sahnelerini oluşturan ve oynatan editör menüsü. Bölümler çalışma anında kodla kurulduğu için
// her sahnede yalnızca bölümü başlatan tek bir nesne bulunur.
static class BolumSahneleri
{
    // Derleme listesindeki sıra = oyundaki bölüm sırası
    static readonly (string ad, Type tur)[] Bolumler =
    {
        ("AnaMenu", typeof(AnaMenu)),
        ("Agiz", typeof(AgizBolumu)),
        ("YemekBorusuMide", typeof(MideBolumu)),
        ("Bagirsak", typeof(BagirsakBolumu)),
        ("KanKalp", typeof(KalpBolumu)),
        ("Akciger", typeof(AkcigerBolumu)),
        ("Bobrek", typeof(BobrekBolumu)),
        ("Mesane", typeof(MesaneBolumu)),
    };

    public static string Yol(string ad) => $"Assets/Scenes/{ad}.unity";

    [MenuItem("Vücut Yolculuğu/Oyunu Baştan Oyna (Ana Menü)")]
    public static void MenuOyna() => Oyna("AnaMenu");

    [MenuItem("Vücut Yolculuğu/Bölüm 1 (Ağız) Oyna")]
    public static void AgizOyna() => Oyna("Agiz");

    [MenuItem("Vücut Yolculuğu/Bölüm 2 (Yemek Borusu ve Mide) Oyna")]
    public static void MideOyna() => Oyna("YemekBorusuMide");

    [MenuItem("Vücut Yolculuğu/Bölüm 3 (Bağırsak) Oyna")]
    public static void BagirsakOyna() => Oyna("Bagirsak");

    [MenuItem("Vücut Yolculuğu/Bölüm 4 (Kan Damarları ve Kalp) Oyna")]
    public static void KalpOyna() => Oyna("KanKalp");

    [MenuItem("Vücut Yolculuğu/Bölüm 5 (Akciğer gezintisi) Oyna")]
    public static void AkcigerOyna() => Oyna("Akciger");

    [MenuItem("Vücut Yolculuğu/Bölüm 6 (Böbrek) Oyna")]
    public static void BobrekOyna() => Oyna("Bobrek");

    [MenuItem("Vücut Yolculuğu/Bölüm 7 (Mesane ve Çıkış) Oyna")]
    public static void MesaneOyna() => Oyna("Mesane");

    // Sahneyi açıp oyunu başlatır. Komut satırından da çağrılabilir (-executeMethod BolumSahneleri.AgizOyna).
    static void Oyna(string ad)
    {
        if (EditorApplication.isPlaying) return;
        EditorSceneManager.OpenScene(Yol(ad));
        EditorApplication.delayCall += () =>
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.EnterPlaymode();
        };
    }

    [MenuItem("Vücut Yolculuğu/İlerlemeyi Sıfırla (yıldızlar ve kartlar)")]
    public static void YildizlariSifirla()
    {
        foreach (var b in BolumListesi.Tumu)
        {
            foreach (string anahtar in new[] { "yildiz_", "bitti_", "kart_baslik_", "kart_metin_" }) PlayerPrefs.DeleteKey(anahtar + b.no);
        }
        PlayerPrefs.Save();
        Debug.Log("Kayıtlı ilerleme sıfırlandı.");
    }

    [MenuItem("Vücut Yolculuğu/Eksik Sahneleri Oluştur")]
    static void SahneMenusu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SahneleriOlustur();
    }

    // Eksik bölüm sahnelerini oluşturur ve derleme listesini bölüm sırasına göre düzenler.
    public static void SahneleriOlustur()
    {
        var liste = new List<EditorBuildSettingsScene>();
        foreach (var (ad, tur) in Bolumler)
        {
            if (!File.Exists(Yol(ad)))
            {
                var sahne = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject(tur.Name).AddComponent(tur);
                EditorSceneManager.SaveScene(sahne, Yol(ad));
                Debug.Log("Sahne oluşturuldu: " + Yol(ad));
            }
            liste.Add(new EditorBuildSettingsScene(Yol(ad), true));
        }
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!liste.Exists(x => x.path == s.path)) liste.Add(s);
        }
        EditorBuildSettings.scenes = liste.ToArray();
        AssetDatabase.SaveAssets();
    }
}
