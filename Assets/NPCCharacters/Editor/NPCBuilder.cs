#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Menü: Tools > NPC Characters > 1) Build NPCs          — 6 NPC için sprite ayarı, animasyon, Animator, NPCData ve Prefab üretir.
///       Tools > NPC Characters > 2) Add NPCs To Scene   — Hepsini açık sahneye yan yana dizer.
/// </summary>
public static class NPCBuilder
{
    const string Root = "Assets/NPCCharacters";
    const int PPU = 170;       // karakterler ~1–1.25 birim boyunda olur
    const int CardPPU = 220;

    struct Def { public string id, name, role, line; public float speed; public Def(string i, string n, string r, string l, float s) { id = i; name = n; role = r; line = l; speed = s; } }

    static readonly Def[] Defs =
    {
        new Def("Rabis",   "Rabiş",   "Çevre araştırmacısı", "Su kirliliğini araştırıyor.",     0.9f),
        new Def("Zeynep",  "Zeynep",  "Bölge sakini",        "Temiz bir doğa istiyor.",         0.85f),
        new Def("Emirhan", "Emirhan", "Fabrika çalışanı",    "İşini kaybetmekten korkuyor.",    0.8f),
        new Def("Elif",    "Elif",    "Susuz kadın",         "Temiz suya ihtiyaç duyuyor.",     0.55f),
        new Def("Anne",    "Anne",    "Merhametli anne",     "Çocuğu için endişeleniyor.",      0.7f),
        new Def("Deniz",   "Deniz",   "Hasta çocuk",         "Temiz su istiyor.",               0.6f),
    };

    [MenuItem("Tools/NPC Characters/1) Build NPCs")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Root + "/Animations");
        Directory.CreateDirectory(Root + "/Data");
        Directory.CreateDirectory(Root + "/Prefabs");

        foreach (var d in Defs) Build(d);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NPC] 6 NPC hazır: " + Root + "/Prefabs  (Sahneye eklemek için: Tools > NPC Characters > 2) Add NPCs To Scene)");
    }

    static void Build(Def d)
    {
        string sp = $"{Root}/Sprites/{d.id}";
        string[] frameNames = { "Idle_0", "Idle_1", "Walk_0", "Walk_1", "Walk_2", "Walk_3" };
        foreach (var f in frameNames) ImportSprite($"{sp}/{d.id}_{f}.png", PPU, new Vector2(0.5f, 0f), false);
        ImportSprite($"{sp}/{d.id}_Card.png", CardPPU, new Vector2(0.5f, 0f), false);

        Sprite S(string f) => AssetDatabase.LoadAssetAtPath<Sprite>($"{sp}/{d.id}_{f}.png");

        // Klipler
        var idle = MakeClip($"{d.id}_Idle", new[] { S("Idle_0"), S("Idle_1") }, 2.5f);
        var walk = MakeClip($"{d.id}_Walk", new[] { S("Walk_0"), S("Walk_1"), S("Walk_2"), S("Walk_3") }, 7f);

        // Animator
        string cPath = $"{Root}/Animations/{d.id}.controller";
        AssetDatabase.DeleteAsset(cPath);
        var c = AnimatorController.CreateAnimatorControllerAtPath(cPath);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = c.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle", new Vector3(300, 0));
        var sWalk = sm.AddState("Walk", new Vector3(300, 100));
        sIdle.motion = idle; sWalk.motion = walk; sm.defaultState = sIdle;
        var t1 = sIdle.AddTransition(sWalk); Instant(t1); t1.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
        var t2 = sWalk.AddTransition(sIdle); Instant(t2); t2.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

        // Data
        string dPath = $"{Root}/Data/{d.id}_Data.asset";
        var data = AssetDatabase.LoadAssetAtPath<NPCData>(dPath);
        if (data == null) { data = ScriptableObject.CreateInstance<NPCData>(); AssetDatabase.CreateAsset(data, dPath); }
        data.displayName = d.name; data.role = d.role; data.line = d.line; data.walkSpeed = d.speed;
        EditorUtility.SetDirty(data);

        // Prefab
        var go = new GameObject("NPC_" + d.id);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S("Idle_0");
        sr.sortingOrder = 5;
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = c;
        animator.applyRootMotion = false;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        float h = sr.sprite.bounds.size.y;
        var trig = go.AddComponent<CircleCollider2D>();
        trig.isTrigger = true;
        trig.radius = 1.3f;
        trig.offset = new Vector2(0f, h * 0.5f);

        var wander = go.AddComponent<NPCWander2D>();
        wander.data = data;

        var card = new GameObject("Card");
        card.transform.SetParent(go.transform, false);
        card.transform.localPosition = new Vector3(0f, h + 0.08f, 0f);
        var csr = card.AddComponent<SpriteRenderer>();
        csr.sprite = S("Card");
        csr.sortingOrder = 50;

        var interact = go.AddComponent<NPCInteract2D>();
        interact.card = card.transform;

        PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/NPC_{d.id}.prefab");
        Object.DestroyImmediate(go);
    }

    [MenuItem("Tools/NPC Characters/2) Add NPCs To Scene")]
    public static void AddToScene()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/NPC_Rabis.prefab") == null) BuildAll();
        var parent = new GameObject("NPCs");
        Undo.RegisterCreatedObjectUndo(parent, "Add NPCs");
        for (int i = 0; i < Defs.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/NPC_{Defs[i].id}.prefab");
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            npc.transform.position = new Vector3(-12f + i * 5f, 0f, 0f);
        }
        Selection.activeGameObject = parent;
        Debug.Log("[NPC] NPC'ler sahneye eklendi (y = 0). Zemininin üst yüzeyi farklıysa 'NPCs' objesini yukarı/aşağı taşı.");
    }

    // ---------- yardımcılar ----------
    static void ImportSprite(string path, int ppu, Vector2 pivot, bool point)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogError("[NPC] Bulunamadı: " + path); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.Tight;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    static AnimationClip MakeClip(string name, Sprite[] frames, float fps)
    {
        string path = $"{Root}/Animations/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves();
        clip.frameRate = fps;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void Instant(AnimatorStateTransition t)
    {
        t.hasExitTime = false; t.duration = 0f; t.offset = 0f; t.hasFixedDuration = true;
    }
}
#endif
