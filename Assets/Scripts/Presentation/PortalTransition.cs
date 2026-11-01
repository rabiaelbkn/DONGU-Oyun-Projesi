using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sahne geçiş animasyonları (videodakiyle aynı):
///  - Portal: kamera portala yakınlaşır, ekranda büyüyüp dönen zaman girdabı, altın ışık -> yeni sahne,
///            yeni sahnede altın ışık yavaşça söner. Uğultu sesi çalar.
///  - Kapı:   kısa karartma -> yeni sahne -> açılma. Kapı sesi çalar.
/// </summary>
public class PortalTransition : MonoBehaviour
{
    static PortalTransition instance;
    public static bool Active { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; Active = false; }

    static readonly Color Gold = new Color(1f, 0.925f, 0.745f, 1f);

    RawImage swirl, flash;
    RectTransform swirlRect, canvasRect;

    public static void Play(string scene, Vector3 worldFocus, bool portal)
    {
        if (Active) return;
        if (instance == null)
        {
            var go = new GameObject("PortalTransition");
            instance = go.AddComponent<PortalTransition>();
            instance.Build();
        }
        instance.StartCoroutine(portal ? instance.PortalRoutine(scene, worldFocus) : instance.DoorRoutine(scene));
    }

    IEnumerator DoorRoutine(string scene)
    {
        Active = true;
        AudioManager.Sfx("sfx_door");
        swirl.enabled = false;
        yield return Fade(flash, Color.black, 0f, 1f, 0.35f);
        yield return Load(scene);
        yield return Fade(flash, Color.black, 1f, 0f, 0.4f);
        Active = false;
    }

    IEnumerator PortalRoutine(string scene, Vector3 focus)
    {
        Active = true;
        AudioManager.Sfx("sfx_whoosh");
        var cam = Camera.main;
        Vector3 camStart = cam ? cam.transform.position : Vector3.zero;
        float sizeStart = cam ? cam.orthographicSize : 5f;
        if (cam)
        {
            foreach (var b in cam.GetComponents<MonoBehaviour>())
                if (b is BoundedCameraFollow || b is CameraFollow || b is CameraFollow2D) b.enabled = false;
        }

        swirl.enabled = true;
        const float zoomTime = 1.4f, total = 2.2f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float z = Smooth(Mathf.Clamp01(t / zoomTime));
            z = Mathf.Pow(z, 1.6f);
            if (cam)
            {
                cam.transform.position = new Vector3(Mathf.Lerp(camStart.x, focus.x, z), Mathf.Lerp(camStart.y, focus.y, z), camStart.z);
                cam.orthographicSize = Mathf.Lerp(sizeStart, sizeStart / 6.5f, z);
            }
            float k = Mathf.Clamp01((t - 0.5f) / (total - 0.5f));
            float size = Mathf.Lerp(200f, 3400f, Smooth(k));
            swirlRect.sizeDelta = new Vector2(size, size);
            swirlRect.localRotation = Quaternion.Euler(0, 0, -t * 45f);
            swirl.color = new Color(1, 1, 1, Mathf.Clamp01(k * 3f));
            float glow = Mathf.Clamp01((k - 0.55f) / 0.45f);
            flash.color = new Color(Gold.r, Gold.g, Gold.b, glow);
            yield return null;
        }
        flash.color = Gold;
        swirl.enabled = false;
        yield return Load(scene);
        yield return Fade(flash, Gold, 1f, 0f, 0.6f);
        Active = false;
    }

    static IEnumerator Load(string scene)
    {
        var op = SceneManager.LoadSceneAsync(scene);
        while (op != null && !op.isDone) yield return null;
        yield return null;
    }

    static IEnumerator Fade(RawImage img, Color c, float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            img.color = new Color(c.r, c.g, c.b, Mathf.SmoothStep(from, to, t / time));
            yield return null;
        }
        img.color = new Color(c.r, c.g, c.b, to);
    }

    static float Smooth(float x) => x * x * (3f - 2f * x);

    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = (RectTransform)transform;

        var sw = new GameObject("Swirl", typeof(RectTransform), typeof(RawImage));
        sw.transform.SetParent(transform, false);
        swirl = sw.GetComponent<RawImage>();
        swirl.texture = Resources.Load<Texture2D>("Portal/TimePortal");
        swirl.raycastTarget = false;
        swirlRect = swirl.rectTransform;
        swirlRect.anchorMin = swirlRect.anchorMax = swirlRect.pivot = new Vector2(0.5f, 0.5f);
        swirl.enabled = false;

        var fl = new GameObject("Flash", typeof(RectTransform), typeof(RawImage));
        fl.transform.SetParent(transform, false);
        flash = fl.GetComponent<RawImage>();
        var r = flash.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        flash.color = new Color(0, 0, 0, 0);
        flash.raycastTarget = false;
    }
}
