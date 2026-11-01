#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menü: Tools > Hero Character > 1) Build Character  — sprite ayarları, animasyon klipleri, Animator Controller ve Prefab üretir.
///       Tools > Hero Character > 2) Create Demo Scene — zemin + platformlar + takip kamerası ile test sahnesi kurar.
/// </summary>
public static class HeroCharacterBuilder
{
    const string Root = "Assets/HeroCharacter";
    const string FramesDir = Root + "/Sprites/Frames";
    const string AnimDir = Root + "/Animations";
    const string PrefabPath = Root + "/Hero.prefab";
    const string ControllerPath = AnimDir + "/Hero.controller";
    const string GroundSpritePath = Root + "/Sprites/Ground.png";
    const int PPU = 100;
    const float FrameH = 128f, FeetY = 2f;

    [MenuItem("Tools/Hero Character/1) Build Character")]
    public static void Build()
    {
        ImportFrames();
        Directory.CreateDirectory(AnimDir);

        var idle = MakeClip("Hero_Idle", Frames("Idle", 0, 1, 2, 3, 4), 8f, true);
        var walk = MakeClip("Hero_Walk", Frames("Walk", 0, 1, 2, 3, 4, 5, 6, 7), 12f, true); // 8 karelik tam adım döngüsü
        var run = MakeClip("Hero_Run", Frames("Run", 0, 1, 2, 3, 4), 13f, true);
        var rise = MakeClip("Hero_JumpRise", Frames("Jump", 0, 1, 2), 14f, false);
        var fall = MakeClip("Hero_JumpFall", Frames("Jump", 3), 8f, true);
        var land = MakeClip("Hero_Land", Frames("Jump", 4, 4), 14f, false);

        var ctrl = BuildController(idle, walk, run, rise, fall, land);
        BuildPrefab(ctrl);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log("[Hero] Karakter hazır: " + PrefabPath + "  (Test için: Tools > Hero Character > 2) Create Demo Scene)");
    }

