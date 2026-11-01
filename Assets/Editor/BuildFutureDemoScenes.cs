using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BuildFutureDemoScenes
{
    // Otomatik kurulum kaldırıldı: birleşik proje "Tools > DÖNGÜ > Oyunu Kur" ile kurulur.
    private const string SceneFolder = "Assets/Scenes/";
    private const string ArtFolder = "Assets/Art/Environment/";
    private const int InteractableLayer = 6;
    private const int GroundLayer = 3;

    [MenuItem("Tools/DÖNGÜ/Eski: Gelecek Sahnelerini Sıfırdan Üret")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        PrepareSprite("FutureExterior.png");
        PrepareSprite("FutureInterior.png");
        PrepareSprite("CabinetPortalOpen.png");
        PrepareSprite("InteriorExitDoor.png");

        Scene sourceScene = SceneManager.GetActiveScene();
        GameObject sourcePlayer = GameObject.Find("Player");
        GameObject sourceCamera = GameObject.Find("Main Camera");
        GameObject sourceLight = GameObject.Find("Global Light 2D");
        if (sourcePlayer == null || sourceCamera == null || sourceLight == null)
        {
            Debug.LogError("Future demo setup needs Player, Main Camera and Global Light 2D in the open source scene.");
            return;
        }

        Scene exterior = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        CreateWorld(exterior, sourcePlayer, sourceCamera, sourceLight, false);
        string exteriorPath = SceneFolder + "FutureExterior.unity";
        EditorSceneManager.SaveScene(exterior, exteriorPath);

        Scene interior = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        CreateWorld(interior, sourcePlayer, sourceCamera, sourceLight, true);
        string interiorPath = SceneFolder + "FutureInterior.unity";
        EditorSceneManager.SaveScene(interior, interiorPath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(interiorPath, true),
            new EditorBuildSettingsScene(exteriorPath, true)
        };

        EditorSceneManager.SetActiveScene(sourceScene);
        EditorSceneManager.CloseScene(exterior, true);
        EditorSceneManager.CloseScene(interior, true);
        EditorSceneManager.OpenScene(interiorPath, OpenSceneMode.Single);
        AssetDatabase.Refresh();
        Debug.Log("Future-only demo created: start inside; the house door toggles inside/outside; the cupboard opens a portal.");
    }

    private static void PrepareSprite(string fileName)
    {
        string path = ArtFolder + fileName;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void CreateWorld(Scene scene, GameObject sourcePlayer, GameObject sourceCamera, GameObject sourceLight, bool indoor)
    {
        string backgroundPath = ArtFolder + (indoor ? "FutureInterior.png" : "FutureExterior.png");
        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
        SpriteRenderer background = new GameObject("House Background").AddComponent<SpriteRenderer>();
        SceneManager.MoveGameObjectToScene(background.gameObject, scene);
        background.sprite = backgroundSprite;
        background.sortingOrder = -20;
        background.transform.position = Vector3.zero;
        if (!indoor) background.transform.localScale = Vector3.one * 0.85f;

        GameObject floor = new GameObject("Ground");
        SceneManager.MoveGameObjectToScene(floor, scene);
        floor.layer = GroundLayer;
        floor.tag = "Ground";
        floor.transform.position = new Vector3(0f, -2.3f, 0f);
        BoxCollider2D floorCollider = floor.AddComponent<BoxCollider2D>();
        floorCollider.size = new Vector2(indoor ? 13f : 18f, 0.4f);

        GameObject player = Object.Instantiate(sourcePlayer);
        player.name = "Player";
        SceneManager.MoveGameObjectToScene(player, scene);
        player.transform.position = new Vector3(indoor ? -4.6f : -5.4f, -1.55f, 0f);
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.interactionRange = 2f;
            interaction.interactableLayer = 1 << InteractableLayer;
        }

        GameObject cameraObject = Object.Instantiate(sourceCamera);
        cameraObject.name = "Main Camera";
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera cameraComponent = cameraObject.GetComponent<Camera>();
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = indoor ? 2.42f : 3.75f;
        CameraFollow oldFollow = cameraObject.GetComponent<CameraFollow>();
        if (oldFollow != null) Object.DestroyImmediate(oldFollow);
        BoundedCameraFollow follow = cameraObject.GetComponent<BoundedCameraFollow>();
        if (follow == null) follow = cameraObject.AddComponent<BoundedCameraFollow>();
        follow.target = player.transform;
        follow.leftBound = indoor ? -6.39f : -7.11f;
        follow.rightBound = indoor ? 6.39f : 7.11f;
        follow.followY = false;

        GameObject lightObject = Object.Instantiate(sourceLight);
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        lightObject.name = "Global Light 2D";
        Light2D light = lightObject.GetComponent<Light2D>();
        if (light != null) light.intensity = 1f;

        if (indoor)
        {
            Sprite doorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "InteriorExitDoor.png");
            SpriteRenderer insideDoorVisual = new GameObject("Inside Exit Door Visual").AddComponent<SpriteRenderer>();
            SceneManager.MoveGameObjectToScene(insideDoorVisual.gameObject, scene);
            insideDoorVisual.sprite = doorSprite;
            insideDoorVisual.sortingOrder = -1;
            insideDoorVisual.transform.position = new Vector3(5.35f, -0.65f, 0f);
            insideDoorVisual.transform.localScale = Vector3.one * 1.05f;
            MakeDoor("House Exit Door", scene, new Vector2(5.35f, -1.3f), new Vector2(1.4f, 2.4f), "FutureExterior");
            CreateCabinet(scene);
        }
        else
        {
            MakeDoor("House Entrance Door", scene, new Vector2(-1.6f, -1.25f), new Vector2(1.5f, 2.5f), "FutureInterior");
        }
    }

    private static void MakeDoor(string objectName, Scene scene, Vector2 position, Vector2 colliderSize, string targetScene)
    {
        GameObject door = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(door, scene);
        door.layer = InteractableLayer;
        door.transform.position = position;
        BoxCollider2D collider = door.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = colliderSize;
        TimeDoorInteraction transition = door.AddComponent<TimeDoorInteraction>();
        transition.sceneToLoad = targetScene;
    }

    private static void CreateCabinet(Scene scene)
    {
        GameObject cabinet = new GameObject("Cabinet Portal");
        SceneManager.MoveGameObjectToScene(cabinet, scene);
        cabinet.layer = InteractableLayer;
        cabinet.transform.position = new Vector3(-4.6f, -0.35f, 0f);
        BoxCollider2D collider = cabinet.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.9f, 3.0f);

        Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "CabinetPortalOpen.png");
        SpriteRenderer preview = new GameObject("Opened Portal Preview").AddComponent<SpriteRenderer>();
        SceneManager.MoveGameObjectToScene(preview.gameObject, scene);
        preview.transform.SetParent(cabinet.transform, false);
        preview.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        preview.transform.localScale = Vector3.one * 0.55f;
        preview.sprite = portalSprite;
        preview.sortingOrder = 10;
        BoxCollider2D previewCollider = preview.gameObject.AddComponent<BoxCollider2D>();
        previewCollider.isTrigger = true;
        previewCollider.size = new Vector2(2.8f, 4.4f);
        preview.gameObject.layer = InteractableLayer;
        preview.gameObject.SetActive(false);

        TimeDoorInteraction timeDoor = cabinet.AddComponent<TimeDoorInteraction>();
        timeDoor.sceneToLoad = "FutureExterior";
        CabinetPortalInteraction portal = cabinet.AddComponent<CabinetPortalInteraction>();
        portal.portalVisual = preview.gameObject;
        portal.timeDoor = timeDoor;
    }
}
