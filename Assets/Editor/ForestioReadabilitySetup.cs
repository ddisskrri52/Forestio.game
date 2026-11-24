using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestioReadabilitySetup
{
    private const string Root = "Assets/Art/Revision/";
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        Directory.CreateDirectory(Root + "Sprites"); AssetDatabase.Refresh();
        Texture2D items = Import("ReadableItems.png"), cat = Import("CatFrames.png"), robot = Import("RobotFrames.png"), tree = Import("GateTree.png");
        Sprite[] icons = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            Rect r = Opaque(items, Cell(items, i, 4, 2));
            icons[i] = SpriteAsset("Item" + i, items, r, Mathf.Max(r.width, r.height), new Vector2(.5f,.5f));
        }
        Sprite[] cats = Frames(cat, "Cat", 4, 4, .94f, .43f);
        Sprite[] robots = Frames(robot, "Robot", 4, 2, 1.15f, .5f);
        GameObject player = Find("Player");
        Animate(player, cats, false);
        GameObject enemy = Find("PatrolRobot"); Animate(enemy, robots, true);
        // Keep the original robot collider; walking frames register to the same feet.
        foreach (ForestioStonePile pile in UnityEngine.Object.FindObjectsByType<ForestioStonePile>(FindObjectsSortMode.None))
        {
            Vector3 p = pile.transform.position;
            Art(pile.gameObject, icons[0], new Vector2(.98f,.62f), new Vector2(p.x,-2.19f));
        }
        Art(Find("Spikes"), icons[2], new Vector2(1.6f,.6f), new Vector2(-8.3f,-2.2f));
        Art(Find("HitButton"), icons[3], new Vector2(.85f,.75f), new Vector2(9.2f,2.375f));
        Find("HitButton").GetComponent<CircleCollider2D>().radius = .5f;
        Art(Find("AbilityModule"), icons[4], new Vector2(1f,1f), new Vector2(20.3f,-1.9f));
        Art(Find("Food"), icons[5], new Vector2(.8f,.45f), new Vector2(18.8f,-2.12f));
        Art(Find("Portal"), icons[6], new Vector2(1.9f,2.7f), new Vector2(-15.1f,3.35f));
        foreach(string name in new[]{"AbilityModule","Food","Portal"})
        {
            GameObject item=Find(name);var source=item.GetComponent<SpriteRenderer>();
            Transform child=item.transform.Find("ItemGlow");
            if(child==null){child=new GameObject("ItemGlow").transform;child.SetParent(item.transform,false);}
            var glow=Component<SpriteRenderer>(child.gameObject);
            glow.sprite=source.sprite;glow.sortingOrder=source.sortingOrder;source.enabled=false;
            if(child.GetComponent<ForestioItemPulse>()==null)child.gameObject.AddComponent<ForestioItemPulse>();
        }
        GameObject held = player.transform.Find("HeldStone").gameObject;
        Pebble(held, icons[1]);
        GameObject projectile = PrefabUtility.LoadPrefabContents("Assets/Prefabs/StoneProjectile.prefab");
        Pebble(projectile, icons[1]);
        PrefabUtility.SaveAsPrefabAsset(projectile,"Assets/Prefabs/StoneProjectile.prefab"); PrefabUtility.UnloadPrefabContents(projectile);
        SetupGate(tree, items, icons[7]);
        Find("ForestBackdrop").GetComponent<SpriteRenderer>().color = new Color(1f,1f,1f,.82f);
        Transform decor = Find("ForestDecoration").transform;
        foreach (string name in new[]{"FlowersAtFinal","FlowersAtButton","BushAtDoor"})
            if (decor.Find(name) != null) decor.Find(name).gameObject.SetActive(false);
        SerializedObject hud = new SerializedObject(Find("GameHud").GetComponent<ForestioHud>());
        hud.FindProperty("stoneIcon").objectReferenceValue = icons[1]; hud.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject home = new SerializedObject(Find("LevelCompletion").GetComponent<ForestioHomeEnding>());
        var sequence = home.FindProperty("walkingFrames"); sequence.arraySize = 4;
        for(int i=0;i<4;i++) sequence.GetArrayElementAtIndex(i).objectReferenceValue = cats[4+i];
        home.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("FORESTIO_READABILITY_SETUP_OK");
    }
    private static void SetupGate(Texture2D tree, Texture2D items, Sprite vines)
    {
        GameObject door = Find("Door");
        if (door.transform.Find("ForestArt") != null) UnityEngine.Object.DestroyImmediate(door.transform.Find("ForestArt").gameObject);
        door.GetComponent<SpriteRenderer>().enabled = false;
        door.transform.position = new Vector3(10.5f,-1.25f,0f); door.transform.localScale = Vector3.one;
        door.GetComponent<BoxCollider2D>().size = new Vector2(1.6f,2.5f);
        ForestioVineGate gate = Component<ForestioVineGate>(door);
        Rect r = vines.rect;
        gate.left = Half("LeftVines", door.transform, SpriteAsset("LeftVines", items, new Rect(r.x,r.y,r.width/2,r.height),r.height,new Vector2(.5f,.5f)), -.4f);
        gate.right = Half("RightVines", door.transform, SpriteAsset("RightVines", items, new Rect(r.x+r.width/2,r.y,r.width/2,r.height),r.height,new Vector2(.5f,.5f)), .4f);
        GameObject arch = GameObject.Find("LivingTreeGate") ?? new GameObject("LivingTreeGate");
        Sprite sprite = SpriteAsset("GateTree",tree,Opaque(tree,new RectInt(0,0,tree.width,tree.height)),tree.height,new Vector2(.5f,.5f));
        // Transparent arch matches the 2.5-unit passage. The solid trunk prevents an upper bypass.
        Art(arch,sprite,new Vector2(4.4f,10.5f),new Vector2(10.5f,2.75f));
        arch.GetComponent<SpriteRenderer>().sortingOrder = -3;
        BoxCollider2D canopy = Component<BoxCollider2D>(arch);
        canopy.size = new Vector2(1.6f/4.4f,8f/10.5f); canopy.offset = new Vector2(0f,1.25f/10.5f);
    }
    private static Transform Half(string name,Transform parent,Sprite sprite,float x)
    {
        Transform t=parent.Find(name); if(t==null){t=new GameObject(name).transform;t.SetParent(parent,false);}
        t.localPosition=new Vector3(x,0,0); SpriteRenderer sr=Component<SpriteRenderer>(t.gameObject);
        sr.sprite=sprite; sr.drawMode=SpriteDrawMode.Simple; sr.sortingOrder=2;
        t.localScale=new Vector3(.8f/sprite.bounds.size.x,2.5f/sprite.bounds.size.y,1f); return t;
    }
    private static void Pebble(GameObject go,Sprite sprite)
    {
        SpriteRenderer sr=go.GetComponent<SpriteRenderer>(); sr.sprite=sprite; sr.drawMode=SpriteDrawMode.Simple; sr.color=Color.white;
        go.transform.localScale=Vector3.one*.26f;
    }
    private static void Animate(GameObject go,Sprite[] frames,bool robot)
    {
        SpriteRenderer sr=go.GetComponent<SpriteRenderer>();
        Vector3 scale=go.transform.localScale; sr.drawMode=SpriteDrawMode.Simple; sr.sprite=frames[0]; go.transform.localScale=scale;
        ForestioFrameAnimation animation=Component<ForestioFrameAnimation>(go);
        animation.frames=frames; animation.robot=robot;
    }
    private static Sprite[] Frames(Texture2D texture,string prefix,int cols,int rows,float bodyWidth,float feetOffset)
    {
        Rect first=Opaque(texture,Cell(texture,0,cols,rows));
        float ppu=first.width/bodyWidth;
        Sprite[] frames=new Sprite[cols*rows];
        for(int i=0;i<frames.Length;i++)
        {
            RectInt cell=Cell(texture,i,cols,rows); Rect opaque=Opaque(texture,cell);
            // Fixed cell x-center and an individual feet registration avoid sheet baseline drift.
            Vector2 pivot=new Vector2(.5f,(opaque.yMin-cell.yMin+feetOffset*ppu)/cell.height);
            frames[i]=SpriteAsset(prefix+i,texture,new Rect(cell.x,cell.y,cell.width,cell.height),ppu,pivot);
        }
        return frames;
    }
    private static void Art(GameObject go,Sprite sprite,Vector2 size,Vector2 position)
    {
        SpriteRenderer sr=Component<SpriteRenderer>(go);
        sr.sprite=sprite; sr.drawMode=SpriteDrawMode.Simple; sr.color=Color.white; sr.enabled=true;
        go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1f);
        go.transform.position=new Vector3(position.x,position.y,0f);
        BoxCollider2D box=go.GetComponent<BoxCollider2D>();
        if(box!=null)box.size=new Vector2(size.x/go.transform.localScale.x,size.y/go.transform.localScale.y);
    }
    private static T Component<T>(GameObject go) where T : Component { T item=go.GetComponent<T>(); return item != null ? item : go.AddComponent<T>(); }
    private static GameObject Find(string name)=>GameObject.Find(name)??throw new InvalidOperationException("Missing "+name);
    private static Texture2D Import(string file)
    {
        string path=Root+file; var ti=(TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.isReadable=true;ti.mipmapEnabled=false;
        ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.Uncompressed;
        ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static RectInt Cell(Texture2D t,int i,int cols,int rows)
    {
        int x=Mathf.RoundToInt((i%cols)*t.width/(float)cols),x1=Mathf.RoundToInt((i%cols+1)*t.width/(float)cols);
        int y=Mathf.RoundToInt((rows-1-i/cols)*t.height/(float)rows),y1=Mathf.RoundToInt((rows-i/cols)*t.height/(float)rows);
        return new RectInt(x,y,x1-x,y1-y);
    }
    private static Rect Opaque(Texture2D t,RectInt cell)
    {
        Color32[] p=t.GetPixels32();int l=cell.xMax,r=cell.xMin,b=cell.yMax,u=cell.yMin;
        for(int y=cell.yMin;y<cell.yMax;y++)for(int x=cell.xMin;x<cell.xMax;x++)
            if(p[y*t.width+x].a>=128){l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);u=Mathf.Max(u,y);}
        if(l>r)throw new InvalidOperationException("Empty sprite");return Rect.MinMaxRect(l,b,r+1,u+1);
    }
    private static Sprite SpriteAsset(string name,Texture2D t,Rect rect,float ppu,Vector2 pivot)
    {
        string path=Root+"Sprites/"+name+".asset";
        Sprite sprite=Sprite.Create(t,rect,pivot,ppu,0,SpriteMeshType.FullRect);
        sprite.name=name;var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(old!=null){EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
}
