using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Bölüm sahnelerini oluşturan editör menüsü. Bölümler çalışma anında kodla kurulduğu için
// sahnede yalnızca bölümü başlatan tek bir nesne bulunur.
static class BolumSahneleri
{
    public const string AgizYolu = "Assets/Scenes/Agiz.unity";

    [MenuItem("Vücut Yolculuğu/Ağız Sahnesini Oluştur")]
    static void AgizMenusu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AgizSahnesiOlustur();
    }

    public static void AgizSahnesiOlustur()
    {
        var sahne = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("AgizBolumu").AddComponent<AgizBolumu>();
        EditorSceneManager.SaveScene(sahne, AgizYolu);

        // Ağız sahnesini derleme listesinin başına koy.
        var liste = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(AgizYolu, true) };
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path != AgizYolu) liste.Add(s);
        }
        EditorBuildSettings.scenes = liste.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Ağız sahnesi oluşturuldu: " + AgizYolu);
    }
}
