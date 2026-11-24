using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageTwelveSetup
{
    [MenuItem("Forestio/Setup stage 12 portal and ending")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        GameObject pile = GameObject.Find("StonePile");
        GameObject branches = GameObject.Find("DryBranches");
        if (pile == null || branches == null) throw new InvalidOperationException("Level prerequisites missing.");
        GameObject ending = GameObject.Find("LevelCompletion");
        if (ending == null) ending = new GameObject("LevelCompletion");
        ForestioLevelCompletion completion = ending.GetComponent<ForestioLevelCompletion>();
        if (completion == null) completion = ending.AddComponent<ForestioLevelCompletion>();
        GameObject portal = GameObject.Find("Portal");
        if (portal == null)
        {
            portal = new GameObject("Portal");
            portal.transform.position = new Vector3(-15.1f, 2.95f, 0f);
            portal.transform.localScale = new Vector3(1.3f, 1.9f, 1f);
        }
        SpriteRenderer visual = portal.GetComponent<SpriteRenderer>();
        if (visual == null) visual = portal.AddComponent<SpriteRenderer>();
        visual.sprite = pile.GetComponent<SpriteRenderer>().sprite;
        visual.color = new Color(0.58f, 0.95f, 0.8f, 0.9f);
        visual.sortingOrder = -1;
        BoxCollider2D collider = portal.GetComponent<BoxCollider2D>();
        if (collider == null) collider = portal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;
        ForestioPortal entrance = portal.GetComponent<ForestioPortal>();
        if (entrance == null) entrance = portal.AddComponent<ForestioPortal>();
        SerializedObject settings = new SerializedObject(entrance);
        settings.FindProperty("completion").objectReferenceValue = completion;
        settings.FindProperty("requiredBarrier").objectReferenceValue = branches.GetComponent<ForestioBranchBarrier>();
        settings.ApplyModifiedPropertiesWithoutUndo();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save level.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE12_SETUP_OK: portal and completion screen added; existing layout preserved.");
    }
}
