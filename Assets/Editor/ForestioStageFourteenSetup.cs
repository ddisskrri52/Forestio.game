using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class ForestioStageFourteenSetup
{
    private const string Forest = "Assets/Art/Forest/";
    private const string Ending = "Assets/Art/Ending/";
    private static readonly string[] Names = { "Terrain", "Bush", "PinkTree", "LavenderTree", "DryBranches", "WoodGate", "Robot", "StonePile", "Stone", "Button", "Module", "Portal", "Spikes", "Flowers", "Heart", "Food" };

    [MenuItem("Forestio/Setup stage 14 forest art and home ending")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        var originalPoses = new System.Collections.Generic.Dictionary<Transform, (Vector3 position, Quaternion rotation, Vector3 scale)>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name != "ForestArt")
                    originalPoses[item] = (item.localPosition, item.localRotation, item.localScale);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject events = new GameObject("EventSystem", typeof(EventSystem));
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        Directory.CreateDirectory(Forest + "Sprites");
        AssetDatabase.Refresh();
        Texture2D atlas = Import(Forest + "EnvironmentAtlas.png", true);
        Sprite[] sprites = new Sprite[16];
        for (int i = 0; i < sprites.Length; i++) sprites[i] = AtlasSprite(atlas, i);
        Texture2D backdrop = Import(Forest + "ForestBackdrop.png", false);
        Texture2D room = Import(Ending + "HomeRoom.png", false);
        Texture2D rest = Import(Ending + "CatResting.png", true);
        Sprite backgroundSprite = SaveSprite(Forest + "Sprites/Backdrop.asset", backdrop,
            new Rect(0f, 0f, backdrop.width, backdrop.height), 100f, Vector4.zero);
        Sprite resting = SaveSprite(Ending + "CatResting.asset", rest, OpaqueRect(rest, new RectInt(0, 0, rest.width, rest.height)), 500f, Vector4.zero);

        Transform geometry = Required("LevelGeometry").transform;
        foreach (Transform item in geometry)
        {
            if (item.name.EndsWith("Boundary")) item.GetComponent<SpriteRenderer>().enabled = false;
            else SurfaceArt(item.gameObject, sprites[0]);
        }
        SurfaceArt(Required("Platform"), sprites[0]);
        SurfaceArt(Required("DryBranches"), sprites[4]);
        Rect gateRect = sprites[5].rect;
        Sprite gatePost = SaveSprite(Forest + "Sprites/GatePost.asset", atlas,
            new Rect(gateRect.x + gateRect.width * 0.035f, gateRect.y, gateRect.width * 0.17f, gateRect.height),
            gateRect.height, new Vector4(5f, 24f, 5f, 24f));
        SurfaceArt(Required("Door"), gatePost, SpriteDrawMode.Sliced);
        ActorArt(Required("PatrolRobot"), sprites[6], true);
        Required("PatrolRobot").transform.Find("Eye").GetComponent<SpriteRenderer>().enabled = false;
        ActorArt(Required("HitButton"), sprites[9], true);
        ActorArt(Required("AbilityModule"), sprites[10], false);
        ActorArt(Required("Portal"), sprites[11], true);
        ActorArt(Required("Spikes"), sprites[12], true);
        ActorArt(Required("Food"), sprites[15], false);
        foreach (ForestioStonePile pile in UnityEngine.Object.FindObjectsByType<ForestioStonePile>(FindObjectsSortMode.None))
            ActorArt(pile.gameObject, sprites[7], true);
        ActorArt(Required("Player").transform.Find("HeldStone").gameObject, sprites[8], false);

        GameObject projectile = PrefabUtility.LoadPrefabContents("Assets/Prefabs/StoneProjectile.prefab");
        ActorArt(projectile, sprites[8], false);
        PrefabUtility.SaveAsPrefabAsset(projectile, "Assets/Prefabs/StoneProjectile.prefab");
        PrefabUtility.UnloadPrefabContents(projectile);

        GameObject background = GameObject.Find("ForestBackdrop");
        if (background == null) background = new GameObject("ForestBackdrop");
        background.transform.position = new Vector3(2f, 3f, 5f);
        background.transform.localScale = Vector3.one * (44f / backgroundSprite.bounds.size.x);
        SpriteRenderer backdropRenderer = Renderer(background);
        backdropRenderer.sprite = backgroundSprite;
        backdropRenderer.sortingOrder = -100;
        backdropRenderer.color = Color.white;
        Camera.main.backgroundColor = new Color(0.92f, 0.77f, 0.83f);
        Decor("LeftTree", sprites[2], -17f, -2.5f, 3.6f, 4.2f);
        Decor("RightTree", sprites[3], 21f, -2.5f, 3.3f, 4f);
        Decor("UpperTree", sprites[2], -12.5f, 2f, 2.9f, 3.1f);
        Decor("BushAtStart", sprites[1], -15.8f, -2.5f, 1f, 0.6f);
        Decor("BushAtHub", sprites[1], -4.5f, -2.5f, 0.8f, 0.45f);
        Decor("BushAtDoor", sprites[1], 8.7f, -2.5f, 0.9f, 0.5f);
        Decor("FlowersAtFinal", sprites[13], -15.7f, 2f, 0.5f, 0.3f);
        Decor("FlowersAtButton", sprites[13], 9.8f, 2f, 0.4f, 0.25f);

        SerializedObject hud = new SerializedObject(Required("GameHud").GetComponent<ForestioHud>());
        hud.FindProperty("heartIcon").objectReferenceValue = sprites[14];
        hud.FindProperty("stoneIcon").objectReferenceValue = sprites[8];
        hud.ApplyModifiedPropertiesWithoutUndo();
        GameObject ending = Required("LevelCompletion");
        ForestioHomeEnding home = ending.GetComponent<ForestioHomeEnding>();
        if (home == null) home = ending.AddComponent<ForestioHomeEnding>();
        SerializedObject homeSettings = new SerializedObject(home);
        homeSettings.FindProperty("room").objectReferenceValue = room;
        homeSettings.FindProperty("walkingCat").objectReferenceValue = Required("Player").GetComponent<SpriteRenderer>().sprite;
        homeSettings.FindProperty("restingCat").objectReferenceValue = resting;
        homeSettings.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject completion = new SerializedObject(ending.GetComponent<ForestioLevelCompletion>());
        completion.FindProperty("homeEnding").objectReferenceValue = home;
        completion.ApplyModifiedPropertiesWithoutUndo();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + item.name);
        AssetDatabase.SaveAssets();
        // SpriteRenderer's Simple -> Sliced conversion can normalize the parent
        // Transform scale. Restore authored poses after all sprite imports finish.
        foreach (var pair in originalPoses)
        {
            if (pair.Key == null) continue;
            pair.Key.localPosition = pair.Value.position;
            pair.Key.localRotation = pair.Value.rotation;
            pair.Key.localScale = pair.Value.scale;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save level.");
        Debug.Log("FORESTIO_STAGE14_SETUP_OK: forest artwork, compact HUD and home reunion configured; colliders and level geometry preserved.");
    }

    private static Texture2D Import(string path, bool readable)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture missing: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.isReadable = readable;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Sprite AtlasSprite(Texture2D atlas, int index)
    {
        int x0 = Mathf.RoundToInt((index % 4) * atlas.width / 4f);
        int x1 = Mathf.RoundToInt((index % 4 + 1) * atlas.width / 4f);
        int y0 = Mathf.RoundToInt((3 - index / 4) * atlas.height / 4f);
        int y1 = Mathf.RoundToInt((4 - index / 4) * atlas.height / 4f);
        Rect rect = OpaqueRect(atlas, new RectInt(x0, y0, x1 - x0, y1 - y0));
        Vector4 border = index == 0 ? new Vector4(16f, 16f, 16f, rect.height * 0.43f)
            : index == 4 || index == 5 ? new Vector4(12f, 20f, 12f, 20f)
            : index == 6 || index == 7 || index == 9 || index == 11 || index == 12 ? Vector4.one : Vector4.zero;
        return SaveSprite(Forest + "Sprites/" + Names[index] + ".asset", atlas, rect, Mathf.Max(rect.width, rect.height), border);
    }
    private static Rect OpaqueRect(Texture2D texture, RectInt cell)
    {
        Color32[] pixels = texture.GetPixels32();
        int left = cell.xMax, right = cell.xMin, bottom = cell.yMax, top = cell.yMin;
        for (int y = cell.yMin; y < cell.yMax; y++)
            for (int x = cell.xMin; x < cell.xMax; x++)
            {
                if (pixels[y * texture.width + x].a < 128) continue;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
            }
        if (left > right || bottom > top) throw new InvalidOperationException("Empty atlas cell.");
        return Rect.MinMaxRect(left, bottom, right + 1, top + 1);
    }
    private static Sprite SaveSprite(string path, Texture2D texture, Rect rect, float ppu, Vector4 border)
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
        sprite.name = Path.GetFileNameWithoutExtension(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(sprite, existing);
            UnityEngine.Object.DestroyImmediate(sprite);
            EditorUtility.SetDirty(existing);
            if (existing.rect != rect) throw new InvalidOperationException("Sprite rectangle was not updated: " + path);
            return existing;
        }
        AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }
    private static GameObject Required(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found == null) throw new InvalidOperationException("Missing " + name);
        return found;
    }
    private static SpriteRenderer Renderer(GameObject item)
    {
        SpriteRenderer visual = item.GetComponent<SpriteRenderer>();
        return visual != null ? visual : item.AddComponent<SpriteRenderer>();
    }
    private static void ActorArt(GameObject item, Sprite sprite, bool fill)
    {
        SpriteRenderer visual = Renderer(item);
        visual.sprite = sprite;
        visual.color = Color.white;
        visual.drawMode = fill ? SpriteDrawMode.Sliced : SpriteDrawMode.Simple;
        if (fill) visual.size = Vector2.one;
    }
    private static void SurfaceArt(GameObject item, Sprite sprite, SpriteDrawMode mode = SpriteDrawMode.Tiled)
    {
        BoxCollider2D collider = item.GetComponent<BoxCollider2D>();
        Bounds bounds = collider.bounds;
        Transform art = item.transform.Find("ForestArt");
        if (art == null) { art = new GameObject("ForestArt").transform; art.SetParent(item.transform, false); }
        art.position = bounds.center;
        Vector3 scale = item.transform.lossyScale;
        art.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
        SpriteRenderer visual = Renderer(art.gameObject);
        visual.sprite = sprite;
        visual.color = Color.white;
        visual.drawMode = mode;
        visual.tileMode = SpriteTileMode.Continuous;
        visual.size = bounds.size;
        visual.sortingOrder = -2;
        // Set scale after changing draw mode; Unity normalizes scale during conversion.
        art.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
        item.GetComponent<SpriteRenderer>().enabled = false;
    }
    private static void Decor(string name, Sprite sprite, float x, float bottom, float width, float height)
    {
        GameObject root = GameObject.Find("ForestDecoration");
        if (root == null) root = new GameObject("ForestDecoration");
        Transform existing = root.transform.Find(name);
        GameObject item = existing != null ? existing.gameObject : new GameObject(name);
        item.transform.SetParent(root.transform, false);
        item.transform.position = new Vector3(x, bottom + height / 2f, 0f);
        item.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1f);
        SpriteRenderer visual = Renderer(item);
        visual.sprite = sprite;
        visual.color = Color.white;
        visual.sortingOrder = -8;
    }
}
