using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioCatRefinementSetup
{
    private const string Art = "Assets/Art/Revision/";
    // Nose tips in the original 1536 x 1024 sheet (top-left coordinates).
    private static readonly Vector2[] Nose = {
        new(343,116), new(727,112), new(1111,117), new(1491,117),
        new(350,365), new(732,362), new(1116,365), new(1495,364),
        new(339,657), new(707,578), new(1099,609), new(1479,635),
        new(336,871), new(724,893), new(1116,894), new(1483,881)
    };

    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player");
        var animation = player.GetComponent<ForestioFrameAnimation>();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "CatFrames.png");
        float ppu = 300f / 1.3f;
        animation.mouthOffsets = new Vector2[16];
        for (int i = 0; i < 16; i++)
        {
            Sprite old = animation.frames[i];
            Rect r = old.rect;
            float bottom = Opaque(texture, new RectInt((int)r.x, (int)r.y, (int)r.width, (int)r.height)).yMin - r.yMin;
            Vector2 pivot = new Vector2(.5f, (bottom + .43f * ppu) / r.height);
            Sprite updated = Save("Cat" + i, texture, r, ppu, pivot);
            animation.frames[i] = updated;
            // Put the stone just in front of and slightly below the nose.
            Vector2 point = new Vector2(Nose[i].x + 18f, texture.height - Nose[i].y - 8f);
            animation.mouthOffsets[i] = (point - r.position - updated.pivot) / ppu;
        }
        player.GetComponent<SpriteRenderer>().sprite = animation.frames[0];
        var collider = player.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1.12f, 1f);
        collider.offset = new Vector2(.04f, .07f);

        // Preserve landing heights while giving the larger cat room underneath.
        foreach (string name in new[] { "TrainingLedge", "ButtonStepOne", "FinalStepOne" })
        {
            GameObject ledge = GameObject.Find(name);
            BoxCollider2D box = ledge.GetComponent<BoxCollider2D>();
            float top = box.bounds.max.y;
            Vector3 scale = ledge.transform.localScale;
            scale.y = .42f / box.size.y;
            ledge.transform.localScale = scale;
            Vector3 position = ledge.transform.position;
            position.y = top - .21f;
            ledge.transform.position = position;
            Transform visual = ledge.transform.Find("ForestArt");
            visual.localPosition = Vector3.zero;
            visual.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            visual.GetComponent<SpriteRenderer>().size = new Vector2(box.size.x * scale.x, .42f);
        }

        Transform decor = GameObject.Find("ForestDecoration").transform;
        foreach (Transform child in decor)
            if (child.name.StartsWith("Bush") || child.name.StartsWith("Flowers")) child.gameObject.SetActive(false);

        Texture2D logs = Import("CrackedLogs.png");
        Sprite logSprite = Save("CrackedLogs", logs, Opaque(logs, new RectInt(0,0,logs.width,logs.height)), logs.height, new Vector2(.5f,.5f));
        GameObject barrier = GameObject.Find("DryBranches");
        if (barrier.transform.Find("ForestArt") != null)
            UnityEngine.Object.DestroyImmediate(barrier.transform.Find("ForestArt").gameObject);
        SpriteRenderer renderer = barrier.GetComponent<SpriteRenderer>();
        renderer.enabled = true; renderer.sprite = logSprite; renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = Color.white;
        barrier.transform.localScale = new Vector3(1.5f / logSprite.bounds.size.x, 2.8f / logSprite.bounds.size.y, 1f);
        barrier.transform.position = new Vector3(-9.3f, 3.4f, 0f);
        barrier.GetComponent<BoxCollider2D>().size = logSprite.bounds.size;

        Texture2D gateTexture = Import("WoodenShutter.png");
        Rect gateRect = Opaque(gateTexture, new RectInt(0,0,gateTexture.width,gateTexture.height));
        ForestioVineGate gate = UnityEngine.Object.FindFirstObjectByType<ForestioVineGate>();
        for (int i = 0; i < 2; i++)
        {
            Transform half = i == 0 ? gate.left : gate.right;
            Sprite sprite = Save("WoodShutter" + i, gateTexture,
                new Rect(gateRect.x + i * gateRect.width / 2f, gateRect.y, gateRect.width / 2f, gateRect.height), gateRect.height, new Vector2(.5f,.5f));
            var sr = half.GetComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = -4;
            half.localScale = new Vector3(.8f / sprite.bounds.size.x, 2.5f / sprite.bounds.size.y, 1f);
        }
        EditorUtility.SetDirty(animation);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("FORESTIO_CAT_REFINEMENT_SETUP_OK");
    }

    private static Texture2D Import(string name)
    {
        string path = Art + name;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true; importer.isReadable = true; importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Rect Opaque(Texture2D texture, RectInt cell)
    {
        Color32[] p = texture.GetPixels32();
        int l=cell.xMax, r=cell.xMin, b=cell.yMax, t=cell.yMin;
        for(int y=cell.yMin;y<cell.yMax;y++) for(int x=cell.xMin;x<cell.xMax;x++)
            if(p[y*texture.width+x].a>=128) { l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);t=Mathf.Max(t,y); }
        if(l>r) throw new InvalidOperationException("Empty sprite");
        return Rect.MinMaxRect(l,b,r+1,t+1);
    }
    private static Sprite Save(string name,Texture2D texture,Rect rect,float ppu,Vector2 pivot)
    {
        string path=Art+"Sprites/"+name+".asset";
        Sprite sprite=Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);
        sprite.name=name;
        Sprite old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(old!=null) { EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(old);return old; }
        AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
}
