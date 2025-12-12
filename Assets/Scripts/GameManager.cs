// Assets/Scripts/GameManager.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // DEBUG
    [Header("Debug Testing")]
    public bool debugStartAtScore = false;
    public float debugScoreValue = 2900f;

    // MOVEMENT
    [Header("Movement")]
    public float baseSpeed = 5f;
    public float speedMultiplier = 1f;

    // SCORE
    [Header("Score")]
    public float score;
    public float scoreRate = 5f;

    // DIFFICULTY
    [Header("Difficulty")]
    public float speedIncreaseRate = 0.05f;
    public float speedIncreaseInterval = 5f;
    private float difficultyTimer;

    // LIVES
    [Header("Life Settings")]
    public int startingLives = 3;
    [HideInInspector] public int currentLives;

    // WIN CONDITION / FINALE GATE
    [Header("Win Condition / Finale Gate")]
    public float targetScore = 3000f;

    [Tooltip("If OFF, the Red Bull finale never triggers in this scene.")]
    public bool finalSequenceEnabled = true;

    // PART-2 STYLE OPTIONS
    [Header("Start Score Override (if no carryover)")]
    [Tooltip("If true and no carryover score found, start from this value (e.g., 3000 for Part 2).")]
    public bool overrideStartScore = false;
    public float startScoreOverride = 3000f;

    [Header("When finale is disabled")]
    [Tooltip("If true and Final Sequence is disabled, reaching targetScore stops and shows a small finish text fade.")]
    public bool autoStopAtTarget = true;

    [Header("Part 2 Finish UI (optional)")]
    public TMP_Text part2EndText;
    public float part2FadeIn = 0.45f;
    public float part2Hold = 1.1f;
    public float part2FadeOut = 0.6f;

    [Header("Finish Fade (Part 2)")]
    [Tooltip("How long the rocket & meteors fade to invisible at the end.")]
    public float finishFadeDuration = 0.6f;
    [Tooltip("Destroy faded objects after they finish dissolving.")]
    public bool destroyAfterFinishFade = true;
    [Tooltip("Freeze rocket (no motion) during the finish fade.")]
    public bool freezeRocketDuringFinish = true;

    // UI
    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text highScoreText;

    [Header("Hearts UI")]
    public Image[] heartImages;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    // FINAL SEQUENCE (safe to leave nulls in scenes without finale)
    [Header("Final Sequence")]
    public GameObject redBullPrefab;
    public TMP_Text wingsText;

    [Header("Final FX (optional)")]
    public ParticleSystem wingsBurstPrefab;
    public Image screenFlashImage;
    public Color flashColor = Color.white;
    [Range(0f, 1f)] public float flashPeakAlpha = 0.35f;
    public float flashInDuration = 0.06f;
    public float flashOutDuration = 0.25f;

    [Header("Final Sequence Tuning")]
    [Tooltip("World slowdown applied during the final approach (no timeScale changes).")]
    public float finalSeqSlowFactor = 0.75f;
    [Tooltip("Delay before spawning the can once target score is reached.")]
    public float canSpawnDelay = 0.10f;

    [Header("Final Sequence Spawn")]
    public float redBullSpawnX = 5.5f;
    public float redBullSpawnY = -0.3f;
    [Tooltip("Designer nudge. Negative lowers the can; positive raises it.")]
    public float redBullSpawnOffsetY = 0f;

    [Header("Part Flow")]
    [Tooltip("Scene to load after the finale (Part 2). Leave empty to stay.")]
    public string nextSceneName = "";
    public bool autoAdvanceAfterFinale = true;

    public bool finalTriggered = false;
    public bool finalSequenceStarted = false;

    private bool gameEnded = false;
    public bool inFinalCinematic = false;

    private float highScore;
    private CameraShake cameraShake;

    // --- Helpers to avoid NREs ------------------------------------------------
    private int HeartCount => heartImages != null ? heartImages.Length : 0;
    private bool HeartIndexValid(int i) => i >= 0 && i < HeartCount && heartImages[i] != null;

    private void WarnIfHeartSlotsMissing()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogWarning("[GameManager] No heart images assigned. Lives UI will be hidden.");
            return;
        }
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
                Debug.LogWarning($"[GameManager] heartImages[{i}] is empty (null). Flash for that slot will be skipped.");
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { DestroyImmediate(gameObject); return; }

        finalTriggered = false;
        finalSequenceStarted = false;
        gameEnded = false;
        inFinalCinematic = false;

        highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        cameraShake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;

        if (screenFlashImage != null)
        {
            var c = screenFlashImage.color; c.a = 0f;
            screenFlashImage.color = c;
            screenFlashImage.gameObject.SetActive(true);
        }

        // Ensure singletons exist
        if (SceneFlowManager.Instance == null)
            new GameObject("SceneFlowManager").AddComponent<SceneFlowManager>();
        if (SessionState.Instance == null)
            new GameObject("SessionState").AddComponent<SessionState>();

        WarnIfHeartSlotsMissing();
    }

    private void Start()
    {
        NewGame();
        UpdateHighScoreUI();

        if (debugStartAtScore)
        {
            score = debugScoreValue;
            UpdateScoreUI();
        }
    }

    public void NewGame()
    {
        // ----- SCORE START (carryover or override) -----
        score = 0f;
        if (SessionState.Instance != null && SessionState.Instance.TryConsumeScore(out float carryScore))
        {
            score = carryScore;
        }
        else if (overrideStartScore)
        {
            score = startScoreOverride;
        }
        UpdateScoreUI();

        // ----- SPEED & DIFFICULTY -----
        speedMultiplier = 1f;
        baseSpeed = 5f;
        difficultyTimer = 0f;

        // ----- LIVES (carryover allowed) -----
        if (SessionState.Instance != null && SessionState.Instance.TryConsumeLives(out int carry))
            startingLives = Mathf.Max(0, carry);

        // Clamp to available heart slots
        int maxHearts = HeartCount > 0 ? HeartCount : startingLives;
        startingLives = Mathf.Clamp(startingLives, 0, maxHearts);
        currentLives = startingLives;

        // ----- FLAGS -----
        gameEnded = false;
        finalTriggered = false;
        finalSequenceStarted = false;
        inFinalCinematic = false;

        if (wingsText != null) wingsText.alpha = 0f;
        if (part2EndText != null) part2EndText.alpha = 0f;

        UpdateHeartsUI();
        UpdateScoreUI();
    }

    private void Update()
    {
        if (inFinalCinematic) return;

        if (!finalTriggered && !gameEnded)
        {
            // Score tick
            score += scoreRate * speedMultiplier * Time.deltaTime;

            // Difficulty ramp
            difficultyTimer += Time.deltaTime;
            if (difficultyTimer >= speedIncreaseInterval)
            {
                difficultyTimer = 0f;
                speedMultiplier += speedIncreaseRate;
            }

            UpdateScoreUI();
            CheckHighScore();
        }

        // Trigger behavior at target
        if (!finalTriggered && !gameEnded && score >= targetScore)
        {
            if (finalSequenceEnabled && redBullPrefab != null)
            {
                finalTriggered = true;
                StartCoroutine(StartFinalSequence());
            }
            else if (autoStopAtTarget)
            {
                // Part-2 style finish (+ fade rocket/meteors)
                StartCoroutine(Part2FinishRoutine());
            }
        }
    }

    // Part-2 finish routine: stop world, fade rocket+meteors cleanly, then fade text.
    private IEnumerator Part2FinishRoutine()
    {
        gameEnded = true;

        // stop spawners and world movement
        DisableAllSpawners();
        baseSpeed = 0f;
        speedMultiplier = 0f;

        // Optionally freeze rocket so it doesn't bob while fading
        if (freezeRocketDuringFinish) inFinalCinematic = true;

        // Fade rocket & obstacles
        yield return StartCoroutine(FadeOutRocketAndObstacles(finishFadeDuration, destroyAfterFinishFade));

        // text fade (realtime)
        if (part2EndText != null)
        {
            part2EndText.gameObject.SetActive(true);
            part2EndText.alpha = 0f;

            float t = 0f;
            while (t < part2FadeIn)
            {
                t += Time.unscaledDeltaTime;
                part2EndText.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, part2FadeIn));
                yield return null;
            }
            part2EndText.alpha = 1f;

            if (part2Hold > 0f)
                yield return new WaitForSecondsRealtime(part2Hold);

            t = 0f;
            while (t < part2FadeOut)
            {
                t += Time.unscaledDeltaTime;
                part2EndText.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.0001f, part2FadeOut));
                yield return null;
            }
            part2EndText.alpha = 0f;
        }
    }

    // Fades the RocketPlayer and all GameObjects tagged "Obstacle" by lerping SpriteRenderer alpha to zero.
    private IEnumerator FadeOutRocketAndObstacles(float duration, bool destroyAfter)
    {
        var renderers = new List<SpriteRenderer>();
        var targets = new List<GameObject>();

        // Rocket
        var rocket = FindObjectOfType<RocketPlayer>();
        if (rocket != null)
        {
            var go = rocket.gameObject;
            targets.Add(go);
            renderers.AddRange(go.GetComponentsInChildren<SpriteRenderer>(true));
            DisableAllColliders(go);
            DisableMoveLeft(go);
        }

        // Obstacles
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var go in obstacles)
        {
            if (!targets.Contains(go)) targets.Add(go);
            renderers.AddRange(go.GetComponentsInChildren<SpriteRenderer>(true));
            DisableAllColliders(go);
            DisableMoveLeft(go);
        }

        if (renderers.Count == 0) yield break;

        // Capture original colors
        var originals = new List<Color>(renderers.Count);
        for (int i = 0; i < renderers.Count; i++)
            originals.Add(renderers[i] != null ? renderers[i].color : Color.white);

        // Fade (realtime)
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                Color c = originals[i];
                c.a = a * c.a;
                r.color = c;
            }
            yield return null;
        }

        // Ensure invisible
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var c = r.color; c.a = 0f; r.color = c;
        }

        if (destroyAfter)
        {
            foreach (var go in targets)
                if (go != null) Destroy(go);
        }
    }

    private static void DisableAllColliders(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        go.tag = "Untagged"; // extra safety
    }

    private static void DisableMoveLeft(GameObject go)
    {
        foreach (var ml in go.GetComponentsInChildren<MoveLeft>(true)) ml.enabled = false;
    }

    // Hard freeze runner after finale moment
    public void ForceFinalFreeze()
    {
        finalTriggered = true;
        finalSequenceStarted = true;
        baseSpeed = 0f;
        speedMultiplier = 0f;
        inFinalCinematic = true;
    }

    private IEnumerator StartFinalSequence()
    {
        finalSequenceStarted = true;

        // Mild slowdown (no timeScale)
        baseSpeed *= finalSeqSlowFactor;
        speedMultiplier *= finalSeqSlowFactor;

        yield return new WaitForSeconds(canSpawnDelay);

        foreach (var sp in FindObjectsOfType<Spawner>()) sp.enabled = false;
        foreach (var o in GameObject.FindGameObjectsWithTag("Obstacle")) Destroy(o);

        float spawnY = redBullSpawnY + redBullSpawnOffsetY;
        Instantiate(redBullPrefab, new Vector3(redBullSpawnX, spawnY, 0f), Quaternion.identity);

        Debug.Log("Final sequence beginning...");
    }

    public void PlayerHit()
    {
        if (finalTriggered || inFinalCinematic || gameEnded) return;

        currentLives = Mathf.Max(0, currentLives - 1);
        cameraShake?.Shake(0.15f, 0.15f);

        FlashLostHeart(currentLives);
        UpdateHeartsUI();

        if (currentLives <= 0)
            StartCoroutine(HandleGameOverFall());
    }

    private IEnumerator HandleGameOverFall()
    {
        gameEnded = true;

        // stop spawners and world scroll
        DisableAllSpawners();
        baseSpeed = 0f;
        speedMultiplier = 0f;

        // tell rocket to free-fall (ignores world freeze)
        var rocket = FindObjectOfType<RocketPlayer>();
        if (rocket != null) rocket.BeginDeathFall();

        yield break;
    }

    private void DisableAllSpawners()
    {
        foreach (var sp in FindObjectsOfType<Spawner>()) sp.enabled = false;
        foreach (var msp in FindObjectsOfType<MeteorSpawner>()) msp.enabled = false;
    }

    private void StopAllMovement()
    {
        gameEnded = true;
        baseSpeed = 0f;
        speedMultiplier = 0f;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = Mathf.FloorToInt(score).ToString("D4");
    }

    private void UpdateHighScoreUI()
    {
        if (highScoreText != null)
            highScoreText.text = "High Score: " + Mathf.FloorToInt(highScore);
    }

    private void CheckHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat("HighScore", highScore);
            UpdateHighScoreUI();
        }
    }

    private void UpdateHeartsUI()
    {
        if (HeartCount == 0) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            var img = heartImages[i];
            if (img == null) continue;
            img.sprite = (i < currentLives ? fullHeartSprite : emptyHeartSprite);
        }
    }

    private void FlashLostHeart(int lostIndex)
    {
        if (!HeartIndexValid(lostIndex)) return;
        StartCoroutine(FlashRoutine(heartImages[lostIndex]));
    }

    private IEnumerator FlashRoutine(Image img)
    {
        if (img == null) yield break;

        Color original = img.color;

        img.color = Color.white; yield return new WaitForSeconds(0.05f);
        img.color = Color.red; yield return new WaitForSeconds(0.05f);
        img.color = Color.white; yield return new WaitForSeconds(0.05f);

        img.color = original;
    }

    // FX helpers
    public void PlayWingsFX(Vector3 atPosition)
    {
        if (wingsBurstPrefab != null)
        {
            var ps = Instantiate(wingsBurstPrefab, atPosition, Quaternion.identity);
            var main = ps.main;
            float life = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax
                : main.startLifetime.constant;
            Destroy(ps.gameObject, main.duration + life + 0.5f);
        }

        if (screenFlashImage != null)
            StartCoroutine(ScreenFlashRealtime(flashPeakAlpha, flashInDuration, flashOutDuration));
    }

    private IEnumerator ScreenFlashRealtime(float peakAlpha, float inDur, float outDur)
    {
        if (screenFlashImage == null) yield break;

        var c = flashColor; c.a = 0f; screenFlashImage.color = c;

        float t = 0f;
        while (t < inDur)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, peakAlpha, (inDur <= 0f ? 1f : t / inDur));
            screenFlashImage.color = c;
            yield return null;
        }
        c.a = peakAlpha; screenFlashImage.color = c;

        t = 0f;
        while (t < outDur)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(peakAlpha, 0f, (outDur <= 0f ? 1f : t / outDur));
            screenFlashImage.color = c;
            yield return null;
        }
        c.a = 0f; screenFlashImage.color = c;
    }

    // Save lives & score, then fade-load next part
    public void AdvanceToNextPart()
    {
        if (!autoAdvanceAfterFinale) return;
        if (string.IsNullOrEmpty(nextSceneName)) return;

        if (SessionState.Instance == null)
            new GameObject("SessionState").AddComponent<SessionState>();

        SessionState.Instance.SetLives(currentLives);
        SessionState.Instance.SetScore(score);

        SceneFlowManager.Instance?.LoadSceneWithFade(nextSceneName);
    }
}
