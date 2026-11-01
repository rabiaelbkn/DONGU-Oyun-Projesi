#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// BİRLEŞİK KURULUM:  Tools > DÖNGÜ > Oyunu Kur
///  1) HeroCharacter ve 6 NPC'yi (animasyonlu) üretir
///  2) Tüm sahnelerdeki eski Player'ı HeroCharacter ile değiştirir (ortak PlayerInteraction ile)
///  3) Ev içi (dolap portalı) / ev dışı / FutureHome sahnelerini yeni karaktere göre ayarlar
///  4) PastScene'e Hackathon'un nehir-fabrika ortamını, Rabiş görevini ve dönüş portalını kurar
///  5) Tüm zaman portallarında aynı görseli (dolaptaki saat girdabı) kullanır
/// Tekrar çalıştırılabilir. Proje ilk açıldığında bir kez kendiliğinden sorar.
/// </summary>
[InitializeOnLoad]
public static class DonguSetup
{
    const string InteriorScene = "Assets/Scenes/FutureInterior.unity";
    const string ExteriorScene = "Assets/Scenes/FutureExterior.unity";
    const string HomeScene = "Assets/Scenes/FutureHome.unity";
    const string PastScenePath = "Assets/Scenes/PastScene.unity";
    const string PastArt = "Assets/PastScene/Art";
    const string DataDir = "Assets/PastScene/Data";
    const string PortalSpritePath = "Assets/Art/Environment/TimePortal.png";
    const string ArtEnv = "Assets/Art/Environment/";
    const string Marker = "Assets/Editor/.dongu_kurulum_soruldu";
    const float GT = -2.5f;              // geçmiş sahnesinde zeminin üst yüzeyi
    const float InteractRange = 1.2f;
    const int InteractableLayer = 6;     // TagManager: "Interectable"

    // ---------- ilk açılışta otomatik sor ----------
    static int waitFrames;
    static DonguSetup()
    {
        if (File.Exists(Marker)) return;
        EditorApplication.update += WaitForImport;
    }

