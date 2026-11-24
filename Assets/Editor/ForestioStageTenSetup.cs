using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageTenSetup
{
    [MenuItem("Forestio/Setup stage 10 hazards")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player");
        GameObject ground = GameObject.Find("Ground");
        if (player == null || ground == null) throw new InvalidOperationException("Required objects missing.");
        if (player.GetComponent<ForestioSafePoint>() == null) player.AddComponent<ForestioSafePoint>();
        SpriteRenderer groundVisual = ground.GetComponent<SpriteRenderer>();
        BoxCollider2D originalCollider = ground.GetComponent<BoxCollider2D>();
        // Collider.bounds is empty after disabling the original floor, including on a repeated setup.
        Bounds floor = new Bounds(ground.transform.TransformPoint(originalCollider.offset),
            Vector3.Scale(originalCollider.size, ground.transform.lossyScale));
        if (Mathf.Abs(floor.min.x + 8f) > 0.01f || Mathf.Abs(floor.max.x - 8f) > 0.01f
            || Mathf.Abs(floor.max.y + 2.5f) > 0.01f)
            throw new InvalidOperationException("Ground dimensions changed; hazard positions need adjustment.");
        // Preserve the original Ground transform and sprite reference for existing setup tools.
        // Visible static segments leave a real 1.2-unit opening between x=-7 and x=-5.8.
        MakeGroundSegment(ground, groundVisual, "PitLeftBank", -8f, -7f);
        MakeGroundSegment(ground, groundVisual, "PitRightBank", -5.8f, 8f);
        groundVisual.enabled = false;
        ground.GetComponent<BoxCollider2D>().enabled = false;
        Sprite square = groundVisual.sprite;
        Hazard("Spikes", new Vector3(3.7f, -2.35f, 0f), new Vector3(0.8f, 0.3f, 1f), square, true);
        // Ground top is -2.5: the pit trigger starts exactly one unit below it.
        Hazard("PitTrigger", new Vector3(-6.4f, -7.5f, 0f), new Vector3(1.2f, 8f, 1f), square, false);
        // Catch accidental falls beyond the temporary test-floor edges as well.
        Hazard("FallSafetyNet", new Vector3(0f, -20f, 0f), new Vector3(100f, 1f, 1f), square, false);
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE10_SETUP_OK: pit, spikes and safe-point tracking added.");
    }

    private static void MakeGroundSegment(GameObject ground, SpriteRenderer source, string name, float left, float right)
    {
        Transform segment = ground.transform.Find(name);
        if (segment == null)
        {
            segment = new GameObject(name).transform;
            segment.SetParent(ground.transform, false);
            segment.position = new Vector3((left + right) / 2f, ground.transform.position.y, ground.transform.position.z);
            segment.localScale = new Vector3((right - left) / ground.transform.lossyScale.x, 1f, 1f);
        }
        SpriteRenderer visual = segment.GetComponent<SpriteRenderer>();
        if (visual == null) visual = segment.gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = source.sprite;
        visual.color = source.color;
        visual.sortingLayerID = source.sortingLayerID;
        visual.sortingOrder = source.sortingOrder;
        BoxCollider2D collider = segment.GetComponent<BoxCollider2D>();
        if (collider == null) collider = segment.gameObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static void Hazard(string name, Vector3 position, Vector3 scale, Sprite sprite, bool visible)
    {
        GameObject hazard = GameObject.Find(name);
        if (hazard == null)
        {
            hazard = new GameObject(name);
            hazard.transform.position = position;
            hazard.transform.localScale = scale;
        }
        BoxCollider2D collider = hazard.GetComponent<BoxCollider2D>();
        if (collider == null) collider = hazard.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        if (hazard.GetComponent<ForestioHazard>() == null) hazard.AddComponent<ForestioHazard>();
        if (!visible) return;
        SpriteRenderer visual = hazard.GetComponent<SpriteRenderer>();
        if (visual == null) visual = hazard.AddComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.color = new Color(1f, 0.35f, 0.55f, 1f);
        visual.sortingOrder = 2;
    }
}
