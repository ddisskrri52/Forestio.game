using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageThirteenSetup
{
    [MenuItem("Forestio/Setup stage 13 HUD and hints")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player");
        if (player == null) throw new InvalidOperationException("Player is missing.");
        GameObject ui = GameObject.Find("GameHud");
        if (ui == null) ui = new GameObject("GameHud");
        ForestioHud hud = ui.GetComponent<ForestioHud>();
        if (hud == null) hud = ui.AddComponent<ForestioHud>();
        SerializedObject settings = new SerializedObject(hud);
        Set(settings, "player", player.GetComponent<ForestioPlayerController>());
        Set(settings, "health", player.GetComponent<ForestioPlayerHealth>());
        Set(settings, "button", UnityEngine.Object.FindFirstObjectByType<ForestioHitButton>());
        Set(settings, "branches", UnityEngine.Object.FindFirstObjectByType<ForestioBranchBarrier>());
        Set(settings, "notice", UnityEngine.Object.FindFirstObjectByType<ForestioAbilityNotice>());
        Set(settings, "completion", UnityEngine.Object.FindFirstObjectByType<ForestioLevelCompletion>());
        settings.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject health = new SerializedObject(player.GetComponent<ForestioPlayerHealth>());
        health.FindProperty("showHealthText").boolValue = false;
        health.ApplyModifiedPropertiesWithoutUndo();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE13_SETUP_OK: HUD and contextual hints configured; geometry preserved.");
    }
    private static void Set(SerializedObject settings, string field, UnityEngine.Object value)
    {
        if (value == null) throw new InvalidOperationException("Missing HUD reference: " + field);
        settings.FindProperty(field).objectReferenceValue = value;
    }
}
