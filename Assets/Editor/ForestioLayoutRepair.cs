using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioLayoutRepair
{
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        GameObject player = Required("Player");
        // Exclude the tail and ear tips from the body collider; keep the larger artwork.
        BoxCollider2D body = player.GetComponent<BoxCollider2D>();
        body.size = new Vector2(1f, .9f);
        body.offset = new Vector2(.04f, .02f);
        player.transform.position = new Vector3(-14.5f, -2.05f, 0f);
        Required("PatrolRobot").transform.position = new Vector3(17.7f, -1.98f, 0f);

        Transform route = Group("ButtonRoute", Vector3.zero);
        Ledge("TrainingLedge", -11.8f, -1f);
        Ledge("FinalStepOne", -2.5f, -1f);
        Ledge("FinalStepTwo", -5.3f, .5f);
        Ledge("FinalRoute", -11.5f, 2f);
        Ledge("ButtonStepOne", 2.5f, -1f, 1.8f).SetParent(route, true);
        Ledge("ButtonStepTwo", 4.9f, .5f, 1.8f).SetParent(route, true);
        Ledge("Platform", 7f, 2f).SetParent(route, true);
        Required("HitButton").transform.SetParent(route, true);
        Required("HitButton").transform.position = new Vector3(7.4f, 2.375f, 0f);

        // The whole tree, solid trunk and moving shutter share one movable parent.
        Transform assembly = Group("GateAssembly", new Vector3(9.5f, -2.5f, 0f));
        GameObject tree = Required("LivingTreeGate");
        tree.transform.SetParent(assembly, true);
        SpriteRenderer treeArt = tree.GetComponent<SpriteRenderer>();
        tree.transform.localScale = new Vector3(4.4f / treeArt.sprite.bounds.size.x,
            10.5f / treeArt.sprite.bounds.size.y, 1f);
        tree.transform.position = new Vector3(9.5f, 2.75f, 0f);
        BoxCollider2D trunk = tree.GetComponent<BoxCollider2D>();
        Vector3 treeScale = tree.transform.lossyScale;
        trunk.size = new Vector2(1.6f / treeScale.x, 8f / treeScale.y);
        trunk.offset = new Vector2(0f, 1.25f / treeScale.y);

        GameObject door = Required("Door");
        door.transform.SetParent(assembly, true);
        door.transform.localScale = Vector3.one;
        door.transform.position = new Vector3(9.5f, -1.25f, 0f);
        var gateBox = door.GetComponent<BoxCollider2D>();
        gateBox.offset = Vector2.zero; gateBox.size = new Vector2(1.6f, 2.5f);
        ForestioVineGate gate = door.GetComponent<ForestioVineGate>();
        Panel(gate.left, -.4f); Panel(gate.right, .4f);
        var settings = new SerializedObject(gate);
        settings.FindProperty("leftClosed").vector3Value = new Vector3(-.4f,0f,0f);
        settings.FindProperty("rightClosed").vector3Value = new Vector3(.4f,0f,0f);
        settings.ApplyModifiedPropertiesWithoutUndo();
        gate.ResetClosed();

        Required("StonePileModule").transform.position = new Vector3(10.9f, -2.19f, 0f);
        foreach (ForestioStonePile pile in UnityEngine.Object.FindObjectsByType<ForestioStonePile>(FindObjectsSortMode.None))
        {
            Vector3 p = pile.transform.position; p.y = -2.19f; pile.transform.position = p;
        }
        Required("DryBranches").transform.position = new Vector3(-9.3f, 3.4f, 0f);
        Required("Spikes").transform.position = new Vector3(-8.3f, -2.2f, 0f);
        Physics2D.SyncTransforms();
        if (treeArt.bounds.max.x > 11.75f) throw new InvalidOperationException("Tree extends over the pit.");
        if (trunk.bounds.min.y < -.001f || trunk.bounds.max.x > 10.31f)
            throw new InvalidOperationException("Trunk blocks the lower route or run-up.");
        foreach (string name in new[]{"TrainingLedge","FinalStepOne","ButtonStepOne"})
        {
            float clearance = Required(name).GetComponent<BoxCollider2D>().bounds.min.y + 2.5f;
            if (clearance < body.bounds.size.y + .25f) throw new InvalidOperationException("Insufficient headroom: " + name);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Debug.Log("FORESTIO_LAYOUT_REPAIR_OK: aligned platforms, body clearance, grouped tree/shutter, shore placement and safe spawns.");
    }

    private static Transform Group(string name, Vector3 position)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        go.transform.position = position; go.transform.localScale = Vector3.one;
        return go.transform;
    }
    private static Transform Ledge(string name, float x, float top, float targetWidth = 0f)
    {
        GameObject go = Required(name);
        BoxCollider2D box = go.GetComponent<BoxCollider2D>();
        float width = targetWidth > 0f ? targetWidth : box.bounds.size.x;
        go.transform.localScale = Vector3.one;
        go.transform.position = new Vector3(x, top - .15f, 0f);
        box.size = new Vector2(width, .3f); box.offset = Vector2.zero;
        Transform art = go.transform.Find("ForestArt");
        art.localPosition = Vector3.zero; art.localRotation = Quaternion.identity;
        art.localScale = Vector3.one;
        SpriteRenderer visual = art.GetComponent<SpriteRenderer>();
        visual.size = box.size;
        return go.transform;
    }
    private static void Panel(Transform panel,float x)
    {
        SpriteRenderer art = panel.GetComponent<SpriteRenderer>();
        panel.localPosition = new Vector3(x,0f,0f);
        panel.localScale = new Vector3(.8f/art.sprite.bounds.size.x,2.5f/art.sprite.bounds.size.y,1f);
        art.sortingOrder = -4;
    }
    private static GameObject Required(string name) => GameObject.Find(name)
        ?? throw new InvalidOperationException("Missing " + name);
}
