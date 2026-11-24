using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageNineSetup
{
    [MenuItem("Forestio/Setup stage 9 player health")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        GameObject ground = GameObject.Find("Ground");
        GameObject platform = GameObject.Find("Platform");
        GameObject pile = GameObject.Find("StonePile");
        if (ground == null || platform == null || pile == null)
            throw new InvalidOperationException("Required scene objects were not loaded.");

        GameObject player = GameObject.Find("Player");
        if (player == null) throw new InvalidOperationException("Player is missing.");
        if (player.GetComponent<ForestioPlayerHealth>() == null)
            player.AddComponent<ForestioPlayerHealth>();
        GameObject food = GameObject.Find("Food");
        if (food == null)
        {
            food = new GameObject("Food");
            food.transform.position = new Vector3(-3.5f, -2.1f, 0f);
            food.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        }
        SpriteRenderer foodVisual = food.GetComponent<SpriteRenderer>();
        if (foodVisual == null) foodVisual = food.AddComponent<SpriteRenderer>();
        foodVisual.sprite = pile.GetComponent<SpriteRenderer>().sprite;
        foodVisual.color = new Color(1f, 0.75f, 0.38f, 1f);
        if (food.GetComponent<CircleCollider2D>() == null) food.AddComponent<CircleCollider2D>();
        food.GetComponent<CircleCollider2D>().isTrigger = true;
        if (food.GetComponent<ForestioFood>() == null) food.AddComponent<ForestioFood>();

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE9_SETUP_OK: health and food added; existing objects preserved.");
    }
}