    // ---------- Sprite içe aktarma ----------
    static void ImportFrames()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FramesDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = new Vector2(0.5f, FeetY / FrameH); // pivot = ayak hizası
            s.spriteMeshType = SpriteMeshType.Tight;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }

        var gi = (TextureImporter)AssetImporter.GetAtPath(GroundSpritePath);
        if (gi != null)
        {
            gi.textureType = TextureImporterType.Sprite;
            gi.spriteImportMode = SpriteImportMode.Single;
            gi.spritePixelsPerUnit = 32;
            gi.filterMode = FilterMode.Point;
            gi.wrapMode = TextureWrapMode.Repeat;
            gi.mipmapEnabled = false;
            var s = new TextureImporterSettings();
            gi.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; // Tiled çizim için gerekli
            gi.SetTextureSettings(s);
            gi.SaveAndReimport();
        }
    }

    static Sprite[] Frames(string anim, params int[] idx) =>
        idx.Select(i =>
        {
            var p = $"{FramesDir}/{anim}_{i}.png";
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (sp == null) Debug.LogError("[Hero] Sprite bulunamadı: " + p);
            return sp;
        }).ToArray();

    // ---------- Klipler ----------
    static AnimationClip MakeClip(string name, Sprite[] frames, float fps, bool loop)
    {
        string path = $"{AnimDir}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves();
        clip.frameRate = fps;

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        // son kareye de süre vermek için tekrar anahtarı
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    // ---------- Animator ----------
    static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run,
                                              AnimationClip rise, AnimationClip fall, AnimationClip land)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        c.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter
        {
            name = "AnimSpeed",
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f
        });

        var sm = c.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle", new Vector3(300, 0));
        var sWalk = sm.AddState("Walk", new Vector3(300, 100));
        var sRun = sm.AddState("Run", new Vector3(300, 200));
        var sRise = sm.AddState("JumpRise", new Vector3(600, 0));
        var sFall = sm.AddState("JumpFall", new Vector3(600, 100));
        var sLand = sm.AddState("Land", new Vector3(600, 200));
        sIdle.motion = idle; sWalk.motion = walk; sRun.motion = run;
        sRise.motion = rise; sFall.motion = fall; sLand.motion = land;
        sm.defaultState = sIdle;
        // Yürüme/koşma animasyon hızı karakterin gerçek hızına bağlı (ayak kayması azalır)
        sWalk.speedParameter = "AnimSpeed"; sWalk.speedParameterActive = true;
        sRun.speedParameter = "AnimSpeed"; sRun.speedParameterActive = true;

        const float walkTh = 0.05f, runTh = 0.75f;
        Link(sIdle, sWalk, AnimatorConditionMode.Greater, "Speed", walkTh);
        Link(sWalk, sIdle, AnimatorConditionMode.Less, "Speed", walkTh);
        Link(sWalk, sRun, AnimatorConditionMode.Greater, "Speed", runTh);
        Link(sRun, sWalk, AnimatorConditionMode.Less, "Speed", runTh);
        Link(sRun, sIdle, AnimatorConditionMode.Less, "Speed", walkTh);

        // Havaya geçiş (her durumdan)
        var toRise = sm.AddAnyStateTransition(sRise);
        Instant(toRise); toRise.canTransitionToSelf = false;
        toRise.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        toRise.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");

        var toFall = sm.AddAnyStateTransition(sFall);
        Instant(toFall); toFall.canTransitionToSelf = false;
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        toFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");

        // Yere iniş
        var fallLand = sFall.AddTransition(sLand); Instant(fallLand);
        fallLand.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        var riseLand = sRise.AddTransition(sLand); Instant(riseLand);
        riseLand.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        riseLand.AddCondition(AnimatorConditionMode.Less, 0.1f, "VelocityY");

        // Land bitince yürüyüşe/koşuya/durmaya dön
        var landRun = sLand.AddTransition(sRun); Instant(landRun);
        landRun.hasExitTime = true; landRun.exitTime = 1f;
        landRun.AddCondition(AnimatorConditionMode.Greater, runTh, "Speed");
        var landWalk = sLand.AddTransition(sWalk); Instant(landWalk);
        landWalk.hasExitTime = true; landWalk.exitTime = 1f;
        landWalk.AddCondition(AnimatorConditionMode.Greater, walkTh, "Speed");
        var landIdle = sLand.AddTransition(sIdle); Instant(landIdle);
        landIdle.hasExitTime = true; landIdle.exitTime = 1f;

        EditorUtility.SetDirty(c);
        return c;
    }

    static void Link(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string p, float th)
    {
        var t = from.AddTransition(to);
        Instant(t);
        t.AddCondition(mode, th, p);
    }

    static void Instant(AnimatorStateTransition t)
    {
        t.hasExitTime = false;
        t.duration = 0f;
        t.offset = 0f;
        t.hasFixedDuration = true;
    }

    // ---------- Prefab ----------
    static void BuildPrefab(AnimatorController ctrl)
    {
        string matPath = Root + "/HeroNoFriction.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("HeroNoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, matPath);
        }

        var go = new GameObject("Hero");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramesDir + "/Idle_0.png");
        sr.sortingOrder = 10;

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Vertical;
        col.size = new Vector2(0.42f, 1.0f);
        col.offset = new Vector2(0f, 0.5f);
        col.sharedMaterial = mat;

        go.AddComponent<HeroController2D>();

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
    }

    // ---------- Demo sahnesi ----------
    [MenuItem("Tools/Hero Character/2) Create Demo Scene")]
    public static void CreateDemoScene()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) Build();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
        camGo.transform.position = new Vector3(0, 1.5f, -10);

        var groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
        MakeBlock("Ground", groundSprite, new Vector2(0, -0.5f), new Vector2(60, 1));
        MakeBlock("Platform A", groundSprite, new Vector2(4, 1.4f), new Vector2(4, 0.5f));
        MakeBlock("Platform B", groundSprite, new Vector2(9, 2.8f), new Vector2(3, 0.5f));
        MakeBlock("Platform C", groundSprite, new Vector2(-5, 1.8f), new Vector2(3, 0.5f));

        var hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        hero.transform.position = new Vector3(0, 0.5f, 0);

        camGo.AddComponent<CameraFollow2D>().target = hero.transform;

        Directory.CreateDirectory(Root + "/Scenes");
        EditorSceneManager.SaveScene(scene, Root + "/Scenes/HeroDemo.unity");
        Debug.Log("[Hero] Demo sahnesi hazır. Play'e bas: A/D yürü, Shift koş, Space zıpla.");
    }

    static void MakeBlock(string name, Sprite sprite, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
    }
}
#endif
