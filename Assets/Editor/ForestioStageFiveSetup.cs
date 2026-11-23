using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ForestioStageFiveSetup
{
    private const string ScenePath = "Assets/Scenes/ForestTest.unity";

    [MenuItem("Forestio/Setup stage 5 module")]
    public static void Configure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ForestioPlayerController player = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerController>();
        if (player == null)
            throw new InvalidOperationException("Player controller was not loaded. Scene has not been saved.");

        GameObject noticeObject = GameObject.Find("AbilityNotice");
        if (noticeObject == null) noticeObject = new GameObject("AbilityNotice");
        ForestioAbilityNotice notice = noticeObject.GetComponent<ForestioAbilityNotice>();
        if (notice == null) notice = noticeObject.AddComponent<ForestioAbilityNotice>();

        GameObject moduleObject = GameObject.Find("AbilityModule");
        bool created = moduleObject == null;
        if (created) moduleObject = new GameObject("AbilityModule");
        SpriteRenderer renderer = moduleObject.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = moduleObject.AddComponent<SpriteRenderer>();
        GameObject pile = GameObject.Find("StonePile");
        if (pile == null || pile.GetComponent<SpriteRenderer>() == null)
            throw new InvalidOperationException("StonePile sprite is needed for the temporary module visual.");
        renderer.sprite = pile.GetComponent<SpriteRenderer>().sprite;
        renderer.color = new Color(1f, 0.82f, 0.35f, 1f);
        renderer.sortingOrder = 2;
        if (created)
        {
            moduleObject.transform.position = new Vector3(6.2f, -2f, 0f);
            moduleObject.transform.localScale = Vector3.one * 0.55f;
        }

        CircleCollider2D trigger = moduleObject.GetComponent<CircleCollider2D>();
        if (trigger == null) trigger = moduleObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        ForestioAbilityModule module = moduleObject.GetComponent<ForestioAbilityModule>();
        if (module == null) module = moduleObject.AddComponent<ForestioAbilityModule>();
        SerializedObject settings = new SerializedObject(module);
        settings.FindProperty("notice").objectReferenceValue = notice;
        settings.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name + "; scene has not been saved.");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE5_SETUP_OK: Module and notice are connected; existing scene objects were preserved.");
    }
}
