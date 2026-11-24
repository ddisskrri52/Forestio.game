using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioStageElevenSetup
{
    public const string LevelPath = "Assets/Scenes/Outskirts.unity";

    [MenuItem("Forestio/Setup stage 11 Outskirts level")]
    public static void Configure()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelPath) == null
            && !AssetDatabase.CopyAsset("Assets/Scenes/ForestTest.unity", LevelPath))
            throw new InvalidOperationException("Unable to create playable scene.");
        var scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Ground") ground = root;
        if (ground == null) throw new InvalidOperationException("Original floor is missing.");
        Sprite square = ground.GetComponent<SpriteRenderer>().sprite;
        GameObject pile = Required("StonePile");
        Sprite circle = pile.GetComponent<SpriteRenderer>().sprite;
        ground.SetActive(false);

        GameObject geometry = GameObject.Find("LevelGeometry");
        if (geometry == null) geometry = new GameObject("LevelGeometry");
        Color floorColor = new Color(0.76f, 0.83f, 0.77f);
        Color ledgeColor = new Color(0.87f, 0.77f, 0.87f);
        Block(geometry.transform, "MainGround", new Vector3(-3f, -3f), new Vector3(30f, 1f, 1f), square, floorColor);
        Block(geometry.transform, "ModuleGround", new Vector3(18f, -3f), new Vector3(8f, 1f, 1f), square, floorColor);
        Block(geometry.transform, "LeftBoundary", new Vector3(-18.25f, 3f), new Vector3(0.5f, 16f, 1f), square, floorColor);
        Block(geometry.transform, "RightBoundary", new Vector3(22.25f, 3f), new Vector3(0.5f, 16f, 1f), square, floorColor);
        Ledge(geometry.transform, "TrainingLedge", -11.8f, 2f, -1f, square, ledgeColor);
        Ledge(geometry.transform, "ButtonStepOne", 3f, 2.4f, -1f, square, ledgeColor);
        Ledge(geometry.transform, "ButtonStepTwo", 6f, 2.4f, 0.5f, square, ledgeColor);
        Ledge(geometry.transform, "FinalStepOne", -2.5f, 2.4f, -1f, square, ledgeColor);
        Ledge(geometry.transform, "FinalStepTwo", -5.3f, 2.4f, 0.5f, square, ledgeColor);
        Ledge(geometry.transform, "FinalRoute", -11.5f, 9f, 2f, square, ledgeColor);

        Pose(Required("Player"), new Vector3(-14.5f, -2.03f), Vector3.one);
        const string materialPath = "Assets/Settings/PlayerMovement.physicsMaterial2D";
        PhysicsMaterial2D movementMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
        if (movementMaterial == null)
        {
            movementMaterial = new PhysicsMaterial2D("PlayerMovement");
            AssetDatabase.CreateAsset(movementMaterial, materialPath);
        }
        movementMaterial.friction = 0f;
        movementMaterial.bounciness = 0f;
        EditorUtility.SetDirty(movementMaterial);
        Required("Player").GetComponent<Collider2D>().sharedMaterial = movementMaterial;
        Pose(Required("Platform"), new Vector3(8.8f, 1.7f), new Vector3(2.4f, 0.6f, 1f));
        Required("Platform").GetComponent<SpriteRenderer>().color = ledgeColor;
        Pose(Required("HitButton"), new Vector3(9.2f, 2.55f), new Vector3(0.5f, 0.5f, 1f));
        Pose(Required("Door"), new Vector3(10.5f, 2.75f), new Vector3(0.55f, 10.5f, 1f));
        Pose(Required("AbilityModule"), new Vector3(20.3f, -2f), new Vector3(0.55f, 0.55f, 1f));
        Pose(Required("DryBranches"), new Vector3(-9.3f, 5f), new Vector3(0.65f, 6f, 1f));
        Pose(Required("PatrolRobot"), new Vector3(17.7f, -1.98f), new Vector3(0.8f, 1f, 1f));
        Pose(Required("Food"), new Vector3(18.8f, -2.1f), new Vector3(0.4f, 0.4f, 1f));
        Pose(Required("Spikes"), new Vector3(-8.8f, -2.35f), new Vector3(1f, 0.3f, 1f));
        Pose(Required("PitTrigger"), new Vector3(13f, -7.5f), new Vector3(2f, 8f, 1f));
        Pile(pile, "StonePile", -13.7f, circle);
        Pile(null, "StonePileHub", 0.2f, circle);
        Pile(null, "StonePileModule", 11f, circle);

        Camera camera = Required("Main Camera").GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.37f, 0.43f, 0.55f);
        camera.transform.position = new Vector3(-10.6f, 1f, -10f);
        ForestioCameraFollow follow = camera.GetComponent<ForestioCameraFollow>();
        if (follow == null) follow = camera.gameObject.AddComponent<ForestioCameraFollow>();
        SerializedObject settings = new SerializedObject(follow);
        settings.FindProperty("target").objectReferenceValue = Required("Player").transform;
        settings.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save level.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LevelPath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("FORESTIO_STAGE11_SETUP_OK: Outskirts, two branches, three piles, camera and build scene configured; ForestTest retained.");
    }

    private static GameObject Required(string name)
    {
        GameObject item = GameObject.Find(name);
        if (item == null) throw new InvalidOperationException("Missing " + name);
        return item;
    }
    private static void Pose(GameObject item, Vector3 position, Vector3 scale)
    {
        item.transform.position = position;
        item.transform.localScale = scale;
    }
    private static void Ledge(Transform parent, string name, float x, float width, float top, Sprite sprite, Color color)
        => Block(parent, name, new Vector3(x, top - 0.3f), new Vector3(width, 0.6f, 1f), sprite, color);
    private static void Block(Transform parent, string name, Vector3 position, Vector3 scale, Sprite sprite, Color color)
    {
        Transform found = parent.Find(name);
        GameObject item = found != null ? found.gameObject : new GameObject(name);
        item.transform.SetParent(parent, true);
        Pose(item, position, scale);
        SpriteRenderer visual = item.GetComponent<SpriteRenderer>();
        if (visual == null) visual = item.AddComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.color = color;
        BoxCollider2D collider = item.GetComponent<BoxCollider2D>();
        if (collider == null) collider = item.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }
    private static void Pile(GameObject item, string name, float x, Sprite sprite)
    {
        if (item == null) item = GameObject.Find(name);
        if (item == null) item = new GameObject(name);
        Pose(item, new Vector3(x, -2.35f), new Vector3(0.4f, 0.3f, 1f));
        SpriteRenderer visual = item.GetComponent<SpriteRenderer>();
        if (visual == null) visual = item.AddComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.color = new Color(0.6f, 0.6f, 0.65f);
        if (item.GetComponent<ForestioStonePile>() == null) item.AddComponent<ForestioStonePile>();
    }
}
