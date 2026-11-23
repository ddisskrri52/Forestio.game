using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageSevenSetup
{
    [MenuItem("Forestio/Setup stage 7 branch barrier")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) throw new InvalidOperationException("Ground was not loaded.");

        GameObject barrier = GameObject.Find("DryBranches");
        bool created = barrier == null;
        if (created) barrier = new GameObject("DryBranches");
        if (created)
        {
            barrier.transform.position = new Vector3(-5.2f, -0.25f, 0f);
            barrier.transform.localScale = new Vector3(0.65f, 4.5f, 1f);
        }

        SpriteRenderer renderer = barrier.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = barrier.AddComponent<SpriteRenderer>();
        renderer.sprite = ground.GetComponent<SpriteRenderer>().sprite;
        renderer.color = new Color(0.58f, 0.34f, 0.26f, 1f);
        BoxCollider2D collider = barrier.GetComponent<BoxCollider2D>();
        if (collider == null) collider = barrier.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.size = Vector2.one;
        if (barrier.GetComponent<ForestioBranchBarrier>() == null)
            barrier.AddComponent<ForestioBranchBarrier>();

        if (Mathf.Abs(barrier.transform.lossyScale.y) * collider.size.y < 4f)
            throw new InvalidOperationException("Barrier is too low for the prototype jump height.");

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE7_SETUP_OK: solid branch barrier added; existing scene objects preserved.");
    }
}
