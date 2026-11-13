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
        ("Agiz", typeof(AgizBolumu)),
        ("YemekBorusuMide", typeof(MideBolumu)),
    };

    public static string Yol(string ad) => $"Assets/Scenes/{ad}.unity";

    [MenuItem("Vücut Yolculuğu/Bölüm 1 (Ağız) Oyna")]
    public static void AgizOyna() => Oyna("Agiz");

    [MenuItem("Vücut Yolculuğu/Bölüm 2 (Yemek Borusu ve Mide) Oyna")]
    public static void MideOyna() => Oyna("YemekBorusuMide");

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