    static void WaitForImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PastArt + "/Factory.png") == null) { if (++waitFrames > 2000) EditorApplication.update -= WaitForImport; return; }
        EditorApplication.update -= WaitForImport;
        File.WriteAllText(Marker, "1");
        if (EditorUtility.DisplayDialog("DÖNGÜ - Birleşik Kurulum",
            "HeroCharacter tüm sahnelere ana karakter olarak yerleştirilecek, geçmiş sahnesi (nehir, fabrika, Rabiş) kurulacak ve tüm portallar aynı görsele geçirilecek.\n\nŞimdi yapılsın mı?\n(Sonra da Tools > DÖNGÜ > Oyunu Kur ile yapabilirsin.)",
            "Evet, kur", "Sonra"))
            RunAll();
    }

    // ---------- ana menü ----------
    [MenuItem("Tools/DÖNGÜ/Oyunu Kur (Hero + Tüm Sahneler)")]
    public static void RunAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            EditorUtility.DisplayProgressBar("DÖNGÜ", "Karakterler hazırlanıyor...", 0.1f);
            HeroCharacterBuilder.Build();
            NPCBuilder.BuildAll();

            EditorUtility.DisplayProgressBar("DÖNGÜ", "Görseller içe aktarılıyor...", 0.25f);
            ImportPastArt();
            ImportPortalSprite();
            ImportEnvironmentSprites();
            ImportInteriorV2();
            ImportNewspaperPages();
            var rabis = BuildRabisDialogue();
            BuildFactoryGuardDialogue();

            EditorUtility.DisplayProgressBar("DÖNGÜ", "Ev içi kuruluyor...", 0.4f);
            SetupInterior();
            EditorUtility.DisplayProgressBar("DÖNGÜ", "Ev dışı kuruluyor...", 0.5f);
            SetupExterior();
            EditorUtility.DisplayProgressBar("DÖNGÜ", "FutureHome kuruluyor...", 0.6f);
            SetupHome();
            EditorUtility.DisplayProgressBar("DÖNGÜ", "Geçmiş sahnesi kuruluyor...", 0.8f);
            SetupPast(rabis);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(InteriorScene, true),
                new EditorBuildSettingsScene(ExteriorScene, true),
                new EditorBuildSettingsScene(PastScenePath, true),
                new EditorBuildSettingsScene(HomeScene, true),
            };

            EditorSceneManager.OpenScene(InteriorScene);
        }
        finally { EditorUtility.ClearProgressBar(); }

        Debug.Log("[DÖNGÜ] Kurulum tamam! FutureInterior'da Play'e bas: A/D yürü, Shift koş, Space zıpla, E (veya fare) etkileşim.");
    }

    // ================= Ev içi (başlangıç) =================
    // Yeni ev içi arka planı (FutureInterior_v2.png, 1600x606) -> 12.78 x 4.84 birim
    const float IntBgPPU = 606f / 4.84f;
    const float IntFloor = -1.62f;     // ayak hizası (videodaki gibi)
    const float HeroIntScale = 2.1f;   // ev içinde gerçek insan boyu
    const float NpcIntScale = 1.95f;
    static float IWX(float px) => px / IntBgPPU - 6.39f;
    static float IWY(float py) => 2.42f - py / IntBgPPU;

    static void SetupInterior()
    {
        var scene = EditorSceneManager.OpenScene(InteriorScene);

        // Arka plan
        var bgObj = GameObject.Find("House Background");
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "FutureInterior_v2.png");
        if (bgObj && bgSprite)
        {
            var bsr = bgObj.GetComponent<SpriteRenderer>();
            bsr.sprite = bgSprite; bsr.sortingOrder = -20;
            bgObj.transform.position = Vector3.zero; bgObj.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(bsr);
        }
        var oldDoorVisual = GameObject.Find("Inside Exit Door Visual"); // yeni arka planda kapı resmin içinde
        if (oldDoorVisual) Object.DestroyImmediate(oldDoorVisual);

        // Zemin: ayaklar tahtaların ortasında
        var ground = GameObject.Find("Ground");
        if (ground)
        {
            ground.transform.position = new Vector3(0f, IntFloor - 0.2f, 0f);
            var gc = ground.GetComponent<BoxCollider2D>();
            if (gc) { gc.size = new Vector2(13f, 0.4f); gc.offset = Vector2.zero; EditorUtility.SetDirty(gc); }
        }

        // Kahraman: gerçek insan boyu, buna uygun hız
        var hero = ReplacePlayer();
        hero.transform.position = new Vector3(-3.4f, IntFloor + 0.02f, 0f);
        hero.transform.localScale = new Vector3(HeroIntScale, HeroIntScale, 1f);
        var hc = hero.GetComponent<HeroController2D>();
        if (hc) { hc.walkSpeed = 4f; hc.runSpeed = 9.6f; hc.jumpVelocity = 14f; }
        var pi = hero.GetComponent<PlayerInteraction>();
        if (pi) pi.interactionRange = 1.6f;
        SetupBoundedCamera(hero);
        AddWalls(-6.6f, 6.6f);

        // Giriş kapısı (arka plandaki kapı)
        var exit = GameObject.Find("House Exit Door");
        if (exit)
        {
            exit.transform.position = new Vector3(IWX(1415), -0.5f, 0f);
            var ec = exit.GetComponent<BoxCollider2D>(); if (ec) ec.size = new Vector2(1.6f, 2.6f);
            var d = exit.GetComponent<TimeDoorInteraction>(); d.prompt = "Dışarı çık"; d.sceneToLoad = "FutureExterior"; d.portalEffect = false;
            EditorUtility.SetDirty(d);
        }

        // Masadaki gazete
        var oldNews = GameObject.Find("Newspaper");
        if (oldNews) Object.DestroyImmediate(oldNews);
        var news = new GameObject("Newspaper");
        news.layer = InteractableLayer;
        news.transform.position = new Vector3(IWX(905), -0.3f, 0f);
        var newsCol = news.AddComponent<BoxCollider2D>();
        newsCol.isTrigger = true;
        newsCol.size = new Vector2(1.5f, 1.0f);
        news.AddComponent<NewspaperInteraction>().prompt = "Gazeteyi oku";

        // Dolap: arka plandaki dolabın kendi kapakları açılır, içinde portal
        var cabinet = GameObject.Find("Cabinet Portal");
        if (cabinet)
        {
            cabinet.transform.position = new Vector3(IWX(205), -0.2f, 0f);
            var cc = cabinet.GetComponent<BoxCollider2D>(); if (cc) cc.size = new Vector2(2.0f, 2.6f);

            var portal = cabinet.GetComponent<CabinetPortalInteraction>();
            if (portal.portalVisual) { Object.DestroyImmediate(portal.portalVisual); portal.portalVisual = null; }
            var oldDoors = GameObject.Find("Cabinet Doors");
            if (oldDoors) Object.DestroyImmediate(oldDoors);
            portal.doors = BuildCabinetDoors();
            portal.openText = "Dolabı aç";
            portal.enterText = "Zaman geçidine gir    <color=#FFD36B>[Esc]</color> Kapat";
            EditorUtility.SetDirty(portal);

            var door = cabinet.GetComponent<TimeDoorInteraction>();
            door.sceneToLoad = "PastScene";
            door.returnToPreviousScene = false;
            door.prompt = "Zaman geçidine gir";
            door.portalEffect = true;
            door.focus = portal.doors.vortex.transform;
            EditorUtility.SetDirty(door);
        }

        var director = AddStoryDirector(HomeStoryDirector.Place.Interior);
        director.floorY = IntFloor;
        director.entryX = IWX(1415);
        director.exitX = IWX(1415) + 0.8f;
        director.npcScale = NpcIntScale;
        director.npcSpeed = 3.2f;
        director.approachGap = 1.6f;
        director.childGap = 1.0f;
        director.hideCards = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static CabinetDoors BuildCabinetDoors()
    {
        const string cab = ArtEnv + "Cabinet/";
        float cy = IWY(301);
        var root = new GameObject("Cabinet Doors");
        var doors = root.AddComponent<CabinetDoors>();

        var inside = new GameObject("Inside");
        inside.transform.SetParent(root.transform, false);
        inside.transform.position = new Vector3((IWX(110) + IWX(299)) * 0.5f, cy, 0f);
        var isr = inside.AddComponent<SpriteRenderer>();
        isr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cab + "CabinetInside.png"); isr.sortingOrder = -19;

        var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(inside.transform, false);
        glow.transform.position = new Vector3(IWX(205), IWY(293), 0f);
        glow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cab + "CabinetGlow.png"); glow.sortingOrder = -18;
        glow.transform.localScale = Vector3.one * 0.81f;

        var vortex = new GameObject("Vortex").AddComponent<SpriteRenderer>();
        vortex.transform.SetParent(inside.transform, false);
        vortex.transform.position = new Vector3(IWX(205), IWY(293), 0f);
        vortex.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath); vortex.sortingOrder = -17;
        float diameter = 0.92f * 189f / IntBgPPU;
        if (vortex.sprite) vortex.transform.localScale = Vector3.one * (diameter / vortex.sprite.bounds.size.x);

        var left = new GameObject("Door Left").AddComponent<SpriteRenderer>();
        left.transform.SetParent(root.transform, false);
        left.transform.position = new Vector3(IWX(110), cy, 0f);
        left.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cab + "CabinetDoorLeft.png"); left.sortingOrder = -16;

        var right = new GameObject("Door Right").AddComponent<SpriteRenderer>();
        right.transform.SetParent(root.transform, false);
        right.transform.position = new Vector3(IWX(299), cy, 0f);
        right.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cab + "CabinetDoorRight.png"); right.sortingOrder = -16;

        doors.leftDoor = left; doors.rightDoor = right; doors.inside = inside;
        doors.vortex = vortex; doors.glow = glow;
        inside.SetActive(false);
        return doors;
    }

    // ================= Ev dışı =================
    static void SetupExterior()
    {
        var scene = EditorSceneManager.OpenScene(ExteriorScene);
        var hero = ReplacePlayer();
        SetupBoundedCamera(hero);
        AddWalls(-7.3f, 7.3f);

        var entrance = GameObject.Find("House Entrance Door");
        if (entrance) { var d = entrance.GetComponent<TimeDoorInteraction>(); d.prompt = "Eve gir"; d.sceneToLoad = "FutureInterior"; d.portalEffect = false; EditorUtility.SetDirty(d); }

        var director = AddStoryDirector(HomeStoryDirector.Place.Exterior);
        director.exitX = 7.8f;
        director.arriveFromX = -7.8f;
        director.tankWaitX = 4.4f;

        // Sağdaki paslı su kulesi (arka plan resminde) -> etkileşimli su deposu
        var oldTank = GameObject.Find("Water Tank");
        if (oldTank) Object.DestroyImmediate(oldTank);
        var tank = new GameObject("Water Tank");
        tank.layer = InteractableLayer;
        tank.transform.position = new Vector3(5.8f, -1.3f, 0f);
        var tankCol = tank.AddComponent<BoxCollider2D>();
        tankCol.isTrigger = true;
        tankCol.size = new Vector2(2.2f, 2.6f);
        tank.AddComponent<WaterTank>().director = director;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ================= FutureHome (gazete + hikâye NPC'leri) =================
    static void SetupHome()
    {
        var scene = EditorSceneManager.OpenScene(HomeScene);
        var hero = ReplacePlayer();
        foreach (var f in Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None)) { f.target = hero.transform; EditorUtility.SetDirty(f); }
        foreach (var f in Object.FindObjectsByType<BoundedCameraFollow>(FindObjectsSortMode.None)) { f.target = hero.transform; EditorUtility.SetDirty(f); }

        var portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath);
        foreach (var door in Object.FindObjectsByType<TimeDoorInteraction>(FindObjectsSortMode.None))
        {
            door.sceneToLoad = "PastScene";
            door.prompt = "Zaman kapısından geç";
            door.portalEffect = true;
            EditorUtility.SetDirty(door);

            // Kapının yer tutucu kare görselini ortak portal görseliyle değiştir
            var visual = door.transform.Find("DoorVisual");
            if (visual && portalSprite)
            {
                var sr = visual.GetComponent<SpriteRenderer>();
                sr.sprite = portalSprite;
                sr.color = Color.white;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.sortingOrder = -2;
                float diameter = 1.9f;
                float s = diameter / portalSprite.bounds.size.x;
                visual.localScale = new Vector3(s, s, 1f);
                visual.localPosition = new Vector3(0f, diameter * 0.5f, 0f);
                if (!visual.GetComponent<PortalFX>()) visual.gameObject.AddComponent<PortalFX>();
                EditorUtility.SetDirty(sr);
            }
        }

        ReplaceStoryNPCs();

        // Eski, sahneye gömülü gazete arayüzünü kaldır (yenisi NewspaperUI tarafından kodla kurulur)
        foreach (var ui in Resources.FindObjectsOfTypeAll<NewspaperUI>())
        {
            if (!ui || EditorUtility.IsPersistent(ui) || ui.gameObject.scene != scene) continue;
            var canvas = ui.GetComponentInParent<Canvas>(true);
            Object.DestroyImmediate(canvas ? canvas.rootCanvas.gameObject : ui.gameObject);
        }
        foreach (var n in Object.FindObjectsByType<NewspaperInteraction>(FindObjectsSortMode.None)) { n.prompt = "Gazeteyi oku"; EditorUtility.SetDirty(n); }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void ReplaceStoryNPCs()
    {
        var map = new Dictionary<StoryCharacter, string[]>
        {
            { StoryCharacter.Elif, new[] { "Elif" } },
            { StoryCharacter.Rabis, new[] { "Rabis" } },
            { StoryCharacter.Zeynep, new[] { "Zeynep" } },
            { StoryCharacter.Emirhan, new[] { "Emirhan" } },
            { StoryCharacter.AnneVeDeniz, new[] { "Anne", "Deniz" } },
        };

        foreach (var old in Object.FindObjectsByType<StoryInteractable>(FindObjectsSortMode.None))
        {
            if (old.GetComponent<NPCWander2D>()) continue; // zaten değiştirilmiş
            if (!map.TryGetValue(old.character, out var ids)) continue;

            var oldSr = old.GetComponent<SpriteRenderer>();
            float footY = oldSr ? oldSr.bounds.min.y : old.transform.position.y;
            float x = old.transform.position.x;

            for (int i = 0; i < ids.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/NPCCharacters/Prefabs/NPC_{ids[i]}.prefab");
                if (!prefab) continue;
                var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                float dx = ids.Length > 1 ? (i - (ids.Length - 1) * 0.5f) * 0.55f : 0f;
                npc.transform.position = new Vector3(x + dx, footY, 0f);
                npc.layer = InteractableLayer;
                var wander = npc.GetComponent<NPCWander2D>();
                if (wander) wander.wander = false;
                var story = npc.AddComponent<StoryInteractable>();
                story.character = old.character;
                story.portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/NPCCharacters/Sprites/{ids[i]}/{ids[i]}_Idle_0.png");
            }
            Object.DestroyImmediate(old.gameObject);
        }
    }

    // ================= Geçmiş: nehir, fabrika, Rabiş =================
    static void SetupPast(DialogueData dialogue)
    {
        var scene = EditorSceneManager.OpenScene(PastScenePath);
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        // eski kurulumu ve DÖNGÜ'nün yer tutucu nesnelerini temizle (tekrar çalıştırılabilsin)
        foreach (var n in new[] { "PastEnvironment", "NPC_Rabis", "NPC_Emirhan", "FactoryPipeClue", "WarningSignClue", "WarningsignClue", "PastIntro", "Bounds", "TimeDoor" })
        {
            var old = GameObject.Find(n);
            if (old) Object.DestroyImmediate(old);
        }
        foreach (var s in Object.FindObjectsByType<StoryInteractable>(FindObjectsSortMode.None)) Object.DestroyImmediate(s.gameObject);

        var hero = ReplacePlayer();
        ApplyMaterial(hero, unlit);

        var ground = GameObject.Find("Ground");
        if (ground)
        {
            ground.transform.position = new Vector3(5f, GT - 0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 1f);
            var gsr = ground.GetComponent<SpriteRenderer>();
            if (gsr) gsr.enabled = false;
        }

        var spawn = GameObject.Find("PastSpawnPoint");
        if (spawn) spawn.transform.position = new Vector3(-8.2f, GT + 0.1f, 0f);
        hero.transform.position = spawn ? spawn.transform.position : new Vector3(-8.2f, GT, 0);

        var camGo = GameObject.FindWithTag("MainCamera");
        Camera cam = camGo ? camGo.GetComponent<Camera>() : null;
        if (cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.62f, 0.56f);
            foreach (var oldFollow in camGo.GetComponents<CameraFollow>()) Object.DestroyImmediate(oldFollow);
            foreach (var oldFollow in camGo.GetComponents<BoundedCameraFollow>()) Object.DestroyImmediate(oldFollow);
            var f = camGo.GetComponent<CameraFollow2D>();
            if (f == null) f = camGo.AddComponent<CameraFollow2D>();
            f.target = hero.transform;
            f.offset = new Vector2(0f, 2f);
            f.smoothTime = 0.2f;
            camGo.transform.position = new Vector3(hero.transform.position.x, hero.transform.position.y + 2f, -10f);
            // eski gökyüzü (kameranın çocuğu) varsa sil
            var oldSky = camGo.transform.Find("Sky");
            if (oldSky) Object.DestroyImmediate(oldSky.gameObject);
        }

        var root = new GameObject("PastEnvironment");

        if (cam)
        {
            var sky = Make("Sky", "Sky", cam.transform, Vector2.zero, -100, unlit);
            sky.transform.localPosition = new Vector3(0, 0, 20);
            sky.transform.localScale = new Vector3(10f, 1.3f, 1f);
        }

        var hillsFar = MakeTiled("HillsFar", "HillsFar", root.transform, new Vector2(-30, -1.3f), new Vector2(80, 2), -48, unlit);
        hillsFar.AddComponent<ParallaxLayer>().factor = 0.6f;
        var hillsNear = MakeTiled("HillsNear", "HillsNear", root.transform, new Vector2(-30, -1.2f), new Vector2(80, 2), -46, unlit);
        hillsNear.AddComponent<ParallaxLayer>().factor = 0.35f;

        var factory = MakeSized("Factory", "Factory", root.transform, new Vector2(5.6f, -0.75f), new Vector2(7f, 150f / 32f), -40, unlit);
        MakeSized("DeadTree", "DeadTree", root.transform, new Vector2(-6.5f, -0.75f), new Vector2(1.5f, 2f), -41, unlit);
        MakeSized("DeadTree2", "DeadTree", root.transform, new Vector2(16.5f, -0.75f), new Vector2(1.2f, 1.6f), -41, unlit);
        MakeTiled("FarBank", "FarBank", root.transform, new Vector2(-30, -1.1f), new Vector2(80, 0.625f), -38, unlit);

        var river = MakeTiled("River", "River_0", root.transform, new Vector2(-30, GT - 0.2f), new Vector2(80, 1.5f), -30, unlit);
        var rl = river.AddComponent<SpriteFrameLoop>();
        rl.frames = Sprites("River_0", "River_1", "River_2", "River_3"); rl.fps = 4f;

        MakeSized("Pipe", "Pipe", root.transform, new Vector2(3.6f, -1.6f), new Vector2(2.5f, 70f / 32f), -35, unlit);
        var sludge = MakeSized("Sludge", "Sludge_0", root.transform, new Vector2(4.03f, -2.3f), new Vector2(0.5f, 60f / 32f * 0.6f), -29, unlit);
        var sl = sludge.AddComponent<SpriteFrameLoop>();
        sl.frames = Sprites("Sludge_0", "Sludge_1", "Sludge_2"); sl.fps = 9f;
        MakeSized("Plume", "Plume", root.transform, new Vector2(2.6f, -2.45f), new Vector2(4.6f, 0.85f), -28, unlit);

        var fishRoot = new GameObject("DeadFish"); fishRoot.transform.SetParent(root.transform);
        Vector2[] fish = { new Vector2(2.2f, -2.0f), new Vector2(5.0f, -1.75f), new Vector2(6.6f, -1.95f), new Vector2(9.3f, -1.85f), new Vector2(11.5f, -2.0f), new Vector2(-3.5f, -1.9f), new Vector2(14f, -1.8f) };
        for (int i = 0; i < fish.Length; i++)
        {
            var fgo = Make("Fish" + i, "DeadFish", fishRoot.transform, fish[i] + new Vector2(0.375f, 0.19f), -27, unlit);
            fgo.transform.localScale = Vector3.one * 1.2f;
            fgo.GetComponent<SpriteRenderer>().flipX = i % 2 == 1;
            var b = fgo.AddComponent<FloatBob>(); b.amplitude = 0.04f; b.speed = 1.2f + i * 0.13f; b.tiltDegrees = 5f;
        }

        MakeTiled("GroundTop", "GroundTop", root.transform, new Vector2(-30, GT - 1f), new Vector2(80, 1), -10, unlit);
        MakeTiled("GroundFill", "GroundFill", root.transform, new Vector2(-30, GT - 9f), new Vector2(80, 8), -11, unlit);
        foreach (var x in new[] { 1.2f, 7.5f, -5f, 12.5f })
            MakeSized("Reeds", "Reeds", root.transform, new Vector2(x, GT), new Vector2(18f / 32f * 1.4f, 26f / 32f * 1.4f), -5, unlit);
        MakeSized("WarningSign", "WarningSign", root.transform, new Vector2(5.2f, GT), new Vector2(26f / 32f * 1.3f, 40f / 32f * 1.3f), -4, unlit);

        var puffs = Sprites("Smoke_0", "Smoke_1");
        foreach (var cx in new[] { 40f, 70f })
        {
            float top = cx == 40f ? 150f : 128f;
            var e = new GameObject("Smoke");
            e.transform.SetParent(factory.transform.parent);
            e.transform.position = new Vector3(5.6f + cx / 224f * 7f, -0.75f + top / 32f, 0f);
            var se = e.AddComponent<SmokeEmitter>();
            se.puffs = puffs; se.material = unlit; se.interval = cx == 40f ? 0.5f : 0.65f;
        }

        // Numune şişesi (görev) — ortak etkileşim sistemi için tetikleyici çarpıştırıcı
        var bottle = Make("SampleBottle", "SampleBottle", root.transform, new Vector2(6.7f, GT + 0.38f), 6, unlit);
        bottle.transform.localScale = Vector3.one * 1.5f;
        bottle.layer = InteractableLayer;
        var bc = bottle.AddComponent<BoxCollider2D>(); bc.isTrigger = true;
        bottle.AddComponent<WaterSamplePickup>();

        // Zaman portalı (geleceğe dönüş) — dolaptaki ile aynı görsel
        var portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath);
        var portal = new GameObject("TimePortal");
        portal.transform.SetParent(root.transform, false);
        const float diameter = 2.3f;
        portal.transform.position = new Vector3(-11.0f, GT + diameter * 0.5f + 0.05f, 0f);
        var psr = portal.AddComponent<SpriteRenderer>();
        psr.sprite = portalSprite;
        psr.sortingOrder = -6;
        if (unlit) psr.sharedMaterial = unlit;
        if (portalSprite) { float s = diameter / portalSprite.bounds.size.x; portal.transform.localScale = new Vector3(s, s, 1f); }
        portal.layer = InteractableLayer;
        var pc = portal.AddComponent<CircleCollider2D>(); pc.isTrigger = true;
        var door = portal.AddComponent<TimeDoorInteraction>();
        door.sceneToLoad = "FutureInterior";
        door.returnToPreviousScene = true;
        door.prompt = "Zaman kapısı: Geleceğe dön";
        door.portalEffect = true;
        portal.AddComponent<PortalFX>();

        var bounds = new GameObject("Bounds");
        foreach (var x in new[] { -12.8f, 23f })
        {
            var w = new GameObject("Wall"); w.transform.SetParent(bounds.transform);
            w.transform.position = new Vector3(x, 2f, 0);
            w.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
        }

        // Bilim insanı Rabiş (görev diyaloğu)
        var rabisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPCCharacters/Prefabs/NPC_Rabis.prefab");
        var rabis = (GameObject)PrefabUtility.InstantiatePrefab(rabisPrefab);
        rabis.transform.position = new Vector3(-3.35f, GT, 0f); // nehir kıyısında, fabrikadan uzakta
        rabis.layer = InteractableLayer;
        var wander = rabis.GetComponent<NPCWander2D>();
        if (wander) wander.wander = false;
        var talk = rabis.AddComponent<NPCDialogue>();
        talk.dialogue = dialogue;
        talk.portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/NPCCharacters/Sprites/Rabis/Rabis_Idle_0.png");
        ApplyMaterial(rabis, unlit);

        // Fabrika görevlisi Emirhan: Rabiş görev vermeden konuşmaz; ikna edilmeden su örneği alınamaz
        var emirhanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPCCharacters/Prefabs/NPC_Emirhan.prefab");
        if (emirhanPrefab)
        {
            var emirhan = (GameObject)PrefabUtility.InstantiatePrefab(emirhanPrefab);
            emirhan.name = "NPC_Emirhan";
            emirhan.transform.position = new Vector3(2.92f, GT, 0f);
            emirhan.layer = InteractableLayer;
            var guardData = AssetDatabase.LoadAssetAtPath<NPCData>("Assets/NPCCharacters/Data/FactoryGuard_Data.asset");
            var ew = emirhan.GetComponent<NPCWander2D>();
            if (ew && guardData) ew.data = guardData; // borunun çevresinde nöbet tutar
            var guardTalk = emirhan.AddComponent<NPCDialogue>();
            guardTalk.dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/PastScene/Data/FactoryGuard_Dialogue.asset");
            emirhan.AddComponent<FactoryGuard>().factoryDoorX = 10.5f; // ikna olunca fabrikaya girer
            guardTalk.portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/NPCCharacters/Sprites/Emirhan/Emirhan_Idle_0.png");
            guardTalk.requiresQuest = false; // görevden önce kendi "araziden çık" cümlesini söyler
            guardTalk.requiredQuestFlag = "gorev_alindi";
            if (!guardTalk.dialogue) Debug.LogWarning("[DÖNGÜ] FactoryGuard_Dialogue.asset bulunamadı.");
            ApplyMaterial(emirhan, unlit);
        }

        // İpuçları: fabrika borusu ve uyarı tabelası
        var pipeClue = new GameObject("FactoryPipeClue");
        pipeClue.layer = InteractableLayer;
        pipeClue.transform.position = new Vector3(4.76f, -1.33f, 0f);
        var pcc = pipeClue.AddComponent<BoxCollider2D>(); pcc.isTrigger = true; pcc.size = new Vector2(2.6f, 1.16f);
        pipeClue.AddComponent<FactoryPipeClue>();

        var signClue = new GameObject("WarningSignClue");
        signClue.layer = InteractableLayer;
        signClue.transform.position = new Vector3(5.85f, -1.53f, 0f);
        var scc = signClue.AddComponent<BoxCollider2D>(); scc.isTrigger = true; scc.size = new Vector2(0.91f, 1.38f);
        signClue.AddComponent<WarningSignClue>();

        var intro = new GameObject("PastIntro").AddComponent<PastSceneIntro>();
        intro.spawnPoint = spawn ? spawn.transform : null;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ================= Ortak yardımcılar =================
    static GameObject ReplacePlayer()
    {
        var old = GameObject.Find("Player");
        Vector3 pos = Vector3.zero;
        if (old)
        {
            pos = old.transform.position;
            var oldSr = old.GetComponent<SpriteRenderer>();
            if (oldSr && !old.GetComponent<HeroController2D>()) pos.y = oldSr.bounds.min.y + 0.05f; // Hero'nun pivotu ayak hizasında
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HeroCharacter/Hero.prefab");
        var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        hero.name = "Player";
        hero.tag = "Player";
        hero.transform.position = pos;

        var pi = hero.AddComponent<PlayerInteraction>();
        pi.interactionRange = InteractRange;
        pi.interactableLayer = ~0;

        var hc = hero.GetComponent<HeroController2D>();
        if (hc) hc.groundLayers = ~0;

        if (old) Object.DestroyImmediate(old);
        return hero;
    }

    static HomeStoryDirector AddStoryDirector(HomeStoryDirector.Place place)
    {
        var old = GameObject.Find("HomeStory");
        if (old) Object.DestroyImmediate(old);
        var director = new GameObject("HomeStory").AddComponent<HomeStoryDirector>();
        director.place = place;
        director.floorY = -2.1f;
        director.zeynepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPCCharacters/Prefabs/NPC_Zeynep.prefab");
        director.annePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPCCharacters/Prefabs/NPC_Anne.prefab");
        director.denizPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NPCCharacters/Prefabs/NPC_Deniz.prefab");
        return director;
    }

    static void SetupBoundedCamera(GameObject hero)
    {
        var camGo = GameObject.Find("Main Camera");
        if (!camGo) return;
        var follow = camGo.GetComponent<BoundedCameraFollow>();
        if (follow) { follow.target = hero.transform; EditorUtility.SetDirty(follow); }
        var plain = camGo.GetComponent<CameraFollow>();
        if (plain) { plain.target = hero.transform; EditorUtility.SetDirty(plain); }
    }

    static void AddWalls(float left, float right)
    {
        var oldBounds = GameObject.Find("Bounds");
        if (oldBounds) Object.DestroyImmediate(oldBounds);
        var bounds = new GameObject("Bounds");
        foreach (var x in new[] { left - 0.5f, right + 0.5f })
        {
            var w = new GameObject("Wall"); w.transform.SetParent(bounds.transform);
            w.transform.position = new Vector3(x, 0f, 0f);
            w.AddComponent<BoxCollider2D>().size = new Vector2(1f, 20f);
        }
    }

    static DialogueData BuildRabisDialogue()
    {
        Directory.CreateDirectory(DataDir);
        string path = DataDir + "/Rabis_Dialogue.asset";
        var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (d == null) { d = ScriptableObject.CreateInstance<DialogueData>(); AssetDatabase.CreateAsset(d, path); }
        RabisDialogue.Fill(d);
        EditorUtility.SetDirty(d);
        AssetDatabase.SaveAssets();
        return d;
    }

    static void BuildFactoryGuardDialogue()
    {
        Directory.CreateDirectory(DataDir);
        string path = DataDir + "/FactoryGuard_Dialogue.asset";
        var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (d == null) { d = ScriptableObject.CreateInstance<DialogueData>(); AssetDatabase.CreateAsset(d, path); }
        FactoryGuardDialogue.Fill(d);
        EditorUtility.SetDirty(d);
        AssetDatabase.SaveAssets();
    }

    static readonly HashSet<string> CenterPivot = new HashSet<string> { "Sky", "DeadFish", "Smoke_0", "Smoke_1", "SampleBottle" };

    static void ImportPastArt()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PastArt }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            string n = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 32;
            ti.filterMode = n == "Sky" ? FilterMode.Bilinear : FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = CenterPivot.Contains(n) ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }

    static void ImportPortalSprite()
    {
        AssetDatabase.ImportAsset(PortalSpritePath, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(PortalSpritePath) as TextureImporter;
        if (ti == null) { Debug.LogError("[DÖNGÜ] Portal görseli bulunamadı: " + PortalSpritePath); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 150;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.spritePivot = new Vector2(0.5f, 0.5f);
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
    }

    static void ImportInteriorV2()
    {
        SetSprite(ArtEnv + "FutureInterior_v2.png", IntBgPPU, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        SetSprite(ArtEnv + "Cabinet/CabinetDoorLeft.png", IntBgPPU, new Vector2(0f, 0.5f), FilterMode.Bilinear);
        SetSprite(ArtEnv + "Cabinet/CabinetDoorRight.png", IntBgPPU, new Vector2(1f, 0.5f), FilterMode.Bilinear);
        SetSprite(ArtEnv + "Cabinet/CabinetInside.png", IntBgPPU, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        SetSprite(ArtEnv + "Cabinet/CabinetGlow.png", 100f, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Portal" }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.SaveAndReimport();
        }
    }

    static void SetSprite(string path, float ppu, Vector2 pivot, FilterMode filter)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[DÖNGÜ] Görsel bulunamadı: " + path); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = filter;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        st.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    static void ImportEnvironmentSprites()
    {
        foreach (var f in new[] { "FutureExterior.png", "FutureInterior.png", "CabinetPortalOpen.png", "InteriorExitDoor.png" })
        {
            var ti = AssetImporter.GetAtPath("Assets/Art/Environment/" + f) as TextureImporter;
            if (ti == null || ti.textureType == TextureImporterType.Sprite) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 100f;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
    }

    static void ImportNewspaperPages()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Newspaper", "Assets/Resources/Ending" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
    }

    static Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{PastArt}/{n}.png");
    static Sprite[] Sprites(params string[] names) { var a = new Sprite[names.Length]; for (int i = 0; i < a.Length; i++) a[i] = S(names[i]); return a; }

    static GameObject Make(string name, string sprite, Transform parent, Vector2 pos, int order, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S(sprite);
        sr.sortingOrder = order;
        if (mat) sr.sharedMaterial = mat;
        return go;
    }

    static GameObject MakeSized(string name, string sprite, Transform parent, Vector2 pos, Vector2 size, int order, Material mat)
    {
        var go = Make(name, sprite, parent, pos, order, mat);
        var sp = go.GetComponent<SpriteRenderer>().sprite;
        if (sp) go.transform.localScale = new Vector3(size.x / sp.bounds.size.x, size.y / sp.bounds.size.y, 1f);
        return go;
    }

    static GameObject MakeTiled(string name, string sprite, Transform parent, Vector2 pos, Vector2 size, int order, Material mat)
    {
        var go = Make(name, sprite, parent, pos, order, mat);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        return go;
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        if (!mat) return;
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.sharedMaterial = mat;
    }
}
#endif
