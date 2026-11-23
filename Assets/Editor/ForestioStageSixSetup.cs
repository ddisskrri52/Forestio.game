using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageSixSetup
{
    [MenuItem("Forestio/Setup stage 6 charged throw")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        ForestioPlayerController player = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerController>();
        if (player == null) throw new InvalidOperationException("Player was not loaded.");
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) throw new InvalidOperationException("Ground sprite is required.");
        Sprite sprite = ground.GetComponent<SpriteRenderer>().sprite;
        SpriteRenderer cat = player.GetComponent<SpriteRenderer>();

        Transform bar = player.transform.Find("ChargeBar");
        if (bar == null)
        {
            bar = new GameObject("ChargeBar").transform;
            bar.SetParent(player.transform, false);
        }
        bar.localPosition = new Vector3(0f, 0.62f, 0f);
        bar.localScale = Vector3.one;
        SpriteRenderer background = CreatePart(bar, "Background", sprite, new Color(0.24f, 0.15f, 0.3f, 1f), cat);
        background.transform.localScale = new Vector3(0.72f, 0.1f, 1f);
        SpriteRenderer fill = CreatePart(bar, "Fill", sprite, new Color(0.96f, 0.65f, 0.8f, 1f), cat);
        fill.transform.localScale = new Vector3(0.66f, 0.055f, 1f);
        fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        fill.sortingOrder = background.sortingOrder + 1;
        background.enabled = false;
        fill.enabled = false;

        ForestioChargeDisplay display = bar.GetComponent<ForestioChargeDisplay>();
        if (display == null) display = bar.gameObject.AddComponent<ForestioChargeDisplay>();
        SerializedObject settings = new SerializedObject(display);
        settings.FindProperty("player").objectReferenceValue = player;
        settings.FindProperty("background").objectReferenceValue = background;
        settings.FindProperty("fill").objectReferenceValue = fill;
        settings.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controls = new SerializedObject(player);
        controls.FindProperty("chargeDuration").floatValue = 0.7f;
        controls.FindProperty("chargingMoveSpeed").floatValue = 3f;
        controls.FindProperty("chargedThrowSpeed").floatValue = 16f;
        controls.FindProperty("chargedThrowDistance").floatValue = 16f;
        controls.FindProperty("chargedThrowUnlocked").boolValue = false;
        controls.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE6_SETUP_OK: charged throw settings and charge bar references validated.");
    }

    private static SpriteRenderer CreatePart(Transform parent, string name, Sprite sprite, Color color, SpriteRenderer cat)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = cat.sortingLayerID;
        renderer.sortingOrder = cat.sortingOrder + 5;
        return renderer;
    }
}
