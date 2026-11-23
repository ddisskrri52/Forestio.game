using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageEightSetup
{
    [MenuItem("Forestio/Setup stage 8 patrol robot")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        GameObject ground = GameObject.Find("Ground");
        GameObject platform = GameObject.Find("Platform");
        GameObject pile = GameObject.Find("StonePile");
        if (ground == null || platform == null || pile == null)
            throw new InvalidOperationException("Required scene objects were not loaded.");

        GameObject robot = GameObject.Find("PatrolRobot");
        bool created = robot == null;
        if (created)
        {
            robot = new GameObject("PatrolRobot");
            Bounds surface = platform.GetComponent<Collider2D>().bounds;
            robot.transform.position = new Vector3(surface.center.x, surface.max.y + 0.52f, 0f);
            robot.transform.localScale = new Vector3(0.8f, 1f, 1f);
        }
        SpriteRenderer visual = robot.GetComponent<SpriteRenderer>();
        if (visual == null) visual = robot.AddComponent<SpriteRenderer>();
        visual.sprite = ground.GetComponent<SpriteRenderer>().sprite;
        visual.color = new Color(0.48f, 0.58f, 0.7f, 1f);
        visual.sortingOrder = 2;
        BoxCollider2D collider = robot.GetComponent<BoxCollider2D>();
        if (collider == null) collider = robot.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = false;
        Rigidbody2D body = robot.GetComponent<Rigidbody2D>();
        if (body == null) body = robot.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        Transform eyeTransform = robot.transform.Find("Eye");
        if (eyeTransform == null)
        {
            eyeTransform = new GameObject("Eye").transform;
            eyeTransform.SetParent(robot.transform, false);
            eyeTransform.localPosition = new Vector3(0.22f, 0.1f, -0.01f);
            eyeTransform.localScale = new Vector3(0.2f, 0.2f, 1f);
        }
        SpriteRenderer eye = eyeTransform.GetComponent<SpriteRenderer>();
        if (eye == null) eye = eyeTransform.gameObject.AddComponent<SpriteRenderer>();
        eye.sprite = pile.GetComponent<SpriteRenderer>().sprite;
        eye.color = new Color(1f, 0.49f, 0.66f, 1f);
        eye.sortingOrder = 3;
        ForestioRobotController controller = robot.GetComponent<ForestioRobotController>();
        if (controller == null) controller = robot.AddComponent<ForestioRobotController>();
        SerializedObject settings = new SerializedObject(controller);
        settings.FindProperty("eye").objectReferenceValue = eye;
        settings.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE8_SETUP_OK: patrol robot added; existing objects preserved.");
    }
}
