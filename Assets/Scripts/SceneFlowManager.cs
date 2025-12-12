// Assets/Scripts/SceneFlowManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Fade (Realtime)")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private Color fadeColor = Color.black;

    private Image overlay;
    private Canvas canvas;
    private bool isLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
    }

    private void EnsureOverlay()
    {
        if (overlay != null) return;

        var canvasGO = new GameObject("SceneFlowCanvas");
        DontDestroyOnLoad(canvasGO);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("FadeOverlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        overlay = imgGO.AddComponent<Image>();
        overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        overlay.raycastTarget = true;

        var rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (isLoading) return;
        EnsureOverlay();
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeOutDuration));
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(1f);

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeInDuration));
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(0f);

        isLoading = false;
    }

    private void SetAlpha(float a)
    {
        if (!overlay) return;
        var c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
